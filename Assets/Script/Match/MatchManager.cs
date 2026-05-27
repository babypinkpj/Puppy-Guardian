using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

namespace Match
{
    public class MatchManager : NetworkBehaviour
    {
        public static MatchManager Instance { get; private set; }

        public enum MatchState
        {
            Waiting,
            Cutscene,
            Playing,
            Escape,
            EndGame
        }

        public NetworkVariable<MatchState> CurrentState = new NetworkVariable<MatchState>(MatchState.Waiting);
        public NetworkVariable<float> MatchTimer = new NetworkVariable<float>(0f);
        public NetworkVariable<ulong> RobberClientId = new NetworkVariable<ulong>(ulong.MaxValue);

        public int TotalRadarsToFix = 5;
        public NetworkVariable<int> RadarsFixed = new NetworkVariable<int>(0);
        public NetworkVariable<int> DogsDefeated = new NetworkVariable<int>(0);
        
        [HideInInspector] public int TotalDogs = 0;
        private int _dogsEscaped = 0;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        private HashSet<ulong> _loadedClients = new HashSet<ulong>();

        public override void OnNetworkSpawn()
        {
            if (IsServer)
            {
                CurrentState.Value = MatchState.Waiting;
                NetworkManager.Singleton.OnClientConnectedCallback += HandleClientConnected;
            }
            
            ClientLoadedServerRpc(NetworkManager.Singleton.LocalClientId);
        }

        [Rpc(SendTo.Server)]
        private void ClientLoadedServerRpc(ulong clientId)
        {
            if (CurrentState.Value != MatchState.Waiting) return;

            _loadedClients.Add(clientId);

            int expectedPlayers = 1;
            ConnectionManager connManager = FindAnyObjectByType<ConnectionManager>();
            if (connManager != null && connManager.ConnectedPlayers.Count > 0)
            {
                expectedPlayers = connManager.ConnectedPlayers.Count;
            }
            else
            {
                expectedPlayers = NetworkManager.Singleton.ConnectedClientsIds.Count;
            }

            if (_loadedClients.Count >= expectedPlayers)
            {
                StartMatch();
            }
        }

        public override void OnNetworkDespawn()
        {
            if (IsServer && NetworkManager.Singleton != null)
            {
                NetworkManager.Singleton.OnClientConnectedCallback -= HandleClientConnected;
            }
        }

        private void HandleClientConnected(ulong clientId)
        {
            // When a client joins mid-game (e.g. testing in Editor), spawn them!
            SpawnManager.Instance?.SpawnSinglePlayer(clientId);
        }

        private void StartMatch()
        {
            // 1. Assign Roles
            ConnectionManager connManager = FindAnyObjectByType<ConnectionManager>();
            if (connManager != null && connManager.ConnectedPlayers.Count > 0)
            {
                int robberIndex = Random.Range(0, connManager.ConnectedPlayers.Count);
                RobberClientId.Value = connManager.ConnectedPlayers[robberIndex].ClientId;
                TotalDogs = connManager.ConnectedPlayers.Count - 1;
            }
            else
            {
                // Fallback for testing directly in the Match scene
                var clients = NetworkManager.Singleton.ConnectedClientsIds;
                if (clients.Count > 0)
                {
                    int robberIndex = Random.Range(0, clients.Count);
                    RobberClientId.Value = clients[robberIndex];
                    TotalDogs = clients.Count - 1;
                }
            }

            MatchUIManager.Instance?.BuildPlayerListRpc(RobberClientId.Value);

            // 2. Start Cutscene Sequence
            StartCutsceneClientRpc();
        }

        [Rpc(SendTo.Everyone)]
        private void StartCutsceneClientRpc()
        {
            if (IsServer)
            {
                CurrentState.Value = MatchState.Cutscene;
                MatchTimer.Value = 7f; // 7 seconds cutscene
                
                // Start Ambient Music (Only Server needs to call this RPC)
                MatchAudioManager.Instance?.SetMusicStateRpc(MatchAudioManager.MusicState.Ambient);
            }
            
            // Local visual reveal on all clients
            LoadingScreenUI.Instance?.FinishLoadingAndReveal();
        }

        private void Update()
        {
            if (!IsServer) return;

            if (CurrentState.Value == MatchState.Cutscene)
            {
                MatchTimer.Value -= Time.deltaTime;
                if (MatchTimer.Value <= 0f)
                {
                    // Transition to Playing
                    CurrentState.Value = MatchState.Playing;
                    MatchTimer.Value = 300f; // 5 minutes
                    
                    SpawnManager.Instance?.SpawnPlayers();
                    MatchUIManager.Instance?.ShowSubtitleRpc("SURVIVE", "Survive until police are come");
                    MatchUIManager.Instance?.UpdateObjectiveRpc(RadarsFixed.Value, TotalRadarsToFix, DogsDefeated.Value, TotalDogs);
                }
            }
            else if (CurrentState.Value == MatchState.Playing)
            {
                MatchTimer.Value -= Time.deltaTime;
                if (MatchTimer.Value <= 0f)
                {
                    // Transition to Escape Phase
                    CurrentState.Value = MatchState.Escape;
                    MatchTimer.Value = 60f; // 1 extra minute for escape
                    
                    MatchUIManager.Instance?.ShowSubtitleRpc("ESCAPE", "Find the police car and get inside");
                    MatchAudioManager.Instance?.SetMusicStateRpc(MatchAudioManager.MusicState.Escape);
                    SpawnManager.Instance?.SpawnPoliceCars();
                }
            }
            else if (CurrentState.Value == MatchState.Escape)
            {
                MatchTimer.Value -= Time.deltaTime;
                if (MatchTimer.Value <= 0f)
                {
                    // Time is out during escape, dogs fail
                    EndGameRpc(false); 
                }
            }
        }

        public void OnRadarFixed()
        {
            if (!IsServer || CurrentState.Value != MatchState.Playing) return;
            
            RadarsFixed.Value++;
            MatchUIManager.Instance?.ShowAnnouncementRpc("Reduce rounds time by 30 seconds");
            MatchUIManager.Instance?.UpdateObjectiveRpc(RadarsFixed.Value, TotalRadarsToFix, DogsDefeated.Value, TotalDogs);
            
            MatchTimer.Value = Mathf.Max(0, MatchTimer.Value - 30f); // Reduce 30s
            
            if (RadarsFixed.Value >= TotalRadarsToFix)
            {
                MatchUIManager.Instance?.ShowSubtitleRpc("SURVIVE", "All tasks have been completed, survive till time is out");
            }
        }

        public void OnDogDefeated()
        {
            if (!IsServer) return;
            
            DogsDefeated.Value++;
            MatchTimer.Value += 25f; // Add 25 seconds
            
            MatchUIManager.Instance?.ShowAnnouncementRpc("Dog is defeated, increase rounds time by 25 seconds");
            MatchUIManager.Instance?.UpdateObjectiveRpc(RadarsFixed.Value, TotalRadarsToFix, DogsDefeated.Value, TotalDogs);

            if (DogsDefeated.Value >= TotalDogs)
            {
                EndGameRpc(false); // Robber wins
            }
        }

        public void OnDogEscaped()
        {
            if (!IsServer || CurrentState.Value != MatchState.Escape) return;
            
            _dogsEscaped++;
            if (_dogsEscaped >= (TotalDogs - DogsDefeated.Value))
            {
                // All remaining alive dogs escaped
                EndGameRpc(true);
            }
        }

        [Rpc(SendTo.Everyone)]
        private void EndGameRpc(bool dogsWin)
        {
            CurrentState.Value = MatchState.EndGame;
            // TODO: Display Result UI
            Debug.Log(dogsWin ? "Dogs Win!" : "Robber Wins!");
        }
    }
}
