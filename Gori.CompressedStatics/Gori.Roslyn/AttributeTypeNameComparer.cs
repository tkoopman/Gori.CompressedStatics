#pragma warning disable IDE0130

using System.Diagnostics.CodeAnalysis;

namespace Gori.Roslyn;

/// <summary>
/// Provides a comparer for attribute type names that ignores the "Attribute" suffix and compares only the base name of the attribute.
/// </summary>
internal class AttributeTypeNameComparer : IEqualityComparer<string>
{
    /// <inheritdoc/>
    public bool Equals([AllowNull] string x, [AllowNull] string y)
    {
        if ((x is null && y is null) || string.Equals(x, y, StringComparison.Ordinal))
        {
            return true;
        }

        if (x is null || y is null)
        {
            return false;
        }

        string xName = RoslynHelper.GetNameFromQualifiedName(x);
        string yName = RoslynHelper.GetNameFromQualifiedName(y);

        int xAttributeIndex = xName.IndexOf("Attribute", StringComparison.Ordinal);
        int yAttributeIndex = yName.IndexOf("Attribute", StringComparison.Ordinal);

        if (xAttributeIndex >= 0)
        {
            xName = xName[..xAttributeIndex];
        }

        if (yAttributeIndex >= 0)
        {
            yName = yName[..yAttributeIndex];
        }

        return string.Equals(xName, yName, StringComparison.Ordinal);
    }

    /// <inheritdoc/>
    public int GetHashCode(string obj)
    {
        if (obj is null)
        {
            return 0;
        }

        string name = RoslynHelper.GetNameFromQualifiedName(obj);
        int attributeIndex = name.IndexOf("Attribute", StringComparison.Ordinal);
        if (attributeIndex >= 0)
        {
            name = name[..attributeIndex];
        }

        return StringComparer.Ordinal.GetHashCode(name);
    }
}
