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
}
