using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Identity;

namespace Bes.Features.Auth;

public sealed class BesPasswordService
{
    private readonly PasswordHasher<BesPasswordUser> _hasher = new();
    private static readonly BesPasswordUser User = new();

    public string HashPassword(string password) =>
        _hasher.HashPassword(User, password);

    public bool Verify(string passwordHash, string password)
    {
        var result = _hasher.VerifyHashedPassword(User, passwordHash, password);
        return result is PasswordVerificationResult.Success
            or PasswordVerificationResult.SuccessRehashNeeded;
    }

    public static string BuildBindingPayloadJson(string passwordHash) =>
        JsonSerializer.Serialize(new BindingPayload { PasswordHash = passwordHash });

    public static byte[] BuildBindingPayloadBytes(string passwordHash) =>
        Encoding.UTF8.GetBytes(BuildBindingPayloadJson(passwordHash));

    public static string? TryReadPasswordHash(ReadOnlySpan<byte> bindingPayload)
    {
        if (bindingPayload.IsEmpty)
        {
            return null;
        }

        try
        {
            var payload = JsonSerializer.Deserialize<BindingPayload>(bindingPayload);
            return string.IsNullOrWhiteSpace(payload?.PasswordHash) ? null : payload.PasswordHash;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    public static string GenerateRandomPassword(int length = 20)
    {
        const string alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz23456789!@#$%";
        var bytes = RandomNumberGenerator.GetBytes(length);
        var chars = new char[length];
        for (var i = 0; i < length; i++)
        {
            chars[i] = alphabet[bytes[i] % alphabet.Length];
        }

        return new string(chars);
    }

    private sealed class BindingPayload
    {
        public string PasswordHash { get; set; } = string.Empty;
    }
}
