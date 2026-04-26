using PVZ3D.Resource;

using System.Collections.Generic;
using UnityEngine;

namespace PVZ3D.NPC
{
    public class NPCFollowManager : MonoBehaviour
    {
        public static NPCFollowManager Instance { get; private set; }

        private readonly List<NPCFollower> followers = new List<NPCFollower>();
        private readonly List<NPC_Trotter> trotters = new List<NPC_Trotter>();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        public void Register(NPCFollower follower)
        {
            if (!followers.Contains(follower))
                followers.Add(follower);

            RefreshFollowerIndices();
        }

        public void Unregister(NPCFollower follower)
        {
            if (followers.Contains(follower))
                followers.Remove(follower);

            RefreshFollowerIndices();
        }

        public void RegisterTrotter(NPC_Trotter trotter)
        {
            if (!trotters.Contains(trotter))
                trotters.Add(trotter);

            RefreshTrotterIndices();
        }

        public void UnregisterTrotter(NPC_Trotter trotter)
        {
            if (trotters.Contains(trotter))
                trotters.Remove(trotter);

            RefreshTrotterIndices();
        }

        private void RefreshFollowerIndices()
        {
            for (int i = 0; i < followers.Count; i++)
            {
                followers[i].SetFollowIndex(i, followers.Count);
            }
        }

        private void RefreshTrotterIndices()
        {
            for (int i = 0; i < trotters.Count; i++)
            {
                trotters[i].SetFollowIndex(i, trotters.Count);
            }
        }
    }
}