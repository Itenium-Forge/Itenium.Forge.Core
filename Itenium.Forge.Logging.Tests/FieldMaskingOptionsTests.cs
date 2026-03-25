namespace Itenium.Forge.Logging.Tests;

[TestFixture]
public class FieldMaskingOptionsTests
{
    [Test]
    public void DefaultFields_ContainsExpectedFields()
    {
        var expected = new[]
        {
            "password", "passwd", "token", "secret", "authorization",
            "client_secret", "api_key", "access_token", "refresh_token"
        };

        foreach (var field in expected)
            Assert.That(FieldMaskingOptions.DefaultFields, Contains.Item(field));
    }

    [Test]
    public void DefaultInstance_MaskedFields_EqualsDefaultFields()
    {
        var options = new FieldMaskingOptions();
        Assert.That(options.MaskedFields, Is.EquivalentTo(FieldMaskingOptions.DefaultFields));
    }

    [Test]
    public void AddFields_AppendsToDefaults()
    {
        var options = new FieldMaskingOptions();
        options.AddFields("credit_card");

        Assert.That(options.MaskedFields, Contains.Item("credit_card"));
        Assert.That(options.MaskedFields, Contains.Item("password"));
    }

    [Test]
    public void AddFields_CaseInsensitive()
    {
        var options = new FieldMaskingOptions();
        options.AddFields("CREDIT_CARD");

        Assert.That(options.MaskedFields.Contains("credit_card"), Is.True);
    }

    [Test]
    public void SetFields_ReplacesDefaults()
    {
        var options = new FieldMaskingOptions();
        options.SetFields("credit_card", "ssn");

        Assert.That(options.MaskedFields, Has.Count.EqualTo(2));
        Assert.That(options.MaskedFields, Contains.Item("credit_card"));
        Assert.That(options.MaskedFields, Contains.Item("ssn"));
        Assert.That(options.MaskedFields, Does.Not.Contain("password"));
    }

    [Test]
    public void SetFields_Empty_ProducesEmptySet()
    {
        var options = new FieldMaskingOptions();
        options.SetFields();

        Assert.That(options.MaskedFields, Is.Empty);
    }

    [Test]
    public void AddFields_MultipleCallsAccumulate()
    {
        var options = new FieldMaskingOptions();
        options.AddFields("field_a");
        options.AddFields("field_b");

        Assert.That(options.MaskedFields, Contains.Item("field_a"));
        Assert.That(options.MaskedFields, Contains.Item("field_b"));
    }
}
