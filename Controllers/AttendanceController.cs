using attendence.Data;
using attendence.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;
using Microsoft.AspNetCore.Authorization;
namespace attendence.Controllers;
[Authorize(Roles="Administrator,Teacher")]
public sealed class AttendanceController(AttendanceRepository attendance) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(int? classId,int? subjectId,int? teacherId,DateTime? date,string? period,CancellationToken ct)
    {
        var model=new AttendanceViewModel { ClassId=classId,SubjectId=subjectId,TeacherId=teacherId,SessionDate=date?.Date??DateTime.Today,Period=string.IsNullOrWhiteSpace(period)?"1":period.Trim() };
        if(User.IsInRole("Teacher") && int.TryParse(User.FindFirst("TeacherId")?.Value,out var linkedTeacherId)) model.TeacherId=linkedTeacherId;
        try
        {
            var teacherScope=User.IsInRole("Teacher")?model.TeacherId:null;
            if(classId is >0 && subjectId is >0) model.Students=(await attendance.RosterAsync(classId.Value,subjectId.Value,model.SessionDate,model.Period,ct,teacherScope)).ToList();
            if(classId is >0 && model.Students.Count==0) model.Notice="No active students are assigned to this class yet.";
            return View(await WithOptionsAsync(model,ct));
        }
        catch(SqlException) { TempData["Error"]="Attendance could not load from SQL Server. Check the database connection and run Database/Schema.sql."; return View(await WithOptionsAsync(model,ct,true)); }
    }
    [HttpPost,ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(AttendanceViewModel model,CancellationToken ct)
    {
        if(User.IsInRole("Teacher"))
        {
            if(!int.TryParse(User.FindFirst("TeacherId")?.Value,out var linkedTeacherId)) return Forbid();
            model.TeacherId=linkedTeacherId;
        }
        if(model.SessionDate.Date>DateTime.Today) ModelState.AddModelError(nameof(model.SessionDate),"Attendance cannot be recorded for a future date.");
        if(!ModelState.IsValid) return View("Index",await WithOptionsAsync(model,ct));
        try { await attendance.SaveAsync(model,User.Identity?.Name??"Authenticated user",ct); TempData["Success"]="Attendance saved. Existing session records were updated and changes audited."; return RedirectToAction(nameof(Index),new {classId=model.ClassId,subjectId=model.SubjectId,teacherId=model.TeacherId,date=model.SessionDate.ToString("yyyy-MM-dd"),period=model.Period}); }
        catch(InvalidOperationException ex) { ModelState.AddModelError(string.Empty,ex.Message); }
        catch(SqlException) { ModelState.AddModelError(string.Empty,"Attendance could not be saved. Check database availability and confirm the schema is current."); }
        return View("Index",await WithOptionsAsync(model,ct));
    }
    private async Task<AttendanceViewModel> WithOptionsAsync(AttendanceViewModel model,CancellationToken ct,bool tolerateDbFailure=false)
    {
        try
        {
            var teacherScope=User.IsInRole("Teacher")?model.TeacherId:null;
            var classes=await attendance.ClassesAsync(ct,teacherScope); model.Classes=Options(classes,model.ClassId);
            if(model.ClassId is >0){var subjects=await attendance.SubjectsAsync(model.ClassId.Value,ct,teacherScope);model.Subjects=Options(subjects,model.SubjectId);}
            if(model.SubjectId is >0){var teachers=await attendance.TeachersAsync(model.SubjectId.Value,ct,teacherScope);model.Teachers=Options(teachers,model.TeacherId);}
        }
        catch(SqlException) when(tolerateDbFailure) { model.Classes=[]; model.Subjects=[]; model.Teachers=[]; }
        return model;
    }
    private static IReadOnlyList<SelectListItem> Options(IEnumerable<(int Id,string Name)> rows,int? selected)=>rows.Select(x=>new SelectListItem(x.Name,x.Id.ToString(),x.Id==selected)).ToList();
}
