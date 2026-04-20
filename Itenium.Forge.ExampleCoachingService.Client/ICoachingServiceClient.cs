using Refit;

namespace Itenium.Forge.ExampleCoachingService.Client;

/// <summary>
/// Typed Refit client for ExampleCoachingService.
/// Register via: <c>builder.AddForgeHttpClient&lt;ICoachingServiceClient&gt;("ExampleCoachingService")</c>
/// </summary>
[Headers("Accept: application/json")]
public interface ICoachingServiceClient
{
    [Get("/coaches")]
    Task<IReadOnlyList<Coach>> GetCoachesAsync(CancellationToken ct = default);

    [Get("/coaches/{id}")]
    Task<Coach> GetCoachAsync(int id, CancellationToken ct = default);
}
