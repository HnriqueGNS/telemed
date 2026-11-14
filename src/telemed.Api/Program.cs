using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Serilog;
using telemed.Api.Services;
using telemed.Application.Abstractions;
using telemed.Domain.Entities;
using telemed.Infrastructure.Identity;
using telemed.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((ctx, cfg) => cfg
    .ReadFrom.Configuration(ctx.Configuration)
    .WriteTo.Console());

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, CurrentUser>();

builder.Services.AddDbContext<AppDbContext>(o =>
    o.UseNpgsql(builder.Configuration.GetConnectionString("Default")));

builder.Services
    .AddIdentity<ApplicationUser, IdentityRole<Guid>>(o =>
    {
        o.Password.RequiredLength = 10;
        o.Lockout.MaxFailedAccessAttempts = 5;
        o.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
        o.User.RequireUniqueEmail = true;
    })
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders()
    .AddClaimsPrincipalFactory<AppClaimsPrincipalFactory>();

builder.Services.ConfigureApplicationCookie(o =>
{
    o.Cookie.Name = "telemed.auth";
    o.Cookie.HttpOnly = true;
    o.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
        ? CookieSecurePolicy.SameAsRequest
        : CookieSecurePolicy.Always;
    o.Cookie.SameSite = SameSiteMode.Strict;
    o.ExpireTimeSpan = TimeSpan.FromMinutes(30);
    o.SlidingExpiration = true;
    o.Events.OnRedirectToLogin = ctx => { ctx.Response.StatusCode = 401; return Task.CompletedTask; };
    o.Events.OnRedirectToAccessDenied = ctx => { ctx.Response.StatusCode = 403; return Task.CompletedTask; };
});

builder.Services.AddRateLimiter(o =>
{
    o.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    o.AddPolicy("auth", http => RateLimitPartition.GetFixedWindowLimiter(
        http.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 10,
            Window = TimeSpan.FromMinutes(1)
        }));
});

builder.Services.AddAuthorization();
builder.Services.AddControllers();
builder.Services.AddOpenApi();

builder.Services.AddCors(o => o.AddDefaultPolicy(p => p
    .WithOrigins("http://localhost:5173")
    .AllowCredentials().AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
    foreach (var role in Roles.All)
        if (!await roleManager.RoleExistsAsync(role))
            await roleManager.CreateAsync(new IdentityRole<Guid>(role));

    if (app.Environment.IsDevelopment())
    {
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var demoId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        if (!await db.Clinics.AnyAsync(c => c.Id == demoId))
        {
            db.Clinics.Add(new Clinic { Id = demoId, Name = "Clínica Demo" });
            await db.SaveChangesAsync();
        }
    }
}

if (app.Environment.IsDevelopment()) app.MapOpenApi();
else app.UseHttpsRedirection();

app.UseCors();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.Run();