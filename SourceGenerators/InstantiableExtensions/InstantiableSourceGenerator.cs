using Godot;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Scriban;

namespace Djentinga.Godot.SourceGenerators.InstantiableExtensions;

[Generator]
internal class InstantiableSourceGenerator : SourceGeneratorForDeclaredTypeWithAttribute<InstantiableAttribute>
{
    private static Template InstantiableTemplate => field ??= Template.Parse(Resources.InstantiableTemplate);

    protected override (string GeneratedCode, DiagnosticDetail Error) GenerateCode(Compilation compilation, SyntaxNode node, INamedTypeSymbol symbol, AttributeData attribute, AnalyzerConfigOptions options)
    {
        var tscn = GD.TSCN(node, options);
        if (tscn is null)
            return (null, Diagnostics.FileNotFound(node.SyntaxTree.FilePath, $"Could not find .tscn file for {symbol.Name}"));

        var hasTscnFilePathAttr = symbol.GetAttributes()
            .Any(a => a.AttributeClass?.Name is nameof(TscnFilePathAttribute) or nameof(SceneTreeAttribute));

        var model = new InstantiableDataModel(symbol, ReconstructAttribute(), tscn, hasTscnFilePathAttr);
        Log.Debug($"--- MODEL ---\n{model}\n");

        var output = InstantiableTemplate.Render(model, Shared.Utils);
        Log.Debug($"--- OUTPUT ---\n{output}<END>\n");

        return (output, null);

        InstantiableAttribute ReconstructAttribute() => new(
            (string)attribute.ConstructorArguments[0].Value,
            (string)attribute.ConstructorArguments[1].Value);
    }
}
