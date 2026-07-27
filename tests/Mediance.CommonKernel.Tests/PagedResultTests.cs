namespace Mediance.CommonKernel.Tests;

public class PagedResultTests
{
    [Fact]
    public void TotalPages_WithExactFit_ReturnsCorrectValue()
    {
        var result = new PagedResult<int>([], 1, 10, 100);
        Assert.Equal(10, result.TotalPages);
    }

    [Fact]
    public void TotalPages_WithPartialPage_ReturnsCeiling()
    {
        var result = new PagedResult<int>([], 1, 10, 101);
        Assert.Equal(11, result.TotalPages);
    }

    [Fact]
    public void TotalPages_WithZeroPageSize_ReturnsZero()
    {
        var result = new PagedResult<int>([], 1, 0, 100);
        Assert.Equal(0, result.TotalPages);
    }

    [Fact]
    public void TotalPages_WithNoItems_ReturnsZero()
    {
        var result = new PagedResult<int>([], 1, 20, 0);
        Assert.Equal(0, result.TotalPages);
    }

    [Fact]
    public void HasNextPage_WhenMorePagesExist_ReturnsTrue()
    {
        var result = new PagedResult<int>([], 1, 10, 100);
        Assert.True(result.HasNextPage);
    }

    [Fact]
    public void HasNextPage_OnLastPage_ReturnsFalse()
    {
        var result = new PagedResult<int>([], 10, 10, 100);
        Assert.False(result.HasNextPage);
    }

    [Fact]
    public void HasNextPage_WithZeroTotal_ReturnsFalse()
    {
        var result = new PagedResult<int>([], 1, 10, 0);
        Assert.False(result.HasNextPage);
    }

    [Fact]
    public void Items_Property_StoresPassedValues()
    {
        var items = new List<int> { 1, 2, 3 };
        var result = new PagedResult<int>(items, 1, 10, 50);
        Assert.Equal(items, result.items);
    }

    [Fact]
    public void Page_And_PageSize_Properties_StoredCorrectly()
    {
        var result = new PagedResult<int>([], 3, 25, 200);
        Assert.Equal(3, result.page);
        Assert.Equal(25, result.pageSize);
    }
}
