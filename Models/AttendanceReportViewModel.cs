using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
namespace attendence.Models;
public sealed class AttendanceReportViewModel
{
    public int? ClassId { get; set; } public int? SubjectId { get; set; } public int? StudentId { get; set; }
    [DataType(DataType.Date)] public DateTime? StartDate { get; set; } [DataType(DataType.Date)] public DateTime? EndDate { get; set; }
    public string? Status { get; set; }
    public double LowAttendanceThreshold {get;set;}=75;
    public IReadOnlyList<LowAttendanceRow> LowAttendance {get;set;}=[];
    public IReadOnlyList<AttendanceReportRow> Rows { get; set; } = [];
    public IReadOnlyList<SelectListItem> Classes { get; set; } = [];
    public IReadOnlyList<SelectListItem> Subjects { get; set; } = [];
    public IReadOnlyList<SelectListItem> Students { get; set; } = [];
    public string? Error { get; set; }
}
public sealed record LowAttendanceRow(string EnrollmentNumber,string StudentName,string? ClassName,double Percentage);
public sealed class AttendanceReportRow
{
    public DateTime Date { get; set; } public string Period { get; set; }=""; public string ClassName { get; set; }=""; public string Subject { get; set; }="";
    public string EnrollmentNumber { get; set; }=""; public string StudentName { get; set; }=""; public string Status { get; set; }=""; public string? Remarks { get; set; }
}
public sealed class StudentPortalViewModel
{
    public string StudentName { get; set; }=""; public string EnrollmentNumber { get; set; }=""; public double AttendancePercentage { get; set; }
    public int Present { get; set; } public int Absent { get; set; } public int Late { get; set; } public int Excused { get; set; }
    public IReadOnlyList<AttendanceReportRow> Records { get; set; }=[];
}
