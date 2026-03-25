namespace Itenium.Forge.Logging.Tests;

[TestFixture]
public class FieldMaskerTests
{
    private static readonly IReadOnlySet<string> DefaultFields = FieldMaskingOptions.DefaultFields;

    // ---------- MaskJsonBody — guard conditions ----------

    [Test]
    public void MaskJsonBody_EmptyString_ReturnsEmpty()
    {
        var result = FieldMasker.MaskJsonBody("", DefaultFields);
        Assert.That(result, Is.Empty);
    }

    [Test]
    public void MaskJsonBody_WhitespaceString_ReturnsWhitespace()
    {
        var result = FieldMasker.MaskJsonBody("   ", DefaultFields);
        Assert.That(result, Is.EqualTo("   "));
    }

    [Test]
    public void MaskJsonBody_NoMaskedFields_ReturnsUnchanged()
    {
        const string json = """{"password":"secret123"}""";
        var result = FieldMasker.MaskJsonBody(json, new HashSet<string>());
        Assert.That(result, Is.EqualTo(json));
    }

    [Test]
    public void MaskJsonBody_InvalidJson_ReturnsUnchanged()
    {
        const string notJson = "this is not json";
        var result = FieldMasker.MaskJsonBody(notJson, DefaultFields);
        Assert.That(result, Is.EqualTo(notJson));
    }

    [Test]
    public void MaskJsonBody_PlainStringJson_ReturnsUnchanged()
    {
        const string json = "\"just a string\"";
        var result = FieldMasker.MaskJsonBody(json, DefaultFields);
        // A quoted string is valid JSON but has no keys to mask
        Assert.That(result, Does.Not.Contain("***"));
    }

    [Test]
    public void MaskJsonBody_JsonNullLiteral_ReturnsUnchanged()
    {
        // JsonNode.Parse("null") returns null — must not throw, must return input
        var result = FieldMasker.MaskJsonBody("null", DefaultFields);
        Assert.That(result, Is.EqualTo("null"));
    }

    // ---------- MaskJsonBody — top-level masking ----------

    [TestCase("password")]
    [TestCase("passwd")]
    [TestCase("token")]
    [TestCase("secret")]
    [TestCase("authorization")]
    [TestCase("client_secret")]
    [TestCase("api_key")]
    [TestCase("access_token")]
    [TestCase("refresh_token")]
    public void MaskJsonBody_DefaultField_IsMasked(string fieldName)
    {
        var json = $$"""{"{{fieldName}}":"sensitive-value"}""";
        var result = FieldMasker.MaskJsonBody(json, DefaultFields);
        Assert.That(result, Does.Contain("***"));
        Assert.That(result, Does.Not.Contain("sensitive-value"));
    }

    [Test]
    public void MaskJsonBody_UnrelatedField_IsNotMasked()
    {
        const string json = """{"username":"alice","email":"alice@example.com"}""";
        var result = FieldMasker.MaskJsonBody(json, DefaultFields);
        Assert.That(result, Does.Contain("alice"));
        Assert.That(result, Does.Contain("alice@example.com"));
    }

    [Test]
    public void MaskJsonBody_MaskedFieldIsNull_IsStillMasked()
    {
        const string json = """{"password":null}""";
        var result = FieldMasker.MaskJsonBody(json, DefaultFields);
        // null values at a masked key are replaced to avoid leaking absence-of-value signal
        Assert.That(result, Does.Contain("***"));
    }

    [Test]
    public void MaskJsonBody_MixedFields_OnlySensitiveMasked()
    {
        const string json = """{"username":"alice","password":"s3cret","email":"alice@example.com"}""";
        var result = FieldMasker.MaskJsonBody(json, DefaultFields);
        Assert.That(result, Does.Contain("alice"));
        Assert.That(result, Does.Contain("alice@example.com"));
        Assert.That(result, Does.Contain("***"));
        Assert.That(result, Does.Not.Contain("s3cret"));
    }

    // ---------- MaskJsonBody — case-insensitive ----------

    [TestCase("PASSWORD")]
    [TestCase("Password")]
    [TestCase("pAsSwOrD")]
    public void MaskJsonBody_CaseInsensitiveField_IsMasked(string fieldName)
    {
        var json = $$"""{"{{fieldName}}":"value"}""";
        var result = FieldMasker.MaskJsonBody(json, DefaultFields);
        Assert.That(result, Does.Contain("***"));
    }

    // ---------- MaskJsonBody — recursive / nested ----------

    [Test]
    public void MaskJsonBody_NestedObject_DeepFieldIsMasked()
    {
        const string json = """{"user":{"name":"alice","password":"secret"}}""";
        var result = FieldMasker.MaskJsonBody(json, DefaultFields);
        Assert.That(result, Does.Contain("alice"));
        Assert.That(result, Does.Contain("***"));
        Assert.That(result, Does.Not.Contain("secret"));
    }

    [Test]
    public void MaskJsonBody_ArrayOfObjects_FieldMaskedInEachElement()
    {
        const string json = """[{"password":"a"},{"password":"b"},{"name":"alice"}]""";
        var result = FieldMasker.MaskJsonBody(json, DefaultFields);
        Assert.That(result, Does.Not.Contain("\"a\""));
        Assert.That(result, Does.Not.Contain("\"b\""));
        Assert.That(result, Does.Contain("alice"));
        Assert.That(result.Split("***").Length - 1, Is.EqualTo(2));
    }

    [Test]
    public void MaskJsonBody_DeeplyNested_AllMatchesMasked()
    {
        const string json = """{"a":{"b":{"c":{"password":"deep"}}}}""";
        var result = FieldMasker.MaskJsonBody(json, DefaultFields);
        Assert.That(result, Does.Contain("***"));
        Assert.That(result, Does.Not.Contain("deep"));
    }

    // ---------- MaskQueryParams ----------

    [Test]
    public void MaskQueryParams_EmptyMaskedFields_ReturnsUnchanged()
    {
        var query = new Dictionary<string, string> { ["api_key"] = "key123" };
        var result = FieldMasker.MaskQueryParams(query, new HashSet<string>());
        Assert.That(result["api_key"], Is.EqualTo("key123"));
    }

    [Test]
    public void MaskQueryParams_SensitiveKey_ValueReplaced()
    {
        var query = new Dictionary<string, string> { ["api_key"] = "key123", ["page"] = "1" };
        var result = FieldMasker.MaskQueryParams(query, DefaultFields);
        Assert.That(result["api_key"], Is.EqualTo("***"));
        Assert.That(result["page"], Is.EqualTo("1"));
    }

    [Test]
    public void MaskQueryParams_CaseInsensitiveKey_ValueReplaced()
    {
        var query = new Dictionary<string, string> { ["API_KEY"] = "key123" };
        var result = FieldMasker.MaskQueryParams(query, DefaultFields);
        Assert.That(result["API_KEY"], Is.EqualTo("***"));
    }

    [Test]
    public void MaskQueryParams_DoesNotMutateOriginalDictionary()
    {
        var query = new Dictionary<string, string> { ["password"] = "original" };
        _ = FieldMasker.MaskQueryParams(query, DefaultFields);
        Assert.That(query["password"], Is.EqualTo("original"));
    }
}
