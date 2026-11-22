using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using telemed.Application.Abstractions;

namespace telemed.Infrastructure.Security;

public sealed class AesGcmFieldEncryptor : IFieldEncryptor
{
    private const string Prefix = "enc:v1:";
    private const int NonceSize = 12;
    private const int TagSize = 16;

    private readonly byte[] _key;
    private readonly byte[] _hmacKey;

    public AesGcmFieldEncryptor(IOptions<EncryptionOptions> options)
    {
        _key = LoadKey(options.Value.Key, "Encryption:Key");
        _hmacKey = LoadKey(options.Value.HmacKey, "Encryption:HmacKey");
    }

    private static byte[] LoadKey(string base64, string name)
    {
        if (string.IsNullOrWhiteSpace(base64))
        {
            throw new InvalidOperationException($"{name} não está configurada.");
        }

        byte[] key;
        try
        {
            key = Convert.FromBase64String(base64);
        }
        catch (FormatException)
        {
            throw new InvalidOperationException($"{name} deve estar em Base64.");
        }

        if(key.Length != 32)
        {
            throw new InvalidOperationException($"{name} deve ter 32 bytes.");
        }

        return key;
    }

    public string Encrypt(string plaintext)
    {
        var nonce = RandomNumberGenerator.GetBytes(NonceSize);
        var data = Encoding.UTF8.GetBytes(plaintext);
        var cipher = new byte[data.Length];
        var tag = new byte[TagSize];

        using var aes = new AesGcm(_key, TagSize);
        aes.Encrypt(nonce, data, cipher, tag);

        //payload = nonce | tag | cipher
        var payload = new byte[NonceSize + TagSize + cipher.Length];
        Buffer.BlockCopy(nonce, 0, payload, 0, NonceSize);
        Buffer.BlockCopy(tag, 0, payload, NonceSize, TagSize);
        Buffer.BlockCopy(cipher, 0, payload, NonceSize + TagSize, cipher.Length);

        return Prefix + Convert.ToBase64String(payload);
    }
    public string Decrypt(string value)
    {
        if(!value.StartsWith(Prefix, StringComparison.Ordinal))
        {
            return value;
        }

        var payload = Convert.FromBase64String(value[Prefix.Length..]);

        if(payload.Length < NonceSize + TagSize)
        {
            throw new CryptographicException("Valor criptografado inválido.");
        }

        var nonce = payload.AsSpan(0, NonceSize);
        var tag = payload.AsSpan(NonceSize, TagSize);
        var cipher = payload.AsSpan(NonceSize + TagSize);
        var plain = new byte[cipher.Length];

        using var aes = new AesGcm(_key, TagSize);
        aes.Decrypt(nonce, cipher, tag, plain); // lança se o dado foi adulterado.

        return Encoding.UTF8.GetString(plain);
    }
    public string BlindIndex(string normalizedValue) =>
        Convert.ToHexString(HMACSHA256.HashData(_hmacKey, Encoding.UTF8.GetBytes(normalizedValue)));
}