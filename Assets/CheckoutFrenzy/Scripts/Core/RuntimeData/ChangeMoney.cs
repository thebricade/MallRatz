using UnityEngine;

namespace CryingSnow.CheckoutFrenzy.Core
{
    public struct ChangeMoney
    {
        public int amount;
        public GameObject money;

        public ChangeMoney(int amount, GameObject money)
        {
            this.amount = amount;
            this.money = money;
        }
    }
}
