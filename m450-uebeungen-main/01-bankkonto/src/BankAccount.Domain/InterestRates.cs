namespace BankAccount.Domain;

public sealed record InterestRates
{
    public InterestRates(decimal credit, decimal debit)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(credit);
        ArgumentOutOfRangeException.ThrowIfNegative(debit);
        Credit = credit;
        Debit = debit;
    }

    public decimal Credit { get; }
    public decimal Debit { get; }
}
