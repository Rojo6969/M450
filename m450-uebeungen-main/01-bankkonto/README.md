# Bank Account TDD Exercise

.NET 8 solution with `Domain`, `Services`, and xUnit `Tests` projects.

Run:

```powershell
dotnet test BankAccount.sln
```

Rates use decimals (`0.036m` = 3.6%) and a 360-day banking year. Interest is accrued daily and booked at year-end. The highest tier remains active above CHF 100,000 because no higher tier is specified.

Types stay simple: `decimal` for money and rates, `DateOnly` for dates, `int` for account numbers, and enums for fixed choices.

Tests use AAA and cover deposits, withdrawals, transfers, account rules, fees, daily interest, equivalence classes, and boundaries.
