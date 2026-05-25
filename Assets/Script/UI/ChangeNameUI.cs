using UnityEngine;
using TMPro;

public class ChangeNameUI : MonoBehaviour
{
    [Header("Main Menu UI")]
    [Tooltip("The text on the main menu that displays the player's current name.")]
    public TMP_Text displayNameText;

    [Header("Change Name Popup")]
    [Tooltip("The entire popup panel that contains the input field and confirm/cancel buttons.")]
    public GameObject changeNamePanel;
    
    [Tooltip("The input field where the player types their new name.")]
    public TMP_InputField nameInputField;

    // The key used to save the name in PlayerPrefs
    private const string NamePrefKey = "PlayerName";

    private void Start()
    {
        // Load the saved name (or default to "Player" if no name is saved)
        string savedName = PlayerPrefs.GetString(NamePrefKey, "Player");
        
        if (displayNameText != null)
        {
            displayNameText.text = savedName;
        }

        // Hide the popup when the game starts
        if (changeNamePanel != null)
        {
            changeNamePanel.SetActive(false);
        }
    }

    /// <summary>
    /// Call this from your "Change Name" button on the main menu
    /// </summary>
    public void OpenChangeNamePanel()
    {
        if (changeNamePanel != null)
        {
            changeNamePanel.SetActive(true);
        }

        if (nameInputField != null)
        {
            // Pre-fill the input box with their current name
            nameInputField.text = PlayerPrefs.GetString(NamePrefKey, "Player");
        }
    }

    /// <summary>
    /// Call this from your "Confirm" button inside the popup
    /// </summary>
    public void ConfirmChangeName()
    {
        if (nameInputField != null && !string.IsNullOrWhiteSpace(nameInputField.text))
        {
            string newName = nameInputField.text.Trim();
            
            // Save it so they keep their name next time they open the game
            PlayerPrefs.SetString(NamePrefKey, newName);
            PlayerPrefs.Save();

            // Update the main menu text
            if (displayNameText != null)
            {
                displayNameText.text = newName;
            }
        }

        // Close the popup
        if (changeNamePanel != null)
        {
            changeNamePanel.SetActive(false);
        }
    }

    /// <summary>
    /// Call this from your "Cancel" button inside the popup
    /// </summary>
    public void CancelChangeName()
    {
        // Just close the popup without saving
        if (changeNamePanel != null)
        {
            changeNamePanel.SetActive(false);
        }
    }
}
