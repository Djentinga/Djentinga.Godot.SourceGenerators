namespace Godot;

[AttributeUsage(AttributeTargets.Class)]
public sealed class InstantiableAttribute(string init = "Init", string name = "New") : Attribute
{
    public string Initialise { get; } = init;
    public string Instantiate { get; } = name;
}
