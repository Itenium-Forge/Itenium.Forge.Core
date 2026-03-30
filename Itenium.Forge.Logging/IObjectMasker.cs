using System.Linq.Expressions;

namespace Itenium.Forge.Logging;

/// <summary>
/// Declares which properties of <typeparamref name="T"/> should be masked when the object
/// is logged via Serilog destructuring (<c>{@obj}</c>).
/// Implement this interface on model classes that contain fields that are sensitive only
/// for that specific type — for example GDPR fields such as <c>Name</c> or <c>Address</c>
/// that are not in the global <see cref="FieldMaskingOptions.MaskedFields"/> blocklist.
/// </summary>
/// <example>
/// <code>
/// public class UserProfile : IObjectMasker&lt;UserProfile&gt;
/// {
///     public string Name { get; set; }
///     public string Address { get; set; }
///
///     public IEnumerable&lt;Expression&lt;Func&lt;UserProfile, object&gt;&gt;&gt; GetMaskedFields()
///     {
///         yield return obj => obj.Name;
///         yield return obj => obj.Address;
///     }
/// }
/// </code>
/// </example>
public interface IObjectMasker<T>
{
    IEnumerable<Expression<Func<T, object>>> GetMaskedFields();
}
