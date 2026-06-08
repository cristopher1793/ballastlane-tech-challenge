using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using TaskApp.Application.DTOs.Auth;

namespace TaskApp.Tests.Integration;

public class RateLimiterTests : IClassFixture<ApiTestFactory>
{
    private readonly ApiTestFactory _factory;

    public RateLimiterTests(ApiTestFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task AuthEndpoint_ExceedsLimit_Returns429()
    {
        HttpClient client = _factory.CreateClient();
        LoginRequestDto dto = new LoginRequestDto { Email = "nope@example.com", Password = "Wrong1!" };

        HttpResponseMessage? lastResponse = null;
        for (int i = 0; i < 15; i++)
        {
            lastResponse = await client.PostAsJsonAsync("/api/auth/login", dto);
            if (lastResponse.StatusCode == HttpStatusCode.TooManyRequests)
                break;
        }

        Assert.Equal(HttpStatusCode.TooManyRequests, lastResponse!.StatusCode);
    }

    [Fact]
    public async Task RateLimited_Response_HasJsonContentType()
    {
        HttpClient client = _factory.CreateClient();
        LoginRequestDto dto = new LoginRequestDto { Email = "nope@example.com", Password = "Wrong1!" };

        HttpResponseMessage? tooManyResponse = null;
        for (int i = 0; i < 15; i++)
        {
            HttpResponseMessage resp = await client.PostAsJsonAsync("/api/auth/login", dto);
            if (resp.StatusCode == HttpStatusCode.TooManyRequests)
            {
                tooManyResponse = resp;
                break;
            }
        }

        Assert.NotNull(tooManyResponse);
        Assert.Contains("application/json", tooManyResponse!.Content.Headers.ContentType?.MediaType ?? string.Empty);
    }

    [Fact]
    public async Task RateLimited_Response_BodyMatchesExpectedFormat()
    {
        HttpClient client = _factory.CreateClient();
        LoginRequestDto dto = new LoginRequestDto { Email = "nope@example.com", Password = "Wrong1!" };

        HttpResponseMessage? tooManyResponse = null;
        for (int i = 0; i < 15; i++)
        {
            HttpResponseMessage resp = await client.PostAsJsonAsync("/api/auth/login", dto);
            if (resp.StatusCode == HttpStatusCode.TooManyRequests)
            {
                tooManyResponse = resp;
                break;
            }
        }

        Assert.NotNull(tooManyResponse);
        string body = await tooManyResponse!.Content.ReadAsStringAsync();
        JsonDocument doc = JsonDocument.Parse(body);
        Assert.True(doc.RootElement.TryGetProperty("error", out JsonElement errorProp));
        Assert.Contains("Too many requests", errorProp.GetString() ?? string.Empty, StringComparison.OrdinalIgnoreCase);
    }
}
