using System.ComponentModel.DataAnnotations;

namespace Itenium.Forge.HttpClients;

/// <summary>
/// Bound from the <c>ForgeConfiguration</c> section.
/// Each key under <c>HttpClients</c> is the logical client name (e.g. <c>CoachingService</c>).
/// </summary>
public sealed class HttpClientsOptions : IValidatableObject
{
    public IDictionary<string, HttpClientEntryOptions> HttpClients { get; set; } = new Dictionary<string, HttpClientEntryOptions>();

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        foreach (var (name, entry) in HttpClients)
        {
            var context = new ValidationContext(entry) { MemberName = $"HttpClients[{name}]" };
            var results = new List<ValidationResult>();
            Validator.TryValidateObject(entry, context, results, validateAllProperties: true);
            foreach (var result in results)
                yield return result;
        }
    }
}
