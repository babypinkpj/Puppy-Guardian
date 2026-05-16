using QFSW.QC;
using TMPro;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public class PlayerStateSync : NetworkBehaviour
{
    [Header("Name UI")]
    [SerializeField] private TMP_Text namePrefab;
    [SerializeField] private Vector3 worldOffset = new Vector3(0f, 1f, 0f);

    [Header("Level UI")]
    [SerializeField] private TMP_Text levelPrefab;
    [SerializeField] private Vector3 levelOffset = new Vector3(0f, 0.5f, 0f);

    [Header("Status Visual")]
    [SerializeField] private Renderer statusRenderer;

    public NetworkVariable<int> ArenaIndex = new NetworkVariable<int>();

    private TMP_Text nameLabel;
    private TMP_Text levelLable;
    private Camera mainCam;

    public NetworkVariable<FixedString64Bytes> PlayerName =
        new NetworkVariable<FixedString64Bytes>(
            default,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Owner);

    public NetworkVariable<bool> IsSpecialStatus =
        new NetworkVariable<bool>(
            false,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Owner);

    public NetworkVariable<int> PlayerLevel = new NetworkVariable<int>(
     1,
     NetworkVariableReadPermission.Everyone,
     NetworkVariableWritePermission.Server
 );

    public NetworkVariable<bool> IsInArena = new NetworkVariable<bool>(
false,
NetworkVariableReadPermission.Everyone,
NetworkVariableWritePermission.Server);


    public override void OnNetworkSpawn()
    {
        mainCam = Camera.main;

        PlayerName.OnValueChanged += OnPlayerNameChanged;
        IsSpecialStatus.OnValueChanged += OnStatusChanged;
        PlayerLevel.OnValueChanged += OnLevelChanged;

        CreateNameLabel();
        UpdateNameUI(PlayerName.Value.ToString(), PlayerLevel.Value);
        UpdateStatusVisual(IsSpecialStatus.Value);

        if (IsOwner && ConnectionManager.Instance != null)
        {
            string localName = ConnectionManager.Instance.LocalUsername;

            if (!string.IsNullOrWhiteSpace(localName))
            {
                PlayerName.Value = localName;
            }
        }

        IsInArena.OnValueChanged += OnArenaStatusChanged;
        IsInArena.OnValueChanged += OnArenaChanged;
    }

    public override void OnNetworkDespawn()
    {
        PlayerName.OnValueChanged -= OnPlayerNameChanged;
        IsSpecialStatus.OnValueChanged -= OnStatusChanged;
        PlayerLevel.OnValueChanged -= OnLevelChanged;
        DestroyNameLabel();
    }

    private void Update()
    {
        UpdateNameLabelPosition();
    }

    public void OnAttack()
    {
        if (!IsOwner) return;
        IsSpecialStatus.Value = !IsSpecialStatus.Value;
    }

    private void CreateNameLabel()
    {
        if (namePrefab == null) return; if(levelPrefab == null) return;

        GameObject canvas = GameObject.FindWithTag("MainCanvas");
        if (canvas == null)
        {
            Debug.LogWarning("MainCanvas with tag 'MainCanvas' not found.");
            return;
        }

        nameLabel = Instantiate(namePrefab, Vector3.zero, Quaternion.identity);
        nameLabel.transform.SetParent(canvas.transform, false);
        levelLable = Instantiate(levelPrefab, Vector3.zero, Quaternion.identity);
        levelLable.transform.SetParent(canvas.transform, false);
    }

    private void DestroyNameLabel()
    {
        if (nameLabel != null)
            Destroy(nameLabel.gameObject);

        if (levelLable != null)
            Destroy(levelLable.gameObject);
    }

    private void UpdateNameLabelPosition()
    {
        if (nameLabel == null) return; if (levelLable == null) return;

        if (mainCam == null)
            mainCam = Camera.main;

        if (mainCam == null) return;

        Vector3 nameScreenPos = mainCam.WorldToScreenPoint(transform.position + worldOffset);
        Vector3 levelScreenPos = mainCam.WorldToScreenPoint(transform.position + levelOffset);

        bool visible = nameScreenPos.z > 0f;

        nameLabel.gameObject.SetActive(visible);
        levelLable.gameObject.SetActive(visible);

        if (visible)
        {
            nameLabel.transform.position = nameScreenPos;
            levelLable.transform.position = levelScreenPos;
        }
    }
    private void OnLevelChanged(int oldValue, int newValue)
    {
        UpdateNameUI(PlayerName.Value.ToString(), newValue);
    }
    private void OnPlayerNameChanged(FixedString64Bytes oldValue, FixedString64Bytes newValue)
    {
        UpdateNameUI(newValue.ToString(), PlayerLevel.Value);
    }

    private void OnStatusChanged(bool oldValue, bool newValue)
    {
        UpdateStatusVisual(newValue);
    }

    private void UpdateNameUI(string newName, int level)
    {
        if (nameLabel != null)
            nameLabel.text = newName;

        if (levelLable != null)
            levelLable.text = $"Lv. {level}";
    }

    private void UpdateStatusVisual(bool active)
    {
        if (statusRenderer == null) return;

        statusRenderer.material.color = active ? Color.red : Color.white;
    }
    [Command("set-level")]
    public void SetLevel(int level)
    {
        if (!IsOwner) return;

        PlayerLevel.Value = Mathf.Max(1, level);
    }

    private void OnArenaStatusChanged(bool wasInArena, bool isInArena)
    {
        if (isInArena)
        {
            // When the server sets this to true, every client moves themselves!
            MoveToArenaPodium();
        }
    }

    private void MoveToArenaPodium()
    {
        // We call a function in ConnectionManager to find the right spot
        if (ConnectionManager.Instance != null)
        {
            ConnectionManager.Instance.TeleportLocalPlayerToArena();
        }
    }
    private void Start()
    {
        if (IsOwner)
        {
            Transform visuals = transform.Find("Visuals");
            if (visuals != null)
                visuals.gameObject.SetActive(false);
        }
    }
    private void OnArenaChanged(bool oldValue, bool newValue)
    {
        if (newValue)
        {
            ConnectionManager.Instance.TeleportLocalPlayerToArena();
        }
    }
}