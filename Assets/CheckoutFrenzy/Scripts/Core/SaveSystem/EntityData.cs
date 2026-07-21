namespace CryingSnow.CheckoutFrenzy.Core
{
    [System.Serializable]
    public class EntityData
    {
        public int ID { get; set; }
        public Location Location { get; set; }
        public Orientation Orientation { get; set; }

        public EntityData() { }

        public EntityData(IPersistentEntity entity)
        {
            ID = entity.EntityID;
            Location = new Location(entity.EntityTransform.position);
            Orientation = new Orientation(entity.EntityTransform.rotation);
        }
    }
}
