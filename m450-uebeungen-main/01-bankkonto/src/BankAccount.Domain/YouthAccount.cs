namespace BankAccount.Domain;

public sealed class YouthAccount : Account
{
    public YouthAccount(DateOnly openedOn, DateOnly birthDate, InterestRates rates, decimal withdrawalLimit, AccountStatus status = AccountStatus.Standard) : base(openedOn, rates, status)
    {
        if (AgeOn(birthDate, openedOn) >= 20)
        {
            throw new InvalidOperationException("The holder must be under 20.");
        }

        if (withdrawalLimit <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(withdrawalLimit));
        }

        BirthDate = birthDate;
        WithdrawalLimit = withdrawalLimit;
    }

    public DateOnly BirthDate { get; }
    public decimal WithdrawalLimit { get; }

    protected override void ValidateWithdrawal(decimal amount)
    {
        if (amount > WithdrawalLimit)
        {
            throw new InvalidOperationException("Withdrawal limit exceeded.");
        }

        if (amount > Balance)
        {
            throw new InvalidOperationException("Youth accounts cannot be overdrawn.");
        }
    }

    private static int AgeOn(DateOnly birthDate, DateOnly date)
    {
        int age = date.Year - birthDate.Year;
        if (birthDate.AddYears(age) > date)
        {
            age--;
        }

        return age;
    }
}
