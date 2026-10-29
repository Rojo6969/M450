using BankAccount.Domain;

namespace BankAccount.Services;

public sealed class AccountService
{
    public void Transfer(IAccount source, IAccount target, decimal amount, DateOnly date)
    {
        source.TransferTo(target, amount, date);
    }
}
