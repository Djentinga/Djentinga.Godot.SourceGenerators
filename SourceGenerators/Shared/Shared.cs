using System.Reflection;
using Scriban;
using Scriban.Runtime;

namespace Djentinga.Godot.SourceGenerators;

internal static class Shared
{
    private const string utils = "Djentinga.Godot.SourceGenerators.Shared.Utils.scriban";
    public static ScriptObject Utils = Template.Parse(Assembly.GetExecutingAssembly().GetEmbeddedResource(utils)).ToScribanScript();
}
