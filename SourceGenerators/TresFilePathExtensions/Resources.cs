using System.Reflection;

namespace Djentinga.Godot.SourceGenerators.TresFilePathExtensions;

internal static class Resources
{
    private const string tresFilePathTemplate = "Djentinga.Godot.SourceGenerators.TresFilePathExtensions.TresFilePathTemplate.scriban";
    public static readonly string TresFilePathTemplate = Assembly.GetExecutingAssembly().GetEmbeddedResource(tresFilePathTemplate);
}
