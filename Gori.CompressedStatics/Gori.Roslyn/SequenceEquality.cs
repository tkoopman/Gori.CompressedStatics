#pragma warning disable IDE0130

using System.Collections.Immutable;

namespace Gori.Roslyn;

/// <summary>
/// Small helpers for value (sequence) equality over ordered collections used inside pipeline models.
/// Keeping equality structural is required so the incremental generator caches correctly.
/// </summary>
internal static class SequenceEquality
{
    /// <summary>
    /// Compares two immutable arrays for equality, considering the order of elements and using the IEquatable&lt;T&gt; implementation of the elements.
    /// </summary>
    /// <typeparam name="T">The type of elements in the arrays.</typeparam>
    /// <param name="left">The first array to compare.</param>
    /// <param name="right">The second array to compare.</param>
    /// <returns><see langword="true"/> if the arrays are equal; otherwise, <see langword="false"/>.</returns>
    public static bool Equals<T>(ImmutableArray<T> left, ImmutableArray<T> right)
        where T : IEquatable<T>
    {
        if (left.IsDefault || right.IsDefault)
        {
            return left.IsDefault && right.IsDefault;
        }

        if (left.Length != right.Length)
        {
            return false;
        }

        for (int i = 0; i < left.Length; i++)
        {
            if (!left[i].Equals(right[i]))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Gets a hash code for an immutable array, considering the order of elements and using the IEquatable&lt;T&gt; implementation of the elements.
    /// </summary>
    /// <typeparam name="T">The type of elements in the array.</typeparam>
    /// <param name="values">The array for which to get the hash code.</param>
    /// <returns>A hash code for the array.</returns>
    public static int GetHashCode<T>(ImmutableArray<T> values)
        where T : IEquatable<T>
    {
        if (values.IsDefaultOrEmpty)
        {
            return 0;
        }

        unchecked
        {
            int hashCode = 17;
            foreach (T value in values)
            {
                hashCode = (hashCode * 397) ^ (value?.GetHashCode() ?? 0);
            }

            return hashCode;
        }
    }
}
