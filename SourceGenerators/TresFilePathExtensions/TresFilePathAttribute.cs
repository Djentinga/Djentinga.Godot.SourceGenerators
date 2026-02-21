using System.Runtime.CompilerServices;

namespace Godot;

[AttributeUsage(AttributeTargets.Class)]
public sealed class TresFilePathAttribute(
    string tresRelativeToClassPath = null,
    [CallerFilePath] string classPath = null) : Attribute
{
    public string ResourceFile { get; } = classPath is null
        ? string.Empty
        : (tresRelativeToClassPath is null
            ? Path.ChangeExtension(classPath, "tres")
            : Path.GetFullPath(Path.Combine(Path.GetDirectoryName(classPath) ?? string.Empty, tresRelativeToClassPath)));
}
