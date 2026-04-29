namespace FrameData.Scraper.Source;

public interface IHitboxSourceClient
{
    Task<string> GetHitboxDisplayPageAsync(string sourcePathOrUrl, CancellationToken cancellationToken = default);

    Task<byte[]> GetBinaryAssetAsync(string sourcePathOrUrl, CancellationToken cancellationToken = default);
}
