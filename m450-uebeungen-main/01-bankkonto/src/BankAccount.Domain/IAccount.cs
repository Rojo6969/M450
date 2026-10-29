namespace BankAccount.Domain;

public interface IAccount
{
    int Number { get; }
    decimal Balance { get; }
    AccountStatus Status { get; }
    void Deposit(decimal amount, DateOnly date);
    void Withdraw(decimal amount, DateOnly date);
    void TransferTo(IAccount target, decimal amount, DateOnly date);
    decimal CloseYear(DateOnly date);
}
