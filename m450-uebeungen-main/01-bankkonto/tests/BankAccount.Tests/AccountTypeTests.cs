using BankAccount.Domain;
using BasicAccount = BankAccount.Domain.BankAccount;

namespace BankAccount.Tests;

public sealed class AccountTypeTests
{
    private static readonly DateOnly Today = new(2026, 1, 1);
    private static readonly InterestRates ZeroRates = new(0m, 0m);

    [Theory]
    [InlineData(500, false)]
    [InlineData(500.01, true)]
    public void CheckingAccount_EnforcesOverdraftLimit(decimal amount, bool throws)
    {
        // Arrange
        CheckingAccount account = Checking(overdraft: 500m);
        // Act
        Action action = () => account.Withdraw(amount, Today);
        // Assert
        if (throws)
        {
            Assert.Throws<InvalidOperationException>(action);
        }
        else
        {
            action();
        }
    }

    [Fact]
    public void SavingsAccount_CannotBeOverdrawn()
    {
        // Arrange
        SavingsAccount account = new(Today, ZeroRates);
        account.Deposit(100m, Today);
        // Act
        Action action = () => account.Withdraw(100.01m, Today);
        // Assert
        Assert.Throws<InvalidOperationException>(action);
    }

    [Theory]
    [InlineData(19, false)]
    [InlineData(20, true)]
    public void YouthAccount_RequiresHolderUnderTwenty(int age, bool throws)
    {
        // Arrange
        DateOnly birthDate = Today.AddYears(-age);
        // Act
        Func<YouthAccount> action = () => new YouthAccount(Today, birthDate, ZeroRates, 500m);
        // Assert
        if (throws)
        {
            Assert.Throws<InvalidOperationException>(action);
        }
        else
        {
            action();
        }
    }

    [Fact]
    public void YouthAccount_BirthdayTomorrow_IsStillUnderTwenty()
    {
        // Arrange
        DateOnly birthDate = new(2006, 1, 2);
        // Act
        YouthAccount account = new(Today, birthDate, ZeroRates, 500m);
        // Assert
        Assert.Equal(birthDate, account.BirthDate);
    }

    [Fact]
    public void YouthAccount_AllowsWithdrawalAtExactBalanceAndLimit()
    {
        // Arrange
        YouthAccount account = new(Today, Today.AddYears(-15), ZeroRates, 100m);
        account.Deposit(100m, Today);
        // Act
        account.Withdraw(100m, Today);
        // Assert
        Assert.Equal(0m, account.Balance);
    }

    [Fact]
    public void YouthAccount_EnforcesWithdrawalLimitAndBalance()
    {
        // Arrange
        YouthAccount account = new(Today, Today.AddYears(-15), ZeroRates, 500m);
        account.Deposit(100m, Today);
        // Act
        Action overBalance = () => account.Withdraw(100.01m, Today);
        Action overLimit = () => account.Withdraw(500.01m, Today);
        // Assert
        Assert.Throws<InvalidOperationException>(overBalance);
        Assert.Throws<InvalidOperationException>(overLimit);
    }

    [Fact]
    public void SavingsAndYouthAccounts_HaveNoAnnualFee()
    {
        // Arrange
        Account[] accounts = [new SavingsAccount(Today, ZeroRates), new YouthAccount(Today, Today.AddYears(-15), ZeroRates, 500m)];
        foreach (Account account in accounts)
        {
            account.Deposit(100m, Today);
        }
        // Act
        foreach (Account account in accounts)
        {
            account.CloseYear(Today.AddYears(1));
        }
        // Assert
        Assert.All(accounts, account => Assert.Equal(100m, account.Balance));
    }

    [Fact]
    public void CheckingAccount_ChargesAnnualFee()
    {
        // Arrange
        CheckingAccount account = Checking(annualFee: 24m);
        account.Deposit(100m, Today);
        // Act
        account.CloseYear(Today.AddYears(1));
        // Assert
        Assert.Equal(76m, account.Balance);
    }

    [Fact]
    public void CheckingAccount_ChargesEleventhPayment()
    {
        // Arrange
        CheckingAccount account = Checking(paymentFee: 2m);
        BasicAccount target = new(Today, ZeroRates);
        account.Deposit(1_000m, Today);
        // Act
        for (int i = 0; i < 11; i++)
        {
            account.Pay(target, 10m, Today);
        }
        // Assert
        Assert.Equal(888m, account.Balance);
        Assert.Equal(110m, target.Balance);
    }

    [Fact]
    public void CheckingAccount_ResetsFreePaymentsAfterYearClose()
    {
        // Arrange
        CheckingAccount account = Checking(paymentFee: 2m);
        BasicAccount target = new(Today, ZeroRates);
        account.Deposit(1_000m, Today);
        for (int i = 0; i < CheckingAccount.FreePaymentsPerYear; i++)
        {
            account.Pay(target, 10m, Today);
        }
        // Act
        account.CloseYear(Today.AddYears(1));
        account.Pay(target, 10m, Today.AddYears(1));
        // Assert
        Assert.Equal(890m, account.Balance);
        Assert.Equal(110m, target.Balance);
    }

    [Theory]
    [InlineData(-1, 0, 0)]
    [InlineData(0, -1, 0)]
    [InlineData(0, 0, -1)]
    public void CheckingAccount_NegativeFeesOrLimit_Throws(
        decimal overdraft, decimal annualFee, decimal paymentFee)
    {
        // Act
        Action action = () => new CheckingAccount(Today, ZeroRates, overdraft, annualFee, paymentFee);
        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(action);
    }

    [Fact]
    public void BankRates_EnforceAccountTypeRates()
    {
        // Arrange / Act
        BankRates rates = new(new(0.005m, 0.05m), new(0.01m, 0.05m), new(0.015m, 0.05m));
        // Assert
        Assert.True(rates.Checking.Credit < rates.Savings.Credit);
        Assert.True(rates.Youth.Credit > rates.Savings.Credit);
    }

    [Theory]
    [InlineData(0.01, 0.01, 0.02)]
    [InlineData(0.01, 0.02, 0.02)]
    public void BankRates_RejectsInvalidAccountTypeRates(
        decimal checkingRate, decimal savingsRate, decimal youthRate)
    {
        // Arrange / Act
        Action action = () => new BankRates(
            new InterestRates(checkingRate, 0m),
            new InterestRates(savingsRate, 0m),
            new InterestRates(youthRate, 0m));
        // Assert
        Assert.Throws<ArgumentException>(action);
    }

    [Fact]
    public void SavingsAccount_AllowsWithdrawalOfEntireBalance()
    {
        // Arrange
        SavingsAccount account = new(Today, ZeroRates);
        account.Deposit(100m, Today);
        // Act
        account.Withdraw(100m, Today);
        // Assert
        Assert.Equal(0m, account.Balance);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    public void YouthAccount_NonPositiveWithdrawalLimit_Throws(decimal limit)
    {
        // Act
        Action action = () => new YouthAccount(Today, Today.AddYears(-15), ZeroRates, limit);
        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(action);
    }

    private static CheckingAccount Checking(decimal overdraft = 500m, decimal annualFee = 0m, decimal paymentFee = 0m)
    {
        return new CheckingAccount(Today, ZeroRates, overdraft, annualFee, paymentFee);
    }
}
