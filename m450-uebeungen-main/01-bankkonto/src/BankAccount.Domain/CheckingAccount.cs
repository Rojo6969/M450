namespace BankAccount.Domain;

public sealed class CheckingAccount : Account
{
    private int _payments;

    public CheckingAccount(DateOnly openedOn, InterestRates rates, decimal overdraftLimit, decimal annualFee, decimal paymentFee, AccountStatus status = AccountStatus.Standard) : base(openedOn, rates, status)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(overdraftLimit);
        ArgumentOutOfRangeException.ThrowIfNegative(annualFee);
        ArgumentOutOfRangeException.ThrowIfNegative(paymentFee);
        OverdraftLimit = overdraftLimit;
        AnnualFee = annualFee;
        PaymentFee = paymentFee;
    }

    public const int FreePaymentsPerYear = 10;
    public decimal OverdraftLimit { get; }
    public decimal AnnualFee { get; }
    public decimal PaymentFee { get; }

    public void Pay(IAccount target, decimal amount, DateOnly date)
    {
        decimal fee = 0m;
        if (_payments >= FreePaymentsPerYear)
        {
            fee = PaymentFee;
        }

        ValidateWithdrawal(amount + fee);
        TransferTo(target, amount, date);
        _payments++;
        if (fee > 0)
        {
            Book(-fee);
        }
    }

    protected override void ValidateWithdrawal(decimal amount)
    {
        if (Balance - amount < -OverdraftLimit)
        {
            throw new InvalidOperationException("Overdraft limit exceeded.");
        }
    }

    protected override void ChargeAnnualFee(DateOnly date)
    {
        if (AnnualFee > 0)
        {
            Book(-AnnualFee);
        }
        _payments = 0;
    }
}
