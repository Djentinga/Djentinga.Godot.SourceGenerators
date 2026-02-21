using Godot;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Scriban;

namespace Djentinga.Godot.SourceGenerators.TresFilePathExtensions;

[Generator]
internal class TresFilePathSourceGenerator : SourceGeneratorForDeclaredTypeWithAttribute<TresFilePathAttribute>
{
    private static Template TresFilePathTemplate => field ??= Template.Parse(Resources.TresFilePathTemplate);

    protected override (string GeneratedCode, DiagnosticDetail Error) GenerateCode(Compilation compilation, SyntaxNode node, INamedTypeSymbol symbol, AttributeData attribute, AnalyzerConfigOptions options)
    {
        var cfg = ReconstructAttribute();

        if (!File.Exists(cfg.ResourceFile))
            return (null, Diagnostics.FileNotFound(cfg.ResourceFile));

        var gdRoot = GD.ROOT(node, options);
        var tresResource = GD.RES(cfg.ResourceFile, gdRoot);

        var model = new TresFilePathDataModel(symbol, tresResource);
        Log.Debug($"--- MODEL ---\n{model}\n");

        var output = TresFilePathTemplate.Render(model, member => member.Name);
        Log.Debug($"--- OUTPUT ---\n{output}<END>\n");

        return (output, null);

        TresFilePathAttribute ReconstructAttribute()
        {
            return new(
                (string)attribute.ConstructorArguments[0].Value,
                (string)attribute.ConstructorArguments[1].Value);
        }
    }
}
