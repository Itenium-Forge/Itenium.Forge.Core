using System.Net.Http.Headers;
using System.Net.Http.Json;
using Itenium.Forge.ExampleCoachingService.Client;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Itenium.Forge.ExampleApp.Tests;

/// <summary>
/// Verifies ExampleApp's <see cref="Itenium.Forge.ExampleApp.Controllers.CoachingController"/>
/// correctly proxies requests to <see cref="ICoachingServiceClient"/>.
///
/// <see cref="ICoachingServiceClient"/> is replaced with an in-memory stub — the tests exercise
/// the controller's routing and serialisation, not downstream network behaviour
/// (that is covered by <c>CoachingFlowTests</c> in Itenium.Forge.HttpClient.Tests).
/// </summary>
[TestFixture]
public class CoachingControllerTests
{
    private WebApplicationFactory<Program> _factory = null!;
    private HttpClient _client = null!;

    [OneTimeSetUp]
    public async Task OneTimeSetUp()
    {
        _factory = new ExampleAppFactory().WithWebHostBuilder(b =>
            b.ConfigureServices(services =>
            {
                services.RemoveAll<ICoachingServiceClient>();
                services.AddSingleton<ICoachingServiceClient>(new StubCoachingServiceClient());
            }));

        _client = _factory.CreateClient();

        var token = await TokenHelper.GetUserTokenAsync(_client);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    [Test]
    public async Task GetAll_ReturnsCoachListFromDownstreamClient()
    {
        var coaches = await _client.GetFromJsonAsync<Coach[]>("/api/coaching");

        Assert.That(coaches, Is.Not.Null);
        Assert.That(coaches!.Length, Is.EqualTo(2));
    }

    [Test]
    public async Task Get_KnownId_ReturnsMatchingCoach()
    {
        var coach = await _client.GetFromJsonAsync<Coach>("/api/coaching/1");

        Assert.That(coach, Is.Not.Null);
        Assert.That(coach!.Id, Is.EqualTo(1));
        Assert.That(coach.Name, Is.EqualTo("Alice"));
    }

    private sealed class StubCoachingServiceClient : ICoachingServiceClient
    {
        private static readonly IReadOnlyList<Coach> Coaches =
        [
            new Coach(1, "Alice", "DDD", Available: true),
            new Coach(2, "Bob", "TDD", Available: false),
        ];

        public Task<IReadOnlyList<Coach>> GetCoachesAsync(CancellationToken ct = default)
            => Task.FromResult(Coaches);

        public Task<Coach> GetCoachAsync(int id, CancellationToken ct = default)
            => Task.FromResult(Coaches.First(c => c.Id == id));
    }
}
