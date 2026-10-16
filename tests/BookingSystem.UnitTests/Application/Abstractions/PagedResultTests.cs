using BookingSystem.Application.Abstractions.Paging;
using Shouldly;

namespace BookingSystem.UnitTests.Application.Abstractions;

public sealed class PagedResultTests
{
    [Theory]
    [InlineData(0, 10, 0)]
    [InlineData(1, 10, 1)]
    [InlineData(10, 10, 1)]
    [InlineData(11, 10, 2)]
    [InlineData(20, 10, 2)]
    public void TotalPages_RoundsUpPartialPages(int totalCount, int pageSize, int expectedTotalPages)
    {
        Page(page: 1, pageSize, totalCount).TotalPages.ShouldBe(expectedTotalPages);
    }

    [Fact]
    public void TotalPages_WithZeroPageSize_IsZeroInsteadOfDividingByZero()
    {
        Page(page: 1, pageSize: 0, totalCount: 5).TotalPages.ShouldBe(0);
    }

    [Theory]
    [InlineData(1, false)]
    [InlineData(2, true)]
    public void HasPreviousPage_IsTrueAfterTheFirstPage(int page, bool expected)
    {
        Page(page, pageSize: 10, totalCount: 30).HasPreviousPage.ShouldBe(expected);
    }

    [Theory]
    [InlineData(1, true)]
    [InlineData(2, true)]
    [InlineData(3, false)]
    [InlineData(4, false)]
    public void HasNextPage_IsTrueBeforeTheLastPage(int page, bool expected)
    {
        Page(page, pageSize: 10, totalCount: 30).HasNextPage.ShouldBe(expected);
    }

    private static PagedResult<int> Page(int page, int pageSize, int totalCount) =>
        new([], page, pageSize, totalCount);
}
