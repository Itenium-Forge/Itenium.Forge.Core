using Serilog.Core;
using Serilog.Events;
using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;

namespace Itenium.Forge.Logging;

/// <summary>
/// Serilog destructuring policy that masks sensitive property values when objects are logged
/// with <c>{@obj}</c>. Masking is applied from two sources:
/// <list type="bullet">
///   <item><see cref="FieldMaskingOptions.MaskedFields"/> — global name-based blocklist applied to all types.</item>
///   <item><see cref="IObjectMasker{T}"/> — type-specific field selector implemented on the model class.</item>
/// </list>
/// Property name matching is case-insensitive. Masked values are replaced with <c>***</c>.
/// Type reflection is performed once per type and cached.
/// </summary>
internal class ObjectMaskerDestructurePolicy : IDestructuringPolicy
{
    private readonly FieldMaskingOptions _options;
    // null = type has no masked properties, string[] = property names to mask
    private readonly ConcurrentDictionary<Type, string[]?> _cache = new();

    public ObjectMaskerDestructurePolicy(FieldMaskingOptions options) => _options = options;

    public bool TryDestructure(object value, ILogEventPropertyValueFactory factory, out LogEventPropertyValue result)
    {
        var type = value.GetType();
        var maskedNames = _cache.GetOrAdd(type, t => ResolveMaskedNames(t, value));

        if (maskedNames is null)
        {
            result = null!;
            return false;
        }

        var maskedSet = new HashSet<string>(maskedNames, StringComparer.OrdinalIgnoreCase);
        var props = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Select(p => new LogEventProperty(
                p.Name,
                maskedSet.Contains(p.Name)
                    ? new ScalarValue("***")
                    : factory.CreatePropertyValue(p.GetValue(value), true)));

        result = new StructureValue(props);
        return true;
    }

    private string[]? ResolveMaskedNames(Type type, object instance)
    {
        var names = new List<string>();

        // Global blocklist — property names matching FieldMaskingOptions.MaskedFields
        foreach (var p in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            if (_options.MaskedFields.Contains(p.Name))
                names.Add(p.Name);

        // Type-specific fields from IObjectMasker<T>
        var maskerType = typeof(IObjectMasker<>).MakeGenericType(type);
        if (maskerType.IsAssignableFrom(type))
        {
            var method = maskerType.GetMethod("GetMaskedFields")!;
            var expressions = (IEnumerable<LambdaExpression>)method.Invoke(instance, null)!;
            foreach (var expr in expressions)
                names.Add(GetMemberName(expr.Body));
        }

        return names.Count > 0 ? names.ToArray() : null;
    }

    private static string GetMemberName(Expression expr) => expr switch
    {
        MemberExpression m => m.Member.Name,
        UnaryExpression u => GetMemberName(u.Operand),
        _ => throw new ArgumentException($"Unsupported expression type: {expr.GetType().Name}")
    };
}
