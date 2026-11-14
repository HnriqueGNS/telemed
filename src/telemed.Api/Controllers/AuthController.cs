using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using telemed.Api.Contracts;
using telemed.Infrastructure.Identity;
using telemed.Infrastructure.Persistence;

namespace telemed.Api.Controllers;

[ApiController]
[Route("api/auth")]
[EnableRateLimiting("auth")]
public class AuthController(
    UserManager<ApplicationUser> users,
    SignInManager<ApplicationUser> signIn,
    AppDbContext db) : ControllerBase
{
    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterRequest req)
    {
        if (!await db.Clinics.AnyAsync(c => c.Id == req.ClinicId))
            return BadRequest(new { error = "Clínica inválida." });

        var user = new ApplicationUser
        {
            UserName = req.Email,
            Email = req.Email,
            FullName = req.FullName.Trim(),
            ClinicId = req.ClinicId
        };

        var result = await users.CreateAsync(user, req.Password);
        if (!result.Succeeded)
            return BadRequest(new { errors = result.Errors.Select(e => e.Description) });

        await users.AddToRoleAsync(user, Roles.Patient);

        return StatusCode(StatusCodes.Status201Created, new { user.Id });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest req)
    {
        var user = await users.FindByEmailAsync(req.Email);
        if (user is null || !user.IsActive)
        {
            return Unauthorized(new { error = "Credenciais inválidas." });
        }
        var result = await signIn.PasswordSignInAsync(
            user, req.Password, isPersistent: false, lockoutOnFailure: true);

        if (result.Succeeded)
        {
            return Ok(new { requiresTwoFactor = false });
        }
        if (result.RequiresTwoFactor)
        {
             return Ok(new { requiresTwoFactor = true });
        }
        if (result.IsLockedOut)
        {  
            return StatusCode(StatusCodes.Status423Locked,
                new { error = "Conta temporariamente bloqueada. Tente mais tarde." });
        }
        return Unauthorized(new { error = "Credenciais inválidas." });
    }

    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        await signIn.SignOutAsync();
        return NoContent();
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> Me()
    {
        var user = await users.GetUserAsync(User);
        if (user is null) return Unauthorized();

        var roles = await users.GetRolesAsync(user);
        return Ok(new
        {
            user.Id,
            user.Email,
            user.FullName,
            user.ClinicId,
            roles,
            twoFactorEnabled = user.TwoFactorEnabled
        });
    }
}
