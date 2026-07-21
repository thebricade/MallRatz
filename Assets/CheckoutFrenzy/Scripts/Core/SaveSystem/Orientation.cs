using UnityEngine;

namespace CryingSnow.CheckoutFrenzy.Core
{
    [System.Serializable]
    public struct Orientation
    {
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }
        public float W { get; set; }

        public Orientation(Quaternion rotation)
        {
            X = rotation.x;
            Y = rotation.y;
            Z = rotation.z;
            W = rotation.w;
        }

        public Quaternion ToQuaternion()
        {
            return new Quaternion(X, Y, Z, W);
        }
    }
}
