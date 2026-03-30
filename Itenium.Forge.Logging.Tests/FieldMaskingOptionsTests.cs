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
    public void AddMaskedFields_AppendsToDefaults()
    {
        var options = new FieldMaskingOptions();
        options.AddMaskedFields("credit_card");

        Assert.That(options.MaskedFields, Contains.Item("credit_card"));
        Assert.That(options.MaskedFields, Contains.Item("password"));
    }

    [Test]
    public void AddMaskedFields_CaseInsensitive()
    {
        var options = new FieldMaskingOptions();
        options.AddMaskedFields("CREDIT_CARD");

        Assert.That(options.MaskedFields.Contains("credit_card"), Is.True);
    }

    [Test]
    public void SetMaskedFields_ReplacesDefaults()
    {
        var options = new FieldMaskingOptions();
        options.SetMaskedFields("credit_card", "ssn");

        Assert.That(options.MaskedFields, Has.Count.EqualTo(2));
        Assert.That(options.MaskedFields, Contains.Item("credit_card"));
        Assert.That(options.MaskedFields, Contains.Item("ssn"));
        Assert.That(options.MaskedFields, Does.Not.Contain("password"));
    }

    [Test]
    public void SetMaskedFields_Empty_ProducesEmptySet()
    {
        var options = new FieldMaskingOptions();
        options.SetMaskedFields();

        Assert.That(options.MaskedFields, Is.Empty);
    }

    [Test]
    public void AddMaskedFields_MultipleCallsAccumulate()
    {
        var options = new FieldMaskingOptions();
        options.AddMaskedFields("field_a");
        options.AddMaskedFields("field_b");

        Assert.That(options.MaskedFields, Contains.Item("field_a"));
        Assert.That(options.MaskedFields, Contains.Item("field_b"));
    }

    // ---------- Masked headers ----------

    [Test]
    public void DefaultMaskedHeaders_ContainsExpectedHeaders()
    {
        var expected = new[] { "Authorization", "Cookie", "Set-Cookie", "X-Api-Key" };
        foreach (var header in expected)
            Assert.That(FieldMaskingOptions.DefaultMaskedHeaders, Contains.Item(header));
    }

    [Test]
    public void DefaultInstance_MaskedHeaders_EqualsDefaultMaskedHeaders()
    {
        var options = new FieldMaskingOptions();
        Assert.That(options.MaskedHeaders, Is.EquivalentTo(FieldMaskingOptions.DefaultMaskedHeaders));
    }

    [Test]
    public void AddMaskedHeaders_AppendsToDefaults()
    {
        var options = new FieldMaskingOptions();
        options.AddMaskedHeaders("X-Custom-Token");

        Assert.That(options.MaskedHeaders, Contains.Item("X-Custom-Token"));
        Assert.That(options.MaskedHeaders, Contains.Item("Authorization"));
    }

    [Test]
    public void SetMaskedHeaders_ReplacesDefaults()
    {
        var options = new FieldMaskingOptions();
        options.SetMaskedHeaders("X-Custom-Token");

        Assert.That(options.MaskedHeaders, Has.Count.EqualTo(1));
        Assert.That(options.MaskedHeaders, Contains.Item("X-Custom-Token"));
        Assert.That(options.MaskedHeaders, Does.Not.Contain("Authorization"));
    }

    // ---------- Allowed headers ----------

    [Test]
    public void DefaultAllowedHeaders_ContainsExpectedHeaders()
    {
        var expected = new[] { "Content-Type", "Accept", "X-Forwarded-For", "traceparent" };
        foreach (var header in expected)
            Assert.That(FieldMaskingOptions.DefaultAllowedHeaders, Contains.Item(header));
    }

    [Test]
    public void DefaultInstance_AllowedHeaders_EqualsDefaultAllowedHeaders()
    {
        var options = new FieldMaskingOptions();
        Assert.That(options.AllowedHeaders, Is.EquivalentTo(FieldMaskingOptions.DefaultAllowedHeaders));
    }

    [Test]
    public void AddAllowedHeaders_AppendsToDefaults()
    {
        var options = new FieldMaskingOptions();
        options.AddAllowedHeaders("X-Custom-Header");

        Assert.That(options.AllowedHeaders, Contains.Item("X-Custom-Header"));
        Assert.That(options.AllowedHeaders, Contains.Item("Content-Type"));
    }

    [Test]
    public void SetAllowedHeaders_ReplacesDefaults()
    {
        var options = new FieldMaskingOptions();
        options.SetAllowedHeaders("X-Custom-Header");

        Assert.That(options.AllowedHeaders, Has.Count.EqualTo(1));
        Assert.That(options.AllowedHeaders, Contains.Item("X-Custom-Header"));
        Assert.That(options.AllowedHeaders, Does.Not.Contain("Content-Type"));
    }

    // ---------- AllowedHeaders + MaskedHeaders interaction ----------

    [Test]
    public void Header_NotInAllowedHeaders_IsNotLogged()
    {
        // Authorization is in MaskedHeaders but not in AllowedHeaders by default
        var options = new FieldMaskingOptions();
        Assert.That(options.AllowedHeaders, Does.Not.Contain("Authorization"));
    }

    [Test]
    public void Header_InAllowedHeaders_NotInMaskedHeaders_IsLoggedWithRealValue()
    {
        // Content-Type is allowed but not sensitive
        var options = new FieldMaskingOptions();
        Assert.That(options.AllowedHeaders, Contains.Item("Content-Type"));
        Assert.That(options.MaskedHeaders, Does.Not.Contain("Content-Type"));
    }

    [Test]
    public void Header_InAllowedHeaders_AndInMaskedHeaders_IsLoggedAsMasked()
    {
        // A header explicitly allowed AND masked → logged as ***
        var options = new FieldMaskingOptions();
        options.AddAllowedHeaders("Authorization");

        Assert.That(options.AllowedHeaders, Contains.Item("Authorization"));
        Assert.That(options.MaskedHeaders, Contains.Item("Authorization"));
    }

    [Test]
    public void Header_InMaskedHeaders_ButNotInAllowedHeaders_IsNeverLogged()
    {
        // Being in MaskedHeaders alone is not enough to cause logging
        var options = new FieldMaskingOptions();
        options.AddMaskedHeaders("Authorization");

        Assert.That(options.MaskedHeaders, Contains.Item("Authorization"));
        Assert.That(options.AllowedHeaders, Does.Not.Contain("Authorization"));
    }
}
