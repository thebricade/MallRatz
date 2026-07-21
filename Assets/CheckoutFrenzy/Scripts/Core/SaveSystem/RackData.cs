using System.Collections.Generic;

namespace CryingSnow.CheckoutFrenzy.Core
{
    [System.Serializable]
    public class RackData
    {
        public int ProductID { get; set; }
        public List<int> Quantities { get; set; }

        public bool IsEmpty => ProductID == 0 || Quantities.Count == 0;

        public RackData() { }

        public RackData(int productId, List<int> quantities)
        {
            ProductID = productId;
            Quantities = quantities;
        }
    }
}