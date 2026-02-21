using Microsoft.CodeAnalysis;

namespace Djentinga.Godot.SourceGenerators.TscnFilePathExtensions;

internal class TscnFilePathDataModel(INamedTypeSymbol symbol, string tscnResource) : ClassDataModel(symbol)
{
    public string TscnResource { get; } = tscnResource;

    protected override string Str()
        => $"Tscn: {TscnResource}";
}
