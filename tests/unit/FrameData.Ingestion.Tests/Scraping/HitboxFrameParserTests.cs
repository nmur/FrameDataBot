using FrameData.Scraper.Parsing;
using Shouldly;

namespace FrameData.Ingestion.Tests.Scraping;

public sealed class HitboxFrameParserTests
{
    private readonly HitboxDisplayParser _parser = new();

    [Fact]
    public void Parse_WhenDataFrameElementsExist_ReadsFrameImagesAndHitboxes()
    {
        const string html = """
            <div data-frame="006" data-frame-image-url="/frames/006.png">
              <span data-hitbox-type="P1_A" data-x="10" data-y="20" data-width="30" data-height="40"></span>
              <span class="P1_V" style="left: 1px; top: 2px; width: 3px; height: 4px"></span>
            </div>
            """;

        var frames = _parser.Parse(html);

        frames.Count.ShouldBe(1);
        frames[0].FrameId.ShouldBe("006");
        frames[0].SourceFrameImageUrl.ShouldBe("/frames/006.png");
        frames[0].Hitboxes.Count.ShouldBe(2);
        frames[0].Hitboxes[0].Type.ShouldBe("P1_A");
        frames[0].Hitboxes[0].Area.ShouldBe(1200);
        frames[0].Hitboxes[1].Type.ShouldBe("P1_V");
        frames[0].Hitboxes[1].X.ShouldBe(1);
    }

    [Fact]
    public void Parse_WhenJsonPayloadExists_ReadsFrames()
    {
        const string html = """
            <script type="application/json" id="hitbox-frames">
            {
              "frames": [
                {
                  "frame": "003",
                  "imageUrl": "/frames/003.png",
                  "hitboxes": [
                    { "type": "P1_A", "x": 4, "y": 5, "width": 6, "height": 7 }
                  ]
                }
              ]
            }
            </script>
            """;

        var frames = _parser.Parse(html);

        frames.Count.ShouldBe(1);
        frames[0].FrameId.ShouldBe("003");
        frames[0].Hitboxes.Single().Area.ShouldBe(42);
    }
}
