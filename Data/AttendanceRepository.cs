using System.Data;
using attendence.Models;
using Microsoft.Data.SqlClient;
namespace attendence.Data;
public sealed class AttendanceRepository(DbConnectionFactory connections)
{
    public async Task<IReadOnlyList<(int Id,string Name)>> ClassesAsync(CancellationToken ct,int? teacherId=null) => await LookupAsync("SELECT DISTINCT c.ClassId,c.Name FROM dbo.Classes c LEFT JOIN dbo.Subjects s ON s.ClassId=c.ClassId LEFT JOIN dbo.TeacherAssignments a ON a.SubjectId=s.SubjectId LEFT JOIN dbo.Teachers t ON t.TeacherId=a.TeacherId WHERE (@TeacherId IS NULL OR (a.TeacherId=@TeacherId AND t.IsActive=1)) ORDER BY c.Name;", null, teacherId, ct);
    public async Task<IReadOnlyList<(int Id,string Name)>> SubjectsAsync(int classId, CancellationToken ct,int? teacherId=null) => await LookupAsync("SELECT s.SubjectId,s.Name FROM dbo.Subjects s WHERE s.ClassId=@Id AND (@TeacherId IS NULL OR EXISTS(SELECT 1 FROM dbo.TeacherAssignments a JOIN dbo.Teachers t ON t.TeacherId=a.TeacherId AND t.IsActive=1 WHERE a.SubjectId=s.SubjectId AND a.TeacherId=@TeacherId)) ORDER BY s.Name;", classId, teacherId, ct);
    public async Task<IReadOnlyList<(int Id,string Name)>> TeachersAsync(int subjectId, CancellationToken ct,int? teacherId=null) => await LookupAsync("SELECT t.TeacherId,t.FullName FROM dbo.TeacherAssignments a INNER JOIN dbo.Teachers t ON t.TeacherId=a.TeacherId WHERE a.SubjectId=@Id AND t.IsActive=1 AND (@TeacherId IS NULL OR t.TeacherId=@TeacherId) ORDER BY t.FullName;", subjectId, teacherId, ct);
    private async Task<IReadOnlyList<(int Id,string Name)>> LookupAsync(string sql, int? id, int? teacherId, CancellationToken ct)
    {
        var result = new List<(int,string)>(); await using var c=connections.CreateConnection(); await using var q=c.CreateCommand(); q.CommandText=sql;
        if(id is int value) q.Parameters.Add("@Id",SqlDbType.Int).Value=value;
        q.Parameters.Add("@TeacherId",SqlDbType.Int).Value=(object?)teacherId??DBNull.Value;
        await c.OpenAsync(ct); await using var r=await q.ExecuteReaderAsync(ct); while(await r.ReadAsync(ct)) result.Add((r.GetInt32(0),r.GetString(1))); return result;
    }
    public async Task<IReadOnlyList<AttendanceEntryViewModel>> RosterAsync(int classId,int subjectId,DateTime date,string period,CancellationToken ct,int? teacherId=null)
    {
        var rows=new List<AttendanceEntryViewModel>(); await using var c=connections.CreateConnection(); await using var q=c.CreateCommand();
        q.CommandText="""SELECT s.StudentId,s.EnrollmentNumber,s.FullName,COALESCE(ar.Status,'Present') AS Status,ar.Remarks FROM dbo.Students s LEFT JOIN dbo.AttendanceSessions ses ON ses.SubjectId=@SubjectId AND ses.SessionDate=@Date AND ses.Period=@Period LEFT JOIN dbo.AttendanceRecords ar ON ar.SessionId=ses.SessionId AND ar.StudentId=s.StudentId WHERE s.ClassId=@ClassId AND s.IsActive=1 AND EXISTS(SELECT 1 FROM dbo.Subjects sub WHERE sub.SubjectId=@SubjectId AND sub.ClassId=@ClassId) AND (@TeacherId IS NULL OR EXISTS(SELECT 1 FROM dbo.TeacherAssignments ta JOIN dbo.Teachers t ON t.TeacherId=ta.TeacherId AND t.IsActive=1 WHERE ta.SubjectId=@SubjectId AND ta.TeacherId=@TeacherId)) ORDER BY s.FullName;""";
        q.Parameters.Add("@SubjectId",SqlDbType.Int).Value=subjectId; q.Parameters.Add("@ClassId",SqlDbType.Int).Value=classId; q.Parameters.Add("@Date",SqlDbType.Date).Value=date.Date; q.Parameters.Add("@Period",SqlDbType.NVarChar,30).Value=period; q.Parameters.Add("@TeacherId",SqlDbType.Int).Value=(object?)teacherId??DBNull.Value;
        await c.OpenAsync(ct); await using var r=await q.ExecuteReaderAsync(ct); while(await r.ReadAsync(ct)) rows.Add(new(){StudentId=r.GetInt32(0),EnrollmentNumber=r.GetString(1),FullName=r.GetString(2),Status=r.GetString(3),Remarks=r.IsDBNull(4)?null:r.GetString(4)}); return rows;
    }
    public async Task SaveAsync(AttendanceViewModel model,string changedBy,CancellationToken ct)
    {
        if(model.ClassId is null || model.SubjectId is null || model.TeacherId is null) throw new InvalidOperationException("Choose a class, subject, and assigned teacher.");
        if(model.Students.Count==0 || model.Students.Select(x=>x.StudentId).Distinct().Count()!=model.Students.Count) throw new InvalidOperationException("Attendance roster is empty or contains duplicate students.");
        await using var c=connections.CreateConnection(); await c.OpenAsync(ct); await using var tx=(SqlTransaction)await c.BeginTransactionAsync(IsolationLevel.Serializable,ct);
        try
        {
            await using(var verify=c.CreateCommand()) { verify.Transaction=tx; verify.CommandText="SELECT COUNT(*) FROM dbo.Subjects s INNER JOIN dbo.TeacherAssignments a ON a.SubjectId=s.SubjectId INNER JOIN dbo.Teachers t ON t.TeacherId=a.TeacherId WHERE s.SubjectId=@SubjectId AND s.ClassId=@ClassId AND t.TeacherId=@TeacherId AND t.IsActive=1;"; verify.Parameters.Add("@SubjectId",SqlDbType.Int).Value=model.SubjectId.Value; verify.Parameters.Add("@ClassId",SqlDbType.Int).Value=model.ClassId.Value; verify.Parameters.Add("@TeacherId",SqlDbType.Int).Value=model.TeacherId.Value; if(Convert.ToInt32(await verify.ExecuteScalarAsync(ct))!=1) throw new InvalidOperationException("That subject is not assigned to the selected class and teacher."); }
            var validIds=new HashSet<int>(); await using(var roster=c.CreateCommand()){roster.Transaction=tx; roster.CommandText="SELECT StudentId FROM dbo.Students WITH (UPDLOCK,HOLDLOCK) WHERE ClassId=@ClassId AND IsActive=1;";roster.Parameters.Add("@ClassId",SqlDbType.Int).Value=model.ClassId.Value;await using var r=await roster.ExecuteReaderAsync(ct);while(await r.ReadAsync(ct))validIds.Add(r.GetInt32(0));}
            if(!validIds.SetEquals(model.Students.Select(x=>x.StudentId))) throw new InvalidOperationException("The submitted student list no longer matches the active class roster. Reload and try again.");
            int sessionId;
            await using(var session=c.CreateCommand()){session.Transaction=tx;session.CommandText="SELECT SessionId FROM dbo.AttendanceSessions WITH (UPDLOCK,HOLDLOCK) WHERE SubjectId=@SubjectId AND SessionDate=@Date AND Period=@Period;";session.Parameters.Add("@SubjectId",SqlDbType.Int).Value=model.SubjectId.Value;session.Parameters.Add("@Date",SqlDbType.Date).Value=model.SessionDate.Date;session.Parameters.Add("@Period",SqlDbType.NVarChar,30).Value=model.Period.Trim();var found=await session.ExecuteScalarAsync(ct); if(found is null){session.CommandText="INSERT dbo.AttendanceSessions(SubjectId,TeacherId,SessionDate,Period) VALUES(@SubjectId,@TeacherId,@Date,@Period); SELECT CONVERT(int,SCOPE_IDENTITY());";session.Parameters.Add("@TeacherId",SqlDbType.Int).Value=model.TeacherId.Value;sessionId=Convert.ToInt32(await session.ExecuteScalarAsync(ct));}else{sessionId=Convert.ToInt32(found);session.CommandText="UPDATE dbo.AttendanceSessions SET TeacherId=@TeacherId WHERE SessionId=@SessionId;";session.Parameters.Add("@TeacherId",SqlDbType.Int).Value=model.TeacherId.Value;session.Parameters.Add("@SessionId",SqlDbType.Int).Value=sessionId;await session.ExecuteNonQueryAsync(ct);}}
            foreach(var item in model.Students)
            {
                int recordId; string? oldStatus=null; string? oldRemarks=null;
                await using(var prior=c.CreateCommand()){prior.Transaction=tx;prior.CommandText="SELECT RecordId,Status,Remarks FROM dbo.AttendanceRecords WITH (UPDLOCK,HOLDLOCK) WHERE SessionId=@SessionId AND StudentId=@StudentId;";prior.Parameters.Add("@SessionId",SqlDbType.Int).Value=sessionId;prior.Parameters.Add("@StudentId",SqlDbType.Int).Value=item.StudentId;await using var r=await prior.ExecuteReaderAsync(ct);if(await r.ReadAsync(ct)){recordId=r.GetInt32(0);oldStatus=r.GetString(1);oldRemarks=r.IsDBNull(2)?null:r.GetString(2);}else recordId=0;}
                await using(var save=c.CreateCommand()){save.Transaction=tx;save.CommandText=recordId==0?"INSERT dbo.AttendanceRecords(SessionId,StudentId,Status,Remarks) VALUES(@SessionId,@StudentId,@Status,@Remarks); SELECT CONVERT(int,SCOPE_IDENTITY());":"UPDATE dbo.AttendanceRecords SET Status=@Status,Remarks=@Remarks WHERE RecordId=@RecordId; SELECT @RecordId;";save.Parameters.Add("@SessionId",SqlDbType.Int).Value=sessionId;save.Parameters.Add("@StudentId",SqlDbType.Int).Value=item.StudentId;save.Parameters.Add("@Status",SqlDbType.NVarChar,10).Value=item.Status;save.Parameters.Add("@Remarks",SqlDbType.NVarChar,300).Value=(object?)item.Remarks?.Trim()??DBNull.Value;if(recordId!=0)save.Parameters.Add("@RecordId",SqlDbType.Int).Value=recordId;recordId=Convert.ToInt32(await save.ExecuteScalarAsync(ct));}
                if(oldStatus is null || oldStatus!=item.Status || oldRemarks!=(item.Remarks?.Trim()))
                {
                    var description=oldStatus is null?$"Attendance recorded as {item.Status}.":$"Attendance corrected from {oldStatus} to {item.Status}. Reason: {(item.Remarks?.Trim() is {Length:>0} note?note:"No reason supplied.")}";
                    await using var audit=c.CreateCommand();audit.Transaction=tx;audit.CommandText="INSERT dbo.AttendanceAuditLogs(RecordId,ChangedBy,ChangeDescription) VALUES(@RecordId,@ChangedBy,@Description);";audit.Parameters.Add("@RecordId",SqlDbType.Int).Value=recordId;audit.Parameters.Add("@ChangedBy",SqlDbType.NVarChar,256).Value=changedBy;audit.Parameters.Add("@Description",SqlDbType.NVarChar,500).Value=description;await audit.ExecuteNonQueryAsync(ct);
                }
            }
            await tx.CommitAsync(ct);
        }
        catch { await tx.RollbackAsync(ct); throw; }
    }
}
