using System.Linq.Expressions;

namespace Itenium.Forge.Logging;

/// <summary>
/// Declares which properties of <typeparamref name="T"/> should be masked when the object
/// is logged via Serilog destructuring (<c>{@obj}</c>).
/// Implement this interface on a dedicated masker class — not on the model itself — to keep
/// logging concerns out of the domain layer. Register the masker in DI so the
/// <c>ObjectMaskerDestructurePolicy</c> can resolve it automatically.
/// </summary>
/// <example>
/// <code>
/// // Separate masker class — keeps the domain model clean
/// public class UserProfileMasker : IObjectMasker&lt;UserProfile&gt;
/// {
///     public IEnumerable&lt;Expression&lt;Func&lt;UserProfile, object&gt;&gt;&gt; GetMaskedFields()
///     {
///         yield return obj => obj.Name;
///         yield return obj => obj.Address;
///     }
/// }
///
/// // Registration
/// services.AddSingleton&lt;IObjectMasker&lt;UserProfile&gt;, UserProfileMasker&gt;();
/// </code>
/// </example>
public interface IObjectMasker<T>
{
    IEnumerable<Expression<Func<T, object>>> GetMaskedFields();
}
