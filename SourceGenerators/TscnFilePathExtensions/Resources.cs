using System.Reflection;

namespace Djentinga.Godot.SourceGenerators.TscnFilePathExtensions;

internal static class Resources
{
    private const string tscnFilePathTemplate = "Djentinga.Godot.SourceGenerators.TscnFilePathExtensions.TscnFilePathTemplate.scriban";
    public static readonly string TscnFilePathTemplate = Assembly.GetExecutingAssembly().GetEmbeddedResource(tscnFilePathTemplate);
}
