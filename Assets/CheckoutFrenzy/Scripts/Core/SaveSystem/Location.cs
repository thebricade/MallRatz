using UnityEngine;

namespace CryingSnow.CheckoutFrenzy.Core
{
    [System.Serializable]
    public struct Location
    {
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }

        public Location(Vector3 position)
        {
            X = position.x;
            Y = position.y;
            Z = position.z;
        }

        public Vector3 ToVector3()
        {
            return new Vector3(X, Y, Z);
        }
    }
}
