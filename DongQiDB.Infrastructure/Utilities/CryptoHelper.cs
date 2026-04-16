using System.Security.Cryptography;
using System.Text;

namespace DongQiDB.Infrastructure.Utilities;

/// <summary>
/// Encryption and hashing utility
/// </summary>
public static class CryptoHelper
{
    private const int KeySize = 256;
    private const int IvSize = 16;

    public static string Md5Hash(string input)
    {
        var bytes = MD5.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes).ToLower();
    }

    public static string Sha256Hash(string input)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes).ToLower();
    }

    public static string HmacSha256(string data, string key)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(key));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
        return Convert.ToHexString(hash).ToLower();
    }

    public static string Base64Encode(string text)
        => Convert.ToBase64String(Encoding.UTF8.GetBytes(text));

    public static string Base64Decode(string base64)
        => Encoding.UTF8.GetString(Convert.FromBase64String(base64));

    public static string GenerateToken(int length = 32)
    {
        var bytes = new byte[length];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToBase64String(bytes);
    }

    /// <summary>
    /// Encrypts a string using AES-256
    /// </summary>
    public static string AesEncrypt(string plainText, string key, string iv)
    {
        if (string.IsNullOrEmpty(plainText))
            return string.Empty;

        var keyBytes = Encoding.UTF8.GetBytes(key.PadRight(KeySize / 8).Substring(0, KeySize / 8));
        var ivBytes = Encoding.UTF8.GetBytes(iv.PadRight(IvSize).Substring(0, IvSize));

        using var aes = Aes.Create();
        aes.Key = keyBytes;
        aes.IV = ivBytes;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;

        using var encryptor = aes.CreateEncryptor();
        var plainBytes = Encoding.UTF8.GetBytes(plainText);
        var encryptedBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);
        return Convert.ToBase64String(encryptedBytes);
    }

    /// <summary>
    /// Decrypts an AES-256 encrypted string
    /// </summary>
    public static string AesDecrypt(string cipherText, string key, string iv)
    {
        if (string.IsNullOrEmpty(cipherText))
            return string.Empty;

        var keyBytes = Encoding.UTF8.GetBytes(key.PadRight(KeySize / 8).Substring(0, KeySize / 8));
        var ivBytes = Encoding.UTF8.GetBytes(iv.PadRight(IvSize).Substring(0, IvSize));

        using var aes = Aes.Create();
        aes.Key = keyBytes;
        aes.IV = ivBytes;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;

        using var decryptor = aes.CreateDecryptor();
        var cipherBytes = Convert.FromBase64String(cipherText);
        var decryptedBytes = decryptor.TransformFinalBlock(cipherBytes, 0, cipherBytes.Length);
        return Encoding.UTF8.GetString(decryptedBytes);
    }

    /// <summary>
    /// Generates a random AES key
    /// </summary>
    public static string GenerateAesKey()
    {
        var keyBytes = new byte[KeySize / 8];
        RandomNumberGenerator.Fill(keyBytes);
        return Convert.ToBase64String(keyBytes);
    }

    /// <summary>
    /// Generates a random AES IV
    /// </summary>
    public static string GenerateAesIv()
    {
        var ivBytes = new byte[IvSize];
        RandomNumberGenerator.Fill(ivBytes);
        return Convert.ToBase64String(ivBytes);
    }
}
