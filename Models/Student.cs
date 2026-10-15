using System.ComponentModel.DataAnnotations;
namespace attendence.Models;
public sealed class Student
{
    public int StudentId { get; set; }
    [Required, StringLength(30)] public string EnrollmentNumber { get; set; } = "";
    [Required, StringLength(120)] public string FullName { get; set; } = "";
    [EmailAddress, StringLength(256)] public string? Email { get; set; }
    [Phone, StringLength(30)] public string? PhoneNumber { get; set; }
    [StringLength(80)] public string? Department { get; set; }
    [StringLength(80)] public string? ClassName { get; set; }
    public int? ClassId { get; set; }
    [Range(1, 12)] public int Semester { get; set; } = 1;
    [StringLength(20)] public string? AcademicYear { get; set; }
    public bool IsActive { get; set; } = true;
}
