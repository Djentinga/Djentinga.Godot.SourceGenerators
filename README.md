# Djentinga.Godot.SourceGenerators

A slimmed fork of https://github.com/Cat-Lips/GodotSharp.SourceGenerators containing bug fixes and new features.

Contains C# Source Generators for use with the Godot Game Engine 4.x and up.

## Features
* `SceneTree` class attribute:
  * Provides strongly typed access to the scene hierarchy (via `_` operator)
  * Generates direct access to uniquely named nodes via class properties
* `Instantiable` class attribute:
  * Generates configurable static method(s) to instantiate scene
* `InputMap` class attribute:
  * Provides strongly typed access to input actions defined in godot.project
  * Attribute option to replace StringName with your own custom object/handler
  * Generates `SortInputActions` helper for sorted dropdowns in the Godot editor
* `TscnFilePath` class attribute:
  * Generates a static `TscnFilePath` property without the full `SceneTree` hierarchy
* `TresFilePath` class attribute:
  * Generates a static `TresFilePath` property pointing to the associated `.tres` resource file
* Includes base classes/helpers to create project specific source generators


## Table of Contents
- [Djentinga.Godot.SourceGenerators](#djentingagodotsourcegenerators)
  - [Features](#features)
  - [Table of Contents](#table-of-contents)
  - [Installation](#installation)
  - [Documentation](#documentation)
    - [`SceneTree`](#scenetree)
    - [`Instantiable`](#instantiable)
    - [`InputMap`](#inputmap)
    - [`TscnFilePath`](#tscnfilepath)
    - [`TresFilePath`](#tresfilepath)

## Installation
Install via [NuGet](https://www.nuget.org/packages/Djentinga.Godot.SourceGenerators)

## Documentation

### `SceneTree`
  * Class attribute
  * Provides strongly typed access to the scene hierarchy (via `_` operator)
  * Generates direct access class properties for uniquely named nodes
  * Generates an interface for static tscn retrieval (ISceneTree.TscnFilePath)
  * Generates an interface for static instantiation (IInstantiable.Instantiate)
  * Note that nodes are cached on first access to avoid interop overhead
  * Advanced options available as attribute arguments:
    * tscnRelativeToClassPath: (default null) Specify path to tscn relative to current class
    * traverseInstancedScenes: (default false) Include instanced scenes in the generated hierarchy
    * root: (default _) Provide alternative to `_` operator (eg, to allow use of C# discard variable)
    * uqScope: (default Public) Default access scope for uniquely named node properties
#### Examples:
```cs
// Attach a C# script on the root node of the scene with the same name
// [SceneTree] will generate the members as the scene hierarchy and TscnFilePath property
[SceneTree]
//[SceneTree(root: "ME")]                       // Use this for alternative to `_`
//[SceneTree("my_scene.tscn")]                  // Use this if tscn has different name
//[SceneTree("../Scenes/MyScene.tscn")]         // Use relative path if tscn located elsewhere
//[SceneTree(traverseInstancedScenes: true)]    // Use this to include instanced scenes in current hierarchy
//[SceneTree(uqScope: Scope.Protected)]         // Use this to specify default scope of uniquely named nodes (default: 'Public')
public partial class MyScene : Node
{
    // Default scope of uniquely named nodes can be overridden using partial properties
    private partial MyNodeType MyNodeWithUniqueName { get; }

    public override void _Ready()
    {
        // You can access the node via '_' object
        GD.Print(_.Node1.Node11.Node12.Node121);
        GD.Print(_.Node4.Node41.Node412);

        // You can also directly access nodes marked as having a unique name in the editor
        GD.Print(MyNodeWithUniqueName);
        GD.Print(_.Path.To.MyNodeWithUniqueName); // Long equivalent

        // Only leaf nodes are Godot types (call .Get() on branch nodes)
        // Lets say you have _.Node1.Node2, observe the following code
        GD.Print(_.Node1.Name); // invalid
        GD.Print(_.Node1.Get().Name); // valid
        Node node1 = _.Node1; // implicit conversion also possible!
        GD.Print(node1.Name); // valid
        GD.Print(_.Node1.Node2.Name); // valid
    }
}

// TscnFilePath usage:
public void NextScene()
    => GetTree().ChangeSceneToFile(MyScene.TscnFilePath);
```
#### ISceneTree
 * Generated for any class decorated with [SceneTree]
```cs
namespace Djentinga.Godot.SourceGenerators;

public partial interface ISceneTree
{
    static abstract string TscnFilePath { get; }
}
```
Usage:
```cs
public void NextScene<T>() where T : ISceneTree
    => GetTree().ChangeSceneToFile(T.TscnFilePath);
```
#### IInstantiable
 * Provides a default Instantiate method that uses TscnFilePath
 * Both non-generic and generic versions are available
 * A default Instantiator class is also available
```cs
public partial interface IInstantiable
{
    static T Instantiate<T>() where T : Node, ISceneTree
        => GD.Load<PackedScene>(T.TscnFilePath).Instantiate<T>();
}

public partial interface IInstantiable<T> where T : Node, IInstantiable<T>, ISceneTree
{
    static T Instantiate() => GD.Load<PackedScene>(T.TscnFilePath).Instantiate<T>();
}

public static partial class Instantiator
{
    public static T Instantiate<T>() where T : Node, ISceneTree
        => GD.Load<PackedScene>(T.TscnFilePath).Instantiate<T>();
}
```
Usage:
```cs
[SceneTree]
public partial class Scene1 : Node;

[SceneTree]
public partial class Scene2 : Node, IInstantiable;

[SceneTree]
public partial class Scene3 : Node, IInstantiable<Scene3>;

*****

// Instantiator works for all ISceneTree types
var scene1 = Instantiator.Instantiate<Scene1>();
var scene2 = Instantiator.Instantiate<Scene2>();
var scene3 = Instantiator.Instantiate<Scene3>();

// The non-generic interface can also instantiate any ISceneTree type (but why would you want to)
var scene1 = IInstantiable.Instantiate<Scene1>();
var scene2 = IInstantiable.Instantiate<Scene2>();
var scene3 = IInstantiable.Instantiate<Scene3>();

// The generic interface can only instantiate it's own ISceneTree type (but why would you want to)
var scene3 = IInstantiable<Scene3>.Instantiate();

// Use generics to instantiate specific types
static T Instantiate<T>() where T : Node, ISceneTree, IInstantiable
    => IInstantiable.Instantiate<T>(); // or Instantiator.Instantiate<T>();
var scene2 = Instantiate<Scene2>();

OR

static T Instantiate<T>() where T : Node, ISceneTree, IInstantiable<T>
    => IInstantiable<T>.Instantiate(); // or Instantiator.Instantiate<T>();
var scene3 = Instantiate<Scene3>();

```

### `Instantiable`
  * Class attribute
  * Generates configurable static method(s) to instantiate scene
  * Generates configurable constructor to ensure safe construction
  * Advanced options available as attribute arguments:
    * init: (default 'Init') Name of init function
    * name: (default 'New') Name of instantiate function
#### Examples:
```cs
[Instantiate]
public partial class Scene1 : Node
{
    // No Init()
}

[Instantiate]
public partial class Scene2 : Node
{
    private void Init()
    private void Init(int arg)
}

[Instantiate(nameof(Initialise), "Instantiate")]
public partial class Scene3 : Node
{
    private void Initialise(int arg1, string arg2, object arg3 = null)
}
```
Generates:
```cs
partial class Scene1
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    private static PackedScene _Scene1 => field ??= GD.Load<PackedScene>("res://Path/To/Scene1.tscn");

    public static Scene1 New() => (Scene1)_Scene1.Instantiate();
}

partial class Scene2
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    private static PackedScene _Scene2 => field ??= GD.Load<PackedScene>("res://Path/To/Scene2.tscn");

    public static Scene2 New()
    {
        var scene = (Scene2)_Scene2.Instantiate();
        scene.Init();
        return scene;
    }

    public static Scene2 New(int arg)
    {
        var scene = (Scene2)_Scene2.Instantiate();
        scene.Init(arg);
        return scene;
    }
}

partial class Scene3
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    private static PackedScene _Scene3 => field ??= GD.Load<PackedScene>("res://Path/To/Scene3.tscn");

    public static Scene3 Instantiate(int arg1, string arg2, object arg3 = null)
    {
        var scene = (Scene3)_Scene3.Instantiate();
        scene.Initialise(arg1, arg2, arg3);
        return scene;
    }
}
```


### `InputMap`
  * Class attribute
  * Provides strongly typed access to input actions defined in godot.project (set via editor)
  * Actions are sorted alphabetically in the generated code
  * Generates a `SortInputActions` helper method for sorted dropdowns in the Godot editor
  * If you want access to built-in actions, see [BuiltinInputActions.cs](https://gist.github.com/qwe321qwe321qwe321/bbf4b135c49372746e45246b364378c4)
  * Advanced options available as attribute arguments:
    * dataType: (default StringName)
#### Examples:
```cs
[InputMap]
public static partial class MyInput;

[InputMap(nameof(GameInput))]
public static partial class MyGameInput;

// Example custom input action class
public class GameInput(StringName action)
{
    public StringName Action => action;

    public bool IsPressed => Input.IsActionPressed(action);
    public bool IsJustPressed => Input.IsActionJustPressed(action);
    public bool IsJustReleased => Input.IsActionJustReleased(action);
    public float Strength => Input.GetActionStrength(action);

    public void Press() => Input.ActionPress(action);
    public void Release() => Input.ActionRelease(action);
}
```
Generates:
```cs
// (static optional)
// (does not provide access to built-in actions)
partial static class MyInput
{
    /// <summary>
    /// Replaces <c>PropertyHint.InputName</c> with a sorted <c>PropertyHint.Enum</c> dropdown in the Godot editor.
    /// Call from <c>_ValidateProperty</c> to sort the input action dropdown.
    /// </summary>
    public static void SortInputActions(Godot.Collections.Dictionary property) { ... }

    public static readonly StringName MoveDown = new("move_down");
    public static readonly StringName MoveLeft = new("move_left");
    public static readonly StringName MoveRight = new("move_right");
    public static readonly StringName MoveUp = new("move_up");
}

partial static class MyGameInput
{
    public static void SortInputActions(Godot.Collections.Dictionary property) { ... }

    public static readonly GameInput MoveDown = new("move_down");
    public static readonly GameInput MoveLeft = new("move_left");
    public static readonly GameInput MoveRight = new("move_right");
    public static readonly GameInput MoveUp = new("move_up");
}
```
#### SortInputActions
Use the generated `SortInputActions` method in `_ValidateProperty` to replace the default unsorted input action dropdown with a sorted enum dropdown:
```cs
public override void _ValidateProperty(Godot.Collections.Dictionary property)
{
    MyInput.SortInputActions(property);
}
```

### `TscnFilePath`
  * Class attribute
  * Lightweight alternative to `[SceneTree]` when you only need the `TscnFilePath` property
  * Generates a static `TscnFilePath` property and implements `ISceneTree`
  * Advanced options available as attribute arguments:
    * tscnRelativeToClassPath: (default null) Specify path to tscn relative to current class
#### Examples:
```cs
// Generates TscnFilePath for the .tscn file with the same name as the class
[TscnFilePath]
public partial class MyScene : Node;

// Specify a custom path relative to the class file
[TscnFilePath("../Scenes/MyScene.tscn")]
public partial class MyScene : Node;
```
Generates:
```cs
partial class MyScene : Godot.ISceneTree
{
    public static string TscnFilePath { get; } = "res://Path/To/MyScene.tscn";
}
```

### `TresFilePath`
  * Class attribute
  * Generates a static `TresFilePath` property pointing to the associated `.tres` resource file
  * Advanced options available as attribute arguments:
    * tresRelativeToClassPath: (default null) Specify path to tres relative to current class
#### Examples:
```cs
// Generates TresFilePath for the .tres file with the same name as the class
[TresFilePath]
public partial class MyResource : Resource;

// Specify a custom path relative to the class file
[TresFilePath("../Resources/MyResource.tres")]
public partial class MyResource : Resource;
```
Generates:
```cs
partial class MyResource
{
    public static string TresFilePath { get; } = "res://Path/To/MyResource.tres";
}
```