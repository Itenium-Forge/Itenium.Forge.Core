using Itenium.Forge.Core;

namespace Itenium.Forge.Core.Tests;

[TestFixture]
public class ForgePageQueryTests
{
    [Test]
    public void DefaultPage_IsOne()
    {
        var query = new ForgePageQuery();

        Assert.That(query.Page, Is.EqualTo(1));
    }

    [Test]
    public void DefaultPageSize_IsTwenty()
    {
        var query = new ForgePageQuery();

        Assert.That(query.PageSize, Is.EqualTo(20));
    }
}
