using System.ComponentModel.DataAnnotations;

namespace telemed.Api.Contracts;

public record RegisterRequest(
    [Required, StringLength(150, MinimumLength = 3)] string FullName,
    [Required, EmailAddress, StringLength(256)] string Email,
    [Required, StringLength(128)] string Password,
    Guid ClinicId);

public record LoginRequest(
    [Required, EmailAddress] string Email,
    [Required] string Password);

public record TwoFactorCodeRequest(
    [Required, StringLength(10, MinimumLength = 6)] string Code);

public record TwoFactorDisableRequest(
    [Required] string Password,
    [Required, StringLength(10, MinimumLength = 6)] string Code);

public record RecoveryCodeRequest(
    [Required, StringLength(20, MinimumLength = 8)] string RecoveryCode);