using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using limbo.Data;
using limbo.Models.ViewModels;

namespace limbo.Controllers;

[Authorize]
public class UsersController : Controller
{
    private readonly AppDbContext _db;

    public UsersController(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IActionResult> Index()
    {
        var model = new UsersViewModel
        {
            Users = await _db.Users
                .OrderByDescending(u => u.LastLogin)
                .ToListAsync(),
            StatusMessage = TempData["StatusMessage"] as string
        };

        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Block(List<int> ids)
    {
        if (ids.Count == 0)
            return RedirectToAction("Index");

        await _db.Users
            .Where(u => ids.Contains(u.Id))
            .ExecuteUpdateAsync(s =>
                s.SetProperty(u => u.Status, "blocked"));

        TempData["StatusMessage"] = "Selected users have been blocked.";
        return RedirectToAction("Index");
    }

    [HttpPost]
    public async Task<IActionResult> Unblock(List<int> ids)
    {
        if (ids.Count == 0)
            return RedirectToAction("Index");

        await _db.Users
            .Where(u => ids.Contains(u.Id) && u.Status == "blocked")
            .ExecuteUpdateAsync(s =>
                s.SetProperty(u => u.Status, "active"));

        TempData["StatusMessage"] = "Selected users have been unblocked.";
        return RedirectToAction("Index");
    }

    [HttpPost]
    public async Task<IActionResult> Delete(List<int> ids)
    {
        if (ids.Count == 0)
            return RedirectToAction("Index");

        await _db.Users
            .Where(u => ids.Contains(u.Id))
            .ExecuteDeleteAsync();

        TempData["StatusMessage"] = "Selected users have been deleted.";
        return RedirectToAction("Index");
    }

    [HttpPost]
    public async Task<IActionResult> DeleteUnverified(List<int> ids)
    {
        if (ids.Count == 0)
            return RedirectToAction("Index");

        await _db.Users
            .Where(u => ids.Contains(u.Id) && u.Status == "unverified")
            .ExecuteDeleteAsync();

        TempData["StatusMessage"] = "Unverified users from selection have been deleted.";
        return RedirectToAction("Index");
    }
}