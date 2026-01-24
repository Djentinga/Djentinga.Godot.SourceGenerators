using Microsoft.CodeAnalysis;

namespace Djentinga.Godot.SourceGenerators;

internal abstract class ClassDataModel(INamedTypeSymbol symbol) : BaseDataModel(symbol, symbol)
{
    public bool IsStatic { get; } = symbol.IsStatic;
}
