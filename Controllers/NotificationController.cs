using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using cateringflow.Data;
using cateringflow.Models;

namespace cateringflow.Controllers;

[Authorize]
public class NotificationController : Controller
{
    private readonly CateringFlowDbContext _db;

    public NotificationController(CateringFlowDbContext db)
    {
        _db = db;
    }

    public async Task<IActionResult> Index()
    {
        var notifications = await _db.Notifications
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync();
        return View(notifications);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkAsRead(int id)
    {
        var notification = await _db.Notifications.FindAsync(id);
        if (notification == null) return NotFound();
        notification.IsRead = true;
        _db.Notifications.Update(notification);
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkAllRead()
    {
        var unread = await _db.Notifications.Where(n => !n.IsRead).ToListAsync();
        foreach (var notification in unread)
        {
            notification.IsRead = true;
        }
        _db.Notifications.UpdateRange(unread);
        await _db.SaveChangesAsync();
        TempData["Success"] = "All notifications marked as read.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var notification = await _db.Notifications.FindAsync(id);
        if (notification == null) return NotFound();
        _db.Notifications.Remove(notification);
        await _db.SaveChangesAsync();
        TempData["Success"] = "Notification deleted.";
        return RedirectToAction(nameof(Index));
    }
}