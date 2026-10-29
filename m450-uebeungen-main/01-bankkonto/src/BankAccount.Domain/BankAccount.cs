namespace BankAccount.Domain;

public sealed class BankAccount : Account
{
    public BankAccount(DateOnly openedOn, InterestRates rates, AccountStatus status = AccountStatus.Standard) : base(openedOn, rates, status)
    {
    }
}
