using System.Text;

namespace Portal.Application.Infrastructure.Integrations;

/// <summary>
/// Cleans raw JDE customer names and merges near-duplicate rows
/// (case variants, legal-suffix variants, residual account-noise suffixes).
/// Defense-in-depth on top of the SQL-side cleanup in spJDE_GetCustomersList.
/// </summary>
public static class CustomerNameNormalizer
{
    // Cut the name at the first occurrence of any of these account-noise markers.
    private static readonly string[] NoiseMarkers =
    [
        " - Cash Sale",
        "- Cash Sale",
        " - Cash",
        "- Cash",
        "-Cash",
        " - Staff Sale",
        "- Staff Sale"
    ];

    // Trailing closed-account markers (JDE names are truncated at 40 chars, so variants abound).
    private static readonly string[] TrailingClosedMarkers =
    [
        "-Closed",
        "-Closd",
        "-Clsed",
        "-Close",
        "-Cls",
        "- Closed",
        "- Cls"
    ];

    // Legal-entity suffixes ignored when building the dedupe key so that
    // "Black & McDonald Ltd" and "Black & McDonald Limited" merge into one row.
    private static readonly HashSet<string> LegalSuffixTokens = new(StringComparer.OrdinalIgnoreCase)
    {
        "LTD", "LIMITED", "INC", "INCORPORATED", "CORP", "CORPORATION",
        "CO", "COMPANY", "LLC", "ULC", "LP", "LLP"
    };

    public static IReadOnlyList<string> NormalizeAndDeduplicate(IEnumerable<string> rawNames)
    {
        var bestByKey = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var rawName in rawNames)
        {
            var cleanName = Clean(rawName);
            if (cleanName.Length < 3)
            {
                continue;
            }

            var key = BuildDedupeKey(cleanName);
            if (key.Length == 0)
            {
                continue;
            }

            if (!bestByKey.TryGetValue(key, out var currentBest) || IsBetterDisplayName(cleanName, currentBest))
            {
                bestByKey[key] = cleanName;
            }
        }

        return bestByKey.Values
            .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public static string Clean(string? rawName)
    {
        if (string.IsNullOrWhiteSpace(rawName))
        {
            return string.Empty;
        }

        // Drop Unicode replacement characters introduced by charset issues upstream.
        var name = rawName.Replace("\uFFFD", string.Empty).Trim();

        foreach (var marker in NoiseMarkers)
        {
            var markerIndex = name.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
            if (markerIndex > 0)
            {
                name = name[..markerIndex];
            }
        }

        foreach (var marker in TrailingClosedMarkers)
        {
            if (name.EndsWith(marker, StringComparison.OrdinalIgnoreCase))
            {
                name = name[..^marker.Length];
                break;
            }
        }

        name = CollapseWhitespace(name);
        return name.TrimEnd(' ', '-', ',', '.');
    }

    private static string BuildDedupeKey(string cleanName)
    {
        // Uppercase, treat punctuation as spaces, collapse runs, then drop trailing legal suffix tokens.
        var builder = new StringBuilder(cleanName.Length);
        foreach (var character in cleanName)
        {
            builder.Append(char.IsLetterOrDigit(character)
                ? char.ToUpperInvariant(character)
                : ' ');
        }

        var tokens = builder.ToString()
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .ToList();

        while (tokens.Count > 1 && LegalSuffixTokens.Contains(tokens[^1]))
        {
            tokens.RemoveAt(tokens.Count - 1);
        }

        return string.Join(' ', tokens);
    }

    private static bool IsBetterDisplayName(string candidate, string current)
    {
        // Prefer mixed-case names over ALL CAPS, then the more complete (longer) variant.
        var candidateHasLower = candidate.Any(char.IsLower);
        var currentHasLower = current.Any(char.IsLower);
        if (candidateHasLower != currentHasLower)
        {
            return candidateHasLower;
        }

        if (candidate.Length != current.Length)
        {
            return candidate.Length > current.Length;
        }

        return string.CompareOrdinal(candidate, current) < 0;
    }

    private static string CollapseWhitespace(string value)
    {
        return string.Join(' ', value.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
    }
}
