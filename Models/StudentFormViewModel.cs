using Microsoft.AspNetCore.Mvc.Rendering;
namespace attendence.Models;
public sealed class StudentFormViewModel
{
    public Student Student { get; set; } = new();
    public IReadOnlyList<SelectListItem> Classes { get; set; } = Array.Empty<SelectListItem>();
}
