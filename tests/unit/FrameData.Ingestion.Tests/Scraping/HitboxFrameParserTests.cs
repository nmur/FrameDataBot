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
    public void Parse_WhenSourceFramesScriptExists_ReadsFrameImagesAndDrawBoxes()
    {
        const string html = """
            <script>
            var sBaseUrl = 'http://example.test/repo/11/fd_normals/001/';
            aFramesInfos = {
              "004": {
                "frame": "004",
                "pngFileName": "004_canvas.png",
                "P1": {
                  "hitboxes": {
                    "p_hb_to_draw": [[1, 4, 2, 6]],
                    "v_hb_to_draw": [],
                    "a_hb_to_draw": [[150, 136, 109, 131]],
                    "t_hb_to_draw": [],
                    "ta_hb_to_draw": []
                  }
                },
                "P2": {
                  "hitboxes": {
                    "p_hb_to_draw": [],
                    "v_hb_to_draw": [],
                    "a_hb_to_draw": [[0, 100, 0, 100]],
                    "t_hb_to_draw": [],
                    "ta_hb_to_draw": []
                  }
                }
              }
            };
            </script>
            """;

        var frames = _parser.Parse(html);

        frames.Count.ShouldBe(1);
        frames[0].FrameId.ShouldBe("004");
        frames[0].SourceFrameImageUrl.ShouldBe("http://example.test/repo/11/fd_normals/001/004_canvas.png");

        var p1ActiveHitbox = frames[0].Hitboxes.Single(hitbox => hitbox.Type == "P1_A");
        p1ActiveHitbox.X.ShouldBe(136);
        p1ActiveHitbox.Y.ShouldBe(109);
        p1ActiveHitbox.Width.ShouldBe(14);
        p1ActiveHitbox.Height.ShouldBe(22);

        frames[0].Hitboxes.ShouldContain(hitbox => hitbox.Type == "P2_A");
    }

    [Fact]
    public void Parse_WhenSourceFrameContainsObject_ReadsProjectileActiveHitboxes()
    {
        const string html = """
            <script>
            var sBaseUrl = 'http://example.test/repo/11/fd_normals/037/';
            aFramesInfos = {
              "011": {
                "frame": "011",
                "objects_list": ["OBJECT_1"],
                "pngFileName": "011_canvas.png",
                "P1": {
                  "hitboxes": {
                    "a_hb_to_draw": []
                  }
                },
                "OBJECT_1": {
                  "hitboxes": {
                    "a_hb_to_draw": [[197, 117, 123, 149]]
                  }
                }
              }
            };
            </script>
            """;

        var frames = _parser.Parse(html);

        frames.Count.ShouldBe(1);
        frames[0].SourceFrameImageUrl.ShouldBe("http://example.test/repo/11/fd_normals/037/011_canvas.png");

        var objectActiveHitbox = frames[0].Hitboxes.Single(hitbox => hitbox.Type == "OBJECT_A");
        objectActiveHitbox.X.ShouldBe(117);
        objectActiveHitbox.Y.ShouldBe(123);
        objectActiveHitbox.Width.ShouldBe(80);
        objectActiveHitbox.Height.ShouldBe(26);
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
