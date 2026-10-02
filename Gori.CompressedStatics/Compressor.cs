using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO.Compression;
using System.Reflection;

namespace Gori.CompressedStatics;

/// <summary>
/// Provides methods for compressing byte arrays using various compression algorithms.
/// In Auto or AutoForced modes, it will try all available algorithms and return the one that produces the smallest output.
///
/// This is used at compile time to compress the values of static fields in the Gori.CompressedStatics library.
/// </summary>
public static class Compressor
{
    private static readonly CompressionAlgorithm[] Algorithms = [.. (Enum.GetValues(typeof(CompressionAlgorithm)) as CompressionAlgorithm[])
                                                                         .Where(alg => alg is not CompressionAlgorithm.Auto and not CompressionAlgorithm.AutoForced)];

    private static readonly ConstructorInfo? ModernBrotliStreamConstructor = ResolveModernBrotliStreamType()?.GetConstructor([typeof(Stream), typeof(CompressionMode)]);

    /// <summary>
    /// Gets a value indicating whether the modern Brotli compression algorithm is available in the current compiler host.
    /// </summary>
    [MemberNotNullWhen(true, nameof(ModernBrotliStreamConstructor))]
    public static bool IsModernBrotliAvailable => ModernBrotliStreamConstructor is not null;

    /// <summary>
    /// Compresses the input byte array using the specified compression algorithm.
    /// If the algorithm is set to Auto or AutoForced, it will try all available
    /// algorithms and return the one that produces the smallest output.
    /// </summary>
    /// <param name="input">The byte array to compress.</param>
    /// <param name="algorithm">
    /// The compression algorithm to use. If set to Auto or AutoForced, this will
    /// be updated to the algorithm that produces the smallest output, or left Auto
    /// if uncompressed smallest.
    /// </param>
    /// <param name="disableBrotli">
    /// If true, Brotli compression will be disabled in auto modes.
    /// Set to true if using Net Standard 2.0 without the Brotli library.
    /// </param>
    /// <param name="cancellationToken">A cancellation token to observe while compressing.</param>
    /// <returns>The compressed byte array.</returns>
    /// <exception cref="NotSupportedException">Thrown if the specified compression algorithm is not supported.</exception>
    internal static byte[] Compress(byte[] input, ref CompressionAlgorithm algorithm, bool disableBrotli = false, CancellationToken cancellationToken = default)
    {
        if (input is null || input.Length == 0)
        {
            return [];
        }

        if (algorithm is CompressionAlgorithm.Brotli && (disableBrotli || !IsModernBrotliAvailable))
        {
            throw new NotSupportedException("Brotli compression is not available in the current compiler host.");
        }

        int length = input.Length;
        byte[] winningBuffer;
        CompressionAlgorithm[] algorithms;

        switch (algorithm)
        {
            case CompressionAlgorithm.Auto:
                winningBuffer = input;
                algorithms = disableBrotli || !IsModernBrotliAvailable ? [.. Algorithms.Where(alg => alg != CompressionAlgorithm.Brotli)] : Algorithms;
                break;

            case CompressionAlgorithm.AutoForced:
                winningBuffer = [];
                algorithms = disableBrotli || !IsModernBrotliAvailable ? [.. Algorithms.Where(alg => alg != CompressionAlgorithm.Brotli)] : Algorithms;
                algorithm = CompressionAlgorithm.Auto;
                break;

            default:
                winningBuffer = [];
                algorithms = [algorithm];
                break;
        }

        foreach (CompressionAlgorithm alg in algorithms)
        {
            cancellationToken.ThrowIfCancellationRequested();

            using var tempMemoryStream = new MemoryStream();
            using (Stream compressionStream = CreateCompressionStream(alg, tempMemoryStream))
            {
                compressionStream.Write(input, 0, length);
                compressionStream.Flush();
                if (tempMemoryStream.Length == 0)
                {
                    Debug.Assert(false, $"The compression algorithm '{alg}' produced an empty output.");
                    continue;
                }

                if (tempMemoryStream.Length >= winningBuffer.Length && winningBuffer.Length != 0)
                {
                    continue;
                }
            }

            winningBuffer = tempMemoryStream.ToArray();
            algorithm = alg;
        }

        if (winningBuffer.Length == 0)
        {
            Debug.Assert(false, "No compression algorithm produced a valid output.");
            return input;
        }

        return winningBuffer;
    }

    private static Stream CreateCompressionStream(CompressionAlgorithm algorithm, Stream output)
        => algorithm switch
        {
            CompressionAlgorithm.Brotli => CreateBrotliStream(output, CompressionMode.Compress),
            CompressionAlgorithm.GZip => new GZipStream(output, CompressionMode.Compress),
            CompressionAlgorithm.Deflate => new DeflateStream(output, CompressionMode.Compress),
            _ => throw new NotSupportedException($"The compression algorithm '{algorithm}' is not supported."),
        };

    private static Stream CreateBrotliStream(Stream stream, CompressionMode mode)
    {
        if (ModernBrotliStreamConstructor is null)
        {
            throw new NotSupportedException("Brotli is not available in the current compiler host.");
        }

        object? instance = ModernBrotliStreamConstructor.Invoke([stream, mode]);
        return instance as Stream ?? throw new NotSupportedException("Unable to create Brotli stream in the current compiler host.");
    }

    private static Type? ResolveModernBrotliStreamType()
        => Type.GetType("System.IO.Compression.BrotliStream, System.IO.Compression.Brotli", throwOnError: false)
        ?? AppDomain.CurrentDomain.GetAssemblies()
                                  .Select(static assembly => assembly.GetType("System.IO.Compression.BrotliStream", throwOnError: false))
                                  .FirstOrDefault(static type => type is not null);
}
