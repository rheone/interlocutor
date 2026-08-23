namespace Interlocutor.Persistence.Abstractions.Tests;

/// <summary>
/// Covers <see cref="DomainRepositoryQueryableExtensions"/>'s filter/sort/paging composition against a
/// plain in-memory <see cref="IQueryable{T}"/> - the same LINQ operators both EF Core's and NHibernate's
/// providers translate to SQL.
/// </summary>
public sealed class DomainRepositoryQueryableExtensionsTests
{
    private static readonly Widget[] Widgets =
    [
        new Widget(1, "beta", 20),
        new Widget(2, "alpha", 10),
        new Widget(3, "alpha", 30),
        new Widget(4, "gamma", 10),
    ];

    [Fact]
    public void ApplyFilter_ReturnsQueryUnchanged_WhenFilterIsNull()
    {
        var result = Widgets.AsQueryable().ApplyFilter(null).ToArray();

        Assert.Equal(Widgets, result);
    }

    [Fact]
    public void ApplyFilter_AppliesPredicate_WhenGiven()
    {
        var result = Widgets.AsQueryable().ApplyFilter(w => w.Name == "alpha").ToArray();

        Assert.Equal([Widgets[1], Widgets[2]], result);
    }

    [Fact]
    public void ApplySort_ReturnsQueryUnchanged_WhenSortIsNullOrEmpty()
    {
        Assert.Equal(Widgets, Widgets.AsQueryable().ApplySort(null).ToArray());
        Assert.Equal(Widgets, Widgets.AsQueryable().ApplySort([]).ToArray());
    }

    [Fact]
    public void ApplySort_OrdersByPrimaryKey_Ascending()
    {
        var result = Widgets
            .AsQueryable()
            .ApplySort([new SortCriterion<Widget>(w => w.Name, SortDirection.Ascending)])
            .ToArray();

        Assert.Equal(["alpha", "alpha", "beta", "gamma"], result.Select(w => w.Name));
    }

    [Fact]
    public void ApplySort_BreaksTiesWithSecondCriterion()
    {
        var result = Widgets
            .AsQueryable()
            .ApplySort([
                new SortCriterion<Widget>(w => w.Name, SortDirection.Ascending),
                new SortCriterion<Widget>(w => w.Value, SortDirection.Descending),
            ])
            .ToArray();

        // Both "alpha" rows tie on the primary key; the secondary descending Value key breaks the tie.
        Assert.Equal([3, 2, 1, 4], result.Select(w => w.Id));
    }

    [Fact]
    public void ApplyPaging_SkipsAndTakes_ForRequestedPage()
    {
        var result = Widgets.AsQueryable().ApplyPaging(pageNumber: 2, pageSize: 2).ToArray();

        Assert.Equal([Widgets[2], Widgets[3]], result);
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(1, 0)]
    [InlineData(-1, 1)]
    public void ApplyPaging_Throws_WhenPageNumberOrPageSizeIsLessThanOne(int pageNumber, int pageSize)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Widgets.AsQueryable().ApplyPaging(pageNumber, pageSize).ToArray());
    }

    private sealed record Widget(int Id, string Name, int Value);
}
