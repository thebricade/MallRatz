using System;

namespace CryingSnow.CheckoutFrenzy.Core
{
    public static class StoreEvents
    {
        // Time Cycle
        public static event Action<SummaryData, Action<bool>> OnSummaryRequested;
        public static void RaiseSummaryRequested(SummaryData data, Action<bool> onContinue) => OnSummaryRequested?.Invoke(data, onContinue);

        public static event Action<Action, Action<Action>> OnSkipDialogRequested;
        public static void RaiseSkipDialogRequested(Action onSkip, Action<Action> onRegisterClose) => OnSkipDialogRequested?.Invoke(onSkip, onRegisterClose);

        public static event Action<int> OnDeliveryTimeChanged;
        public static void RaiseDeliveryTimeChanged(int time) => OnDeliveryTimeChanged?.Invoke(time);

        // User Interface
        public static event Action<Action> OnPCMonitorRequested;
        public static void RaisePCMonitor(Action onClose) => OnPCMonitorRequested?.Invoke(onClose);

        public static event Action<IBox> OnBoxSelected;
        public static void RaiseBoxSelected(IBox box) => OnBoxSelected?.Invoke(box);

        public static event Action<Product> OnPriceCustomizerRequested;
        public static void RaisePriceCustomizer(Product product) => OnPriceCustomizerRequested?.Invoke(product);

        public static event Action<ILabelable> OnLabelCustomizerRequested;
        public static void RaiseLabelCustomizerRequested(ILabelable target) => OnLabelCustomizerRequested?.Invoke(target);

        public static event Action<ITextReceiver, Action> OnStoreNameChangeRequested;
        public static void RaiseStoreNameChangeRequested(ITextReceiver target, Action onComplete) => OnStoreNameChangeRequested?.Invoke(target, onComplete);
    }
}
