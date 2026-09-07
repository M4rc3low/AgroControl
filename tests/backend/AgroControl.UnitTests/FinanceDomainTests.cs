using AgroControl.Domain.Modules.Finance;

namespace AgroControl.UnitTests;

public sealed class FinanceDomainTests
{
    [Fact]
    public void Financial_transaction_rejects_non_positive_amount()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => FinancialTransaction.Create(
            Guid.NewGuid(), null, null, FinancialEntryType.Expense, "Insumo", null, 0m,
            new DateOnly(2026, 9, 1), null, null, null, null, null, DateTime.UtcNow));
    }

    [Fact]
    public void Expense_settlement_becomes_paid()
    {
        var item = Create(FinancialEntryType.Expense);
        item.Settle(new DateOnly(2026, 9, 7), DateTime.UtcNow);
        Assert.Equal(FinancialStatus.Paid, item.Status);
        Assert.Equal(new DateOnly(2026, 9, 7), item.SettledOn);
    }

    [Fact]
    public void Revenue_settlement_becomes_received()
    {
        var item = Create(FinancialEntryType.Revenue);
        item.Settle(new DateOnly(2026, 9, 7), DateTime.UtcNow);
        Assert.Equal(FinancialStatus.Received, item.Status);
    }

    [Fact]
    public void Settled_transaction_cannot_be_edited()
    {
        var item = Create(FinancialEntryType.Expense);
        item.Settle(new DateOnly(2026, 9, 7), DateTime.UtcNow);
        Assert.Throws<InvalidOperationException>(() => item.Update(
            null, null, FinancialEntryType.Expense, "Alterado", null, 200m,
            new DateOnly(2026, 9, 1), null, null, null, null, null, DateTime.UtcNow));
    }

    [Fact]
    public void Pending_transaction_can_be_cancelled()
    {
        var item = Create(FinancialEntryType.Revenue);
        item.Cancel(DateTime.UtcNow);
        Assert.Equal(FinancialStatus.Cancelled, item.Status);
    }

    private static FinancialTransaction Create(FinancialEntryType type) => FinancialTransaction.Create(
        Guid.NewGuid(), null, null, type, "Lançamento", "Fornecedor", 100m,
        new DateOnly(2026, 9, 1), new DateOnly(2026, 9, 10), null, null, null, null, DateTime.UtcNow);
}
