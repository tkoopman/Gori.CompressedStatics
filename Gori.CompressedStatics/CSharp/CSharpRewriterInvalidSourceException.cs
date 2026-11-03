namespace Gori.CompressedStatics.CSharp;

/// <summary>
/// Thrown when a syntax error is found in provided source code.
/// </summary>
public class CSharpRewriterInvalidSourceException : CSharpRewriterException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CSharpRewriterInvalidSourceException"/> class.
    /// </summary>
    /// <param name="syntaxError">The syntax error that explains the reason for the exception.</param>
    public CSharpRewriterInvalidSourceException(string syntaxError)
        : base($"Invalid C# source: {syntaxError}")
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CSharpRewriterInvalidSourceException"/> class.
    /// </summary>
    /// <param name="syntaxError">The syntax error that explains the reason for the exception.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public CSharpRewriterInvalidSourceException(string syntaxError, Exception innerException)
        : base($"Invalid C# source: {syntaxError}", innerException)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CSharpRewriterInvalidSourceException"/> class.
    /// </summary>
    public CSharpRewriterInvalidSourceException()
    {
    }
}
