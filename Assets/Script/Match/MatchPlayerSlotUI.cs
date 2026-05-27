using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Match
{
    public class MatchPlayerSlotUI : MonoBehaviour
    {
        public TMP_Text playerNameText;
        public Image iconImage;
        public Image hpBar;

        public void Setup(string playerName, Sprite icon, float currentHP, float maxHP)
        {
            if (playerNameText) playerNameText.text = playerName;
            if (iconImage && icon != null)
            {
                iconImage.sprite = icon;
                iconImage.enabled = true;
            }
            UpdateHP(currentHP, maxHP);
        }

        public void UpdateHP(float current, float max)
        {
            if (hpBar) hpBar.fillAmount = current / max;
        }
    }
}
