using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using limbo.Data;
using limbo.Models;
using limbo.Models.ViewModels;
using limbo.Services;

namespace limbo.Controllers;

public class AuthController : Controller
{
    private readonly AppDbContext _db;
    private readonly EmailService _email;

    public AuthController(AppDbContext db, EmailService email)
    {
        _db = db;
        _email = email;
    }

    [HttpGet]
    public IActionResult Login()
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAction("Index", "Users");

        return View(new LoginViewModel());
    }

    [HttpPost]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.Email == model.Email);

        if (user == null || !BCrypt.Net.BCrypt.Verify(model.Password, user.Password))
        {
            model.ErrorMessage = "Invalid email or password.";
            return View(model);
        }

        if (user.Status == "blocked")
        {
            model.ErrorMessage = "Your account has been blocked.";
            return View(model);
        }

        user.LastLogin = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        var claims = new List<Claim>
        {
            new Claim("userId", user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Name),
            new Claim(ClaimTypes.Email, user.Email)
        };

        var identity = new ClaimsIdentity(
            claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity));

        return RedirectToAction("Index", "Users");
    }

    [HttpGet]
    public IActionResult Register()
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAction("Index", "Users");

        return View(new RegisterViewModel());
    }

    [HttpPost]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (string.IsNullOrWhiteSpace(model.Name) ||
            string.IsNullOrWhiteSpace(model.Email) ||
            string.IsNullOrWhiteSpace(model.Password))
        {
            model.ErrorMessage = "All fields are required.";
            return View(model);
        }

        var token = Guid.NewGuid().ToString();
        var user = new User
        {
            Name = model.Name,
            Email = model.Email,
            Password = BCrypt.Net.BCrypt.HashPassword(model.Password),
            EmailToken = token
        };

        try
        {
            _db.Users.Add(user);
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            model.ErrorMessage = "This email is already registered.";
            return View(model);
        }

        _ = Task.Run(async () =>
        {
            try
            {
                await _email.SendVerificationEmailAsync(model.Email, token);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"EMAIL SEND FAILED: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
            }
        });

        model.SuccessMessage = "Registration successful! Check your email to verify your account.";
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> VerifyEmail(string token)
    {
        if (string.IsNullOrEmpty(token))
            return RedirectToAction("Login");

        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.EmailToken == token);

        if (user == null)
        {
            TempData["StatusMessage"] = "Invalid or expired verification link.";
            return RedirectToAction("Login");
        }

        if (user.Status == "unverified")
            user.Status = "active";

        user.EmailToken = null;
        await _db.SaveChangesAsync();

        TempData["StatusMessage"] = "Email verified successfully. You can now log in.";
        return RedirectToAction("Login");
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Login");
    }
}