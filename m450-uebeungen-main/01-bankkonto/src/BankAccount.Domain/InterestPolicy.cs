namespace BankAccount.Domain;

public static class InterestPolicy
{
    public const int DaysPerYear = 360;

    public static decimal CreditRate(decimal balance, AccountStatus status, decimal baseRate)
    {
        if (balance < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(balance));
        }

        if (balance < 10_000m)
        {
            return baseRate;
        }

        if (balance < 50_000m)
        {
            return baseRate + 0.005m;
        }

        if (status == AccountStatus.Vip)
        {
            return baseRate + 0.015m;
        }

        return baseRate + 0.0075m;
    }

    public static decimal DailyInterest(decimal balance, AccountStatus status, InterestRates rates)
    {
        decimal rate;
        if (balance >= 0)
        {
            rate = CreditRate(balance, status, rates.Credit);
        }
        else
        {
            rate = rates.Debit;
        }

        return balance * rate / DaysPerYear;
    }
}
