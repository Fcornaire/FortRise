using System;
using System.IO;

namespace FortRise;

public class FolderModResource : ModResource
{
    public string FolderDirectory;
    public FolderModResource(ModuleMetadata metadata, IModContent content) : base(metadata, content)
    {
        FolderDirectory = FileSystemUtils.NormalizedPath(metadata.PathDirectory);
    }

    public override void Lookup(string prefix)
    {
        Lookup(prefix, FolderDirectory, FolderDirectory, null);
    }

    public void Lookup(string prefix, string path, string modDirectory, FileResourceInfo folderResource)
    {
        bool isRoot = folderResource is null;
        int modLength = modDirectory.Length;

        if (isRoot)
        {
            folderResource = new FileResourceInfo(this, string.Empty, modDirectory + "/");
        }

        var files = Directory.GetFiles(path);
        Array.Sort(files);

        for (int i = 0; i < files.Length; i++)
        {
            var rawFile = files[i];
            var filePath = FileSystemUtils.NormalizedPath(rawFile);
            var simplifiedPath = FileSystemUtils.GetSimplifiedPath(rawFile, modLength);

            var fileResource = new FileResourceInfo(this, simplifiedPath, filePath);
            Add(simplifiedPath, fileResource);
            folderResource.Childrens.Add(fileResource);
        }
        var folders = Directory.GetDirectories(path);
        Array.Sort(folders);

        foreach (var folder in folders)
        {
            var fixedFolder = FileSystemUtils.NormalizedPath(folder);
            var simpliPath = FileSystemUtils.GetSimplifiedPath(folder, modLength);

            string resourcePath = string.IsNullOrEmpty(prefix) ? fixedFolder : prefix + fixedFolder;

            var newFolderResource = new FileResourceInfo(this, simpliPath, resourcePath);
            Lookup(prefix, folder, modDirectory, newFolderResource);
            Add(simpliPath, newFolderResource);
            folderResource.Childrens.Add(newFolderResource);
        }

        if (isRoot)
        {
            Add(string.Empty, folderResource);
        }
    }
}
