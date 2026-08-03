using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace DragChain.API.Sensor.Security;

public static class RbacPasswordHasher
{
    private const string Algorithm = "pbkdf2-sha256";
    private const int Iterations = 210_000;
    private const int MaximumAcceptedIterations = 1_000_000;
    private const int SaltSize = 16;
    private const int HashSize = 32;

    public static string HashPassword(string password)
    {
        ArgumentNullException.ThrowIfNull(password);

        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var hash = Rfc2898DeriveBytes.Pbkdf2(
            password,
            salt,
            Iterations,
            HashAlgorithmName.SHA256,
            HashSize);

        return string.Join(
            '$',
            Algorithm,
            Iterations.ToString(CultureInfo.InvariantCulture),
            Convert.ToBase64String(salt),
            Convert.ToBase64String(hash));
    }

    public static bool VerifyPassword(string? storedPassword, string? providedPassword, out bool needsUpgrade)
    {
        needsUpgrade = false;
        if (storedPassword == null || providedPassword == null)
            return false;

        if (!storedPassword.StartsWith($"{Algorithm}$", StringComparison.Ordinal))
        {
            var storedDigest = SHA256.HashData(Encoding.UTF8.GetBytes(storedPassword));
            var providedDigest = SHA256.HashData(Encoding.UTF8.GetBytes(providedPassword));
            var matchesLegacyPassword = CryptographicOperations.FixedTimeEquals(storedDigest, providedDigest);
            needsUpgrade = matchesLegacyPassword;
            return matchesLegacyPassword;
        }

        var parts = storedPassword.Split('$');
        if (parts.Length != 4
            || !int.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out var iterations)
            || iterations <= 0
            || iterations > MaximumAcceptedIterations)
        {
            return false;
        }

        try
        {
            var salt = Convert.FromBase64String(parts[2]);
            var expectedHash = Convert.FromBase64String(parts[3]);
            if (salt.Length is < SaltSize or > 64 || expectedHash.Length != HashSize)
                return false;

            var actualHash = Rfc2898DeriveBytes.Pbkdf2(
                providedPassword,
                salt,
                iterations,
                HashAlgorithmName.SHA256,
                expectedHash.Length);
            var matches = CryptographicOperations.FixedTimeEquals(expectedHash, actualHash);
            needsUpgrade = matches && iterations < Iterations;
            return matches;
        }
        catch (FormatException)
        {
            return false;
        }
        catch (CryptographicException)
        {
            return false;
        }
    }

    public static bool IsHashedPassword(string? storedPassword) =>
        storedPassword?.StartsWith($"{Algorithm}$", StringComparison.Ordinal) == true;

    public static string CreateSecurityStamp() =>
        Convert.ToHexString(RandomNumberGenerator.GetBytes(32));

    public static string? ValidateStrongPassword(string? password, bool required)
    {
        if (string.IsNullOrWhiteSpace(password))
            return required ? "密码不能为空" : null;
        if (password.Length < 12)
            return "密码至少需要 12 个字符";
        if (password.Length > 256)
            return "密码不能超过 256 个字符";

        var hasLetter = password.Any(char.IsLetter);
        var hasDigit = password.Any(char.IsDigit);
        var hasSymbol = password.Any(character => !char.IsLetterOrDigit(character));
        return hasLetter && hasDigit && hasSymbol
            ? null
            : "密码必须同时包含字母、数字和符号";
    }
}
