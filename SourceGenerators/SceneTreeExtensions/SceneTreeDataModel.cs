using Microsoft.CodeAnalysis;

namespace Djentinga.Godot.SourceGenerators.SceneTreeExtensions;

internal class SceneTreeDataModel : ClassDataModel
{
    public string Root { get; }
    public string TscnResource { get; }
    public Tree<SceneTreeNode> SceneTree { get; }
    public IEnumerable<UniqueNode> UniqueNodes { get; }
    internal record UniqueNode(SceneTreeNode Node, string Scope);

    public bool HasTscnFilePathAttribute { get; }

    public SceneTreeDataModel(Compilation compilation, INamedTypeSymbol symbol, string root, string source, string uqScope, bool deep, string gdRoot, bool hasTscnFilePathAttribute) : base(symbol)
    {
        HasTscnFilePathAttribute = hasTscnFilePathAttribute;
        Root = root;
        TscnResource = GD.RES(source, gdRoot);
        var (sceneTree, uniqueNodes) = SceneTreeScraper.GetNodes(compilation, source, deep);
        SceneTree = sceneTree;
        UniqueNodes = [.. uniqueNodes.Select(x => new UniqueNode(x, Scope(x.Name)))];

        string Scope(string name)
        {
            return Scope()?.AddSuffix(" partial") ?? uqScope;

            string Scope() => symbol
                .GetMembers(name)
                .OfType<IPropertySymbol>()
                .SingleOrDefault()?.Scope();
        }
    }

    protected override string Str()
    {
        return string.Join("\n", Parts());

        IEnumerable<string> Parts()
        {
            yield return $"Root: {Root}";
            yield return $"Tscn: {TscnResource}";
            yield return $"Tree:-\n{SceneTree.ToString().TrimEnd()}";

            if (UniqueNodes.Any())
                yield return $"\nUniqueNodes:\n - {string.Join("\n - ", UniqueNodes)}";
        }
    }
}
