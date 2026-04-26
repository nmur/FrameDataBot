using FrameData.Scraper.Source;
using Shouldly;
using System.Net;

namespace FrameData.Ingestion.Tests.Scraping;

public sealed class SourceHttpClientTests
{
    [Fact]
    public async Task GetCharacterPageAsync_WhenContentCharLoadsViaAjax_WaitsForAndComposesFrameDataSections()
    {
        var handler = new AjaxFrameDataHandler();
        using var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("http://example.test/index.php")
        };
        var sourceClient = new SourceHttpClient(httpClient);

        var html = await sourceClient.GetCharacterPageAsync(1);

        html.ShouldContain("<h2>Normals</h2>");
        html.ShouldContain("<h2>Specials</h2>");
        html.ShouldContain("<h2>Super Arts</h2>");
        html.ShouldContain("<h2>Misc</h2>");
        html.ShouldContain("Jab");
        html.ShouldContain("Flash Chop");
        html.ShouldContain("Hyper Bomb");
        handler.PostBodies.Count.ShouldBe(4);
        handler.PostBodies.Any(body => body.Contains("div=content_char")).ShouldBeTrue();
        handler.PostBodies.Any(body => body.Contains("id=normals")).ShouldBeTrue();
        handler.PostBodies.Any(body => body.Contains("id=specials")).ShouldBeTrue();
        handler.PostBodies.Any(body => body.Contains("id=supers")).ShouldBeTrue();
        handler.PostBodies.Any(body => body.Contains("id=misc")).ShouldBeTrue();
    }

    private sealed class AjaxFrameDataHandler : HttpMessageHandler
    {
        public List<string> PostBodies { get; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            if (request.Method == HttpMethod.Get)
            {
                return CreateResponse("""
                    <html><body>
                      <div id="content_char"></div>
                      <script>loadData('content_char', '1', 'fd_normals');</script>
                    </body></html>
                    """);
            }

            var body = request.Content is null
                ? string.Empty
                : await request.Content.ReadAsStringAsync(cancellationToken);
            PostBodies.Add(body);

            if (body.Contains("id=normals"))
            {
                return CreateResponse(CreateFrameTable("Jab"));
            }

            if (body.Contains("id=specials"))
            {
                return CreateResponse(CreateFrameTable("Flash Chop"));
            }

            if (body.Contains("id=supers"))
            {
                return CreateResponse(CreateFrameTable("Hyper Bomb"));
            }

            if (body.Contains("id=misc"))
            {
                return CreateResponse("<div class=\"miscFd\">Wakeup : 67</div>");
            }

            return CreateResponse(string.Empty);
        }

        private static HttpResponseMessage CreateResponse(string html)
            => new(HttpStatusCode.OK)
            {
                Content = new StringContent(html)
            };

        private static string CreateFrameTable(string moveName)
            => $"""
                <table id="fd_table">
                  <thead>
                    <tr><th></th><th>Name</th><th>Startup</th><th>Hit</th><th>Recovery</th><th>Blk. Adv.</th><th>Hit Adv.</th></tr>
                  </thead>
                  <tbody>
                    <tr><td></td><td>{moveName}</td><td>4</td><td>2</td><td>9</td><td>1</td><td>1</td></tr>
                  </tbody>
                </table>
                """;
    }
}
