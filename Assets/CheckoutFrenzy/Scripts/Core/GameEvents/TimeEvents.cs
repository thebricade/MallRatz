using System;

namespace CryingSnow.CheckoutFrenzy.Core
{
    public static class TimeEvents
    {
        public static event Action<bool> OnNightTimeChanged;
        public static void RaiseNightTimeChanged(bool isNight) => OnNightTimeChanged?.Invoke(isNight);

        public static event Action OnMinutePassed;
        public static void RaiseMinutePassed() => OnMinutePassed?.Invoke();
    }
}
