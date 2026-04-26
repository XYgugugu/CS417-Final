using PVZ3D.NPC;

using UnityEngine;

namespace PVZ3D.Resource
{
    public class Coin : MonoBehaviour
    {
        [Header("Coin Data")]
        public int value = 1;

        [HideInInspector] public bool isClaimed = false;
        [HideInInspector] public Trotter claimedByTrotter = null;
    }
}