using BankAccount.Domain;
using BasicAccount = BankAccount.Domain.BankAccount;

namespace BankAccount.Tests;

public sealed class AccountTests
{
    private static readonly DateOnly OpenedOn = new(2026, 3, 1);
    private static readonly InterestRates Rates = new(0.036m, 0.072m);

    [Fact]
    public void Deposit_CreditsImmediately()
    {
        // Arrange
        BasicAccount account = new(OpenedOn, Rates);
        // Act
        account.Deposit(1_000m, OpenedOn);
        // Assert
        Assert.Equal(1_000m, account.Balance);
    }

    [Fact]
    public void Account_ExposesAssignedNumberAndStatus()
    {
        // Arrange
        BasicAccount account = new(OpenedOn, Rates);
        // Act / Assert
        Assert.True(account.Number > 0);
        Assert.Equal(AccountStatus.Standard, account.Status);
    }

    [Fact]
    public void ValidationScenario_BooksDailyCreditAndDebitInterestAtYearEnd()
    {
        // Arrange
        BasicAccount account = new(OpenedOn, Rates);
        // Act
        account.Deposit(1_000m, new DateOnly(2026, 3, 1));
        account.Deposit(1_000m, new DateOnly(2026, 7, 1));
        account.Withdraw(3_000m, new DateOnly(2026, 8, 1));
        account.Deposit(2_000m, new DateOnly(2026, 10, 1));
        decimal interest = account.CloseYear(new DateOnly(2026, 12, 31));
        // Assert
        Assert.Equal(15.30m, interest);
        Assert.Equal(1_015.30m, account.Balance);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Deposit_NonPositiveAmount_Throws(decimal amount)
    {
        // Arrange
        BasicAccount account = new(OpenedOn, Rates);
        // Act
        Action action = () => account.Deposit(amount, OpenedOn);
        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(action);
    }

    [Fact]
    public void Deposit_EarlierDate_ThrowsWithoutChangingBalance()
    {
        // Arrange
        BasicAccount account = new(OpenedOn, Rates);
        // Act
        Action action = () => account.Deposit(100m, OpenedOn.AddDays(-1));
        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(action);
        Assert.Equal(0m, account.Balance);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Withdraw_NonPositiveAmount_Throws(decimal amount)
    {
        // Arrange
        BasicAccount account = new(OpenedOn, Rates);
        // Act
        Action action = () => account.Withdraw(amount, OpenedOn);
        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(action);
    }

    [Fact]
    public void Transfer_BooksBothAccounts()
    {
        // Arrange
        BasicAccount source = new(OpenedOn, Rates);
        BasicAccount target = new(OpenedOn, Rates);
        source.Deposit(100m, OpenedOn);
        // Act
        source.TransferTo(target, 40m, OpenedOn);
        // Assert
        Assert.Equal(60m, source.Balance);
        Assert.Equal(40m, target.Balance);
    }

    [Fact]
    public void Transfer_NullTarget_Throws()
    {
        // Arrange
        BasicAccount source = new(OpenedOn, Rates);
        // Act
        Action action = () => source.TransferTo(null!, 1m, OpenedOn);
        // Assert
        Assert.Throws<ArgumentNullException>(action);
    }

    [Fact]
    public void Transfer_SameAccount_ThrowsWithoutChangingBalance()
    {
        // Arrange
        BasicAccount account = new(OpenedOn, Rates);
        account.Deposit(100m, OpenedOn);
        // Act
        Action action = () => account.TransferTo(account, 10m, OpenedOn);
        // Assert
        Assert.Throws<InvalidOperationException>(action);
        Assert.Equal(100m, account.Balance);
    }

    [Fact]
    public void Transfer_NonDomainAccount_Throws()
    {
        // Arrange
        BasicAccount source = new(OpenedOn, Rates);
        source.Deposit(100m, OpenedOn);
        // Act
        Action action = () => source.TransferTo(new NonDomainAccount(), 10m, OpenedOn);
        // Assert
        Assert.Throws<ArgumentException>(action);
        Assert.Equal(100m, source.Balance);
    }

    [Fact]
    public void Transfer_EarlierDate_ThrowsWithoutChangingBalances()
    {
        // Arrange
        BasicAccount source = new(OpenedOn, Rates);
        BasicAccount target = new(OpenedOn, Rates);
        source.Deposit(100m, OpenedOn);
        // Act
        Action action = () => source.TransferTo(target, 10m, OpenedOn.AddDays(-1));
        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(action);
        Assert.Equal(100m, source.Balance);
        Assert.Equal(0m, target.Balance);
    }

    [Fact]
    public void CloseYear_WithoutAccruedInterest_ReturnsZero()
    {
        // Arrange
        BasicAccount account = new(OpenedOn, Rates);
        account.Deposit(100m, OpenedOn);
        // Act
        decimal interest = account.CloseYear(OpenedOn);
        // Assert
        Assert.Equal(0m, interest);
        Assert.Equal(100m, account.Balance);
    }

    private sealed class NonDomainAccount : IAccount
    {
        public int Number => 0;
        public decimal Balance => 0m;
        public AccountStatus Status => AccountStatus.Standard;
        public void Deposit(decimal amount, DateOnly date) => throw new NotSupportedException();
        public void Withdraw(decimal amount, DateOnly date) => throw new NotSupportedException();
        public void TransferTo(IAccount target, decimal amount, DateOnly date) => throw new NotSupportedException();
        public decimal CloseYear(DateOnly date) => throw new NotSupportedException();
    }
}
