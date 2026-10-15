using System.Text;
using attendence.Data;
using attendence.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;
using attendence.Services;
using Microsoft.AspNetCore.Authorization;
namespace attendence.Controllers;
[Authorize(Roles="Administrator,Teacher")]
public sealed class ReportsController(AttendanceReportRepository reports,AttendanceRepository attendance):Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(AttendanceReportViewModel filters,CancellationToken ct)
    {
        filters.StartDate??=new DateTime(DateTime.Today.Year,DateTime.Today.Month,1);filters.EndDate??=DateTime.Today;
        if(filters.StartDate>filters.EndDate)ModelState.AddModelError(nameof(filters.StartDate),"Start date must be on or before end date.");
        if(filters.Status is not null && filters.Status is not ("Present" or "Absent" or "Late" or "Excused"))ModelState.AddModelError(nameof(filters.Status),"Choose a valid attendance status.");
        var teacherId=TeacherScope();if(User.IsInRole("Teacher")&&teacherId is null)return Forbid();
        try
        {
            var classes=await attendance.ClassesAsync(ct,teacherId);filters.Classes=ToOptions(classes,filters.ClassId);
            if(filters.ClassId is int cid)filters.Subjects=ToOptions(await attendance.SubjectsAsync(cid,ct,teacherId),filters.SubjectId);
            filters.Students=ToOptions(await reports.StudentsAsync(ct,teacherId),filters.StudentId);
            if(ModelState.IsValid)filters.Rows=await reports.GetAsync(filters,ct,teacherId);
            var low=await reports.GetLowAttendanceAsync(ct,teacherId);filters.LowAttendanceThreshold=low.Threshold;filters.LowAttendance=low.Students;
        }
        catch(SqlException){filters.Error="Reports are unavailable because the database or required tables could not be reached.";}
        return View(filters);
    }
    [HttpGet]
    public async Task<IActionResult> Export(AttendanceReportViewModel filters,CancellationToken ct)
    {
        if(filters.StartDate>filters.EndDate)return BadRequest("Start date must be on or before end date.");
        if(filters.Status is not null && filters.Status is not ("Present" or "Absent" or "Late" or "Excused"))return BadRequest("Invalid attendance status.");
        var teacherId=TeacherScope();if(User.IsInRole("Teacher")&&teacherId is null)return Forbid();
        try
        {
            var rows=await reports.GetAsync(filters,ct,teacherId);var csv=new StringBuilder();csv.AppendLine("Date,Period,Class,Subject,Enrollment Number,Student,Status,Remarks");
            foreach(var r in rows)csv.AppendLine(string.Join(',',new[]{r.Date.ToString("yyyy-MM-dd"),r.Period,r.ClassName,r.Subject,r.EnrollmentNumber,r.StudentName,r.Status,r.Remarks??""}.Select(CsvExportFormatter.EscapeCell)));
            return File(new UTF8Encoding(true).GetBytes(csv.ToString()),"text/csv; charset=utf-8",$"smartattend-report-{DateTime.Today:yyyyMMdd}.csv");
        }
        catch(SqlException){return Problem("Could not create the report because the database is unavailable.",statusCode:503);}
    }
    private static IReadOnlyList<SelectListItem> ToOptions(IEnumerable<(int Id,string Name)> rows,int? selected)=>rows.Select(x=>new SelectListItem(x.Name,x.Id.ToString(),selected==x.Id)).ToList();
    private int? TeacherScope()=>int.TryParse(User.FindFirst("TeacherId")?.Value,out var id)?id:null;
}
