using System.Security.Cryptography;
using System.Text;

namespace DragChain.API.Sensor.Security;

public static class AuthSigningKeyProvider
{
    private const int MinimumKeyBytes = 32;

    public static byte[] Resolve(string? configuredKey, string? environmentName)
    {
        if (!string.IsNullOrWhiteSpace(configuredKey))
        {
            var keyBytes = Encoding.UTF8.GetBytes(configuredKey);
            if (keyBytes.Length < MinimumKeyBytes)
            {
                throw new InvalidOperationException(
                    $"DRAGCHAIN_AUTH_SIGNING_KEY 必须至少包含 {MinimumKeyBytes} 个 UTF-8 字节。");
            }

            return keyBytes;
        }

        if (string.Equals(environmentName, "Development", StringComparison.OrdinalIgnoreCase)
            || string.Equals(environmentName, "Testing", StringComparison.OrdinalIgnoreCase))
        {
            return RandomNumberGenerator.GetBytes(MinimumKeyBytes);
        }

        throw new InvalidOperationException(
            "当前环境必须显式配置 DRAGCHAIN_AUTH_SIGNING_KEY，且长度至少为 32 个 UTF-8 字节。");
    }
}
