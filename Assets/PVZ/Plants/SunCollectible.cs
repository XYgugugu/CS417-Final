using UnityEngine;

namespace PVZ3D.Plants
{
    public class SunCollectible : MonoBehaviour
    {
        [SerializeField] private int value = 25;

        public int Value => value;

        public void SetValue(int sunValue)
        {
            value = Mathf.Max(0, sunValue);
        }
    }
}
