using Microsoft.AspNetCore.Mvc;
using attendence.Models;
using attendence.Data;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;
using Microsoft.AspNetCore.Authorization;
namespace attendence.Controllers;
[Authorize(Roles="Administrator")]
public sealed class StudentsController(StudentRepository students) : Controller
{
    public async Task<IActionResult> Index(string? search, CancellationToken cancellationToken)
    {
        try { ViewBag.Search = search; return View(await students.GetAllAsync(search, cancellationToken)); }
        catch (SqlException) { TempData["Error"] = "Could not load students. Confirm SmartAttend is created and Database/Schema.sql has been applied."; return View(Array.Empty<Student>()); }
    }
    public async Task<IActionResult> Create(CancellationToken cancellationToken) => View(await BuildFormAsync(new Student(), cancellationToken));
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Student student, CancellationToken cancellationToken)
    {
        if (student.ClassId is int classId && !(await students.GetClassesAsync(cancellationToken)).Any(x => x.Id == classId)) ModelState.AddModelError("Student.ClassId", "Select a valid class.");
        if (!ModelState.IsValid) return View(await BuildFormAsync(student, cancellationToken));
        try { await students.SaveAsync(student, cancellationToken); TempData["Success"] = "Student registered successfully."; return RedirectToAction(nameof(Index)); }
        catch (SqlException ex) when (ex.Number is 2601 or 2627) { ModelState.AddModelError("Student.EnrollmentNumber", "That enrollment number is already registered."); return View(await BuildFormAsync(student, cancellationToken)); }
        catch (SqlException) { TempData["Error"] = "Could not save the student. Confirm SmartAttend is created and Database/Schema.sql has been applied."; return View(await BuildFormAsync(student, cancellationToken)); }
    }
    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        var student = await students.FindAsync(id, cancellationToken);
        return student is null ? NotFound() : View("Create", await BuildFormAsync(student, cancellationToken));
    }
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Student student, CancellationToken cancellationToken)
    {
        if (id != student.StudentId) return BadRequest();
        if (student.ClassId is int classId && !(await students.GetClassesAsync(cancellationToken)).Any(x => x.Id == classId)) ModelState.AddModelError("Student.ClassId", "Select a valid class.");
        if (!ModelState.IsValid) return View("Create", await BuildFormAsync(student, cancellationToken));
        try { await students.SaveAsync(student, cancellationToken); TempData["Success"] = "Student details updated."; return RedirectToAction(nameof(Index)); }
        catch (SqlException ex) when (ex.Number is 2601 or 2627) { ModelState.AddModelError("Student.EnrollmentNumber", "That enrollment number is already registered."); return View("Create", await BuildFormAsync(student, cancellationToken)); }
    }
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        try { var deleted = await students.DeleteAsync(id, cancellationToken); TempData[deleted ? "Success" : "Error"] = deleted ? "Student deactivated." : "Student was already inactive or could not be found."; }
        catch (SqlException) { TempData["Error"] = "Could not deactivate student. Check the database connection."; }
        return RedirectToAction(nameof(Index));
    }
    private async Task<StudentFormViewModel> BuildFormAsync(Student student, CancellationToken cancellationToken)
    {
        IReadOnlyList<(int Id, string Label)> classes;
        try { classes = await students.GetClassesAsync(cancellationToken); }
        catch (SqlException)
        {
            TempData["Error"] = "The student form loaded, but classes could not be fetched. Apply Database/Schema.sql and confirm the LocalDB instance is running before saving records.";
            classes = Array.Empty<(int, string)>();
        }
        return new StudentFormViewModel { Student = student, Classes = new[] { new SelectListItem("No class assigned", "") }.Concat(classes.Select(c => new SelectListItem(c.Label, c.Id.ToString()))).ToList() };
    }
}
