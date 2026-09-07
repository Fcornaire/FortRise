using System;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using Microsoft.Extensions.Logging;

namespace FortRise;

internal static class Resolver
{
    public static Assembly LoadModAssembly(ModuleMetadata meta, string asmDLL, Stream stream) 
    {
        return LoadAssembly(meta, Path.GetFileNameWithoutExtension(asmDLL), stream);
    }

    public static Assembly LoadAssembly(ModuleMetadata meta, ReadOnlySpan<char> name, Stream stream)
    {
        Span<char> asmName = stackalloc char[name.Length];
        name.Replace(asmName, ' ', '_');

        var dirPath = Path.Combine(RiseCore.GameRootPath, "Mods", "_RelinkerCache");
        if (!Directory.Exists(dirPath))
        {
            Directory.CreateDirectory(dirPath);
        }

        var cachedPath = Path.Combine(dirPath, $"{asmName}.{meta.Name}.dll");
        var cachedChecksumPath = string.Concat(cachedPath.AsSpan(0, cachedPath.Length - 4), ".sum");

        var checksums = new string[2];
        checksums[0] = RiseCore.GameChecksum;
        checksums[1] = RiseCore.GetChecksum(ref stream).ToHexadecimalString();
        

        if (File.Exists(cachedPath) && File.Exists(cachedChecksumPath) && 
            ChecksumsEqual(checksums, File.ReadAllLines(cachedChecksumPath))) 
        {
            RiseCore.logger.LogInformation(
                "[Resolver] Loading cached assembly for {meta} - {asm}", 
                meta, 
                new string(asmName)
            );

            try 
            {
                return meta.AssemblyLoadContext.LoadRelinkedAssembly(cachedPath);
            }
            catch (Exception e) 
            {
                RiseCore.logger.LogError(
                    "[Resolver] Failed Loading {meta} - {asm}", 
                    meta, 
                    new string(asmName));

                RiseCore.logger.LogError("[Resolver] Exception: {exception}", e);
                return null;
            }
        }

        var symbolPath = $"{name}.pdb"; 
        var symbolStream = OpenSymbol(meta, symbolPath);
        var cachedSymbolPath = Path.ChangeExtension(cachedPath, "pdb");

        if (symbolStream is not null)
        {
            using var fs = File.Create(cachedSymbolPath);
            symbolStream.CopyTo(fs);
        }


        using (var fs = File.Create(cachedPath))
        {
            stream.CopyTo(fs);
        }

        try 
        {
            if (File.Exists(cachedChecksumPath))
            {
                File.Delete(cachedChecksumPath);
            }
            
            File.WriteAllLines(cachedChecksumPath, checksums);

            return meta.AssemblyLoadContext.LoadRelinkedAssembly(cachedPath);
        }
        catch (Exception e) 
        {
            RiseCore.logger.LogError(
                "[Resolver] Failed Loading {meta} - {asm}", 
                meta, 
                new string(asmName));

            RiseCore.logger.LogError("[Resolver] Exception: {exception}", e);
            return null;
        }
    }

    private static Stream OpenSymbol(ModuleMetadata metadata, string pdbFile) 
    {
        if (!string.IsNullOrEmpty(metadata.PathZip)) 
        {
            using var zipFile = ZipFile.OpenRead(metadata.PathZip);
            foreach (var entry in zipFile.Entries) 
            {
                if (!pdbFile.Contains(entry.FullName))
                {
                    continue;
                }

                return entry.ExtractStream();
            }
        }
        if (!string.IsNullOrEmpty(metadata.PathDirectory)) 
        {
            var pdbPath = Path.Combine(metadata.PathDirectory, pdbFile);
            if (File.Exists(pdbPath)) 
            {
                return File.OpenRead(pdbPath);
            }
        }
        return null;
    }

    public static bool ChecksumsEqual(string[] a, string[] b) {
        if (a.Length != b.Length)
        {
            return false;
        }

        for (int i = 0; i < a.Length; i++) 
        {
            var left = a[i].AsSpan().Trim();
            var right = b[i].AsSpan().Trim();
            if (!left.SequenceEqual(right))
            {
                return false;
            }
        }
        return true;
    }
}
