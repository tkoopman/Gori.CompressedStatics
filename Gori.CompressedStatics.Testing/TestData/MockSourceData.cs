using System.Text.Json;

using Xunit.Sdk;

namespace Gori.CompressedStatics.Testing.TestData;

public class MockSourceData : IXunitSerializable
{
    /* 
     * General test properties.
     */

    public required string SourceCode { get; set; }
    public string? SourcePath { get; set => field = value is null || value.Length == 0 ? null : value; }
    public string[]? AdditionalFiles { get; set => field = value is null || value.Length == 0 ? null : value; }
    public KeyValuePair<string, string>[]? AdditionalFilesWithContent { get; set => field = value is null || value.Length == 0 ? null : value; }

    /* 
     * Analyzer diagnostic test properties.
     */

    /// <summary>
    /// The diagnostic ID this check targets.
    /// Must be non-null value for code fix tests, but can be null for diagnostic message checks.
    /// Diagnostic tests when null will rely on message content checks, or if no message contents checks are specified, will expect no diagnostics to be raised.
    /// </summary>
    public string? DiagnosticId { get; set => field = value is null || value.Length == 0 ? null : value; }

    /// <summary>
    /// Should the diagnostic ID be expected for the test. If false, then ID must not be raised and messages and code fix checks are ignored.
    /// If true, then ID must be raised and messages and code fix checks are performed.
    /// If null, then:<br/>
    ///    - If DiagnosticId is not null, then it will be expected (same as setting this to true).<br/>
    ///    - If DiagnosticId null and <see cref="DiagnosticMessageExpected"/> or <see cref="DiagnosticMessageNotExpected"/><br/>
    ///    -   - is not specified, any diagnostic test will expect none to be raised.<br/>
    ///    -   - is specified, then ID is not checked at all, leaving it to those properties' checks.
    /// </summary>
    public bool? DiagnosticIdIsExpected { get; set; }

    /// <summary>
    /// The expected diagnostic message for the test. If null, the test will not check for any specific message content.
    /// If <see cref="DiagnosticId"/> is not null, only that ID's message is checked.
    /// If <see cref="DiagnosticId"/> is null, all diagnostics are checked for the expected message content, and passes if any one contains the expected message.
    /// </summary>
    public string[]? DiagnosticMessageExpected { get; set => field = value is null || value.Length == 0 ? null : value; }

    /// <summary>
    /// The diagnostic message content that should not be present for the given diagnostic.
    /// If <see cref="DiagnosticId"/> is not null, only that ID's message is checked.
    /// If <see cref="DiagnosticId"/> is null, all diagnostics are checked for the message content, and passes if none contain the specified message.
    /// </summary>
    public string[]? DiagnosticMessageNotExpected { get; set => field = value is null || value.Length == 0 ? null : value; }

    /*
     * CodeFix test properties.
     */

    /// <summary>
    /// The expected code fix title for other CodeFix properties to check against.
    /// Title just must contain this value (case-sensitive), not exact match.
    /// If null, all other CodeFix properties are used against any CodeFix returned.
    /// </summary>
    public string? CodeFixTitle { get; set => field = value is null || value.Length == 0 ? null : value; }

    /// <summary>
    /// Indicates whether a code fix should be offered for the given diagnostic.
    /// </summary>
    public bool? CodeFixShouldOffer { get; set; }

    /// <summary>
    /// A collection of strings that should be present in the resulting source code after applying the code fix for the given diagnostic.
    /// If <see cref="CodeFixTitle"/> is null, contains just must exist in any of the available code fixes.
    /// </summary>
    public string[]? CodeFixExpected { get; set => field = value is null || value.Length == 0 ? null : value; }

    /// <summary>
    /// A collection of strings that should not be present in the resulting source code after applying the code fix for the given diagnostic.
    /// If <see cref="CodeFixTitle"/> is null, contains must not exist in any of the available code fixes.
    /// </summary>
    public string[]? CodeFixNotExpected { get; set => field = value is null || value.Length == 0 ? null : value; }

    public virtual void Deserialize(IXunitSerializationInfo info)
    {
        SourceCode = info.GetValue<string>(nameof(SourceCode)) ?? throw new InvalidOperationException($"{nameof(SourceCode)} cannot be null");
        SourcePath = info.GetValue<string>(nameof(SourcePath));
        AdditionalFiles = info.GetValue<string[]>(nameof(AdditionalFiles)) ?? [];
        AdditionalFilesWithContent = DeserializeAdditionalFilesWithContent(info.GetValue<string[]?>(nameof(AdditionalFilesWithContent)));

        DiagnosticId = info.GetValue<string>(nameof(DiagnosticId));
        DiagnosticMessageExpected = info.GetValue<string[]>(nameof(DiagnosticMessageExpected));
        DiagnosticMessageNotExpected = info.GetValue<string[]>(nameof(DiagnosticMessageNotExpected));

        CodeFixShouldOffer = info.GetValue<bool?>(nameof(CodeFixShouldOffer));
        CodeFixExpected = info.GetValue<string[]?>(nameof(CodeFixExpected));
        CodeFixNotExpected = info.GetValue<string[]?>(nameof(CodeFixNotExpected));
    }

    public virtual void Serialize(IXunitSerializationInfo info)
    {
        info.AddValue(nameof(SourceCode), SourceCode);
        info.AddValue(nameof(SourcePath), SourcePath);
        info.AddValue(nameof(AdditionalFiles), AdditionalFiles);
        info.AddValue(nameof(AdditionalFilesWithContent), SerializeAdditionalFilesWithContent(AdditionalFilesWithContent));

        info.AddValue(nameof(DiagnosticId), DiagnosticId);
        info.AddValue(nameof(DiagnosticMessageExpected), DiagnosticMessageExpected);
        info.AddValue(nameof(DiagnosticMessageNotExpected), DiagnosticMessageNotExpected);

        info.AddValue(nameof(CodeFixShouldOffer), CodeFixShouldOffer);
        info.AddValue(nameof(CodeFixExpected), CodeFixExpected);
        info.AddValue(nameof(CodeFixNotExpected), CodeFixNotExpected);
    }

    private static string[]? SerializeAdditionalFilesWithContent(KeyValuePair<string, string>[]? additionalFiles)
        => additionalFiles is null ? null : [.. additionalFiles.Select(file => JsonSerializer.Serialize(new[] { file.Key, file.Value }))];

    private static KeyValuePair<string, string>[]? DeserializeAdditionalFilesWithContent(string[]? additionalFiles)
        => additionalFiles is null ? null : [.. additionalFiles.Select(file => JsonSerializer.Deserialize<string[]>(file) is { Length: 2 } pair
                ? new KeyValuePair<string, string>(pair[0], pair[1])
                : throw new InvalidOperationException($"Invalid serialized additional file entry: {file}"))];

    private sealed record CodeFixEntry(string Key, string[]? Value);
}
