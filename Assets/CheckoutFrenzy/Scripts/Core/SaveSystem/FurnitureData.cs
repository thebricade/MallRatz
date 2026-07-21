using System.Collections.Generic;

namespace CryingSnow.CheckoutFrenzy.Core
{
    [System.Serializable]
    public class FurnitureData
    {
        public int FurnitureID { get; set; }
        public string Name { get; set; }
        public Location Location { get; set; }
        public Orientation Orientation { get; set; }
        public Location LastMoved { get; set; }
        public List<ShelfData> SavedShelves { get; set; }
        public List<RackData> SavedRacks { get; set; }

        public FurnitureData() { }
    }
}
