namespace BankAccount.Domain;

public abstract class Account : IAccount
{
    private static int _nextNumber;
    private readonly InterestRates _rates;
    private DateOnly _interestDate;
    private decimal _creditInterest;
    private decimal _debitInterest;

    protected Account(DateOnly openedOn, InterestRates rates, AccountStatus status = AccountStatus.Standard)
    {
        Number = ++_nextNumber;
        _rates = rates;
        Status = status;
        _interestDate = openedOn;
    }

    public int Number { get; }
    public decimal Balance { get; private set; }
    public AccountStatus Status { get; }

    public void Deposit(decimal amount, DateOnly date)
    {
        ValidateAmount(amount);
        AccrueInterest(date);
        Book(amount);
    }

    public virtual void Withdraw(decimal amount, DateOnly date)
    {
        ValidateAmount(amount);
        AccrueInterest(date);
        ValidateWithdrawal(amount);
        Book(-amount);
    }

    public void TransferTo(IAccount target, decimal amount, DateOnly date)
    {
        ArgumentNullException.ThrowIfNull(target);
        if (ReferenceEquals(this, target)) throw new InvalidOperationException("Source and target must differ.");
        if (target is not Account account) throw new ArgumentException("Unsupported target account.", nameof(target));
        ValidateAmount(amount);
        AccrueInterest(date);
        ValidateWithdrawal(amount);
        account.AccrueInterest(date);
        Book(-amount);
        account.Book(amount);
    }

    public decimal CloseYear(DateOnly date)
    {
        AccrueInterest(date);
        decimal interest = decimal.Round(_creditInterest + _debitInterest, 2, MidpointRounding.AwayFromZero);
        if (interest != 0)
        {
            Book(interest);
        }
        ChargeAnnualFee(date);
        _creditInterest = _debitInterest = 0;
        return interest;
    }

    protected virtual void ValidateWithdrawal(decimal amount)
    {
    }

    protected virtual void ChargeAnnualFee(DateOnly date)
    {
    }

    protected void Book(decimal amount)
    {
        Balance += amount;
    }

    protected static void ValidateAmount(decimal amount)
    {
        if (amount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), "Amount must be positive.");
        }
    }

    private void AccrueInterest(DateOnly date)
    {
        if (date < _interestDate)
        {
            throw new ArgumentOutOfRangeException(nameof(date), "Transactions must be chronological.");
        }

        int days = date.DayNumber - _interestDate.DayNumber;
        decimal interest = InterestPolicy.DailyInterest(Balance, Status, _rates) * days;
        if (Balance >= 0)
        {
            _creditInterest += interest;
        }
        else
        {
            _debitInterest += interest;
        }
        _interestDate = date;
    }
}
