using BookingSystem.Domain.Shared;

namespace BookingSystem.UnitTests.Domain.Shared;

public sealed class MoneyTests
{
    [Fact]
    public void Create_WithPositiveAmountAndNoCurrency_ReturnsMoneyInDefaultCurrency()
    {
        var result = Money.Create(25.50m);

        Assert.True(result.IsSuccess);
        Assert.Equal(25.50m, result.Value.Amount);
        Assert.Equal("USD", result.Value.Currency);
    }

    [Fact]
    public void Create_WithZeroAmount_ReturnsSuccess()
    {
        var result = Money.Create(0m);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void Create_WithNegativeAmount_ReturnsNegativeAmountError()
    {
        var result = Money.Create(-0.01m);

        Assert.True(result.IsFailure);
        Assert.Equal(MoneyErrors.NegativeAmount, result.Error);
    }

    [Theory]
    [InlineData("")]
    [InlineData("US")]
    [InlineData("USDD")]
    [InlineData("U5D")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Create_WithInvalidCurrency_ReturnsInvalidCurrencyError(string? currency)
    {
        var result = Money.Create(10m, currency!);

        Assert.True(result.IsFailure);
        Assert.Equal(MoneyErrors.InvalidCurrency, result.Error);
    }

    [Fact]
    public void Create_WithLowercaseCurrency_NormalizesToUppercase()
    {
        var result = Money.Create(10m, "eur");

        Assert.Equal("EUR", result.Value.Currency);
    }

    [Fact]
    public void Zero_WithCurrency_ReturnsZeroAmountInThatCurrency()
    {
        var zero = Money.Zero("EUR");

        Assert.Equal(0m, zero.Amount);
        Assert.Equal("EUR", zero.Currency);
    }

    [Fact]
    public void Zero_WithInvalidCurrency_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => Money.Zero("XX"));
    }

    [Fact]
    public void Add_WithSameCurrency_ReturnsSum()
    {
        var first = Money.Create(10.25m).Value;
        var second = Money.Create(4.75m).Value;

        var sum = first.Add(second);

        Assert.Equal(Money.Create(15m).Value, sum);
    }

    [Fact]
    public void Add_WithDifferentCurrency_ThrowsInvalidOperationException()
    {
        var dollars = Money.Create(10m, "USD").Value;
        var euros = Money.Create(10m, "EUR").Value;

        Assert.Throws<InvalidOperationException>(() => dollars.Add(euros));
    }

    [Fact]
    public void Add_WithNull_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => Money.Create(10m).Value.Add(null!));
    }

    [Theory]
    [MemberData(nameof(MultiplyCases))]
    public void Multiply_ByNonNegativeFactor_ReturnsProductRoundedToTwoDecimals(
        decimal amount, decimal factor, decimal expected)
    {
        var money = Money.Create(amount).Value;

        var product = money.Multiply(factor);

        Assert.Equal(expected, product.Amount);
        Assert.Equal(money.Currency, product.Currency);
    }

    public static TheoryData<decimal, decimal, decimal> MultiplyCases => new()
    {
        { 20m, 2m, 40m },
        { 20m, 0.5m, 10m },
        { 20m, 0m, 0m },
        { 10m, 1m / 3m, 3.33m },
        { 0.05m, 0.5m, 0.03m },
    };

    [Fact]
    public void Multiply_ByNegativeFactor_ThrowsArgumentOutOfRangeException()
    {
        var money = Money.Create(10m).Value;

        Assert.Throws<ArgumentOutOfRangeException>(() => money.Multiply(-1m));
    }

    [Fact]
    public void Equals_SameAmountAndCurrency_ReturnsTrue()
    {
        Assert.Equal(Money.Create(10m, "USD").Value, Money.Create(10.00m, "usd").Value);
    }

    [Theory]
    [InlineData(25.5, "25.50 USD")]
    [InlineData(0, "0.00 USD")]
    [InlineData(1234.567, "1234.57 USD")]
    public void ToString_FormatsAmountWithTwoDecimalsAndCurrency(double amount, string expected)
    {
        Assert.Equal(expected, Money.Create((decimal)amount).Value.ToString());
    }

    [Fact]
    public void Equals_SameAmountAndDifferentCurrency_ReturnsFalse()
    {
        Assert.NotEqual(Money.Create(10m, "USD").Value, Money.Create(10m, "EUR").Value);
    }
}
