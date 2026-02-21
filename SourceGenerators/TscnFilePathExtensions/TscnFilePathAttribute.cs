using System.Runtime.CompilerServices;

namespace Godot;

[AttributeUsage(AttributeTargets.Class)]
public sealed class TscnFilePathAttribute(
    string tscnRelativeToClassPath = null,
    [CallerFilePath] string classPath = null) : Attribute
{
    public string SceneFile { get; } = classPath is null
        ? string.Empty
        : (tscnRelativeToClassPath is null
            ? Path.ChangeExtension(classPath, "tscn")
            : Path.GetFullPath(Path.Combine(Path.GetDirectoryName(classPath) ?? string.Empty, tscnRelativeToClassPath)));
}
