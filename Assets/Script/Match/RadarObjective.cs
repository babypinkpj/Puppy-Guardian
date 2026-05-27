using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Unity.Netcode;
using System.Collections.Generic;

namespace Match
{
    public class RadarObjective : NetworkBehaviour
    {
        public NetworkVariable<float> progress = new NetworkVariable<float>(0f);
        public NetworkVariable<bool> isFixed = new NetworkVariable<bool>(false);

        [Header("UI Reference")]
        public GameObject minigameCanvas;
        public RectTransform[] leftSlots; // 7 slots
        public RectTransform[] rightSlots; // 7 slots
        public GameObject wirePrefab;
        public Sprite[] symbolSprites; // 13 symbols
        public Color[] symbolColors; // Make sure this is the same length as symbolSprites!

        [Header("Audio")]
        public AudioClip fixClip;
        public AudioClip completeClip;

        private DogController _currentInteractingDog;
        private List<WireLogic> _activeWires = new List<WireLogic>();
        private int _wiresToConnect = 0;
        private int _wiresConnected = 0;

        private void OnTriggerEnter(Collider other)
        {
            if (isFixed.Value) return;
            DogController dog = other.GetComponent<DogController>();
            if (dog != null && dog.IsOwner)
            {
                // Can show interact prompt here
            }
        }

        // Called locally by Dog Input
        public void Interact(DogController dog)
        {
            if (isFixed.Value) return;
            _currentInteractingDog = dog;
            OpenMinigame();
        }

        private void OpenMinigame()
        {
            if (minigameCanvas != null) minigameCanvas.SetActive(true);
            GenerateMinigame();
        }

        public void CloseMinigame()
        {
            if (minigameCanvas != null) minigameCanvas.SetActive(false);
            _currentInteractingDog = null;
        }

        private void GenerateMinigame()
        {
            // Clear old
            foreach (var w in _activeWires) Destroy(w.gameObject);
            _activeWires.Clear();
            _wiresConnected = 0;

            _wiresToConnect = Random.Range(4, 7); // 4 to 6

            List<int> availableLeft = new List<int>{0,1,2,3,4,5,6};
            List<int> availableRight = new List<int>{0,1,2,3,4,5,6};
            List<int> availableSymbols = new List<int>();
            for(int i=0; i<symbolSprites.Length; i++) availableSymbols.Add(i);

            for (int i = 0; i < _wiresToConnect; i++)
            {
                int leftIndex = availableLeft[Random.Range(0, availableLeft.Count)];
                availableLeft.Remove(leftIndex);

                int rightIndex = availableRight[Random.Range(0, availableRight.Count)];
                availableRight.Remove(rightIndex);

                int symbolIndex = availableSymbols[Random.Range(0, availableSymbols.Count)];
                availableSymbols.Remove(symbolIndex);

                // Instantiate Wire
                GameObject wireObj = Instantiate(wirePrefab, minigameCanvas.transform);
                WireLogic wire = wireObj.GetComponent<WireLogic>();
                
                Color wireColor = (symbolColors != null && symbolIndex < symbolColors.Length) ? symbolColors[symbolIndex] : Color.white;
                
                wire.Setup(this, leftSlots[leftIndex], rightSlots[rightIndex], symbolSprites[symbolIndex], wireColor);
                _activeWires.Add(wire);
            }
        }

        public void OnWireConnected()
        {
            _wiresConnected++;
            if (fixClip) AudioSource.PlayClipAtPoint(fixClip, transform.position);

            if (_wiresConnected >= _wiresToConnect)
            {
                // Minigame success
                CloseMinigame();
                SubmitProgressServerRpc();
            }
        }

        [Rpc(SendTo.Server)]
        private void SubmitProgressServerRpc()
        {
            if (isFixed.Value) return;

            int totalPlayers = MatchManager.Instance.TotalDogs + 1; // +1 for Robber
            float progressPerFix = 0.4f;
            if (totalPlayers >= 5) progressPerFix = 0.2f;
            else if (totalPlayers == 4) progressPerFix = 0.26f;
            else if (totalPlayers == 3) progressPerFix = 0.33f;

            progress.Value += progressPerFix;

            if (progress.Value >= 1f)
            {
                progress.Value = 1f;
                isFixed.Value = true;
                MatchManager.Instance.OnRadarFixed();
                RadarFixedClientRpc();
            }
        }

        [Rpc(SendTo.Everyone)]
        private void RadarFixedClientRpc()
        {
            if (completeClip) AudioSource.PlayClipAtPoint(completeClip, transform.position);
            // Visuals: Turn off outline if it was glowing, or change color
        }
    }
}
