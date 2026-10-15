using attendence.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
namespace attendence.Controllers;
[Authorize(Roles="Student")]
public sealed class StudentPortalController(AttendanceReportRepository reports,ILogger<StudentPortalController> logger):Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        if(!int.TryParse(User.FindFirst("StudentId")?.Value,out var studentId))return Forbid();
        try{var model=await reports.GetStudentPortalAsync(studentId,ct);return model is null?NotFound():View(model);}
        catch(SqlException ex){logger.LogError(ex,"Student self-service lookup failed for account {AccountId}",User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value);TempData["Error"]="Your attendance is temporarily unavailable.";return View("Unavailable");}
    }
}
