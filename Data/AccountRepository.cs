using System.Data;
using attendence.Models;
using Microsoft.Data.SqlClient;
namespace attendence.Data;
public sealed class AccountRepository(DbConnectionFactory connections)
{
    public async Task CreateAsync(AppUser user,CancellationToken ct=default)
    {
        await using var c=connections.CreateConnection();await c.OpenAsync(ct);await using var q=c.CreateCommand();q.CommandText="INSERT dbo.AppUsers(Email,NormalizedEmail,PasswordHash,Role,StudentId,TeacherId,IsActive) VALUES(@Email,@Normalized,@Hash,@Role,@StudentId,@TeacherId,1);";
        q.Parameters.Add("@Email",SqlDbType.NVarChar,256).Value=user.Email.Trim();q.Parameters.Add("@Normalized",SqlDbType.NVarChar,256).Value=user.Email.Trim().ToUpperInvariant();q.Parameters.Add("@Hash",SqlDbType.NVarChar,500).Value=user.PasswordHash;q.Parameters.Add("@Role",SqlDbType.NVarChar,20).Value=user.Role;q.Parameters.Add("@StudentId",SqlDbType.Int).Value=(object?)user.StudentId??DBNull.Value;q.Parameters.Add("@TeacherId",SqlDbType.Int).Value=(object?)user.TeacherId??DBNull.Value;await q.ExecuteNonQueryAsync(ct);
    }
    public async Task<IReadOnlyList<UserAccountRow>> GetAccountsAsync(CancellationToken ct=default)
    {var rows=new List<UserAccountRow>();await using var c=connections.CreateConnection();await using var q=c.CreateCommand();q.CommandText="SELECT u.AppUserId,u.Email,u.Role,COALESCE(s.FullName,t.FullName),u.IsActive FROM dbo.AppUsers u LEFT JOIN dbo.Students s ON s.StudentId=u.StudentId LEFT JOIN dbo.Teachers t ON t.TeacherId=u.TeacherId ORDER BY u.Role,u.Email;";await c.OpenAsync(ct);await using var r=await q.ExecuteReaderAsync(ct);while(await r.ReadAsync(ct))rows.Add(new(r.GetInt32(0),r.GetString(1),r.GetString(2),r.IsDBNull(3)?null:r.GetString(3),r.GetBoolean(4)));return rows;}
    public async Task<bool> DeactivateAsync(int id,CancellationToken ct=default)
    {await using var c=connections.CreateConnection();await c.OpenAsync(ct);await using var q=c.CreateCommand();q.CommandText="UPDATE dbo.AppUsers SET IsActive=0 WHERE AppUserId=@Id AND IsActive=1 AND Role<>N'Administrator';";q.Parameters.Add("@Id",SqlDbType.Int).Value=id;return await q.ExecuteNonQueryAsync(ct)==1;}
    public async Task<AppUser?> FindByEmailAsync(string email,CancellationToken ct=default)
    {
        await using var c=connections.CreateConnection();await using var q=c.CreateCommand();q.CommandText="SELECT AppUserId,Email,PasswordHash,Role,StudentId,TeacherId FROM dbo.AppUsers WHERE NormalizedEmail=@Email AND IsActive=1;";q.Parameters.Add("@Email",SqlDbType.NVarChar,256).Value=email.Trim().ToUpperInvariant();await c.OpenAsync(ct);await using var r=await q.ExecuteReaderAsync(ct);return await r.ReadAsync(ct)?new(){Id=r.GetInt32(0),Email=r.GetString(1),PasswordHash=r.GetString(2),Role=r.GetString(3),StudentId=r.IsDBNull(4)?null:r.GetInt32(4),TeacherId=r.IsDBNull(5)?null:r.GetInt32(5)}:null;
    }
    public async Task ValidateLoginQueryAsync(CancellationToken ct=default)
    {
        // Compile and execute the login projection without reading any account
        // rows or password hashes. This lets /healthz detect auth schema drift.
        await using var c=connections.CreateConnection();await using var q=c.CreateCommand();
        q.CommandText="SELECT TOP (0) AppUserId,Email,PasswordHash,Role,StudentId,TeacherId FROM dbo.AppUsers WHERE NormalizedEmail=@Email AND IsActive=1;";
        q.Parameters.Add("@Email",SqlDbType.NVarChar,256).Value="__SMARTATTEND_SCHEMA_CHECK__";
        await c.OpenAsync(ct);await using var r=await q.ExecuteReaderAsync(ct);
    }
    public async Task<bool> CreateDevelopmentAdminAsync(string email,string hash,CancellationToken ct=default)
    {
        await using var c=connections.CreateConnection();await c.OpenAsync(ct);await using var tx=await c.BeginTransactionAsync(IsolationLevel.Serializable,ct);
        try{await using var q=c.CreateCommand();q.Transaction=(SqlTransaction)tx;q.CommandText="IF NOT EXISTS(SELECT 1 FROM dbo.AppUsers WITH(UPDLOCK,HOLDLOCK) WHERE NormalizedEmail=@Email) BEGIN INSERT dbo.AppUsers(Email,NormalizedEmail,PasswordHash,Role,IsActive) VALUES(@Original,@Email,@Hash,N'Administrator',1); SELECT 1; END ELSE SELECT 0;";q.Parameters.Add("@Original",SqlDbType.NVarChar,256).Value=email.Trim();q.Parameters.Add("@Email",SqlDbType.NVarChar,256).Value=email.Trim().ToUpperInvariant();q.Parameters.Add("@Hash",SqlDbType.NVarChar,500).Value=hash;var created=Convert.ToInt32(await q.ExecuteScalarAsync(ct))==1;await tx.CommitAsync(ct);return created;}catch{await tx.RollbackAsync(ct);throw;}
    }
}
