using Serilog.Core;
using Serilog.Events;
using System.Linq.Expressions;

namespace Itenium.Forge.Logging.Tests;

[TestFixture]
public class ObjectMaskerDestructurePolicyTests
{
    private static ObjectMaskerDestructurePolicy Build(
        Action<FieldMaskingOptions>? configure = null,
        Action<SimpleServiceProvider>? registerMaskers = null)
    {
        var options = new FieldMaskingOptions();
        configure?.Invoke(options);

        var sp = new SimpleServiceProvider();
        registerMaskers?.Invoke(sp);

        return new ObjectMaskerDestructurePolicy(options, sp);
    }

    private static Dictionary<string, object?> Destructure(ObjectMaskerDestructurePolicy policy, object value)
    {
        policy.TryDestructure(value, new SimpleFactory(), out var result);
        return ((StructureValue)result).Properties
            .ToDictionary(p => p.Name, p => ((ScalarValue)p.Value).Value);
    }

    // ---------- blocklist (MaskedFields) ----------

    [Test]
    public void TryDestructure_TypeWithNoSensitiveFields_ReturnsFalse()
    {
        var policy = Build();
        var result = policy.TryDestructure(new { Name = "Alice", Age = 30 }, new SimpleFactory(), out _);
        Assert.That(result, Is.False);
    }

    [Test]
    public void TryDestructure_BlocklistedProperty_IsMasked()
    {
        var policy = Build();
        var props = Destructure(policy, new { Username = "alice", Password = "s3cret" });
        Assert.That(props["Password"], Is.EqualTo("***"));
        Assert.That(props["Username"], Is.EqualTo("alice"));
    }

    [Test]
    public void TryDestructure_BlocklistMatchIsCaseInsensitive()
    {
        var policy = Build();
        var props = Destructure(policy, new { PASSWORD = "s3cret" });
        Assert.That(props["PASSWORD"], Is.EqualTo("***"));
    }

    [Test]
    public void TryDestructure_CustomBlocklistedField_IsMasked()
    {
        var policy = Build(o => o.AddMaskedFields("iban"));
        var props = Destructure(policy, new { Iban = "BE71096123456769", Name = "Alice" });
        Assert.That(props["Iban"], Is.EqualTo("***"));
        Assert.That(props["Name"], Is.EqualTo("Alice"));
    }

    // ---------- IObjectMasker<T> via DI ----------

    [Test]
    public void TryDestructure_RegisteredMasker_MasksSpecifiedFields()
    {
        var policy = Build(registerMaskers: sp =>
            sp.Register<IObjectMasker<UserProfile>>(new UserProfileMasker()));

        var props = Destructure(policy, new UserProfile { Name = "Alice", Address = "123 Street" });
        Assert.That(props["Name"], Is.EqualTo("***"));
        Assert.That(props["Address"], Is.EqualTo("***"));
    }

    [Test]
    public void TryDestructure_RegisteredMasker_NonMaskedField_IsLogged()
    {
        var policy = Build(registerMaskers: sp =>
            sp.Register<IObjectMasker<UserProfile>>(new UserProfileMasker()));

        var props = Destructure(policy, new UserProfile { Email = "alice@example.com" });
        Assert.That(props["Email"], Is.EqualTo("alice@example.com"));
    }

    // ---------- combined ----------

    [Test]
    public void TryDestructure_BothBlocklistAndRegisteredMasker_BothMasked()
    {
        var policy = Build(registerMaskers: sp =>
            sp.Register<IObjectMasker<UserProfile>>(new UserProfileMasker()));

        var props = Destructure(policy, new UserProfile { Name = "Alice", Password = "s3cret" });
        Assert.That(props["Name"], Is.EqualTo("***"));      // IObjectMasker<T>
        Assert.That(props["Password"], Is.EqualTo("***"));  // blocklist
    }

    // ---------- helpers ----------

    private class UserProfile
    {
        public string? Name { get; set; }
        public string? Address { get; set; }
        public string? Email { get; set; }
        public string? Password { get; set; }
    }

    private class UserProfileMasker : IObjectMasker<UserProfile>
    {
        public IEnumerable<Expression<Func<UserProfile, object>>> GetMaskedFields()
        {
            yield return obj => obj.Name!;
            yield return obj => obj.Address!;
        }
    }

    internal class SimpleServiceProvider : IServiceProvider
    {
        private readonly Dictionary<Type, object> _services = new();

        public void Register<T>(T instance) where T : class => _services[typeof(T)] = instance;

        public object? GetService(Type serviceType)
            => _services.TryGetValue(serviceType, out var service) ? service : null;
    }

    private class SimpleFactory : ILogEventPropertyValueFactory
    {
        public LogEventPropertyValue CreatePropertyValue(object? value, bool destructureObjects = false)
            => new ScalarValue(value);
    }
}
