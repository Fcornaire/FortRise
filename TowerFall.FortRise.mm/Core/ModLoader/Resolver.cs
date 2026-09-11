using System;
using System.IO;
using System.Reflection;
using Microsoft.Extensions.Logging;

namespace FortRise;

internal static class Resolver
{
    public static Assembly LoadModAssembly(IModResource modResource, string asmDLL, Stream stream) 
    {
        return LoadAssembly(modResource, Path.GetFileNameWithoutExtension(asmDLL), stream);
    }

    public static Assembly LoadAssembly(IModResource modResource, ReadOnlySpan<char> name, Stream stream)
    {
        Span<char> asmName = stackalloc char[name.Length];
        name.Replace(asmName, ' ', '_');

        var dirPath = Path.Combine(RiseCore.GameRootPath, "Mods", "_RelinkerCache");
        if (!Directory.Exists(dirPath))
        {
            Directory.CreateDirectory(dirPath);
        }

        var cachedPath = Path.Combine(dirPath, $"{asmName}.{modResource.Metadata.Name}.dll");
        var cachedChecksumPath = string.Concat(cachedPath.AsSpan(0, cachedPath.Length - 4), ".sum");

        var checksums = new string[2];
        checksums[0] = RiseCore.GameChecksum;
        checksums[1] = RiseCore.GetChecksum(ref stream).ToHexadecimalString();
        

        if (File.Exists(cachedPath) && File.Exists(cachedChecksumPath) && 
            ChecksumsEqual(checksums, File.ReadAllLines(cachedChecksumPath))) 
        {
            RiseCore.logger.LogInformation(
                "[Resolver] Loading cached assembly for {meta} - {asm}", 
                modResource.Metadata, 
                new string(asmName)
            );

            try 
            {
                return modResource.Metadata.AssemblyLoadContext.LoadRelinkedAssembly(cachedPath);
            }
            catch (Exception e) 
            {
                RiseCore.logger.LogError(
                    "[Resolver] Failed Loading {meta} - {asm}", 
                    modResource.Metadata, 
                    new string(asmName));

                RiseCore.logger.LogError("[Resolver] Exception: {exception}", e);
                return null;
            }
        }

        var symbolPath = $"{name}.pdb"; 
        using var symbolStream = OpenSymbol(modResource, symbolPath);
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

            return modResource.Metadata.AssemblyLoadContext.LoadRelinkedAssembly(cachedPath);
        }
        catch (Exception e) 
        {
            RiseCore.logger.LogError(
                "[Resolver] Failed Loading {meta} - {asm}", 
                modResource.Metadata, 
                new string(asmName));

            RiseCore.logger.LogError("[Resolver] Exception: {exception}", e);
            return null;
        }
    }

    private static Stream OpenSymbol(IModResource modResource, string pdbFile) 
    {
        if (modResource.OwnedResources.TryGetValue(pdbFile, out var pdb))
        {
            return pdb.Stream;
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
