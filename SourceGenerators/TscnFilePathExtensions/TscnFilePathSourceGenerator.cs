using Godot;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Scriban;

namespace Djentinga.Godot.SourceGenerators.TscnFilePathExtensions;

[Generator]
internal class TscnFilePathSourceGenerator : SourceGeneratorForDeclaredTypeWithAttribute<TscnFilePathAttribute>
{
    private static Template TscnFilePathTemplate => field ??= Template.Parse(Resources.TscnFilePathTemplate);

    protected override (string GeneratedCode, DiagnosticDetail Error) GenerateCode(Compilation compilation, SyntaxNode node, INamedTypeSymbol symbol, AttributeData attribute, AnalyzerConfigOptions options)
    {
        var cfg = ReconstructAttribute();

        if (!File.Exists(cfg.SceneFile))
            return (null, Diagnostics.FileNotFound(cfg.SceneFile));

        var gdRoot = GD.ROOT(node, options);
        var tscnResource = GD.RES(cfg.SceneFile, gdRoot);

        var model = new TscnFilePathDataModel(symbol, tscnResource);
        Log.Debug($"--- MODEL ---\n{model}\n");

        var output = TscnFilePathTemplate.Render(model, member => member.Name);
        Log.Debug($"--- OUTPUT ---\n{output}<END>\n");

        return (output, null);

        TscnFilePathAttribute ReconstructAttribute()
        {
            return new(
                (string)attribute.ConstructorArguments[0].Value,
                (string)attribute.ConstructorArguments[1].Value);
        }
    }
}
