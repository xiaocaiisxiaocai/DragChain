using System.Text;

namespace DragChain.API.Sensor.Controllers;

public static class InternalCode
{
    public static string FromName(string? name, string prefix)
    {
        var normalized = new StringBuilder();
        foreach (var ch in name ?? string.Empty)
        {
            if (char.IsLetterOrDigit(ch))
            {
                normalized.Append(char.ToLowerInvariant(ch));
            }
            else if (normalized.Length > 0 && normalized[^1] != '-')
            {
                normalized.Append('-');
            }
        }

        var baseCode = normalized.ToString().Trim('-');
        if (string.IsNullOrWhiteSpace(baseCode))
            baseCode = prefix;

        return $"{baseCode}-{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}";
    }
}
