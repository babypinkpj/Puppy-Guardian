using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class RoomEntryUI : MonoBehaviour
{
    [Tooltip("Text to display the server name.")]
    public TMP_Text serverNameText;

    [Tooltip("Text to display the player count (e.g., 1/5).")]
    public TMP_Text playerCountText;

    [Tooltip("Text to display 'Required Password' if the room is locked.")]
    public TMP_Text passwordRequiredText;

    [Tooltip("Button used to join this room.")]
    public Button joinButton;
}
