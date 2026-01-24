using Microsoft.CodeAnalysis;

namespace Djentinga.Godot.SourceGenerators;

public static class EnumExtensions
{
    public static string GetEnumValue(this ITypeSymbol type, object value)
    {
        var member = type?.GetMembers().OfType<IFieldSymbol>()
            .FirstOrDefault(m => Equals(m.ConstantValue, value));

        return member is null
            ? $"({type.FullName()}){value}"
            : $"{type.FullName()}.{member.Name}";
    }
}
