namespace Djentinga.Godot.SourceGenerators;

internal static class Diagnostics
{
    public static DiagnosticDetail FileNotFound(string path, string msg = null) => new() { Title = "File not found", Message = msg ?? $"Could not find file: {path}" };
    public static DiagnosticDetail FolderNotFound(string path, string msg = null) => new() { Title = "Folder not found", Message = msg ?? $"Could not find folder: {path}" };

    public static DiagnosticDetail InvalidRootType(string cfgSceneFile)
    {
        return new DiagnosticDetail
        {
            Title = "Invalid root type",
            Message = $"The root node of the scene {cfgSceneFile} must be a Node2D or a Node3D.",
        };
    }
}
