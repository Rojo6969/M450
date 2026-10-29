namespace BankAccount.Domain;

public sealed class SavingsAccount : Account
{
    public SavingsAccount(DateOnly openedOn, InterestRates rates, AccountStatus status = AccountStatus.Standard) : base(openedOn, rates, status)
    {
    }

    protected override void ValidateWithdrawal(decimal amount)
    {
        if (amount > Balance)
        {
            throw new InvalidOperationException("Savings accounts cannot be overdrawn.");
        }
    }
}
