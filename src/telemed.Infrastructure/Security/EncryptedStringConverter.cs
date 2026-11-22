using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using telemed.Application.Abstractions;

namespace telemed.Infrastructure.Security;

public class EncryptedStringConverter : ValueConverter<string, string>
{
    public EncryptedStringConverter(IFieldEncryptor encryptor)
        : base(v => encryptor.Encrypt(v), v => encryptor.Decrypt(v))
    {
    }
}