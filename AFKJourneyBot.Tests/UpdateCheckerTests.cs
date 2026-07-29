using System.Net;
using System.Text;
using AFKJourneyBot.App.Updates;

namespace AFKJourneyBot.Tests;

public sealed class UpdateCheckerTests
{
    [TestCase("1.5.1", true)]
    [TestCase("1.5.2", false)]
    [TestCase("1.5.3", false)]
    public async Task CheckAsyncComparesVersions(string currentVersion, bool updateExpected)
    {
        using var handler = new StubHttpMessageHandler(_ => JsonResponse(ValidReleaseJson));
        using var client = new HttpClient(handler);
        var checker = CreateChecker(client, currentVersion);

        var update = await checker.CheckAsync();

        Assert.That(update is not null, Is.EqualTo(updateExpected));
        if (update is not null)
        {
            Assert.Multiple(() =>
            {
                Assert.That(update.CurrentVersion, Is.EqualTo(Version.Parse(currentVersion)));
                Assert.That(update.LatestVersion, Is.EqualTo(Version.Parse("1.5.2")));
                Assert.That(update.ReleaseUri.AbsoluteUri,
                    Is.EqualTo("https://github.com/jeriks31/AFKJourneyBot/releases/tag/v1.5.2"));
            });
        }
    }

    [TestCase("{")]
    [TestCase("{}")]
    [TestCase("""{"version":"invalid","release_url":"https://github.com/jeriks31/AFKJourneyBot"}""")]
    [TestCase("""{"version":"1.5.2","release_url":"not-a-url"}""")]
    public async Task CheckAsyncReturnsNoUpdateForMalformedMetadata(string json)
    {
        using var handler = new StubHttpMessageHandler(_ => JsonResponse(json));
        using var client = new HttpClient(handler);

        var update = await CreateChecker(client, "1.5.1").CheckAsync();

        Assert.That(update, Is.Null);
    }

    [Test]
    public async Task CheckAsyncReturnsNoUpdateForHttpFailure()
    {
        using var handler = new StubHttpMessageHandler(
            _ => new HttpResponseMessage(HttpStatusCode.InternalServerError));
        using var client = new HttpClient(handler);

        var update = await CreateChecker(client, "1.5.1").CheckAsync();

        Assert.That(update, Is.Null);
    }

    [Test]
    public async Task CheckAsyncReturnsNoUpdateWhenRequestTimesOut()
    {
        using var handler = new StubHttpMessageHandler(async (_, cancellationToken) =>
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            return new HttpResponseMessage(HttpStatusCode.OK);
        });
        using var client = new HttpClient(handler);
        var checker = new UpdateChecker(client, Version.Parse("1.5.1"), TimeSpan.FromMilliseconds(20));

        var update = await checker.CheckAsync();

        Assert.That(update, Is.Null);
    }

    [Test]
    public async Task CheckAsyncUsesExpectedEndpointAndUserAgent()
    {
        HttpRequestMessage? capturedRequest = null;
        using var handler = new StubHttpMessageHandler(request =>
        {
            capturedRequest = request;
            return JsonResponse(ValidReleaseJson);
        });
        using var client = new HttpClient(handler);

        await CreateChecker(client, "1.5.1").CheckAsync();

        Assert.Multiple(() =>
        {
            Assert.That(capturedRequest?.RequestUri, Is.EqualTo(UpdateChecker.LatestInfoUri));
            Assert.That(capturedRequest?.Headers.UserAgent.ToString(), Is.EqualTo("AFKJourneyBot/1.5.1"));
        });
    }

    private const string ValidReleaseJson =
        """
        {
          "version": "1.5.2",
          "release_url": "https://github.com/jeriks31/AFKJourneyBot/releases/tag/v1.5.2",
          "download_url": "https://github.com/jeriks31/AFKJourneyBot/releases/download/v1.5.2/AFKJourneyBot-win-x64.zip"
        }
        """;

    private static UpdateChecker CreateChecker(HttpClient client, string currentVersion)
        => new(client, Version.Parse(currentVersion), TimeSpan.FromSeconds(1));

    private static HttpResponseMessage JsonResponse(string json)
        => new(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json"),
        };

    private sealed class StubHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> _send;

        internal StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> send)
            : this((request, _) => Task.FromResult(send(request)))
        {
        }

        internal StubHttpMessageHandler(
            Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> send)
        {
            _send = send;
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
            => _send(request, cancellationToken);
    }
}
