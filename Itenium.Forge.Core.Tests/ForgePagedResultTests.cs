using Itenium.Forge.Core;

namespace Itenium.Forge.Core.Tests;

[TestFixture]
public class ForgePagedResultTests
{
    // ---------- stored values ----------

    [Test]
    public void Constructor_StoresPagePageSizeAndTotalCount()
    {
        var result = new ForgePagedResult<int>([1, 2, 3], totalCount: 10, page: 2, pageSize: 3);

        Assert.That(result.Page, Is.EqualTo(2));
        Assert.That(result.PageSize, Is.EqualTo(3));
        Assert.That(result.TotalCount, Is.EqualTo(10));
    }

    [Test]
    public void Items_ContainsAllProvidedItems()
    {
        var result = new ForgePagedResult<string>(["a", "b", "c"], totalCount: 3, page: 1, pageSize: 20);

        Assert.That(result.Items, Is.EqualTo(new[] { "a", "b", "c" }));
    }

    [Test]
    public void Items_IsEmptyWhenNoItemsProvided()
    {
        var result = new ForgePagedResult<int>([], totalCount: 0, page: 1, pageSize: 20);

        Assert.That(result.Items, Is.Empty);
    }

    // ---------- TotalPages ----------

    [Test]
    public void TotalPages_RoundsUpWhenItemsDoNotFillLastPage()
    {
        var result = new ForgePagedResult<int>([], totalCount: 7, page: 1, pageSize: 3);

        Assert.That(result.TotalPages, Is.EqualTo(3));
    }

    [Test]
    public void TotalPages_IsExactWhenItemsDivideEvenly()
    {
        var result = new ForgePagedResult<int>([], totalCount: 6, page: 1, pageSize: 3);

        Assert.That(result.TotalPages, Is.EqualTo(2));
    }

    [Test]
    public void TotalPages_IsOneWhenAllItemsFitOnASinglePage()
    {
        var result = new ForgePagedResult<int>([], totalCount: 5, page: 1, pageSize: 20);

        Assert.That(result.TotalPages, Is.EqualTo(1));
    }

    [Test]
    public void TotalPages_IsZeroWhenTotalCountIsZero()
    {
        var result = new ForgePagedResult<int>([], totalCount: 0, page: 1, pageSize: 20);

        Assert.That(result.TotalPages, Is.EqualTo(0));
    }

    // ---------- HasPreviousPage ----------

    [Test]
    public void HasPreviousPage_IsFalseOnFirstPage()
    {
        var result = new ForgePagedResult<int>([], totalCount: 100, page: 1, pageSize: 20);

        Assert.That(result.HasPreviousPage, Is.False);
    }

    [Test]
    public void HasPreviousPage_IsTrueOnSecondPage()
    {
        var result = new ForgePagedResult<int>([], totalCount: 100, page: 2, pageSize: 20);

        Assert.That(result.HasPreviousPage, Is.True);
    }

    // ---------- HasNextPage ----------

    [Test]
    public void HasNextPage_IsTrueWhenNotOnLastPage()
    {
        var result = new ForgePagedResult<int>([], totalCount: 100, page: 1, pageSize: 20);

        Assert.That(result.HasNextPage, Is.True);
    }

    [Test]
    public void HasNextPage_IsFalseOnLastPage()
    {
        var result = new ForgePagedResult<int>([], totalCount: 100, page: 5, pageSize: 20);

        Assert.That(result.HasNextPage, Is.False);
    }

    [Test]
    public void HasNextPage_IsFalseWhenTotalCountIsZero()
    {
        var result = new ForgePagedResult<int>([], totalCount: 0, page: 1, pageSize: 20);

        Assert.That(result.HasNextPage, Is.False);
    }

    // ---------- single-page result ----------

    [Test]
    public void SinglePage_HasNeitherPreviousNorNextPage()
    {
        var result = new ForgePagedResult<int>([1, 2, 3], totalCount: 3, page: 1, pageSize: 20);

        Assert.That(result.HasPreviousPage, Is.False);
        Assert.That(result.HasNextPage, Is.False);
    }

    // ---------- constructor validation ----------

    [Test]
    public void Constructor_ThrowsWhenItemsIsNull()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new ForgePagedResult<int>(null!, totalCount: 0, page: 1, pageSize: 20));
    }

    [Test]
    public void Constructor_ThrowsWhenPageIsZero()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new ForgePagedResult<int>([], totalCount: 0, page: 0, pageSize: 20));
    }

    [Test]
    public void Constructor_ThrowsWhenPageIsNegative()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new ForgePagedResult<int>([], totalCount: 0, page: -1, pageSize: 20));
    }

    [Test]
    public void Constructor_ThrowsWhenPageSizeIsZero()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new ForgePagedResult<int>([], totalCount: 0, page: 1, pageSize: 0));
    }

    [Test]
    public void Constructor_ThrowsWhenPageSizeIsNegative()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new ForgePagedResult<int>([], totalCount: 0, page: 1, pageSize: -1));
    }

    [Test]
    public void Constructor_ThrowsWhenTotalCountIsNegative()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new ForgePagedResult<int>([], totalCount: -1, page: 1, pageSize: 20));
    }
}
