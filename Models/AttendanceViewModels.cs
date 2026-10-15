using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
namespace attendence.Models;
public sealed class AttendanceViewModel
{
    public int? ClassId { get; set; }
    public int? SubjectId { get; set; }
    public int? TeacherId { get; set; }
    [DataType(DataType.Date), Required] public DateTime SessionDate { get; set; } = DateTime.Today;
    [Required, StringLength(30)] public string Period { get; set; } = "1";
    public List<AttendanceEntryViewModel> Students { get; set; } = [];
    public IReadOnlyList<SelectListItem> Classes { get; set; } = [];
    public IReadOnlyList<SelectListItem> Subjects { get; set; } = [];
    public IReadOnlyList<SelectListItem> Teachers { get; set; } = [];
    public string? Notice { get; set; }
}
public sealed class AttendanceEntryViewModel
{
    public int StudentId { get; set; }
    public string EnrollmentNumber { get; set; } = "";
    public string FullName { get; set; } = "";
    [Required, RegularExpression("^(Present|Absent|Late|Excused)$")] public string Status { get; set; } = "Present";
    [StringLength(300)] public string? Remarks { get; set; }
}
