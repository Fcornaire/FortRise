using System;

namespace FortRise;

public static class FileSystemUtils 
{
    public static string GetSimplifiedPath(string path, int baseLength)
    {
        var span = path.AsSpan();
        int offset = baseLength;

        if (offset < span.Length && (span[offset] == '/' || span[offset] == '\\'))
        {
            offset += 1;
        }

        var relativeSpan = span[Math.Min(offset, span.Length)..];

        if (relativeSpan.IndexOf('\\') < 0)
        {
            return relativeSpan.ToString();
        }

        return string.Create(relativeSpan.Length, path, (dest, src) => 
        {
            src.AsSpan().CopyTo(dest);
            dest.Replace('\\', '/');
        });
    }

    public static string NormalizedPath(string path)
    {
        var span = path.AsSpan();
        if (span.IndexOf('\\') < 0)
        {
            return path;
        }

        return string.Create(span.Length, path, (dest, src) => 
        {
            src.AsSpan().CopyTo(dest);
            dest.Replace('\\', '/');
        });
    }
}

