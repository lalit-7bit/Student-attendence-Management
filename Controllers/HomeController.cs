using attendence.Data;
using attendence.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Data.SqlClient;
namespace attendence.Controllers;
[Authorize(Roles="Administrator")]
public sealed class HomeController(DbConnectionFactory connections) : Controller
{
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var model=new DashboardViewModel();
        try
        {
            await using var c=connections.CreateConnection(); await c.OpenAsync(ct); await using var q=c.CreateCommand();
            q.CommandText="""
                SELECT (SELECT COUNT(*) FROM dbo.Students WHERE IsActive=1) AS ActiveStudents,
                       COALESCE(TRY_CONVERT(float,(SELECT [Value] FROM dbo.ApplicationSettings WHERE [Key]=N'LowAttendanceThreshold')),75.0) AS Threshold,
                       COALESCE((SELECT [Value] FROM dbo.ApplicationSettings WHERE [Key]=N'InstitutionName'),N'SmartAttend') AS InstitutionName;
                SELECT COALESCE(SUM(CASE WHEN ar.Status='Present' THEN 1 ELSE 0 END),0) AS PresentCount,
                       COALESCE(SUM(CASE WHEN ar.Status='Absent' THEN 1 ELSE 0 END),0) AS AbsentCount,
                       COALESCE(SUM(CASE WHEN ar.Status='Late' THEN 1 ELSE 0 END),0) AS LateCount,
                       COALESCE(SUM(CASE WHEN ar.Status='Excused' THEN 1 ELSE 0 END),0) AS ExcusedCount
                FROM dbo.AttendanceSessions s LEFT JOIN dbo.AttendanceRecords ar ON ar.SessionId=s.SessionId WHERE s.SessionDate=CONVERT(date,GETDATE());
                WITH Totals AS(SELECT ar.StudentId,100.0*SUM(CASE WHEN ar.Status IN('Present','Late') THEN 1.0 ELSE 0 END)/NULLIF(COUNT(CASE WHEN ar.Status IN('Present','Absent','Late') THEN 1 END),0) AS Pct FROM dbo.AttendanceRecords ar JOIN dbo.Students stu ON stu.StudentId=ar.StudentId AND stu.IsActive=1 GROUP BY ar.StudentId)
                SELECT CAST(COALESCE(100.0*SUM(CASE WHEN Status IN('Present','Late') THEN 1.0 ELSE 0 END)/NULLIF(COUNT(CASE WHEN Status IN('Present','Absent','Late') THEN 1 END),0),0) AS FLOAT) AS AveragePct,
                       (SELECT COUNT(*) FROM Totals WHERE Pct < COALESCE(TRY_CONVERT(float,(SELECT [Value] FROM dbo.ApplicationSettings WHERE [Key]=N'LowAttendanceThreshold')),75.0)) AS LowCount
                FROM dbo.AttendanceRecords;
                SELECT CONVERT(date,s.SessionDate) AS ReportDate,SUM(CASE WHEN r.Status='Present' THEN 1 ELSE 0 END),SUM(CASE WHEN r.Status='Absent' THEN 1 ELSE 0 END),SUM(CASE WHEN r.Status='Late' THEN 1 ELSE 0 END)
                FROM dbo.AttendanceSessions s JOIN dbo.AttendanceRecords r ON r.SessionId=s.SessionId WHERE s.SessionDate>=DATEADD(day,-6,CONVERT(date,GETDATE())) AND r.Status IN('Present','Absent','Late') GROUP BY CONVERT(date,s.SessionDate) ORDER BY ReportDate;
                SELECT COALESCE(SUM(CASE WHEN r.Status='Present' THEN 1 ELSE 0 END),0),COALESCE(SUM(CASE WHEN r.Status='Absent' THEN 1 ELSE 0 END),0),COALESCE(SUM(CASE WHEN r.Status='Late' THEN 1 ELSE 0 END),0),COALESCE(SUM(CASE WHEN r.Status='Excused' THEN 1 ELSE 0 END),0) FROM dbo.AttendanceSessions s JOIN dbo.AttendanceRecords r ON r.SessionId=s.SessionId WHERE s.SessionDate>=DATEFROMPARTS(YEAR(GETDATE()),MONTH(GETDATE()),1);
                SELECT c.Name,COUNT(DISTINCT s.StudentId),COALESCE(100.0*SUM(CASE WHEN r.Status IN('Present','Late') THEN 1.0 ELSE 0 END)/NULLIF(COUNT(CASE WHEN r.Status IN('Present','Absent','Late') THEN 1 END),0),0)
                FROM dbo.Classes c LEFT JOIN dbo.Students s ON s.ClassId=c.ClassId AND s.IsActive=1 LEFT JOIN dbo.AttendanceRecords r ON r.StudentId=s.StudentId AND r.Status IN('Present','Absent','Late') GROUP BY c.Name ORDER BY c.Name;
                SELECT TOP(6) ses.SessionDate,c.Name,sub.Name,COUNT(ar.RecordId) FROM dbo.AttendanceSessions ses JOIN dbo.Subjects sub ON sub.SubjectId=ses.SubjectId JOIN dbo.Classes c ON c.ClassId=sub.ClassId LEFT JOIN dbo.AttendanceRecords ar ON ar.SessionId=ses.SessionId GROUP BY ses.SessionDate,ses.SessionId,c.Name,sub.Name ORDER BY ses.SessionDate DESC,ses.SessionId DESC;
                """;
            await using var r=await q.ExecuteReaderAsync(ct);
             if(await r.ReadAsync(ct)){model.TotalStudents=r.GetInt32(0);model.LowAttendanceThreshold=Convert.ToDouble(r.GetValue(1)??75.0);model.InstitutionName=r.GetString(2);}
             if(await r.NextResultAsync(ct)&&await r.ReadAsync(ct)){model.AttendanceToday=r.GetInt32(0);model.AbsentToday=r.GetInt32(1);model.LateToday=r.GetInt32(2);model.ExcusedToday=r.GetInt32(3);}
             if(await r.NextResultAsync(ct)&&await r.ReadAsync(ct)){model.AverageAttendance=Convert.ToDouble(r.GetValue(0)??0.0);model.LowAttendance=r.GetInt32(1);}
             if(await r.NextResultAsync(ct))while(await r.ReadAsync(ct))model.Weekly.Add(new(){Date=r.GetDateTime(0),Present=r.GetInt32(1),Absent=r.GetInt32(2),Late=r.GetInt32(3)});
             if(await r.NextResultAsync(ct)&&await r.ReadAsync(ct)){model.MonthPresent=r.GetInt32(0);model.MonthAbsent=r.GetInt32(1);model.MonthLate=r.GetInt32(2);model.MonthExcused=r.GetInt32(3);}
             if(await r.NextResultAsync(ct))while(await r.ReadAsync(ct))model.Classes.Add(new(){ClassName=r.GetString(0),Students=r.GetInt32(1),Percentage=Convert.ToDouble(r.GetValue(2)??0.0)});
             if(await r.NextResultAsync(ct))while(await r.ReadAsync(ct))model.Recent.Add(new(){Date=r.GetDateTime(0),ClassName=r.GetString(1),Subject=r.GetString(2),Records=r.GetInt32(3)});
        }
        catch(SqlException){TempData["Error"]="Could not load live dashboard metrics. Verify the SmartAttend database schema and SQL Server connection.";}
        return View(model);
    }
    public IActionResult Privacy()=>View();
    [AllowAnonymous]
    public IActionResult Error()=>View();
}
