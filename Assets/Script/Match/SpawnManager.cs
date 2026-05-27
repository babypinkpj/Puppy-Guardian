using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

namespace Match
{
    public class SpawnManager : NetworkBehaviour
    {
        public static SpawnManager Instance { get; private set; }

        [Header("Spawn Points")]
        public Transform[] robberSpawnPoints;
        public Transform[] dogSpawnPoints;
        public Transform[] radarSpawnPoints; // 9 total
        public Transform[] foodSpawnPoints;
        public Transform[] robberItemSpawnPoints;
        public Transform[] policeCarSpawnPoints; // 2 locations

        [Header("Prefabs")]
        public NetworkObject[] characterPrefabs; // 0=Shiba, 1=Pug, 2=Duchun, 3=Husky, 4=Robber
        public NetworkObject radarPrefab;
        public NetworkObject dogFoodPrefab;
        public NetworkObject[] robberItemPrefabs;
        public NetworkObject policeCarPrefab;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        public void SpawnPlayers()
        {
            if (!IsServer) return;

            ConnectionManager connManager = FindAnyObjectByType<ConnectionManager>();
            List<Transform> availableDogSpawns = new List<Transform>(dogSpawnPoints);
            ulong robberId = MatchManager.Instance.RobberClientId.Value;

            if (connManager != null && connManager.ConnectedPlayers.Count > 0)
            {
                foreach (var playerState in connManager.ConnectedPlayers)
                {
                    bool isRobber = playerState.ClientId == robberId;
                    Transform spawnPoint = null;

                    if (isRobber && robberSpawnPoints.Length > 0)
                    {
                        spawnPoint = robberSpawnPoints[Random.Range(0, robberSpawnPoints.Length)];
                    }
                    else if (!isRobber && availableDogSpawns.Count > 0)
                    {
                        int index = Random.Range(0, availableDogSpawns.Count);
                        spawnPoint = availableDogSpawns[index];
                        availableDogSpawns.RemoveAt(index);
                    }
                    else
                    {
                        spawnPoint = transform; // Fallback
                    }

                    // Get prefab based on character ID
                    int charId = playerState.CharacterId;
                    if (isRobber) charId = 4; // Force Robber prefab

                    NetworkObject prefabToSpawn = characterPrefabs[Mathf.Clamp(charId, 0, characterPrefabs.Length - 1)];
                    NetworkObject spawnedPlayer = Instantiate(prefabToSpawn, spawnPoint.position, spawnPoint.rotation);
                    spawnedPlayer.SpawnAsPlayerObject(playerState.ClientId, true);

                    // Initialize player setup
                    MatchPlayer mp = spawnedPlayer.GetComponent<MatchPlayer>();
                    if (mp != null)
                    {
                        mp.IsRobber.Value = isRobber;
                        mp.SetupVisualsRpc(playerState.DogHatId, playerState.RobberHatId);
                    }
                }
            }
            else
            {
                // Fallback for testing directly in the Match scene
                var clients = NetworkManager.Singleton.ConnectedClientsIds;
                foreach (ulong clientId in clients)
                {
                    bool isRobber = clientId == robberId;
                    Transform spawnPoint = null;

                    if (isRobber && robberSpawnPoints.Length > 0)
                    {
                        spawnPoint = robberSpawnPoints[Random.Range(0, robberSpawnPoints.Length)];
                    }
                    else if (!isRobber && availableDogSpawns.Count > 0)
                    {
                        int index = Random.Range(0, availableDogSpawns.Count);
                        spawnPoint = availableDogSpawns[index];
                        availableDogSpawns.RemoveAt(index);
                    }
                    else
                    {
                        spawnPoint = transform; // Fallback
                    }

                    int charId = isRobber ? 4 : Random.Range(0, 4); // 0-3 for dogs, 4 for robber
                    
                    NetworkObject prefabToSpawn = characterPrefabs[Mathf.Clamp(charId, 0, characterPrefabs.Length - 1)];
                    NetworkObject spawnedPlayer = Instantiate(prefabToSpawn, spawnPoint.position, spawnPoint.rotation);
                    spawnedPlayer.SpawnAsPlayerObject(clientId, true);

                    MatchPlayer mp = spawnedPlayer.GetComponent<MatchPlayer>();
                    if (mp != null)
                    {
                        mp.IsRobber.Value = isRobber;
                        mp.SetupVisualsRpc(-1, -1); // No cosmetics in test mode
                    }
                }
            }

            SpawnObjectives();
            SpawnItems();
        }

        public void SpawnSinglePlayer(ulong clientId)
        {
            if (!IsServer) return;

            // If we don't have a robber yet, make this client the robber
            if (MatchManager.Instance.RobberClientId.Value == ulong.MaxValue)
            {
                MatchManager.Instance.RobberClientId.Value = clientId;
            }

            bool isRobber = clientId == MatchManager.Instance.RobberClientId.Value;
            if (!isRobber)
            {
                MatchManager.Instance.TotalDogs++; // Increment total dogs
            }

            Transform spawnPoint = transform;
            if (isRobber && robberSpawnPoints.Length > 0)
            {
                spawnPoint = robberSpawnPoints[Random.Range(0, robberSpawnPoints.Length)];
            }
            else if (!isRobber && dogSpawnPoints.Length > 0)
            {
                spawnPoint = dogSpawnPoints[Random.Range(0, dogSpawnPoints.Length)];
            }

            int charId = isRobber ? 4 : Random.Range(0, 4); // 0-3 for dogs, 4 for robber
            
            NetworkObject prefabToSpawn = characterPrefabs[Mathf.Clamp(charId, 0, characterPrefabs.Length - 1)];
            NetworkObject spawnedPlayer = Instantiate(prefabToSpawn, spawnPoint.position, spawnPoint.rotation);
            spawnedPlayer.SpawnAsPlayerObject(clientId, true);

            MatchPlayer mp = spawnedPlayer.GetComponent<MatchPlayer>();
            if (mp != null)
            {
                mp.IsRobber.Value = isRobber;
                mp.SetupVisualsRpc(-1, -1);
            }
        }

        private void SpawnObjectives()
        {
            if (!IsServer) return;

            // Spawn 5 Radars randomly out of 9
            List<Transform> availableRadars = new List<Transform>(radarSpawnPoints);
            int radarsToSpawn = Mathf.Min(5, availableRadars.Count);

            for (int i = 0; i < radarsToSpawn; i++)
            {
                int index = Random.Range(0, availableRadars.Count);
                Transform spawnPt = availableRadars[index];
                availableRadars.RemoveAt(index);

                NetworkObject spawnedRadar = Instantiate(radarPrefab, spawnPt.position, spawnPt.rotation);
                spawnedRadar.Spawn();
            }
        }

        private void SpawnItems()
        {
            if (!IsServer) return;

            // Spawn 2-3 Dog Foods
            List<Transform> availableFoodSpawns = new List<Transform>(foodSpawnPoints);
            int foodToSpawn = Random.Range(2, 4);
            foodToSpawn = Mathf.Min(foodToSpawn, availableFoodSpawns.Count);

            for (int i = 0; i < foodToSpawn; i++)
            {
                int index = Random.Range(0, availableFoodSpawns.Count);
                Transform spawnPt = availableFoodSpawns[index];
                availableFoodSpawns.RemoveAt(index);

                NetworkObject spawnedFood = Instantiate(dogFoodPrefab, spawnPt.position, spawnPt.rotation);
                spawnedFood.Spawn();
            }

            // Spawn Robber items (Randomly spawn 4 items)
            List<Transform> availableRobberSpawns = new List<Transform>(robberItemSpawnPoints);
            for (int i = 0; i < 4; i++)
            {
                if (availableRobberSpawns.Count == 0 || robberItemPrefabs.Length == 0) break;
                
                int index = Random.Range(0, availableRobberSpawns.Count);
                Transform spawnPt = availableRobberSpawns[index];
                availableRobberSpawns.RemoveAt(index);

                NetworkObject itemToSpawn = robberItemPrefabs[Random.Range(0, robberItemPrefabs.Length)];
                NetworkObject spawnedItem = Instantiate(itemToSpawn, spawnPt.position, spawnPt.rotation);
                spawnedItem.Spawn();
            }
        }

        public void SpawnPoliceCars()
        {
            if (!IsServer) return;

            foreach (var spawnPt in policeCarSpawnPoints)
            {
                NetworkObject car = Instantiate(policeCarPrefab, spawnPt.position, spawnPt.rotation);
                car.Spawn();
            }
        }
    }
}
