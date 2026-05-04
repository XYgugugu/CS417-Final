using UnityEngine;

namespace PVZ3D.Shop
{
    internal static class ShopItemUtility
    {
        public static Transform GetAvailableSpawnPoint(Transform[] spawnPoints, float occupiedCheckRadius, LayerMask occupiedLayer)
        {
            if (spawnPoints == null || spawnPoints.Length == 0)
            {
                return null;
            }

            foreach (Transform point in spawnPoints)
            {
                if (point == null)
                {
                    continue;
                }

                if (!Physics.CheckSphere(point.position, occupiedCheckRadius, occupiedLayer))
                {
                    return point;
                }
            }

            return null;
        }

        public static void PlaySound(AudioSource audioSource, AudioClip clip, Vector3 fallbackPosition, float volume)
        {
            if (clip == null)
            {
                return;
            }

            if (audioSource != null)
            {
                audioSource.PlayOneShot(clip, volume);
                return;
            }

            AudioSource.PlayClipAtPoint(clip, fallbackPosition, volume);
        }

        public static void PlayPurchaseConfetti(
            GameObject purchaseConfettiPrefab,
            GameObject purchasedItem,
            Vector3 offset,
            float lifetime)
        {
            if (purchaseConfettiPrefab == null || purchasedItem == null)
            {
                return;
            }

            GameObject confetti = Object.Instantiate(
                purchaseConfettiPrefab,
                purchasedItem.transform.position + offset,
                Quaternion.identity);

            if (lifetime > 0f)
            {
                Object.Destroy(confetti, lifetime);
            }
        }
    }
}
