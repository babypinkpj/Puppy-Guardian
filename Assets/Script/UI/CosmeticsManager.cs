using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CosmeticsManager : MonoBehaviour
{
    [Header("Dynamic UI Setup")]
    [Tooltip("Prefab containing a Button and a TMP_Text component")]
    public GameObject buttonPrefab;
    public Transform dogListContent;
    public Transform dogHatListContent;
    public Transform robberHatListContent;

    [Header("Naming Texts")]
    public TMP_Text currentDogNameText;
    public TMP_Text currentHatNameText;

    [Header("Optional Custom Names")]
    [Tooltip("If left empty, the script will automatically use the prefab's name.")]
    public string[] customDogNames;
    public string[] customDogHatNames;
    public string[] customRobberHatNames;

    [Header("Optional Icons")]
    [Tooltip("Sprites to show on the buttons. Make sure your Button Prefab has an Image component named 'Icon'.")]
    public Sprite[] dogIcons;
    public Sprite[] dogHatIcons;
    public Sprite[] robberHatIcons;

    [Header("Dog Selection")]
    [Tooltip("The 3D dog models spawned in the UI scene. Order must match the CharacterDropdown order (0=Puppy/Shiba, 1=Pug, 2=Duchun, 3=Husky)")]
    public Transform[] dogModels;
    private int currentDogIndex = 0;

    [Header("Robber Selection")]
    [Tooltip("The 3D robber model spawned in the UI scene.")]
    public Transform robberModel;
    
    [Header("Hats for Dog")]
    [Tooltip("Hat prefabs that can be equipped by the dog. Index 0 should be 'None' (null or empty GameObject)")]
    public GameObject[] dogHatPrefabs;
    private int currentDogHatIndex = 0;
    private GameObject currentSpawnedDogHat;

    [Header("Hats for Robber")]
    [Tooltip("Hat prefabs that can be equipped by the robber. Index 0 should be 'None'")]
    public GameObject[] robberHatPrefabs;
    private int currentRobberHatIndex = 0;
    private GameObject currentSpawnedRobberHat;

    [Header("Categories")]
    public GameObject dogCategoryPanel;
    public GameObject robberCategoryPanel;

    private bool isEditingDog = true;

    private void Start()
    {
        // Load saved preferences
        currentDogIndex = PlayerPrefs.GetInt("SelectedDog", 0);
        currentDogHatIndex = PlayerPrefs.GetInt("SelectedDogHat", 0);
        currentRobberHatIndex = PlayerPrefs.GetInt("SelectedRobberHat", 0);

        GenerateDynamicLists();
        SwitchToDogCategory();
    }

    private string GetDisplayName(int index, string[] customNamesArray, string defaultName)
    {
        if (customNamesArray != null && index < customNamesArray.Length && !string.IsNullOrWhiteSpace(customNamesArray[index]))
        {
            return customNamesArray[index];
        }
        return CleanName(defaultName);
    }

    private void SetButtonIcon(GameObject btnObj, int index, Sprite[] iconsArray)
    {
        if (iconsArray != null && index < iconsArray.Length && iconsArray[index] != null)
        {
            // First try to find a child specifically named "Icon"
            Transform iconTransform = btnObj.transform.Find("Icon");
            if (iconTransform != null)
            {
                Image iconImg = iconTransform.GetComponent<Image>();
                if (iconImg != null)
                {
                    iconImg.sprite = iconsArray[index];
                    iconImg.color = Color.white; // Ensure it's fully visible
                    return;
                }
            }

            // Fallback: just change the main button background image
            Image mainImg = btnObj.GetComponent<Image>();
            if (mainImg != null)
            {
                mainImg.sprite = iconsArray[index];
            }
        }
    }

    private void GenerateDynamicLists()
    {
        if (buttonPrefab == null) return;

        // 1. Generate Dog Buttons
        if (dogListContent != null)
        {
            for (int i = 0; i < dogModels.Length; i++)
            {
                if (dogModels[i] == null) continue;
                int index = i; // local copy for closure
                GameObject btnObj = Instantiate(buttonPrefab, dogListContent);
                
                string displayName = GetDisplayName(i, customDogNames, dogModels[i].name);
                btnObj.name = displayName + " Button";

                TMP_Text btnText = btnObj.GetComponentInChildren<TMP_Text>();
                if (btnText != null) btnText.text = displayName;
                
                SetButtonIcon(btnObj, i, dogIcons);

                Button btn = btnObj.GetComponent<Button>();
                if (btn != null) btn.onClick.AddListener(() => SelectDog(index));
            }
        }

        // 2. Generate Dog Hat Buttons
        if (dogHatListContent != null)
        {
            for (int i = 0; i < dogHatPrefabs.Length; i++)
            {
                int index = i;
                GameObject btnObj = Instantiate(buttonPrefab, dogHatListContent);
                
                string displayName = (i == 0 || dogHatPrefabs[i] == null) 
                    ? GetDisplayName(i, customDogHatNames, "None") 
                    : GetDisplayName(i, customDogHatNames, dogHatPrefabs[i].name);
                
                btnObj.name = displayName + " Button";

                TMP_Text btnText = btnObj.GetComponentInChildren<TMP_Text>();
                if (btnText != null) btnText.text = displayName;
                
                SetButtonIcon(btnObj, i, dogHatIcons);

                Button btn = btnObj.GetComponent<Button>();
                if (btn != null) btn.onClick.AddListener(() => SelectDogHat(index));
            }
        }

        // 3. Generate Robber Hat Buttons
        if (robberHatListContent != null)
        {
            for (int i = 0; i < robberHatPrefabs.Length; i++)
            {
                int index = i;
                GameObject btnObj = Instantiate(buttonPrefab, robberHatListContent);

                string displayName = (i == 0 || robberHatPrefabs[i] == null) 
                    ? GetDisplayName(i, customRobberHatNames, "None") 
                    : GetDisplayName(i, customRobberHatNames, robberHatPrefabs[i].name);

                btnObj.name = displayName + " Button";

                TMP_Text btnText = btnObj.GetComponentInChildren<TMP_Text>();
                if (btnText != null) btnText.text = displayName;
                
                SetButtonIcon(btnObj, i, robberHatIcons);

                Button btn = btnObj.GetComponent<Button>();
                if (btn != null) btn.onClick.AddListener(() => SelectRobberHat(index));
            }
        }
    }

    private string CleanName(string rawName)
    {
        return rawName.Replace("(Clone)", "").Replace("_", " ").Trim();
    }

    public void SwitchToDogCategory()
    {
        isEditingDog = true;
        dogCategoryPanel.SetActive(true);
        robberCategoryPanel.SetActive(false);

        if (robberModel != null) robberModel.gameObject.SetActive(false);
        UpdateDogModelDisplay();
    }

    public void SwitchToRobberCategory()
    {
        isEditingDog = false;
        dogCategoryPanel.SetActive(false);
        robberCategoryPanel.SetActive(true);

        foreach (var dog in dogModels)
        {
            if (dog != null) dog.gameObject.SetActive(false);
        }

        if (robberModel != null)
        {
            robberModel.gameObject.SetActive(true);
            EquipHatOnModel(robberModel, robberHatPrefabs, currentRobberHatIndex, ref currentSpawnedRobberHat);
        }
        UpdateTextDisplays();
    }

    public void SelectDog(int index)
    {
        if (index < 0 || index >= dogModels.Length) return;
        currentDogIndex = index;
        PlayerPrefs.SetInt("SelectedDog", currentDogIndex);
        UpdateDogModelDisplay();
    }

    public void SelectDogHat(int hatIndex)
    {
        currentDogHatIndex = hatIndex;
        PlayerPrefs.SetInt("SelectedDogHat", currentDogHatIndex);
        if (isEditingDog) UpdateDogModelDisplay();
    }

    public void SelectRobberHat(int hatIndex)
    {
        currentRobberHatIndex = hatIndex;
        PlayerPrefs.SetInt("SelectedRobberHat", currentRobberHatIndex);
        if (!isEditingDog && robberModel != null)
        {
            EquipHatOnModel(robberModel, robberHatPrefabs, currentRobberHatIndex, ref currentSpawnedRobberHat);
        }
        UpdateTextDisplays();
    }

    private void UpdateDogModelDisplay()
    {
        for (int i = 0; i < dogModels.Length; i++)
        {
            if (dogModels[i] != null)
            {
                dogModels[i].gameObject.SetActive(i == currentDogIndex);
                if (i == currentDogIndex)
                {
                    EquipHatOnModel(dogModels[i], dogHatPrefabs, currentDogHatIndex, ref currentSpawnedDogHat);
                }
            }
        }
        UpdateTextDisplays();
    }

    private void EquipHatOnModel(Transform model, GameObject[] hatArray, int hatIndex, ref GameObject spawnedHatObj)
    {
        if (spawnedHatObj != null)
        {
            Destroy(spawnedHatObj);
        }

        if (hatIndex <= 0 || hatIndex >= hatArray.Length || hatArray[hatIndex] == null)
            return;

        // Find a bone named "HeadPoint" or "Head" to attach the hat. 
        Transform headPoint = FindChildRecursive(model, "HeadPoint");
        if (headPoint == null) headPoint = FindChildRecursive(model, "Head");
        if (headPoint == null) headPoint = model;

        spawnedHatObj = Instantiate(hatArray[hatIndex], headPoint);
        spawnedHatObj.transform.localPosition = Vector3.zero;
        spawnedHatObj.transform.localRotation = Quaternion.identity;
    }

    private void UpdateTextDisplays()
    {
        if (isEditingDog)
        {
            if (currentDogNameText != null && currentDogIndex < dogModels.Length && dogModels[currentDogIndex] != null)
            {
                currentDogNameText.text = GetDisplayName(currentDogIndex, customDogNames, dogModels[currentDogIndex].name);
            }
            if (currentHatNameText != null)
            {
                string hatName = (currentDogHatIndex == 0 || dogHatPrefabs[currentDogHatIndex] == null) 
                    ? GetDisplayName(currentDogHatIndex, customDogHatNames, "None") 
                    : GetDisplayName(currentDogHatIndex, customDogHatNames, dogHatPrefabs[currentDogHatIndex].name);
                currentHatNameText.text = hatName;
            }
        }
        else
        {
            if (currentDogNameText != null) currentDogNameText.text = "Robber";
            if (currentHatNameText != null)
            {
                string hatName = (currentRobberHatIndex == 0 || robberHatPrefabs[currentRobberHatIndex] == null) 
                    ? GetDisplayName(currentRobberHatIndex, customRobberHatNames, "None") 
                    : GetDisplayName(currentRobberHatIndex, customRobberHatNames, robberHatPrefabs[currentRobberHatIndex].name);
                currentHatNameText.text = hatName;
            }
        }
    }

    private Transform FindChildRecursive(Transform parent, string name)
    {
        if (parent.name == name) return parent;
        foreach (Transform child in parent)
        {
            Transform result = FindChildRecursive(child, name);
            if (result != null) return result;
        }
        return null;
    }

    public void SetButtonsInteractable(bool state)
    {
        if (dogListContent != null)
        {
            foreach (Button btn in dogListContent.GetComponentsInChildren<Button>())
            {
                if (btn != null && btn.targetGraphic != null) btn.interactable = state;
            }
        }
        if (dogHatListContent != null)
        {
            foreach (Button btn in dogHatListContent.GetComponentsInChildren<Button>())
            {
                if (btn != null && btn.targetGraphic != null) btn.interactable = state;
            }
        }
        if (robberHatListContent != null)
        {
            foreach (Button btn in robberHatListContent.GetComponentsInChildren<Button>())
            {
                if (btn != null && btn.targetGraphic != null) btn.interactable = state;
            }
        }
    }
}
