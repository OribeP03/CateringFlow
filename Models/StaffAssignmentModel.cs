using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace cateringflow.Models;

public class StaffAssignmentModel
{
    [Key]
    public int Id { get; set; }

    [Required]
    [Display(Name = "Staff")]
    public int StaffId { get; set; }

    [ForeignKey("StaffId")]
    public StaffModel? Staff { get; set; }

    [Required]
    [Display(Name = "Event")]
    public int EventId { get; set; }

    [ForeignKey("EventId")]
    public EventModel? Event { get; set; }

    [Required]
    [Display(Name = "Role at Event")]
    public string RoleAtEvent { get; set; } = "Server";

    [Display(Name = "Assigned At")]
    public DateTime AssignedAt { get; set; } = DateTime.Now;

    public string? Notes { get; set; }

    [NotMapped]
    public string StaffName => Staff?.FullName ?? $"Staff #{StaffId}";
}
