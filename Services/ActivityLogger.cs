using cateringflow.Data;
using cateringflow.Models;

namespace cateringflow.Services;

public static class ActivityLogger
{
    public static async Task LogAsync(
        CateringFlowDbContext db,
        string action,
        string entityType,
        int? entityId,
        string description,
        string? performedBy = null)
    {
        db.ActivityLogs.Add(new ActivityLogModel
        {
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            Description = description,
            PerformedBy = performedBy,
            CreatedAt = DateTime.Now
        });
        await db.SaveChangesAsync();
    }
}