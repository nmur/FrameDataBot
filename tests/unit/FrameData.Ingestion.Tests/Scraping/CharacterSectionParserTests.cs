using FrameData.Scraper.Parsing;
using Shouldly;

namespace FrameData.Ingestion.Tests.Scraping;

public sealed class CharacterSectionParserTests
{
    private readonly CharacterSectionParser _parser = new();

    [Fact]
    public void Parse_WhenSupportedSectionsPresent_ParsesMoveRows()
    {
        const string html = """
            <html><body>
              <h2>Normals</h2>
              <table>
                <tr><th>Move</th><th>Startup</th><th>Active</th><th>Recovery</th><th>On Hit</th><th>On Block</th><th>Frame Advantage</th></tr>
                <tr><td>2mk</td><td>6</td><td>3</td><td>17</td><td>+1</td><td>-2</td><td>-2</td></tr>
              </table>
              <h2>Specials</h2>
              <table>
                <tr><th>Move</th><th>Startup</th><th>Active</th></tr>
                <tr><td>hayate</td><td>12</td><td>4</td></tr>
              </table>
            </body></html>
            """;

        var parsed = _parser.Parse(html);

        parsed.Count.ShouldBe(2);
        parsed[0].Section.ShouldBe("Normals");
        parsed[0].CanonicalName.ShouldBe("2mk");
        parsed[0].Startup.ShouldBe("6");
        parsed[0].OnBlock.ShouldBe("-2");
        parsed[1].Section.ShouldBe("Specials");
        parsed[1].CanonicalName.ShouldBe("hayate");
        parsed[1].Recovery.ShouldBeNull();
    }

    [Fact]
    public void Parse_WhenSectionNotSupported_IgnoresRows()
    {
        const string html = """
            <html><body>
              <h2>Throws</h2>
              <table>
                <tr><th>Move</th><th>Startup</th></tr>
                <tr><td>normal throw</td><td>2</td></tr>
              </table>
            </body></html>
            """;

        var parsed = _parser.Parse(html);

        parsed.ShouldBeEmpty();
    }

    [Fact]
    public void Parse_WhenSourceFrameTableIsLoadedIntoContentChar_ParsesRealSourceHeaders()
    {
        const string html = """
            <html><body>
              <h2>Normals</h2>
              <div id="content_char">
                <table id="fd_table">
                  <thead>
                    <tr><th></th><th>Name</th><th>Startup</th><th>Hit</th><th>Recovery</th><th>Blk. Adv.</th><th>Hit Adv.</th></tr>
                  </thead>
                  <tbody>
                    <tr><td></td><td>Jab</td><td>4</td><td>2</td><td>9</td><td>1</td><td>1</td></tr>
                  </tbody>
                </table>
              </div>
            </body></html>
            """;

        var parsed = _parser.Parse(html);

        parsed.Count.ShouldBe(1);
        parsed[0].Section.ShouldBe("Normals");
        parsed[0].CanonicalName.ShouldBe("Jab");
        parsed[0].Startup.ShouldBe("4");
        parsed[0].Active.ShouldBe("2");
        parsed[0].Recovery.ShouldBe("9");
        parsed[0].OnBlock.ShouldBe("1");
        parsed[0].OnHit.ShouldBe("1");
        parsed[0].FrameAdvantage.ShouldBe("1");
    }

    [Fact]
    public void Parse_WhenMotionDamageAndStunColumnsExist_ParsesSourceAttributes()
    {
        const string html = """
            <html><body>
              <h2>Specials</h2>
              <table>
                <tr><th>Name</th><th>Motion</th><th>Damage</th><th>Stun</th><th>Startup</th><th>Hit</th></tr>
                <tr><td>Hayate</td><td>236P</td><td>120</td><td>17</td><td>12</td><td>3</td></tr>
              </table>
              <h2>Super Arts</h2>
              <table>
                <tr><th>Name</th><th>Motion</th><th>Dmg.</th><th>Stun</th><th>Startup</th></tr>
                <tr><td>Seichusen Godanzuki</td><td>236236P</td><td>320</td><td>0</td><td>1</td></tr>
              </table>
            </body></html>
            """;

        var parsed = _parser.Parse(html);

        parsed.Count.ShouldBe(2);
        parsed[0].Section.ShouldBe("Specials");
        parsed[0].Motion.ShouldBe("236P");
        parsed[0].Damage.ShouldBe("120");
        parsed[0].Stun.ShouldBe("17");
        parsed[1].Section.ShouldBe("Super Arts");
        parsed[1].Motion.ShouldBe("236236P");
        parsed[1].Damage.ShouldBe("320");
        parsed[1].Stun.ShouldBe("0");
    }

    [Fact]
    public void Parse_WhenSourceUsesHitboxMetadata_CapturesSourceMoveId()
    {
        const string html = """
            <html><body>
              <h2>Normals</h2>
              <table>
                <tr><th></th><th>Name</th><th>Startup</th><th>Hit</th><th>Recovery</th></tr>
                <tr>
                  <td title="1"><div class="linkHitboxes" id="load_1"><div class="none">00001</div></div></td>
                  <td>Jab</td>
                  <td>3</td><td>3</td><td>5</td>
                </tr>
              </table>
            </body></html>
            """;

        var parsed = _parser.Parse(html);

        parsed.Count.ShouldBe(1);
        parsed[0].CanonicalName.ShouldBe("Jab");
        parsed[0].SourceMoveId.ShouldBe("1");
        parsed[0].SourceHitboxPath.ShouldBeNull();
    }

    [Fact]
    public void Parse_WhenMoveLinksToHitboxDisplay_CapturesSourceMoveIdAndPath()
    {
        const string html = """
            <html><body>
              <h2>Normals</h2>
              <table>
                <tr><th>Name</th><th>Startup</th></tr>
                <tr>
                  <td><a href="hitboxesDisplay.php?iChar=14&amp;sMoveType=fd_normals&amp;iMove=027">Forward MP</a></td>
                  <td>5</td>
                </tr>
              </table>
            </body></html>
            """;

        var parsed = _parser.Parse(html);

        parsed.Single().SourceMoveId.ShouldBe("027");
        parsed.Single().SourceHitboxPath.ShouldBe("hitboxesDisplay.php?iChar=14&sMoveType=fd_normals&iMove=027");
    }
}
