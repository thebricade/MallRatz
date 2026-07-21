namespace CryingSnow.CheckoutFrenzy.Core
{
    [System.Serializable]
    public class EmployeeData
    {
        public EmployeeType Type;
        public int PointIndex;

        public override bool Equals(object obj)
        {
            if (obj is not EmployeeData other) return false;
            return Type == other.Type && PointIndex == other.PointIndex;
        }

        public override int GetHashCode()
        {
            return Type.GetHashCode() ^ PointIndex.GetHashCode();
        }
    }
}
