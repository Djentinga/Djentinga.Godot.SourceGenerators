using Godot;
using Microsoft.CodeAnalysis;

namespace Djentinga.Godot.SourceGenerators.InstantiableExtensions;

internal class InstantiableDataModel(INamedTypeSymbol symbol, InstantiableAttribute data, string tscn) : ClassDataModel(symbol)
{
    public record InitArgs(string Params, string Args)
    {
        public override string ToString() => $"{Params} -> {Args}";
    }

    public string Tscn { get; } = tscn;
    public string Initialise { get; } = data.Initialise;
    public string Instantiate { get; } = data.Instantiate;
    public InitArgs[] InitList { get; } = [..
        symbol
            .GetMembers(data.Initialise)
            .OfType<IMethodSymbol>()
            .Select(x => new InitArgs(
                Params: string.Join(", ", x.Parameters.Select(p => p.ToParameterString())),
                Args: string.Join(", ", x.Parameters.Select(p => p.ToArgumentString()))))];

    protected override string Str()
    {
        return string.Join("\n", Parts());

        IEnumerable<string> Parts()
        {
            yield return $" - Tscn: {Tscn}";
            yield return $" - Initialise: {Initialise}";
            yield return $" - Instantiate: {Instantiate}";
            yield return $" - InitList:";
            foreach (var initFunc in InitList)
                yield return $"   - {initFunc}";
        }
    }
}
