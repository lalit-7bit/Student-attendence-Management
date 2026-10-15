using Microsoft.Data.SqlClient;
namespace attendence.Data;
public sealed class DbConnectionFactory(IConfiguration configuration)
{
    private readonly string _connectionString = configuration.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is missing.");
    public SqlConnection CreateConnection() => new(_connectionString);
}
