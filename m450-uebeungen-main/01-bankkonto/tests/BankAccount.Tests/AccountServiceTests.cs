using BankAccount.Domain;
using BankAccount.Services;
using BasicAccount = BankAccount.Domain.BankAccount;

namespace BankAccount.Tests;

public sealed class AccountServiceTests
{
    [Fact]
    public void Transfer_DelegatesToDomainAccounts()
    {
        // Arrange
        DateOnly date = new(2026, 1, 1);
        InterestRates rates = new(0m, 0m);
        BasicAccount source = new(date, rates);
        BasicAccount target = new(date, rates);
        source.Deposit(100m, date);
        // Act
        new AccountService().Transfer(source, target, 25m, date);
        // Assert
        Assert.Equal(75m, source.Balance);
        Assert.Equal(25m, target.Balance);
    }
}
