namespace Cli.Args.Options;

using Setup.Validation.Primitives;

public abstract class ContentOptions
{
    public string ContentPath { get; init; } = string.Empty;

    public ContentValidationMode ValidationMode { get; init; } = ContentValidationMode.Strict;
}
