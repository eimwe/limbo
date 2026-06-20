using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using limbo.Data;
using limbo.Middleware;
using limbo.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

var dbHost     = Environment.GetEnvironmentVariable("DB_HOST") ?? "localhost";
var dbPort     = Environment.GetEnvironmentVariable("DB_PORT") ?? "5432";
var dbName     = Environment.GetEnvironmentVariable("DB_NAME") ?? "limbo";
var dbUser     = Environment.GetEnvironmentVariable("DB_USER") ?? "postgres";
var dbPassword = Environment.GetEnvironmentVariable("DB_PASSWORD") ?? "";

var connectionString =
    $"Host={dbHost};Port={dbPort};Database={dbName};Username={dbUser};Password={dbPassword}";

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));

var emailPassword = Environment.GetEnvironmentVariable("EMAIL_PASSWORD") ?? "";
var emailUser     = Environment.GetEnvironmentVariable("EMAIL_USER") ?? "";

builder.Services.AddSingleton(new EmailSettings
{
    Host     = builder.Configuration["Smtp:Host"] ?? "smtp-relay.brevo.com",
    Port     = int.Parse(builder.Configuration["Smtp:Port"] ?? "587"),
    User     = emailUser,
    Password = emailPassword,
    From     = emailUser
});

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/auth/login";
        options.ExpireTimeSpan = TimeSpan.FromHours(24);
        options.SlidingExpiration = true;
    });

builder.Services.AddAuthorization();

builder.Services.AddHttpClient<EmailService>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();

app.UseMiddleware<UserStatusMiddleware>();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Auth}/{action=Login}/{id?}");

app.Run();