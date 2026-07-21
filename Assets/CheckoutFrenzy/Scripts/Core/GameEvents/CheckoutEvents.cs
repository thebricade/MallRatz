using System;

namespace CryingSnow.CheckoutFrenzy.Core
{
    public static class CheckoutEvents
    {
        // Cash Register
        public static event Action<bool> OnCashRegisterToggleRequested;
        public static void RaiseCashRegisterToggleRequested(bool open) => OnCashRegisterToggleRequested?.Invoke(open);

        public static event Action<int> OnCashRegisterDraw;
        public static void RaiseCashRegisterDraw(int amount) => OnCashRegisterDraw?.Invoke(amount);

        public static event Action OnCashRegisterUndo;
        public static void RaiseCashRegisterUndo() => OnCashRegisterUndo?.Invoke();

        public static event Action OnCashRegisterClear;
        public static void RaiseCashRegisterClear() => OnCashRegisterClear?.Invoke();

        public static event Action OnCashRegisterConfirm;
        public static void RaiseCashRegisterConfirm() => OnCashRegisterConfirm?.Invoke();

        // Payment Terminal
        public static event Action<bool> OnPaymentTerminalToggleRequested;
        public static void RaisePaymentTerminalToggleRequested(bool open) => OnPaymentTerminalToggleRequested?.Invoke(open);

        public static event Action<decimal> OnPaymentTerminalConfirm;
        public static void RaisePaymentTerminalConfirm(decimal amount) => OnPaymentTerminalConfirm?.Invoke(amount);
    }
}
