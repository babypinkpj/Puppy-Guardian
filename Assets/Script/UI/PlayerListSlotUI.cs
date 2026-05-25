using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerListSlotUI : MonoBehaviour
{
    public TMP_Text playerNameText;
    public Image dogIcon;
    public GameObject hostCrownIcon;
    public TMP_Text readyText;
    public Image backgroundImage;
    
    [Header("Colors")]
    public Color notReadyColor = new Color(0.8f, 0.2f, 0.2f, 1f); // Reddish
    public Color readyColor = new Color(0.2f, 0.8f, 0.2f, 1f);   // Greenish

    public void Setup(string playerName, Sprite iconSprite, bool isHost, bool isReady, bool isLocalPlayer)
    {
        if (playerNameText != null) 
        {
            playerNameText.text = playerName;
            if (isLocalPlayer)
            {
                playerNameText.text += " (You)";
            }
        }

        if (dogIcon != null && iconSprite != null)
        {
            dogIcon.sprite = iconSprite;
            dogIcon.color = isReady ? new Color(0.8f, 1f, 0.8f, 1f) : new Color(1f, 1f, 1f, 1f);
            dogIcon.enabled = true;
        }

        if (hostCrownIcon != null)
        {
            hostCrownIcon.SetActive(isHost);
        }

        if (readyText != null)
        {
            readyText.text = isReady ? "READY" : "NOT READY";
            readyText.color = isReady ? readyColor : notReadyColor;
        }

    }
}
