using BankAccount.Domain;

namespace BankAccount.Tests;

public sealed class InterestPolicyTests
{
    public static TheoryData<decimal, AccountStatus, decimal> Boundaries { get; } = new()
    {
        { 0m, AccountStatus.Standard, 0.01m },
        { 9_999.99m, AccountStatus.Vip, 0.01m },
        { 10_000m, AccountStatus.Standard, 0.015m },
        { 49_999.99m, AccountStatus.Vip, 0.015m },
        { 50_000m, AccountStatus.Standard, 0.0175m },
        { 50_000m, AccountStatus.Vip, 0.025m },
        { 99_999.99m, AccountStatus.Standard, 0.0175m },
        { 99_999.99m, AccountStatus.Vip, 0.025m },
        { 100_000m, AccountStatus.Standard, 0.0175m }
    };

    [Theory]
    [MemberData(nameof(Boundaries))]
    public void CreditRate_UsesBalanceClassAndStatus(decimal balance, AccountStatus status, decimal expected)
    {
        // Arrange / Act
        decimal actual = InterestPolicy.CreditRate(balance, status, 0.01m);
        // Assert
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void CreditRate_NegativeBalance_Throws()
    {
        // Act
        Action action = () => InterestPolicy.CreditRate(-0.01m, AccountStatus.Standard, 0.01m);
        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(action);
    }

    [Theory]
    [InlineData(1_000, 0.01, 0.02778)]
    [InlineData(-1_000, 0.036, -0.1)]
    public void DailyInterest_UsesCreditOrDebitRate(decimal balance, decimal rate, decimal expected)
    {
        // Arrange
        InterestRates rates = balance >= 0
            ? new InterestRates(rate, 0m)
            : new InterestRates(0m, rate);
        // Act
        decimal actual = InterestPolicy.DailyInterest(balance, AccountStatus.Standard, rates);
        // Assert
        Assert.Equal(expected, decimal.Round(actual, 5));
    }

    [Theory]
    [InlineData(-0.01, 0)]
    [InlineData(0, -0.01)]
    public void InterestRates_NegativeRate_Throws(decimal credit, decimal debit)
    {
        // Act
        Action action = () => new InterestRates(credit, debit);
        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(action);
    }
}
