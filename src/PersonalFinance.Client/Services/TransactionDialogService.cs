using PersonalFinance.Domain.Entities;

namespace PersonalFinance.Client.Services;

/// <summary>
/// Coordinates the single shared "new/edit transaction" dialog (screen 1f) so any page —
/// or the shell's floating action button — can open it, without each page owning its own copy.
/// </summary>
public class TransactionDialogService
{
    public bool IsOpen { get; private set; }

    public Transaction? Editing { get; private set; }

    /// <summary>Set when the dialog should open straight into the Transfer segment (Accounts page).</summary>
    public bool OpenAsTransfer { get; private set; }

    public event Action? Changed;

    public event Action? Saved;

    public void OpenNew()
    {
        Editing = null;
        OpenAsTransfer = false;
        IsOpen = true;
        Changed?.Invoke();
    }

    public void OpenNewTransfer()
    {
        Editing = null;
        OpenAsTransfer = true;
        IsOpen = true;
        Changed?.Invoke();
    }

    public void OpenEdit(Transaction transaction)
    {
        Editing = transaction;
        OpenAsTransfer = false;
        IsOpen = true;
        Changed?.Invoke();
    }

    public void Close()
    {
        IsOpen = false;
        Editing = null;
        OpenAsTransfer = false;
        Changed?.Invoke();
    }

    public void NotifySaved() => Saved?.Invoke();
}
