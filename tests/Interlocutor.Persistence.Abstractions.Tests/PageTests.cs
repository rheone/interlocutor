namespace Interlocutor.Persistence.Abstractions.Tests;

/// <summary>Covers <see cref="Page{T}.TotalPageCount"/>'s derivation from <see cref="Page{T}.TotalCount"/> and <see cref="Page{T}.PageSize"/>.</summary>
public sealed class PageTests
{
    [Theory]
    [InlineData(0, 10, 0)]
    [InlineData(1, 10, 1)]
    [InlineData(10, 10, 1)]
    [InlineData(11, 10, 2)]
    [InlineData(25, 10, 3)]
    public void TotalPageCount_IsCeilingOfTotalCountOverPageSize(int totalCount, int pageSize, int expected)
    {
        var page = new Page<string>(Items: [], PageNumber: 1, PageSize: pageSize, TotalCount: totalCount);

        Assert.Equal(expected, page.TotalPageCount);
    }
}
