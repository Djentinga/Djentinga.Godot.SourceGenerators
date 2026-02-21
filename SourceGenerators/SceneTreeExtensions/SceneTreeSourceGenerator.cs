using Godot;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Scriban;

namespace Djentinga.Godot.SourceGenerators.SceneTreeExtensions;

[Generator]
internal class SceneTreeSourceGenerator : SourceGeneratorForDeclaredTypeWithAttribute<SceneTreeAttribute>
{
    private static Template SceneTreeTemplate => field ??= Template.Parse(Resources.SceneTreeTemplate);

    protected override IEnumerable<(string Name, string Source)> StaticSources
    {
        get
        {
            yield return (nameof(Resources.ISceneTree), Resources.ISceneTree);
            yield return (nameof(Resources.IInstantiable), Resources.IInstantiable);
        }
    }

    private static readonly object ScrapeLock = new();

    protected override (string GeneratedCode, DiagnosticDetail Error) GenerateCode(Compilation compilation, SyntaxNode node, INamedTypeSymbol symbol, AttributeData attribute, AnalyzerConfigOptions options)
    {
        lock (ScrapeLock)
        {
            SceneTreeScraper.ClearCache();

            var cfg = ReconstructAttribute();

            if (!File.Exists(cfg.SceneFile))
                return (null, Diagnostics.FileNotFound(cfg.SceneFile));

            var model = new SceneTreeDataModel(compilation, symbol, cfg.Root, cfg.SceneFile, cfg.DefaultUniqueNodeScope, cfg.TraverseInstancedScenes, GD.ROOT(node, options));
            Log.Debug($"--- MODEL ---\n{model}\n");

            if (string.IsNullOrEmpty(model.SceneTree?.Root?.Type))
                return (null, Diagnostics.InvalidRootType(cfg.SceneFile));

            var output = SceneTreeTemplate.Render(model, member => member.Name);
            Log.Debug($"--- OUTPUT ---\n{output}<END>\n");

            return (output, null);
        }

        SceneTreeAttribute ReconstructAttribute()
        {
            return new(
                (string)attribute.ConstructorArguments[0].Value,
                (bool)attribute.ConstructorArguments[1].Value,
                (string)attribute.ConstructorArguments[2].Value,
                (Scope)attribute.ConstructorArguments[3].Value,
                (string)attribute.ConstructorArguments[4].Value);
        }
    }
}
