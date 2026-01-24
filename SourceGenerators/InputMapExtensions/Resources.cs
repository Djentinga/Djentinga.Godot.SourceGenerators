using System.Reflection;

namespace Djentinga.Godot.SourceGenerators.InputMapExtensions;

internal static class Resources
{
    private const string inputMapTemplate = "Djentinga.Godot.SourceGenerators.InputMapExtensions.InputMapTemplate.scriban";
    public static readonly string InputMapTemplate = Assembly.GetExecutingAssembly().GetEmbeddedResource(inputMapTemplate);
}
