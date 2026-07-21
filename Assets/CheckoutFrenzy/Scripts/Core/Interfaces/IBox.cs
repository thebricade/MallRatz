using System.Collections.Generic;
using UnityEngine;

namespace CryingSnow.CheckoutFrenzy.Core
{
    public interface IBox
    {
        string Name { get; }
        Transform Transform { get; }
        Product Product { get; }
        public int Quantity { get; }
        public Vector3 Size { get; }
        public bool IsOpen { get; }
        Sprite GetIcon();
        IEnumerable<BoxDetail> GetDetails();
    }
}
