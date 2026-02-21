using Microsoft.CodeAnalysis;
using InputAction = (string MemberName, string GodotAction);
using NestedInputAction = (string ClassName, (string MemberName, string GodotAction) InputAction);

namespace Djentinga.Godot.SourceGenerators.InputMapExtensions;

internal class InputMapDataModel : ClassDataModel
{
    private string Type { get; }
    private IList<InputAction> Actions { get; }
    private ILookup<string, InputAction> NestedActions { get; }

    public InputMapDataModel(INamedTypeSymbol symbol, string type, string csPath, string gdRoot) : base(symbol)
    {
        var actions = InputMapScraper
            .GetInputActions(csPath, gdRoot)
            .ToLookup(IsNestedAction);

        Type = type;
        Actions = actions[false].Select(InputAction)
            .OrderBy(x => x.MemberName)
            .ToArray();
        NestedActions = actions[true].Select(NestedInputAction)
            .OrderBy(x => x.ClassName)
            .ThenBy(x => x.InputAction.MemberName)
            .ToLookup(x => x.ClassName, x => x.InputAction);
    }

    private static NestedInputAction NestedInputAction(string source)
    {
        var parts = source.Split(['.'], 2);
        var className = parts.First().ToPascalCase();
        var memberName = parts.Last().Replace(".", "").ToPascalCase();
        return (className, (memberName, source));
    }

    private static InputAction InputAction(string source)
        => (source.ToPascalCase(), source);

    private static bool IsNestedAction(string source)
        => source.Contains('.');

    protected override string Str()
    {
        return string.Join("\n", Type().Concat(Actions().Concat(NestedActions())));

        IEnumerable<string> Type()
        {
            yield return $"Type: {this.Type}";
        }

        IEnumerable<string> Actions()
        {
            foreach (var (name, action) in this.Actions)
                yield return $"MemberName: {name}, GodotAction: {action}";
        }

        IEnumerable<string> NestedActions()
        {
            foreach (var lookup in this.NestedActions)
            {
                foreach (var (name, action) in lookup)
                    yield return $"ClassName: {lookup.Key}, MemberName: {name}, GodotAction: {action}";
            }
        }
    }
}
