namespace FrameData.Scraper.Source;

public interface ISourceHttpClient
{
    Task<string> GetCharacterPageAsync(int sourceCharacterId, CancellationToken cancellationToken = default);
}
