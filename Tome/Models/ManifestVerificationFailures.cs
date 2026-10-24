namespace Tome.Models;

public sealed class ManifestVerificationFailures
{
    public required IReadOnlyDictionary<string, string> ProblematicMods { get; set; }
}
