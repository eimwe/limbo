using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using limbo.Data;

namespace limbo.Middleware;

public class UserStatusMiddleware
{
    private readonly RequestDelegate _next;

    public UserStatusMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, AppDbContext db)
    {
        var path = context.Request.Path.Value?.ToLower() ?? "";
        var isPublicPath =
            path.StartsWith("/auth") ||
            path.StartsWith("/css") ||
            path.StartsWith("/js") ||
            path.StartsWith("/lib") ||
            path.StartsWith("/_framework") ||
            path == "/";

        if (!isPublicPath && context.User.Identity?.IsAuthenticated == true)
        {
            var userIdClaim = context.User.FindFirst("userId")?.Value;

            if (int.TryParse(userIdClaim, out var userId))
            {
                var user = await db.Users.FindAsync(userId);

                if (user == null || user.Status == "blocked")
                {
                    await context.SignOutAsync(
                        CookieAuthenticationDefaults.AuthenticationScheme);
                    context.Response.Redirect("/auth/login");
                    return;
                }
            }
        }

        await _next(context);
    }
}