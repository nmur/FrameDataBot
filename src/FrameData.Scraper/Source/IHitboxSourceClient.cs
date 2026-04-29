namespace FrameData.Scraper.Source;

public interface IHitboxSourceClient
{
    Task<string> GetHitboxDisplayPageAsync(string sourcePathOrUrl, CancellationToken cancellationToken = default);
}
