using System.Diagnostics;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;

namespace Djentinga.Godot.SourceGenerators.SceneTreeExtensions;

internal static class SceneTreeScraper
{
    private const string SectionRegexStr = @"^\[(?<Name>node|editable|ext_resource)( (?<Key>\S+?)=(""(?<Value>.+?)""|(?<Value>.+?)))*]$";
    private const string ValueRegexStr = @"^(?<Key>script|unique_name_in_owner) = ""?(?<Value>.+?)""?$";
    private const string ResIdRegexStr = @"^ExtResource\([ ""]?(?<Id>.+?)[ ""]?\)$";
    private const string ResPathRegexStr = @"^res:/(?<Path>.+?)$";

    private static readonly Regex SectionRegex = new(SectionRegexStr, RegexOptions.Compiled | RegexOptions.ExplicitCapture);
    private static readonly Regex ValueRegex = new(ValueRegexStr, RegexOptions.Compiled | RegexOptions.ExplicitCapture);
    private static readonly Regex ResIdRegex = new(ResIdRegexStr, RegexOptions.Compiled | RegexOptions.ExplicitCapture);
    private static readonly Regex ResPathRegex = new(ResPathRegexStr, RegexOptions.Compiled | RegexOptions.ExplicitCapture);

    private static string _resPath = null;
    private static readonly Dictionary<string, Tree<SceneTreeNode>> sceneTreeCache = [];

    public static void ClearCache()
    {
        sceneTreeCache.Clear();
        _resPath = null;
    }

    public static (Tree<SceneTreeNode> SceneTree, List<SceneTreeNode> UniqueNodes) GetNodes(Compilation compilation, string tscnFile, bool traverseInstancedScenes)
    {
        tscnFile = tscnFile.Replace("\\", "/");
        Log.Debug($"Scraping {tscnFile} [Cached? {sceneTreeCache.ContainsKey(tscnFile)}, CacheCount: {sceneTreeCache.Count}]");

        var valueMatch = false;
        SceneTreeNode curNode = null;
        Tree<SceneTreeNode> sceneTree = null;
        List<SceneTreeNode> uniqueNodes = [];
        var resources = new Dictionary<string, string>();
        var nodeLookup = new Dictionary<string, TreeNode<SceneTreeNode>>();

        foreach (var line in File.ReadLines(tscnFile).Skip(2))
        {
            Log.Debug($"Line: {line}");

            if (line is "") valueMatch = false;
            else if (valueMatch) ValueMatch();
            else SectionMatch();

            void SectionMatch()
            {
                var match = SectionRegex.Match(line);
                if (!match.Success) return;
                Log.Debug($" - Section {SectionRegex.GetGroupsAsStr(match)}");
                var name = match.Groups["Name"].Value;
                var values = match.Groups["Key"].ToDictionary(match.Groups["Value"]);

                switch (name)
                {
                    case "node":
                        NodeMatch();
                        valueMatch = true;
                        break;
                    case "editable":
                        EditableMatch();
                        break;
                    case "ext_resource":
                        ExtResourceMatch();
                        break;
                }

                void NodeMatch()
                {
                    var nodeName = values.Get("name");
                    var nodeType = values.Get("type");
                    var parentPath = values.Get("parent");
                    var resourceId = values.Get("instance");
                    var placeholder = values.Get("instance_placeholder");

                    if (nodeType is not null) nodeType = compilation.GetValidType(nodeType) ?? "Node";
                    if (resourceId is not null) resourceId = ResIdRegex.Match(resourceId).Groups["Id"].Value;
                    if (placeholder is not null) nodeType = "InstancePlaceholder";

                    var nodePath = GetNodePath();
                    var safeNodeName = nodeName.Replace("-", "_").Replace(" ", "");

                    AddNode(safeNodeName, nodePath);

                    bool IsRootNode()
                        => parentPath is null;

                    bool IsChildNode()
                        => parentPath is ".";

                    string GetNodePath()
                        => IsRootNode() ? "" : IsChildNode() ? nodeName : $"{parentPath}/{nodeName}";

                    bool HasResource()
                        => resourceId is not null;

                    string GetResource()
                    {
                        var resource = resources[resourceId];
                        return GetResPath(resource) + resource;

                        string GetResPath(string resource)
                        {
                            return _resPath is null || !tscnFile.StartsWith(_resPath)
                                ? _resPath = TryGetFromSceneCache() ?? TryGetFromFileSystem() : _resPath;

                            string TryGetFromSceneCache()
                                => sceneTreeCache.Keys.FirstOrDefault(x => x.EndsWith(resource))?[..^resource.Length];

                            string TryGetFromFileSystem()
                            {
                                const string GodotProjectFile = "project.godot";
                                var tscnFolder = Path.GetDirectoryName(tscnFile);

                                while (tscnFolder is not null)
                                {
                                    if (File.Exists($"{tscnFolder}/{GodotProjectFile}"))
                                        return tscnFolder;

                                    tscnFolder = Path.GetDirectoryName(tscnFolder);
                                }

                                throw new Exception($"Could not find {GodotProjectFile} in path {Path.GetDirectoryName(tscnFile)}");
                            }
                        }
                    }

                    void AddNode(string nodeName, string nodePath)
                    {
                        if (IsRootNode())
                        {
                            if (HasResource())
                                AddInheritedScene();
                            else
                                AddRootNode();
                        }
                        else
                        {
                            if (HasResource())
                                AddInstancedScene();
                            else
                                AddChildNode();
                        }

                        void AddInheritedScene()
                        {
                            var resource = GetResource();
                            Log.Debug($" - InheritedScene: {resource}");
                            var inheritedScene = GetCachedScene(resource);

                            inheritedScene.Traverse(x =>
                            {
                                if (x.IsRoot)
                                {
                                    curNode = new SceneTreeNode(nodeName, x.Value.Type, x.Value.Path);
                                    AddNode(curNode);
                                }
                                else
                                {
                                    var node = new SceneTreeNode(x.Value.Name, x.Value.Type, x.Value.Path);
                                    var parent = x.Parent.IsRoot ? "." : x.Parent.Value.Path;
                                    AddNode(node, nodeLookup[parent]);
                                }
                            });
                        }

                        void AddInstancedScene()
                        {
                            var resource = GetResource();
                            Log.Debug($" - InstancedScene: {resource}");
                            var instancedScene = GetCachedScene(resource);

                            instancedScene.Traverse(x =>
                            {
                                if (x.IsRoot)
                                {
                                    curNode = new SceneTreeNode(nodeName, x.Value.Type, nodePath);
                                    AddNode(curNode, nodeLookup[parentPath]);
                                }
                                else
                                {
                                    var node = new SceneTreeNode(x.Value.Name, x.Value.Type, $"{nodePath}/{x.Value.Path}", traverseInstancedScenes);
                                    var parent = x.Parent.IsRoot ? nodePath : $"{nodePath}/{x.Parent.Value.Path}";
                                    AddNode(node, nodeLookup[parent]);
                                }
                            });
                        }

                        void AddRootNode()
                        {
                            var resolvedType = nodeType ?? "Node";
                            curNode = new SceneTreeNode(nodeName, $"Godot.{resolvedType}", nodePath);
                            Log.Debug($" - RootNode [{curNode}]");
                            Debug.Assert(parentPath is null);
                            AddNode(curNode);
                        }

                        void AddChildNode()
                        {
                            var parent = nodeLookup.Get(parentPath);

                            if (UnsupportedParent())
                            {
                                curNode = null;
                                Log.Debug(" - ChildNode ignored (parent not supported)");
                                return;
                            }

                            if (ParentOverride())
                            {
                                curNode = nodeLookup.Get(nodePath)?.Value;
                                Log.Debug(curNode is null
                                    ? " - ChildNode ignored (parent not supported)"
                                    : $" - ChildNode (override) [{curNode}]");
                                return;
                            }

                            curNode = new SceneTreeNode(nodeName, $"Godot.{nodeType}", nodePath);
                            Log.Debug($" - ChildNode [{curNode}]");
                            AddNode(curNode, parent);

                            bool UnsupportedParent()
                                => parent is null;

                            bool ParentOverride()
                                => nodeType is null;
                        }

                        void AddNode(SceneTreeNode node, TreeNode<SceneTreeNode> parent = null)
                        {
                            if (sceneTree is null) // Root
                            {
                                Debug.Assert(parent is null);
                                nodeLookup.Add(".", sceneTree = new(node));
                            }
                            else
                            {
                                Debug.Assert(parent is not null);
                                nodeLookup.Add(node.Path, parent.Add(node));
                            }
                        }

                        Tree<SceneTreeNode> GetCachedScene(string resource)
                        {
                            if (sceneTreeCache.TryGetValue(resource, out var scene))
                            {
                                if (!File.Exists(resource))
                                {
                                    sceneTreeCache.Remove(resource);
                                    scene = null;
                                }
                            }

                            if (scene is null)
                            {
                                if (resource.EndsWith(".tscn"))
                                {
                                    if (!File.Exists(resource))
                                    {
                                        Log.Debug($"Resource not found: {resource}");
                                        scene = new(new("Missing", "Godot.Node", ""));
                                    }
                                    else
                                    {
                                        scene = GetNodes(compilation, resource, traverseInstancedScenes).SceneTree;
                                        Log.Debug($"\n<<< {tscnFile}");
                                    }
                                }
                                else
                                {
                                    Log.Debug($"NB: {Path.GetExtension(resource)} files not supported, adding {nodeName} as Node ({resource})");
                                    scene = new(new(nodeName, "Godot.Node", ""));
                                }
                            }

                            return scene;
                        }
                    }
                }

                void EditableMatch()
                {
                    var path = values.Get("path");
                    var node = nodeLookup[path];
                    node.Value.Visible = true;
                    Log.Debug($" - EditableNode [{node.Value}]");
                }

                void ExtResourceMatch()
                {
                    var type = values.Get("type");
                    if (type is "Script" or "PackedScene")
                    {
                        var id = values.Get("id");
                        var path = values.Get("path");
                        if (path is not null) path = ResPathRegex.Match(path).Groups["Path"].Value;
                        resources.Add(id, path);
                    }
                }
            }

            void ValueMatch()
            {
                var isValueMatch = ValueRegex.Match(line);
                if (!isValueMatch.Success) return;
                Log.Debug($" - Value {ValueRegex.GetGroupsAsStr(isValueMatch)}");
                var key = isValueMatch.Groups["Key"].Value;
                var value = isValueMatch.Groups["Value"].Value;

                switch (key)
                {
                    case "script": ScriptMatch(); break;
                    case "unique_name_in_owner": UniqueNameMatch(); break;
                }

                void ScriptMatch()
                {
                    if (value is "null") return;
                    if (curNode is null) return;
                    var isScriptMatch = ResIdRegex.Match(value);
                    if (!isScriptMatch.Success) return;
                    var resourceId = isScriptMatch.Groups["Id"].Value;
                    Log.Debug($" - ResourceId: {resourceId}");
                    var resource = resources[resourceId];
                    if (!resource.EndsWith(".cs")) return;

                    var scriptPath = _resPath + resource;
                    if (!File.Exists(scriptPath))
                    {
                        Log.Debug($" - WARNING: Script file not found: {scriptPath}");
                        return;
                    }

                    var name = Path.GetFileNameWithoutExtension(resource);
                    var resolvedType = compilation.GetFullName(name, resource);

                    if (resolvedType is null)
                    {
                        Log.Debug($" - WARNING: Could not resolve type for script: {resource} (did you rename the file?)");
                        return;
                    }

                    curNode.Type = resolvedType;
                    Log.Debug($" - ScriptType [{curNode}]");
                }

                void UniqueNameMatch()
                {
                    if (curNode is null) return;
                    if (bool.TryParse(value, out var result) && result is true)
                    {
                        uniqueNodes.Add(curNode);
                        Log.Debug($" - UniqueName [{curNode}]");
                    }
                }
            }
        }

        if (sceneTree?.Root?.Type is null or "")
        {
            Log.Debug($"ERROR: Root type is null/empty for {tscnFile}");
        }

        sceneTreeCache[tscnFile] = sceneTree;
        return (sceneTree, uniqueNodes);
    }
}
