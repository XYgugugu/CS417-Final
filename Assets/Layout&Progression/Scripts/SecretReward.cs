using UnityEngine;

public class SecretReward : MonoBehaviour
{

    public int rewardAmount = 50;
    private bool collected = false;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void OnTriggerEnter(Collider other)
    {
        if (collected) return;

        if (other.CompareTag("Player") || other.CompareTag("Controller"))
        {
            collected = true;
            Debug.Log("Secret found! Reward: +" + rewardAmount);

            // Later: connect to resource/economy system
            // ResourceManager.Instance.AddSun(rewardAmount);

            gameObject.SetActive(false);
        }
    }
}
