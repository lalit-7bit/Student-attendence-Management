using System.Security.Claims;
using attendence.Data;
using attendence.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
namespace attendence.Controllers;
public sealed class AccountController(AccountRepository accounts,PasswordHasher<AppUser> hasher,ILogger<AccountController> logger,IHostEnvironment environment):Controller
{
    [HttpGet,Microsoft.AspNetCore.Authorization.AllowAnonymous]
    public IActionResult Login(string? returnUrl=null)=>View(new LoginViewModel{ReturnUrl=returnUrl});
    [HttpPost,ValidateAntiForgeryToken,Microsoft.AspNetCore.Authorization.AllowAnonymous,Microsoft.AspNetCore.RateLimiting.EnableRateLimiting("login")]
    public async Task<IActionResult> Login(LoginViewModel model,CancellationToken ct)
    {
        if(!ModelState.IsValid)return View(model);
        try
        {
            var user=await accounts.FindByEmailAsync(model.Email,ct);
            if(user is null||hasher.VerifyHashedPassword(user,user.PasswordHash,model.Password)==PasswordVerificationResult.Failed){ModelState.AddModelError(string.Empty,"Email or password is incorrect.");return View(model);}
            var claims=new List<Claim>{new(ClaimTypes.NameIdentifier,user.Id.ToString()),new(ClaimTypes.Name,user.Email),new(ClaimTypes.Email,user.Email),new(ClaimTypes.Role,user.Role)};
            if(user.StudentId is int studentId)claims.Add(new("StudentId",studentId.ToString()));if(user.TeacherId is int teacherId)claims.Add(new("TeacherId",teacherId.ToString()));
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,new ClaimsPrincipal(new ClaimsIdentity(claims,CookieAuthenticationDefaults.AuthenticationScheme)),new AuthenticationProperties{IsPersistent=false,AllowRefresh=true});
            if(!string.IsNullOrEmpty(model.ReturnUrl)&&Url.IsLocalUrl(model.ReturnUrl))return LocalRedirect(model.ReturnUrl);
            return user.Role switch { "Teacher" => RedirectToAction("Index","Attendance"), "Student" => RedirectToAction("Index","StudentPortal"), _ => RedirectToAction("Index","Home") };
        }
        catch(SqlException ex)
        {
            // The development-only message explains SQL/LocalDB failures. Never
            // log the submitted password, connection string, or SQL parameters.
            if (environment.IsDevelopment())
                logger.LogError("Login database lookup failed. SqlErrorNumber={SqlErrorNumber}, SqlState={SqlState}, SqlClass={SqlClass}, SqlMessage={SqlMessage}, InnerMessage={InnerMessage}", ex.Number, ex.State, ex.Class, ex.Message, ex.InnerException?.Message);
            else
                logger.LogError("Login database lookup failed. SqlErrorNumber={SqlErrorNumber}, SqlState={SqlState}, SqlClass={SqlClass}", ex.Number, ex.State, ex.Class);
            ModelState.AddModelError(string.Empty,"Sign-in is temporarily unavailable. Check the database setup and try again.");return View(model);
        }
    }
    [HttpPost,ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout(){await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);return RedirectToAction(nameof(Login));}
    [HttpGet,Microsoft.AspNetCore.Authorization.AllowAnonymous]
    public IActionResult AccessDenied()=>View();
}
