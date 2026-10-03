using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using cateringflow.Data;
using cateringflow.Models;

namespace cateringflow.Controllers;

[Authorize]
public class NotificationController : AppController
{
    private readonly CateringFlowDbContext _db;

    public NotificationController(CateringFlowDbContext db)
    {
        _db = db;
    }

    public async Task<IActionResult> Index(int? page)
    {
        ViewData["UnreadCount"] = await _db.Notifications.CountAsync(n => !n.IsRead);
        var notifications = await PagedResult<NotificationModel>.CreateAsync(
            _db.Notifications.OrderByDescending(n => n.CreatedAt),
            page);
        return View(notifications);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkAsRead(int id, string? returnUrl = null)
    {
        var notification = await _db.Notifications.FindAsync(id);
        if (notification == null) return NotFound();
        notification.IsRead = true;
        _db.Notifications.Update(notification);
        await _db.SaveChangesAsync();
        return RedirectToIndex(returnUrl);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkAllRead(string? returnUrl = null)
    {
        var unread = await _db.Notifications.Where(n => !n.IsRead).ToListAsync();
        foreach (var notification in unread)
        {
            notification.IsRead = true;
        }
        _db.Notifications.UpdateRange(unread);
        await _db.SaveChangesAsync();
        TempData["Success"] = "All notifications marked as read.";
        return RedirectToIndex(returnUrl);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, string? returnUrl = null)
    {
        var notification = await _db.Notifications.FindAsync(id);
        if (notification == null) return NotFound();
        _db.Notifications.Remove(notification);
        await _db.SaveChangesAsync();
        TempData["Success"] = "Notification deleted.";
        return RedirectToIndex(returnUrl);
    }
}