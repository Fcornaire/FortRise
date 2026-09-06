#nullable enable
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace FortRise;

public delegate void AwaitApiCallback<T>(T api);

public interface IModInterop
{
    IReadOnlyList<IModResource> LoadedMods { get; }
    IReadOnlyList<Mod> LoadedFortModules {get; }

    IModResource? GetMod(string tag);
    IReadOnlyList<IModResource> GetModDependents();
    IModRegistry? GetModRegistry(string modName);
    IModRegistry? GetModRegistry(ModuleMetadata metadata);
    ILogger? GetModLogger(string modName);
    ILogger? GetModLogger(ModuleMetadata metadata);
    bool IsModDepends(ModuleMetadata metadata);

    T? GetApi<T>(string name, Option<SemanticVersion> minimumVersion = default) where T : class;
    void AwaitApi<T>(string name, AwaitApiCallback<T> callback, Option<SemanticVersion> minimumVersion = default) where T : class;

    bool IsModExists(string name);
}
