using System.ComponentModel.DataAnnotations;

namespace cateringflow.Models;

public class StaffModel
{
    [Key]
    public int Id { get; set; }

    [Required(ErrorMessage = "Full name is required.")]
    [Display(Name = "Full Name")]
    public string FullName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Phone]
    public string? Phone { get; set; }

    [Required]
    [Display(Name = "Job Title")]
    public string Position { get; set; } = string.Empty;

    [Required]
    public string Availability { get; set; } = "Available";

    [Required]
    [Display(Name = "Employment Type")]
    public string EmploymentType { get; set; } = "Full-time";

    public string? Specialty { get; set; }

    [Display(Name = "Date Hired")]
    [DataType(DataType.Date)]
    public DateTime DateHired { get; set; } = DateTime.Now;

    [Display(Name = "Created At")]
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public ICollection<StaffAssignmentModel> Assignments { get; set; } = new List<StaffAssignmentModel>();
}
