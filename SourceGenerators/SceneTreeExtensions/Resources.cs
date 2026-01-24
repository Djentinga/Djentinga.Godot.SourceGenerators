using System.Reflection;

namespace Djentinga.Godot.SourceGenerators.SceneTreeExtensions;

internal static class Resources
{
    private const string sceneTreeTemplate = "Djentinga.Godot.SourceGenerators.SceneTreeExtensions.SceneTreeTemplate.scriban";
    public static readonly string SceneTreeTemplate = Assembly.GetExecutingAssembly().GetEmbeddedResource(sceneTreeTemplate);

    public static readonly string ISceneTree = @"
        namespace Godot;

        public partial interface ISceneTree
        {
            static abstract string TscnFilePath { get; }
        }
        ".Trim();

    public static readonly string IInstantiable = @"
        namespace Godot;

        public partial interface IInstantiable<T> where T : Node, IInstantiable<T>, ISceneTree
        {
            static T Instantiate() => GD.Load<PackedScene>(T.TscnFilePath).Instantiate<T>();
        }

        public partial interface IInstantiable
        {
            static T Instantiate<T>() where T : Node, ISceneTree
                => GD.Load<PackedScene>(T.TscnFilePath).Instantiate<T>();
        }

        public static partial class Instantiator
        {
            public static T Instantiate<T>() where T : Node, ISceneTree
                => GD.Load<PackedScene>(T.TscnFilePath).Instantiate<T>();
        }".Trim();
}
