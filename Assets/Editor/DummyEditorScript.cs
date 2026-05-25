using UnityEditor;
using UnityEngine;

public class DummyEditorScript : EditorWindow
{
    // This script exists solely to force Unity to compile the Assembly-CSharp-Editor.dll
    // which resolves "Failed to find entry-points:" assembly resolution errors.
}
