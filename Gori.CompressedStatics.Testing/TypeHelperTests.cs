using System.Diagnostics.CodeAnalysis;

using Gori.CompressedStatics.Generators;

using Microsoft.CodeAnalysis;

namespace Gori.CompressedStatics.Testing;

public class TypeHelperTests
{
    public static TheoryData<string> StringTransformers
        => [
            new(typeof(StringTransformer).AssemblyQualifiedName!) { Label = "Assembly-qualified constructor" },
            new(typeof(StringTransformer).FullName!) { Label = "Full-name fallback" },
            new(nameof(StringTransformer)) { Label = "Short-name fallback" },
            new($"{typeof(TransformerProvider).AssemblyQualifiedName}::{nameof(TransformerProvider.StringField)}") { Label = "Static interface field" },
            new($"{typeof(TransformerProvider).AssemblyQualifiedName}::{nameof(TransformerProvider.StringProperty)}") { Label = "Static interface property" },
        ];

    public static TheoryData<string, string> InvalidSelectors
        => [
            new(string.Empty, nameof(ValueTransformerHelper.Error.InvalidFormat)) { Label = "Empty selector" },
            new("   ", nameof(ValueTransformerHelper.Error.InvalidFormat)) { Label = "Whitespace selector" },
            new("::Transformer", nameof(ValueTransformerHelper.Error.InvalidFormat)) { Label = "Missing type" },
            new($"{typeof(StringTransformer).AssemblyQualifiedName}::", nameof(ValueTransformerHelper.Error.InvalidFormat)) { Label = "Explicitly empty member" },
            new($"{typeof(StringTransformer).AssemblyQualifiedName}::   ", nameof(ValueTransformerHelper.Error.InvalidFormat)) { Label = "Whitespace member" },
            new($"{typeof(StringTransformer).AssemblyQualifiedName}::One::Two", nameof(ValueTransformerHelper.Error.InvalidFormat)) { Label = "Multiple member separators" },
            new("Gori.CompressedStatics.Testing.MissingTransformer", nameof(ValueTransformerHelper.Error.TypeNotFound)) { Label = "Missing transformer type" },
            new($"{typeof(TransformerProvider).AssemblyQualifiedName}::Missing", nameof(ValueTransformerHelper.Error.MemberNotFound)) { Label = "Missing static member" },
            new($"{typeof(TransformerProvider).AssemblyQualifiedName}::{nameof(TransformerProvider.NullProperty)}", nameof(ValueTransformerHelper.Error.UnknownError)) { Label = "Null transformer" },
            new($"{typeof(TransformerProvider).AssemblyQualifiedName}::{nameof(TransformerProvider.PrivateGetter)}", nameof(ValueTransformerHelper.Error.MemberNotFound)) { Label = "Private getter with public setter" },
            new($"{typeof(TransformerProvider).AssemblyQualifiedName}::{nameof(TransformerProvider.SetterOnly)}", nameof(ValueTransformerHelper.Error.MemberNotFound)) { Label = "Setter-only property" },
            new($"{typeof(TransformerProvider).AssemblyQualifiedName}::{nameof(TransformerProvider.ObjectField)}", nameof(ValueTransformerHelper.Error.MemberNotAssignable)) { Label = "Field declared as object" },
            new($"{typeof(TransformerProvider).AssemblyQualifiedName}::{nameof(TransformerProvider.ObjectProperty)}", nameof(ValueTransformerHelper.Error.MemberNotAssignable)) { Label = "Property declared as object" },
            new($"{typeof(InstanceProvider).AssemblyQualifiedName}::{nameof(InstanceProvider.Transformer)}", nameof(ValueTransformerHelper.Error.MemberNotFound)) { Label = "Instance property" },
            new(typeof(AbstractTransformer).AssemblyQualifiedName!, nameof(ValueTransformerHelper.Error.TypeNotConcreteClass)) { Label = "Abstract transformer" },
            new(typeof(GenericTransformer<>).AssemblyQualifiedName!, nameof(ValueTransformerHelper.Error.TypeIsGeneric)) { Label = "Open generic transformer" },
            new(typeof(NoDefaultConstructorTransformer).AssemblyQualifiedName!, nameof(ValueTransformerHelper.Error.TypeHasNoPublicParameterlessConstructor)) { Label = "No public parameterless constructor" },
            new(typeof(StructTransformer).AssemblyQualifiedName!, nameof(ValueTransformerHelper.Error.TypeNotConcreteClass)) { Label = "Struct is not a transformer class" },
            new($"{typeof(GenericProvider<>).AssemblyQualifiedName}::{nameof(GenericProvider<>.Transformer)}", nameof(ValueTransformerHelper.Error.TypeIsGeneric)) { Label = "Open generic provider" },
        ];

    public static TheoryData<string> ThrowingMembers
        => [
            new($"{typeof(TransformerProvider).AssemblyQualifiedName}::{nameof(TransformerProvider.ThrowingProperty)}") { Label = "Throwing getter" },
            new($"{typeof(ThrowingFieldProvider).AssemblyQualifiedName}::{nameof(ThrowingFieldProvider.Transformer)}") { Label = "Throwing static initializer" },
            new(typeof(ThrowingConstructorTransformer).AssemblyQualifiedName!) { Label = "Throwing constructor" },
        ];

    public static TheoryData<string> PaddedSelectors
        => [
            new($"  {typeof(StringTransformer).AssemblyQualifiedName}  ") { Label = "Padded constructor selector" },
            new($"  {typeof(TransformerProvider).AssemblyQualifiedName}  ::  {nameof(TransformerProvider.StringProperty)}  ") { Label = "Padded type and member" },
        ];

    [Theory]
    [MemberData(nameof(StringTransformers))]
    public void ValidStringTransformerIsResolved(string selector)
    {
        Assert.True(ValueTransformerHelper.IsValidTransformerValue(selector, out string? type, out string? member));
        Assert.NotNull(type);
        Assert.True(ValueTransformerHelper.TryGetTransformer(type, member, out IStringTransformer? transformer, out ValueTransformerHelper.Error error));
        Assert.Equal(ValueTransformerHelper.Error.None, error);
        Assert.NotNull(transformer);
        AssertStringTransform(transformer);
    }

    [Fact]
    public void BinaryTypeSelectorPrefersSingleton()
    {
        Assert.True(ValueTransformerHelper.TryGetTransformer(typeof(Base64Transformer).AssemblyQualifiedName!, null, out IBinaryTransformer? transformer, out ValueTransformerHelper.Error error));
        Assert.Equal(ValueTransformerHelper.Error.None, error);
        Assert.Same(Base64Transformer.Instance, transformer);
        AssertBinaryTransform(transformer);
    }

    [Fact]
    public void BinaryStaticFieldReturnsExistingInstance()
    {
        string selector = $"{typeof(Base64Transformer).AssemblyQualifiedName}::{nameof(Base64Transformer.Instance)}";

        Assert.True(ValueTransformerHelper.IsValidTransformerValue(selector, out string? type, out string? member));
        Assert.NotNull(type);
        Assert.True(ValueTransformerHelper.TryGetTransformer(type, member, out IBinaryTransformer? transformer, out ValueTransformerHelper.Error error));
        Assert.Equal(ValueTransformerHelper.Error.None, error);
        Assert.Same(Base64Transformer.Instance, transformer);
        AssertBinaryTransform(transformer);
    }

    [Fact]
    public void StaticPropertyReturnsExistingInstance()
    {
        string selector = $"{typeof(TransformerProvider).AssemblyQualifiedName}::{nameof(TransformerProvider.StringProperty)}";

        Assert.True(ValueTransformerHelper.IsValidTransformerValue(selector, out string? type, out string? member));
        Assert.NotNull(type);
        Assert.True(ValueTransformerHelper.TryGetTransformer(type, member, out IStringTransformer? transformer, out ValueTransformerHelper.Error error));
        Assert.Equal(ValueTransformerHelper.Error.None, error);
        Assert.Same(TransformerProvider.StringProperty, transformer);
    }

    [Fact]
    public void BinaryTransformerCannotBeUsedForStringResult()
    {
        Assert.False(ValueTransformerHelper.TryGetTransformer(typeof(Base64Transformer).AssemblyQualifiedName!, null, out IStringTransformer? transformer, out ValueTransformerHelper.Error error));
        Assert.Equal(ValueTransformerHelper.Error.TypeNotAssignable, error);
        Assert.Null(transformer);
    }

    [Fact]
    public void StringTransformerCannotBeUsedForBinaryResult()
    {
        Assert.False(ValueTransformerHelper.TryGetTransformer(typeof(StringTransformer).AssemblyQualifiedName!, null, out IBinaryTransformer? transformer, out ValueTransformerHelper.Error error));
        Assert.Equal(ValueTransformerHelper.Error.TypeNotAssignable, error);
        Assert.Null(transformer);
    }

    [Theory]
    [MemberData(nameof(InvalidSelectors))]
    public void InvalidSelectorIsRejected(string selector, string expectedError)
    {

        if (expectedError == nameof(ValueTransformerHelper.Error.InvalidFormat))
        {
            Assert.False(ValueTransformerHelper.IsValidTransformerValue(selector, out _, out _));
            return;
        }

        Assert.True(ValueTransformerHelper.IsValidTransformerValue(selector, out string? type, out string? member));
        Assert.NotNull(type);
        Assert.False(ValueTransformerHelper.TryGetTransformer(type, member, out IStringTransformer? transformer, out ValueTransformerHelper.Error error));
        Assert.Equal(expectedError, error.ToString());
        Assert.Null(transformer);
    }

    [Theory]
    [MemberData(nameof(ThrowingMembers))]
    public void MemberFailureDoesNotEscapeTryGetTransformer(string selector)
    {
        Assert.True(ValueTransformerHelper.IsValidTransformerValue(selector, out string? type, out string? member));
        Assert.NotNull(type);
        Assert.False(ValueTransformerHelper.TryGetTransformer(type, member, out IStringTransformer? transformer, out ValueTransformerHelper.Error error));
        Assert.Equal(ValueTransformerHelper.Error.ExceptionThrown, error);
        Assert.Null(transformer);
    }

    [Fact]
    public void MalformedAssemblyIdentityIsRejectedWithoutThrowing()
    {
        const string selector = "System.String, Test, Version=not-a-version";
        Assert.True(ValueTransformerHelper.IsValidTransformerValue(selector, out string? type, out string? member));
        Assert.NotNull(type);
        Assert.False(ValueTransformerHelper.TryGetTransformer(type, member, out IStringTransformer? transformer, out ValueTransformerHelper.Error error));
        Assert.Equal(ValueTransformerHelper.Error.ExceptionThrown, error);
        Assert.Null(transformer);
    }

    [Fact]
    public void BinaryInvocationFailureDoesNotEscapeTryGetTransformer()
    {
        Assert.False(ValueTransformerHelper.TryGetTransformer(typeof(ThrowingConstructorTransformer).AssemblyQualifiedName!, null, out IBinaryTransformer? transformer, out ValueTransformerHelper.Error error));
        Assert.Equal(ValueTransformerHelper.Error.ExceptionThrown, error);
        Assert.Null(transformer);
    }

    [Theory]
    [MemberData(nameof(PaddedSelectors))]
    public void SelectorSegmentsAreTrimmed(string selector)
    {
        Assert.True(ValueTransformerHelper.IsValidTransformerValue(selector, out string? type, out string? member));
        Assert.NotNull(type);
        Assert.True(ValueTransformerHelper.TryGetTransformer(type, member, out IStringTransformer? transformer, out ValueTransformerHelper.Error error));
        Assert.Equal(ValueTransformerHelper.Error.None, error);
        Assert.NotNull(transformer);
        AssertStringTransform(transformer);
    }

    [Fact]
    public void IncompatibleTypeIsRejectedBeforeItsConstructorRuns()
    {
        Assert.False(ValueTransformerHelper.TryGetTransformer(typeof(UnrelatedType).AssemblyQualifiedName!, null, out IStringTransformer? transformer, out ValueTransformerHelper.Error error));
        Assert.Equal(ValueTransformerHelper.Error.TypeNotAssignable, error);
        Assert.Null(transformer);
        Assert.Equal(0, UnrelatedType.ConstructorCalls);
    }

    [Fact]
    public void IncompatiblePropertyIsRejectedBeforeItsGetterRuns()
    {
        string selector = $"{typeof(SideEffectProvider).AssemblyQualifiedName}::{nameof(SideEffectProvider.Transformer)}";

        Assert.True(ValueTransformerHelper.IsValidTransformerValue(selector, out string? type, out string? member));
        Assert.NotNull(type);
        Assert.False(ValueTransformerHelper.TryGetTransformer(type, member, out IStringTransformer? transformer, out ValueTransformerHelper.Error error));
        Assert.Equal(ValueTransformerHelper.Error.MemberNotAssignable, error);
        Assert.Null(transformer);
        Assert.Equal(0, SideEffectProvider.GetterCalls);
    }

    public static TheoryData<Type, object> Singletons
        => [
            new(typeof(FieldSingleton), FieldSingleton.Instance) { Label = "Singleton field with private constructor" },
            new(typeof(PropertySingleton), PropertySingleton.Instance) { Label = "Singleton property with private constructor" },
            new(typeof(PublicConstructorSingleton), PublicConstructorSingleton.Instance) { Label = "Singleton preferred over public constructor" },
        ];

    public static TheoryData<Type> SingletonFallbacks
        => [
            new(typeof(NullSingleton)) { Label = "Null singleton falls back to constructor" },
            new(typeof(IncompatibleSingleton)) { Label = "Incompatible singleton falls back to constructor" },
            new(typeof(InterfaceSingleton)) { Label = "Interface-typed singleton falls back to constructor" },
        ];

    [Theory]
    [MemberData(nameof(Singletons))]
    public void ImplicitSingletonReturnsExistingInstance(Type type, object expected)
    {
        Assert.True(ValueTransformerHelper.TryGetTransformer(type.AssemblyQualifiedName!, null, out IStringTransformer? transformer, out ValueTransformerHelper.Error error));
        Assert.Equal(ValueTransformerHelper.Error.None, error);
        Assert.Same(expected, transformer);
        AssertStringTransform(transformer);
    }

    [Theory]
    [MemberData(nameof(SingletonFallbacks))]
    public void UnsuitableSingletonFallsBackToConstruction(Type type)
    {
        Assert.True(ValueTransformerHelper.TryGetTransformer(type.AssemblyQualifiedName!, null, out IStringTransformer? transformer, out ValueTransformerHelper.Error error));
        Assert.Equal(ValueTransformerHelper.Error.None, error);
        Assert.NotNull(transformer);
        Assert.Equal(type, transformer.GetType());
        Assert.NotSame(InterfaceSingleton.Instance, transformer);
        AssertStringTransform(transformer);
    }

    [Fact]
    public void InterfaceTypedSingletonCanBeSelectedExplicitly()
    {
        string selector = $"{typeof(InterfaceSingleton).AssemblyQualifiedName}::Instance";

        Assert.True(ValueTransformerHelper.IsValidTransformerValue(selector, out string? type, out string? member));
        Assert.NotNull(type);
        Assert.True(ValueTransformerHelper.TryGetTransformer(type, member, out IStringTransformer? transformer, out ValueTransformerHelper.Error error));
        Assert.Equal(ValueTransformerHelper.Error.None, error);
        Assert.Same(InterfaceSingleton.Instance, transformer);
    }

    [Fact]
    public void ThrowingImplicitSingletonDoesNotFallBackToConstructor()
    {
        Assert.False(ValueTransformerHelper.TryGetTransformer(typeof(ThrowingSingleton).AssemblyQualifiedName!, null, out IStringTransformer? transformer, out ValueTransformerHelper.Error error));
        Assert.Equal(ValueTransformerHelper.Error.ExceptionThrown, error);
        Assert.Null(transformer);
        Assert.Equal(0, ThrowingSingleton.ConstructorCalls);
    }

    public sealed class FieldSingleton : StringTransformer
    {
        public static readonly FieldSingleton Instance = new();

        private FieldSingleton()
        {
        }
    }

    public sealed class PropertySingleton : StringTransformer
    {
        public static PropertySingleton Instance { get; } = new();

        private PropertySingleton()
        {
        }
    }

    public sealed class PublicConstructorSingleton : StringTransformer
    {
        public static PublicConstructorSingleton Instance { get; } = new();
    }

    public sealed class NullSingleton : StringTransformer
    {
        public static NullSingleton? Instance => null;
    }

    public sealed class IncompatibleSingleton : StringTransformer
    {
        public static object Instance => throw new InvalidOperationException("Incompatible getter must not run.");
    }

    public sealed class InterfaceSingleton : StringTransformer
    {
        public static IStringTransformer Instance { get; } = new InterfaceSingleton();
    }

    public sealed class ThrowingSingleton : StringTransformer
    {
        public ThrowingSingleton() => ConstructorCalls++;

        public static int ConstructorCalls { get; private set; }

        public static ThrowingSingleton Instance => throw new InvalidOperationException("Singleton getter failed.");
    }

    private static CompressAttributeInfo CreateInput(string value, ReturnType returnType)
        => new()
        {
            ContainingType = default,
            MemberName = "Payload",
            MemberKind = MemberKind.Property,
            Accessibility = "public",
            IsPartial = true,
            ReturnType = returnType,
            AttributeType = CompressAttributeType.CompressString,
            SourceValue = value,
            Location = default,
        };

    private static void AssertStringTransform(IStringTransformer transformer)
    {
        Assert.True(transformer.TryTransform(CreateInput("payload", ReturnType.String), Microsoft.CodeAnalysis.CSharp.CSharpCompilation.Create("TransformerTest"), out string? output, out DiagnosticInfo? diagnostic, TestContext.Current.CancellationToken));
        Assert.Null(diagnostic);
        Assert.Equal("processed:payload", output);
    }

    private static void AssertBinaryTransform(IBinaryTransformer transformer)
    {
        Assert.True(transformer.TryTransform(CreateInput("AQID", ReturnType.ByteArray), Microsoft.CodeAnalysis.CSharp.CSharpCompilation.Create("TransformerTest"), out byte[]? output, out DiagnosticInfo? diagnostic, TestContext.Current.CancellationToken));
        Assert.Null(diagnostic);
        Assert.Equal(new byte[] { 1, 2, 3 }, output);
    }

    public class StringTransformer : IStringTransformer
    {
        public bool TryTransform(ICompressionAttributeInfo attribute, Compilation compilation, [NotNullWhen(true)] out string? output, [NotNullWhen(false)] out DiagnosticInfo? diagnostic, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            output = "processed:" + attribute.SourceValue;
            diagnostic = null;
            return true;
        }
    }

    public sealed class ThrowingConstructorTransformer : StringTransformer, IBinaryTransformer
    {
        public ThrowingConstructorTransformer() => throw new InvalidOperationException("Constructor failed.");

        bool IBinaryTransformer.TryTransform(ICompressionAttributeInfo attribute, Compilation compilation, [NotNullWhen(true)] out byte[]? output, [NotNullWhen(false)] out DiagnosticInfo? diagnostic, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            output = [];
            diagnostic = null;
            return true;
        }
    }

    internal static class TransformerProvider
    {
        public static readonly IStringTransformer StringField = new StringTransformer();

        public static readonly object ObjectField = new StringTransformer();

        public static IStringTransformer StringProperty { get; } = new StringTransformer();

        public static IStringTransformer? NullProperty => null;

        public static IStringTransformer PrivateGetter { private get; set; } = new StringTransformer();

        public static IStringTransformer SetterOnly { set { } }

        public static object ObjectProperty => StringProperty;

        public static IStringTransformer ThrowingProperty => throw new InvalidOperationException("Getter failed.");
    }

    internal sealed class InstanceProvider
    {
        public IStringTransformer Transformer { get; } = new StringTransformer();
    }

    public abstract class AbstractTransformer : StringTransformer
    {
    }

    public struct StructTransformer : IStringTransformer
    {
        public readonly bool TryTransform(ICompressionAttributeInfo attribute, Compilation compilation, [NotNullWhen(true)] out string? output, [NotNullWhen(false)] out DiagnosticInfo? diagnostic, CancellationToken cancellationToken)
            => new StringTransformer().TryTransform(attribute, compilation, out output, out diagnostic, cancellationToken);
    }

    internal static class GenericProvider<T>
    {
        public static IStringTransformer Transformer { get; } = new StringTransformer();
    }

    public sealed class GenericTransformer<T> : StringTransformer
    {
    }

    public sealed class NoDefaultConstructorTransformer : StringTransformer
    {
        private NoDefaultConstructorTransformer()
        {
        }
    }

    public sealed class UnrelatedType
    {
        public UnrelatedType() => ConstructorCalls++;

        public static int ConstructorCalls { get; private set; }
    }

    public static class SideEffectProvider
    {
        public static int GetterCalls { get; private set; }

        public static object Transformer
        {
            get
            {
                GetterCalls++;
                return new object();
            }
        }
    }

    internal static class ThrowingFieldProvider
    {
        public static readonly IStringTransformer Transformer = new StringTransformer();

        static ThrowingFieldProvider() => throw new InvalidOperationException("Static initialization failed.");
    }
}
