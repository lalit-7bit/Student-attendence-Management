var builder = WebApplication.CreateBuilder(args);

// Initialize database BEFORE building the app
if (builder.Environment.IsDevelopment())
{
    try
    {
        var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
        var logProvider = LoggerFactory.Create(x => x.AddConsole());
        var dbLogger = logProvider.CreateLogger("DatabaseInit");
        attendence.Data.DatabaseInitializer.InitializeDatabase(connectionString, dbLogger);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"⚠️ Database initialization warning: {ex.Message}");
    }
}

builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Services.AddControllersWithViews();
builder.Services.AddAuthentication(Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationDefaults.AuthenticationScheme).AddCookie(options =>
{
    options.LoginPath = "/Account/Login"; options.AccessDeniedPath = "/Account/AccessDenied";
    options.Cookie.Name = "SmartAttend.Auth"; options.Cookie.HttpOnly = true; options.Cookie.SameSite = Microsoft.AspNetCore.Http.SameSiteMode.Lax;
    options.Cookie.SecurePolicy = builder.Environment.IsDevelopment() ? Microsoft.AspNetCore.Http.CookieSecurePolicy.SameAsRequest : Microsoft.AspNetCore.Http.CookieSecurePolicy.Always;
    options.SlidingExpiration = true; options.ExpireTimeSpan = TimeSpan.FromHours(8);
});
builder.Services.AddAuthorization();
builder.Services.AddRateLimiter(options => { options.RejectionStatusCode=StatusCodes.Status429TooManyRequests; options.AddPolicy("login", context => System.Threading.RateLimiting.RateLimitPartition.GetFixedWindowLimiter(context.Connection.RemoteIpAddress?.ToString()??"unknown", _=>new System.Threading.RateLimiting.FixedWindowRateLimiterOptions{PermitLimit=8,Window=TimeSpan.FromMinutes(1),QueueLimit=0,AutoReplenishment=true})); });
builder.Services.AddAntiforgery(options => { options.Cookie.HttpOnly = true; options.Cookie.SameSite = Microsoft.AspNetCore.Http.SameSiteMode.Strict; });
builder.Services.AddSingleton(new Microsoft.AspNetCore.Identity.PasswordHasher<attendence.Models.AppUser>());
builder.Services.AddSingleton<attendence.Data.DbConnectionFactory>();
builder.Services.AddScoped<attendence.Data.StudentRepository>();
builder.Services.AddScoped<attendence.Data.AttendanceRepository>();
builder.Services.AddScoped<attendence.Data.AttendanceReportRepository>();
builder.Services.AddScoped<attendence.Data.AccountRepository>();
builder.Services.AddScoped<attendence.Data.SchoolManagementRepository>();
var app = builder.Build();
if (app.Environment.IsDevelopment())
{
    var configuredConnection = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(
        app.Configuration.GetConnectionString("DefaultConnection"));
    var configurationProviders = ((IConfigurationRoot)app.Configuration).Providers
        .Where(provider => provider.TryGet("ConnectionStrings:DefaultConnection", out _))
        .Select(provider => provider.GetType().Name);
    app.Logger.LogInformation(
        "SmartAttend SQL target: DataSource={DataSource}, Database={Database}, IntegratedSecurity={IntegratedSecurity}, ConfigurationProviders={ConfigurationProviders}",
        configuredConnection.DataSource,
        configuredConnection.InitialCatalog,
        configuredConnection.IntegratedSecurity,
        string.Join(",", configurationProviders));
}
if (!app.Environment.IsDevelopment()) { app.UseExceptionHandler("/Home/Error"); app.UseHsts(); }
if (app.Environment.IsDevelopment() && !string.IsNullOrWhiteSpace(app.Configuration["BootstrapAdmin:Email"]) && !string.IsNullOrWhiteSpace(app.Configuration["BootstrapAdmin:Password"]))
{
    try
    {
        var bootstrapPassword=app.Configuration["BootstrapAdmin:Password"]!;
        if(bootstrapPassword.Length<12 || !System.Text.RegularExpressions.Regex.IsMatch(bootstrapPassword,"^(?=.*[a-z])(?=.*[A-Z])(?=.*\\d)(?=.*[^A-Za-z0-9]).{12,}$")) throw new InvalidOperationException("Bootstrap administrator password must be 12+ characters with upper/lower case, a number, and a symbol.");
        await using var scope = app.Services.CreateAsyncScope();
        var hasher = scope.ServiceProvider.GetRequiredService<Microsoft.AspNetCore.Identity.PasswordHasher<attendence.Models.AppUser>>();
        var account = new attendence.Models.AppUser { Email = app.Configuration["BootstrapAdmin:Email"]! };
        var hash = hasher.HashPassword(account, bootstrapPassword);
        var created = await scope.ServiceProvider.GetRequiredService<attendence.Data.AccountRepository>().CreateDevelopmentAdminAsync(account.Email, hash);
        if (!created) app.Logger.LogInformation("Bootstrap administrator already exists; no account was changed.");
    }
    catch (Microsoft.Data.SqlClient.SqlException ex)
    {
        // Keep diagnostics useful without writing exception text or credentials to logs.
        app.Logger.LogWarning("Development admin seed skipped. SqlErrorNumber={SqlErrorNumber}, SqlState={SqlState}, SqlClass={SqlClass}", ex.Number, ex.State, ex.Class);
    }
}
app.UseHttpsRedirection(); app.UseStaticFiles(); app.UseRouting(); app.UseRateLimiter(); app.UseAuthentication(); app.UseAuthorization();
app.MapControllerRoute(name: "default", pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapGet("/healthz", async (attendence.Data.AccountRepository accounts, ILoggerFactory loggerFactory, CancellationToken ct) =>
{
    try
    {
        await accounts.ValidateLoginQueryAsync(ct);
        return Results.Ok(new { status = "Healthy" });
    }
    catch (Microsoft.Data.SqlClient.SqlException ex)
    {
        if (app.Environment.IsDevelopment())
        {
            loggerFactory.CreateLogger("SmartAttend.DatabaseHealth").LogError(
                "SQL connectivity check failed. SqlErrorNumber={SqlErrorNumber}, SqlState={SqlState}, SqlClass={SqlClass}, SqlMessage={SqlMessage}, InnerMessage={InnerMessage}",
                ex.Number, ex.State, ex.Class, ex.Message, ex.InnerException?.Message);
        }
        return Results.StatusCode(StatusCodes.Status503ServiceUnavailable);
    }
}).AllowAnonymous();
app.Run();
