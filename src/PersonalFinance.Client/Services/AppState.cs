using PersonalFinance.Domain.Entities;

namespace PersonalFinance.Client.Services;

// UI-wide coordination between the layout, the global "new transaction" dialog and pages.
public class AppState
{
    /// <summary>Raised with the transaction to edit, or null for a new one.</summary>
    public event Action<Transaction?>? TransactionEditorRequested;

    /// <summary>Local data changed (a local edit, or remote rows applied by a sync pull).</summary>
    public event Action? DataChanged;

    public void OpenTransactionEditor(Transaction? transaction = null) =>
        TransactionEditorRequested?.Invoke(transaction);

    public void NotifyDataChanged() => DataChanged?.Invoke();
}
