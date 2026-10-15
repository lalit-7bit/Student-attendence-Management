using System.ComponentModel.DataAnnotations;
namespace attendence.Models;
public sealed class SettingsForm { [Range(1,100)] public int LowAttendanceThreshold {get;set;}=75; [StringLength(120)] public string InstitutionName {get;set;}="SmartAttend"; }
public sealed class DepartmentForm { public int Id {get;set;} [Required,StringLength(100)] public string Name {get;set;}=""; }
public sealed class ClassForm { public int Id {get;set;} [Range(1,int.MaxValue)] public int DepartmentId {get;set;} [Required,StringLength(80)] public string Name {get;set;}=""; [Range(1,12)] public byte Semester {get;set;}=1; [Required,StringLength(20)] public string AcademicYear {get;set;}=""; }
public sealed class TeacherForm { public int Id {get;set;} [Required,StringLength(120)] public string FullName {get;set;}=""; [EmailAddress,StringLength(256)] public string? Email {get;set;} }
public sealed class SubjectForm { public int Id {get;set;} [Range(1,int.MaxValue)] public int ClassId {get;set;} [Required,StringLength(30)] public string Code {get;set;}=""; [Required,StringLength(120)] public string Name {get;set;}=""; public List<int> TeacherIds {get;set;}=[]; }
public sealed record DepartmentRow(int Id,string Name);
public sealed record ClassRow(int Id,int DepartmentId,string Department,string Name,byte Semester,string AcademicYear);
public sealed record TeacherRow(int Id,string FullName,string? Email,bool IsActive);
public sealed record SubjectRow(int Id,int ClassId,string ClassName,string Code,string Name,string? TeacherName,string? TeacherIds);
public sealed class SchoolManagementViewModel
{
 [Range(1,100)] public int LowAttendanceThreshold {get;set;}=75;
 [StringLength(120)] public string InstitutionName {get;set;}="SmartAttend";
 public IReadOnlyList<DepartmentRow> Departments {get;set;}=[]; public IReadOnlyList<ClassRow> Classes {get;set;}=[]; public IReadOnlyList<TeacherRow> Teachers {get;set;}=[]; public IReadOnlyList<SubjectRow> Subjects {get;set;}=[];
 public DepartmentForm Department {get;set;}=new(); public ClassForm Class {get;set;}=new(); public TeacherForm Teacher {get;set;}=new(); public SubjectForm Subject {get;set;}=new();
}
