namespace telemed.Infrastructure.Security;

public class EncryptionOptions
{
    public const string Section = "Encryption";
    public string Key { get; set; } = "";
    public string HmacKey { get; set; } = "";
}