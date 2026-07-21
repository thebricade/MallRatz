namespace CryingSnow.CheckoutFrenzy.Core
{
    [System.Serializable]
    public class BoxData
    {
        public string Name { get; set; }
        public Location Location { get; set; }
        public Orientation Orientation { get; set; }
        public int ProductID { get; set; }
        public int Quantity { get; set; }
        public bool IsOpen { get; set; }

        public bool IsEmpty => ProductID == 0 || Quantity == 0;

        public BoxData() { }

        public BoxData(IBox box)
        {
            Name = box.Name;
            Location = new Location(box.Transform.position);
            Orientation = new Orientation(box.Transform.rotation);
            if (box.Product != null) ProductID = box.Product.ProductID;
            Quantity = box.Quantity;
            IsOpen = box.IsOpen;
        }
    }
}
