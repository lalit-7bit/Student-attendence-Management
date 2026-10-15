using System.Data;
using attendence.Models;
using Microsoft.Data.SqlClient;
namespace attendence.Data;
public sealed class StudentRepository(DbConnectionFactory connections)
{
    public async Task<IReadOnlyList<Student>> GetAllAsync(string? search, CancellationToken cancellationToken = default)
    {
        var rows = new List<Student>();
        await using var connection = connections.CreateConnection();
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT s.StudentId,s.EnrollmentNumber,s.FullName,s.Email,s.PhoneNumber,s.ClassId,c.Name AS ClassName,c.Semester,c.AcademicYear,s.IsActive FROM dbo.Students AS s LEFT JOIN dbo.Classes AS c ON c.ClassId=s.ClassId WHERE (@Search IS NULL OR s.FullName LIKE @Pattern OR s.EnrollmentNumber LIKE @Pattern OR s.Email LIKE @Pattern) ORDER BY s.FullName;";
        command.Parameters.Add("@Search", SqlDbType.NVarChar, 120).Value = string.IsNullOrWhiteSpace(search) ? DBNull.Value : search.Trim();
        command.Parameters.Add("@Pattern", SqlDbType.NVarChar, 124).Value = $"%{search?.Trim()}%";
        await connection.OpenAsync(cancellationToken);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken)) rows.Add(ReadStudent(reader));
        return rows;
    }
    public async Task<IReadOnlyList<(int Id,string Label)>> GetClassesAsync(CancellationToken cancellationToken = default)
    {
        var rows = new List<(int,string)>();
        await using var connection = connections.CreateConnection();
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT c.ClassId,c.Name,d.Name AS DepartmentName,c.Semester,c.AcademicYear FROM dbo.Classes AS c INNER JOIN dbo.Departments AS d ON d.DepartmentId=c.DepartmentId ORDER BY d.Name,c.Name,c.Semester;";
        await connection.OpenAsync(cancellationToken);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken)) rows.Add((reader.GetInt32(0), $"{reader.GetString(1)} · {reader.GetString(2)} · Sem {reader.GetByte(3)} · {reader.GetString(4)}"));
        return rows;
    }
    public async Task<Student?> FindAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var connection = connections.CreateConnection();
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT s.StudentId,s.EnrollmentNumber,s.FullName,s.Email,s.PhoneNumber,s.ClassId,c.Name AS ClassName,c.Semester,c.AcademicYear,s.IsActive FROM dbo.Students AS s LEFT JOIN dbo.Classes AS c ON c.ClassId=s.ClassId WHERE s.StudentId=@Id;";
        command.Parameters.Add("@Id", SqlDbType.Int).Value = id;
        await connection.OpenAsync(cancellationToken);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? ReadStudent(reader) : null;
    }
    public async Task<int> SaveAsync(Student student, CancellationToken cancellationToken = default)
    {
        await using var connection = connections.CreateConnection();
        await using var command = connection.CreateCommand();
        command.CommandText = student.StudentId == 0
            ? "INSERT dbo.Students(EnrollmentNumber,FullName,Email,PhoneNumber,ClassId) VALUES(@EnrollmentNumber,@FullName,@Email,@PhoneNumber,@ClassId); SELECT CONVERT(int,SCOPE_IDENTITY());"
            : "UPDATE dbo.Students SET EnrollmentNumber=@EnrollmentNumber,FullName=@FullName,Email=@Email,PhoneNumber=@PhoneNumber,ClassId=@ClassId WHERE StudentId=@Id; SELECT @Id;";
        AddStudentParameters(command, student);
        if (student.StudentId != 0) command.Parameters.Add("@Id", SqlDbType.Int).Value = student.StudentId;
        await connection.OpenAsync(cancellationToken);
        return Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken));
    }
    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var connection = connections.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        try
        {
            await using var command = connection.CreateCommand(); command.Transaction=(SqlTransaction)transaction;
            command.CommandText = "UPDATE dbo.Students SET IsActive=0 WHERE StudentId=@Id AND IsActive=1; DECLARE @StudentChanged int=@@ROWCOUNT; IF @StudentChanged=1 UPDATE dbo.AppUsers SET IsActive=0 WHERE StudentId=@Id; SELECT @StudentChanged;";
            command.Parameters.Add("@Id", SqlDbType.Int).Value = id;
            var changed=Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken))>0;
            await transaction.CommitAsync(cancellationToken);return changed;
        }
        catch { await transaction.RollbackAsync(cancellationToken); throw; }
    }
    private static void AddStudentParameters(SqlCommand command, Student student)
    {
        command.Parameters.Add("@EnrollmentNumber", SqlDbType.NVarChar,30).Value = student.EnrollmentNumber.Trim();
        command.Parameters.Add("@FullName", SqlDbType.NVarChar,120).Value = student.FullName.Trim();
        command.Parameters.Add("@Email", SqlDbType.NVarChar,256).Value = (object?)student.Email?.Trim() ?? DBNull.Value;
        command.Parameters.Add("@PhoneNumber", SqlDbType.NVarChar,30).Value = (object?)student.PhoneNumber?.Trim() ?? DBNull.Value;
        command.Parameters.Add("@ClassId", SqlDbType.Int).Value = (object?)student.ClassId ?? DBNull.Value;
    }
    private static Student ReadStudent(SqlDataReader reader) => new()
    {
        StudentId = reader.GetInt32(0), EnrollmentNumber = reader.GetString(1), FullName = reader.GetString(2),
        Email = reader.IsDBNull(3) ? null : reader.GetString(3), PhoneNumber = reader.IsDBNull(4) ? null : reader.GetString(4),
        ClassId = reader.IsDBNull(5) ? null : reader.GetInt32(5), ClassName = reader.IsDBNull(6) ? null : reader.GetString(6),
        Semester = reader.IsDBNull(7) ? 1 : reader.GetByte(7), AcademicYear = reader.IsDBNull(8) ? null : reader.GetString(8), IsActive = reader.GetBoolean(9)
    };
}
