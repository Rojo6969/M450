namespace BankAccount.Domain;

public sealed record BankRates
{
    public BankRates(InterestRates checking, InterestRates savings, InterestRates youth)
    {
        if (checking.Credit >= savings.Credit)
        {
            throw new ArgumentException("Checking interest must be lower than savings interest.");
        }

        if (youth.Credit <= savings.Credit)
        {
            throw new ArgumentException("Youth accounts require a preferential interest rate.");
        }
        Checking = checking;
        Savings = savings;
        Youth = youth;
    }

    public InterestRates Checking { get; }
    public InterestRates Savings { get; }
    public InterestRates Youth { get; }
}
