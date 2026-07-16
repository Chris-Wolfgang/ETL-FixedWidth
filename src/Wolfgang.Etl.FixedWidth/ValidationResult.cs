using Wolfgang.Etl.FixedWidth.Enums;

namespace Wolfgang.Etl.FixedWidth;

/// <summary>
/// The result returned by a <see cref="FixedWidthExtractor{TRecord}.RecordValidator"/>
/// delegate: the <see cref="ValidationAction"/> to take for a parsed record and an
/// optional human-readable reason (surfaced in debug logs).
/// </summary>
public sealed class ValidationResult
{
    private ValidationResult(ValidationAction action, string? reason)
    {
        Action = action;
        Reason = reason;
    }



    /// <summary>The action the extractor should take for the record.</summary>
    public ValidationAction Action { get; }



    /// <summary>
    /// An optional reason describing why the record was skipped or extraction
    /// was stopped. <see langword="null"/> when not supplied.
    /// </summary>
    public string? Reason { get; }



    /// <summary>Accept the record and yield it.</summary>
    public static ValidationResult Accept() => new(ValidationAction.Accept, reason: null);



    /// <summary>
    /// Skip the record, optionally recording <paramref name="reason"/> in the
    /// debug log.
    /// </summary>
    /// <param name="reason">An optional reason the record was skipped.</param>
    public static ValidationResult Skip(string? reason = null) => new(ValidationAction.Skip, reason);



    /// <summary>
    /// Stop extraction, optionally recording <paramref name="reason"/> in the
    /// debug log.
    /// </summary>
    /// <param name="reason">An optional reason extraction was stopped.</param>
    public static ValidationResult Stop(string? reason = null) => new(ValidationAction.Stop, reason);
}
