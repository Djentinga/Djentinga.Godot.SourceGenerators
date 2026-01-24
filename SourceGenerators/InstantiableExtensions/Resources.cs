using System.Reflection;

namespace Djentinga.Godot.SourceGenerators.InstantiableExtensions;

internal static class Resources
{
    private const string instantiableTemplate = "Djentinga.Godot.SourceGenerators.InstantiableExtensions.InstantiableTemplate.scriban";
    public static readonly string InstantiableTemplate = Assembly.GetExecutingAssembly().GetEmbeddedResource(instantiableTemplate);
}
