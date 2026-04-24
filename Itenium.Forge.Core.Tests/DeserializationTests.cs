using System.Text.Json;
using Itenium.Forge.Core;

namespace Itenium.Forge.Core.Tests;

[TestFixture]
public class DeserializationTests
{
    [Test]
    public void ForgePagedResult_ShouldDeserializeCorrectly()
    {
        // Arrange
        var json = "{\"items\":[{\"name\":\"dark-mode\",\"enabled\":true}],\"page\":{\"page\":1,\"pageSize\":5,\"totalCount\":15,\"totalPages\":3,\"hasNextPage\":true}}";
        
        // Use default options (case-sensitive by default, but STJ handles camelCase -> PascalCase for properties usually)
        // However, constructor parameters are more strict.
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        // Act
        var result = JsonSerializer.Deserialize<ForgePagedResult<TestFlag>>(json, options);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Items, Has.Count.EqualTo(1));
        Assert.That(result.Items[0].Name, Is.EqualTo("dark-mode"));
        Assert.That(result.Page.Page, Is.EqualTo(1));
        Assert.That(result.Page.TotalCount, Is.EqualTo(15));
    }

    public record TestFlag(string Name, bool Enabled);
}
