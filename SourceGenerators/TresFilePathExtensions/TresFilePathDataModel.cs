using Microsoft.CodeAnalysis;

namespace Djentinga.Godot.SourceGenerators.TresFilePathExtensions;

internal class TresFilePathDataModel(INamedTypeSymbol symbol, string tresResource) : ClassDataModel(symbol)
{
    public string TresResource { get; } = tresResource;

    protected override string Str()
        => $"Tres: {TresResource}";
}
