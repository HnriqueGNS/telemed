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
    private const string Issuer = "Telemed";

    private static string CleanCode(string code) => code.Replace(" ", "").Replace("-", "");

    private static string FormatKey(string key) => string.Join(' ', key.Chunk(4).Select(c => new string(c))).ToLowerInvariant();

    private static string BuildUri(string email, string key) =>
        $"otpauth://totp/{Uri.EscapeDataString(Issuer)}:{Uri.EscapeDataString(email)}" +
        $"?secret={key}&issuer={Uri.EscapeDataString(Issuer)}&digits=6";

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
            var roles = await users.GetRolesAsync(user);
            var mustSetUp = !user.TwoFactorEnabled && roles.Any(r => SecurityPolicies.TwoFactorRoles.Contains(r));
            return Ok(new { requiresTwoFactor = false, requiresTwoFactorSetup = mustSetUp });
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

    [HttpPost("login-2fa")]
    public async Task<IActionResult> LoginTwoFactor(TwoFactorCodeRequest req)
    {
        var result = await signIn.TwoFactorAuthenticatorSignInAsync(CleanCode(req.Code), isPersistent: false, rememberClient: false);

        if (result.Succeeded)
        {
            return Ok(new { ok = true });
        }

        if (result.IsLockedOut)
        {
            return StatusCode(StatusCodes.Status423Locked, new { error = "Conta temporariamente bloqueada. Tente novamente mais tarde."  });
        }

        return Unauthorized(new { error = "Código inválido." });
    }

    [HttpPost("login-2fa-recovery")]
    public async Task<IActionResult> LoginRecovery(RecoveryCodeRequest req)
    {
        var result = await signIn.TwoFactorRecoveryCodeSignInAsync(req.RecoveryCode.Replace(" ", ""));

        if (result.Succeeded)
        {
            return Ok(new { ok = true });
        }

        if (result.IsLockedOut)
        {
            return StatusCode(StatusCodes.Status423Locked, new { error = "Conta temporariamente bloqueada. Tente novamente mais tarde." });
        }

        return Unauthorized(new { error = "Código de recuperação inválido." });
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

    [Authorize]
    [HttpPost("2fa/setup")]
    public async Task<IActionResult> TwoFactorSetup()
    {
        var user = await users.GetUserAsync(User);

        if(user is null)
        {
            return Unauthorized();
        }

        if (user.TwoFactorEnabled)
        {
            return Conflict(new {error = "O 2FA já está ativo." });
        }

        await users.ResetAuthenticatorKeyAsync(user);
        var key = await users.GetAuthenticatorKeyAsync(user);

        return Ok(new
        {
            sharedKey = FormatKey(key!),
            otpauthUri = BuildUri(user.Email!, key!)
        });
    }

    [Authorize]
    [HttpPost("2fa/enable")]
    public async Task<IActionResult> TwoFactorEnable(TwoFactorCodeRequest req)
    {
        var user = await users.GetUserAsync(User);

        if(user is null)
        {
            return Unauthorized();
        }

        if (user.TwoFactorEnabled)
        {
            return Conflict(new { error = "O 2FA já está ativo" });
        }

        var valid = await users.VerifyTwoFactorTokenAsync(user, TokenOptions.DefaultAuthenticatorProvider, CleanCode(req.Code));

        if (!valid)
        {
            return BadRequest(new { error = "Código inválido." });
        }

        await users.SetTwoFactorEnabledAsync(user, true);

        var codes = await users.GenerateNewTwoFactorRecoveryCodesAsync(user, 10);
        await signIn.RefreshSignInAsync(user);
        return Ok(new { recoveryCodes = codes });
    }
    [Authorize]
    [HttpPost("2fa/disable")]
    public async Task<IActionResult> TwoFactorDisable(TwoFactorDisableRequest req)
    {
        var user = await users.GetUserAsync(User);

        if(user is null)
        {
            return Unauthorized();
        }

        var roles = await users.GetRolesAsync(user);

        if(roles.Any(r => SecurityPolicies.TwoFactorRoles.Contains(r)))
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { error = "O 2FA é obrigatório para o seu perfil." });
        }

        if(!await users.CheckPasswordAsync(user, req.Password))
        {
            return BadRequest(new { error = "Senha incorreta." });
        }

        var valid = await users.VerifyTwoFactorTokenAsync(user, TokenOptions.DefaultAuthenticatorProvider, CleanCode(req.Code));
        if (!valid)
        {
            return BadRequest(new { error = "Código inválido." });
        }

        await users.SetTwoFactorEnabledAsync(user, false);
        await users.ResetAuthenticatorKeyAsync(user);
        await signIn.RefreshSignInAsync(user);

        return NoContent();
    }

    //Endpoint temporário para testar a policy: remover após testes.
    [Authorize(Policy = SecurityPolicies.ClinicalAccess)]
    [HttpGet("clinical-check")]
    public IActionResult ClinicalCheck() => Ok(new { ok = true });
}
