using UnityEngine;
using UnityEditor;
using System.IO;

public class IconGeneratorWindow : EditorWindow
{
    private GameObject targetPrefab;
    private int imageSize = 256;
    private Vector3 cameraOffset = new Vector3(0, 1.2f, 2.5f);
    private Vector3 cameraEulerAngles = new Vector3(15f, 180f, 0f);
    private Color backgroundColor = new Color(0, 0, 0, 0); // Transparent background

    [MenuItem("Tools/Icon Generator")]
    public static void ShowWindow()
    {
        GetWindow<IconGeneratorWindow>("Icon Generator");
    }

    private void OnGUI()
    {
        GUILayout.Label("Generate Transparent Icon from Prefab", EditorStyles.boldLabel);
        
        targetPrefab = (GameObject)EditorGUILayout.ObjectField("Target Prefab", targetPrefab, typeof(GameObject), false);
        imageSize = EditorGUILayout.IntField("Image Size", imageSize);
        
        GUILayout.Space(10);
        GUILayout.Label("Camera Setup", EditorStyles.boldLabel);
        cameraOffset = EditorGUILayout.Vector3Field("Camera Offset", cameraOffset);
        cameraEulerAngles = EditorGUILayout.Vector3Field("Camera Rotation", cameraEulerAngles);

        GUILayout.Space(20);

        if (GUILayout.Button("Generate Icon", GUILayout.Height(40)))
        {
            if (targetPrefab == null)
            {
                EditorUtility.DisplayDialog("Error", "Please assign a Target Prefab first.", "OK");
                return;
            }
            
            GenerateIcon();
        }
    }

    private void GenerateIcon()
    {
        // 1. Create a safe, hidden position far from the main scene
        Vector3 spawnPos = new Vector3(0, -10000, 0);

        // 2. Spawn the prefab
        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(targetPrefab);
        instance.transform.position = spawnPos;
        instance.transform.rotation = Quaternion.identity;

        // 3. Create a temporary camera
        GameObject camObj = new GameObject("IconCamera");
        camObj.transform.position = spawnPos + cameraOffset;
        camObj.transform.eulerAngles = cameraEulerAngles;
        
        Camera cam = camObj.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = backgroundColor;
        cam.orthographic = false;
        cam.fieldOfView = 60f;

        // 4. Create a temporary light so the model isn't dark
        GameObject lightObj = new GameObject("IconLight");
        Light light = lightObj.AddComponent<Light>();
        light.type = LightType.Directional;
        light.transform.eulerAngles = new Vector3(45, -45, 0);
        light.intensity = 1.5f;

        // 5. Setup RenderTexture with Alpha channel
        RenderTexture rt = new RenderTexture(imageSize, imageSize, 24, RenderTextureFormat.ARGB32);
        rt.antiAliasing = 8;
        cam.targetTexture = rt;

        // 6. Render the Camera view
        Texture2D screenShot = new Texture2D(imageSize, imageSize, TextureFormat.ARGB32, false);
        cam.Render();
        RenderTexture.active = rt;
        screenShot.ReadPixels(new Rect(0, 0, imageSize, imageSize), 0, 0);
        screenShot.Apply();

        // 7. Cleanup
        cam.targetTexture = null;
        RenderTexture.active = null;
        DestroyImmediate(rt);
        DestroyImmediate(camObj);
        DestroyImmediate(instance);
        DestroyImmediate(lightObj);

        // 8. Save the PNG to the Assets folder
        byte[] bytes = screenShot.EncodeToPNG();
        DestroyImmediate(screenShot);

        string directoryPath = Application.dataPath + "/Icons";
        if (!Directory.Exists(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }
        
        string fileName = targetPrefab.name + "_Icon.png";
        string fullPath = Path.Combine(directoryPath, fileName);
        File.WriteAllBytes(fullPath, bytes);

        // 9. Refresh Unity so the file appears
        AssetDatabase.Refresh();
        
        // 10. Automatically set the imported image type to "Sprite"
        string relativePath = "Assets/Icons/" + fileName;
        TextureImporter importer = AssetImporter.GetAtPath(relativePath) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.SaveAndReimport();
        }

        EditorUtility.DisplayDialog("Success", $"Icon successfully saved to:\n{relativePath}", "Awesome!");
    }
}
