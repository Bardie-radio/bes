using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
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

    public static byte[] BuildBindingPayloadBytes(
        string passwordHash,
        IReadOnlyList<string> roles,
        bool mustRotate)
    {
        var payload = new BindingPayload
        {
            PasswordHash = passwordHash,
            Roles = roles.ToList(),
            MustRotate = mustRotate,
        };
        return Encoding.UTF8.GetBytes(JsonSerializer.Serialize(payload));
    }

    public static BindingState? TryReadBinding(ReadOnlySpan<byte> bindingPayload)
    {
        if (bindingPayload.IsEmpty)
        {
            return null;
        }

        try
        {
            var payload = JsonSerializer.Deserialize<BindingPayload>(bindingPayload);
            if (payload is null || string.IsNullOrWhiteSpace(payload.PasswordHash))
            {
                return null;
            }

            var roles = payload.Roles?
                .Where(r => !string.IsNullOrWhiteSpace(r))
                .Select(r => r.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray()
                ?? [];

            return new BindingState(payload.PasswordHash, roles, payload.MustRotate);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    /// <summary>Legacy helper — password hash only (roles/rotate read separately).</summary>
    public static string? TryReadPasswordHash(ReadOnlySpan<byte> bindingPayload) =>
        TryReadBinding(bindingPayload)?.PasswordHash;

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

    public sealed record BindingState(string PasswordHash, IReadOnlyList<string> Roles, bool MustRotate);

    private sealed class BindingPayload
    {
        public string PasswordHash { get; set; } = string.Empty;

        [JsonPropertyName("roles")]
        public List<string>? Roles { get; set; }

        [JsonPropertyName("mustRotate")]
        public bool MustRotate { get; set; }
    }
}
