using UnityEngine;

public class Coin : MonoBehaviour
{
    [Header("Coin Data")]
    public int value = 1;

    [HideInInspector] public bool isClaimed = false;
    [HideInInspector] public NPC_Trotter claimedByTrotter = null;
}