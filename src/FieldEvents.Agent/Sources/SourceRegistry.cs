namespace FieldEvents.Agent.Sources;

public record RegisteredSource(string Id, string ApiKey);

/// <summary>
/// Every source that may report events, keyed by X-Api-Key. Backed by config for this exercise
/// (see appsettings.json "Sources") - connecting a new source is just adding an entry there and
/// giving it the key, no code change required. A production version would move this to the DB
/// with an admin UI for issuing/revoking keys per source.
/// </summary>
public class SourceRegistry(IConfiguration config)
{
    private readonly List<RegisteredSource> _sources = config.GetSection("Sources").Get<List<RegisteredSource>>() ?? [];

    public bool Validate(string sourceId, string apiKey) =>
        _sources.Any(s => s.Id == sourceId && s.ApiKey == apiKey);
}
