namespace attendence.Models;
public sealed class AppUser
{
    public int Id { get; set; }
    public string Email { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public string Role { get; set; } = "";
    public int? StudentId { get; set; }
    public int? TeacherId { get; set; }
}
public sealed class LoginViewModel
{
    [System.ComponentModel.DataAnnotations.Required, System.ComponentModel.DataAnnotations.EmailAddress]
    public string Email { get; set; } = "";
    [System.ComponentModel.DataAnnotations.Required, System.ComponentModel.DataAnnotations.DataType(System.ComponentModel.DataAnnotations.DataType.Password)]
    public string Password { get; set; } = "";
    public string? ReturnUrl { get; set; }
}
public sealed class UserAccountForm
{
    [System.ComponentModel.DataAnnotations.Required,System.ComponentModel.DataAnnotations.EmailAddress] public string Email {get;set;}="";
    [System.ComponentModel.DataAnnotations.Required,System.ComponentModel.DataAnnotations.MinLength(12),System.ComponentModel.DataAnnotations.RegularExpression("^(?=.*[a-z])(?=.*[A-Z])(?=.*\\d)(?=.*[^A-Za-z0-9]).{12,}$",ErrorMessage="Use 12+ characters with upper and lowercase letters, a number, and a symbol.")] public string Password {get;set;}="";
    [System.ComponentModel.DataAnnotations.Required] public string Role {get;set;}="Teacher";
    public int? StudentId {get;set;} public int? TeacherId {get;set;}
}
public sealed record UserAccountRow(int Id,string Email,string Role,string? LinkedName,bool IsActive);
