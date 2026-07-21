namespace CryingSnow.CheckoutFrenzy.Core
{
    [System.Serializable]
    public class ShelfData
    {
        public int ProductID { get; set; }
        public int AssignedProductID { get; set; }
        public int Quantity { get; set; }

        public bool IsEmpty => ProductID == 0 || Quantity == 0;

        public ShelfData() { }

        public ShelfData(int productId, int assignedProductId, int quantity)
        {
            ProductID = productId;
            AssignedProductID = assignedProductId;
            Quantity = quantity;
        }
    }
}
