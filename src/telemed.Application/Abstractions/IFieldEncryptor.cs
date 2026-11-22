namespace telemed.Application.Abstractions;

public interface IFieldEncryptor
{
    string Encrypt(string plaintext);
    string Decrypt(string value);

    ///<summary>Hash determinístico (HMAC) para buscas por igualdade em campos criptografados. </summary>
    string BlindIndex(string normalizedValue);
}