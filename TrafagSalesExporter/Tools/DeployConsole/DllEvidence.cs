using System.Security.Cryptography;
using System.Text;

namespace DeployConsole;

public sealed record EvidenceHit(string Needle, bool Found, string Encoding);

/// <summary>
/// "Wirknachweis" on the shipped assembly: does the DLL on the server actually contain
/// the types and texts this deploy was about, and is the text it replaced really gone?
///
/// Two encodings matter and mixing them up produces false negatives: metadata names
/// (types, members) are UTF-8, but string literals live UTF-16 in the #US heap. A
/// UTF-8-only search finds the member name and misses the literal - which is exactly
/// how a stale translation once looked like it had been removed when it had not.
/// </summary>
public static class DllEvidence
{
    public static string Sha256(string path)
    {
        using var stream = File.OpenRead(path);
        return Convert.ToHexString(SHA256.HashData(stream));
    }

    public static List<EvidenceHit> Probe(string dllPath, IEnumerable<string> needles)
    {
        var bytes = File.ReadAllBytes(dllPath);
        var hits = new List<EvidenceHit>();
        foreach (var needle in needles)
        {
            if (string.IsNullOrWhiteSpace(needle))
            {
                continue;
            }
            var text = needle.Trim();
            var utf8 = Contains(bytes, Encoding.UTF8.GetBytes(text));
            var utf16 = Contains(bytes, Encoding.Unicode.GetBytes(text));
            var how = (utf8, utf16) switch
            {
                (true, true) => "UTF-8 + UTF-16",
                (true, false) => "UTF-8 (Metadatenname)",
                (false, true) => "UTF-16 (#US-Literal)",
                _ => "-",
            };
            hits.Add(new EvidenceHit(text, utf8 || utf16, how));
        }
        return hits;
    }

    private static bool Contains(byte[] haystack, byte[] needle)
    {
        if (needle.Length == 0 || haystack.Length < needle.Length)
        {
            return false;
        }
        var first = needle[0];
        var last = haystack.Length - needle.Length;
        for (var i = 0; i <= last; i++)
        {
            if (haystack[i] != first)
            {
                continue;
            }
            var match = true;
            for (var j = 1; j < needle.Length; j++)
            {
                if (haystack[i + j] != needle[j])
                {
                    match = false;
                    break;
                }
            }
            if (match)
            {
                return true;
            }
        }
        return false;
    }
}
