using System.ComponentModel.DataAnnotations;

namespace cateringflow.Models;

public class SettingsModel
{
    [Key]
    public int Id { get; set; }

    [Required]
    [Display(Name = "Company Name")]
    public string CompanyName { get; set; } = "CateringFlow";

    [Display(Name = "Company Email")]
    [EmailAddress]
    public string? CompanyEmail { get; set; }

    [Display(Name = "Company Phone")]
    [Phone]
    public string? CompanyPhone { get; set; }

    [Display(Name = "Company Address")]
    public string? CompanyAddress { get; set; }

    public string? Tagline { get; set; }

    [Display(Name = "Last Updated")]
    public DateTime UpdatedAt { get; set; } = DateTime.Now;
}
