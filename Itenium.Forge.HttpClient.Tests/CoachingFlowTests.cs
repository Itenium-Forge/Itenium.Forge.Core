using Itenium.Forge.ExampleCoachingService.Client;
using Refit;

namespace Itenium.Forge.HttpClients.Tests;

/// <summary>
/// End-to-end flow: Refit ICoachingServiceClient → ExampleCoachingService running in-process.
/// No real network socket involved.
/// </summary>
[TestFixture]
public class CoachingFlowTests
{
    private ExampleCoachingServiceFactory _factory = null!;
    private ICoachingServiceClient _client = null!;

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        _factory = new ExampleCoachingServiceFactory();
        _client = RestService.For<ICoachingServiceClient>(_factory.CreateClient());
    }

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        _factory.Dispose();
    }

    [Test]
    public async Task GetCoachesAsync_ReturnsCoachesFromService()
    {
        var coaches = await _client.GetCoachesAsync();

        Assert.That(coaches, Is.Not.Null);
        Assert.That(coaches.Count, Is.GreaterThan(0));
    }

    [Test]
    public async Task GetCoachAsync_KnownId_ReturnsMatchingCoach()
    {
        var coach = await _client.GetCoachAsync(1);

        Assert.That(coach, Is.Not.Null);
        Assert.That(coach.Id, Is.EqualTo(1));
        Assert.That(coach.Name, Is.Not.Empty);
    }

    [Test]
    public void GetCoachAsync_UnknownId_ThrowsApiException()
    {
        // Refit surfaces non-2xx responses as ApiException (or a subclass such as ValidationApiException)
        Assert.That(
            async () => await _client.GetCoachAsync(99999),
            Throws.InstanceOf<ApiException>());
    }
}
