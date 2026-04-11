using UnityEditor;
using UnityEngine;

public class MiniMapWindow : EditorWindow
{
    [MenuItem("Window/MiniMap Camera")]
    public static void ShowWindow()
    {
        GetWindow<MiniMapWindow>("MiniMap Camera");
    }

    void OnGUI()
    {
        Camera miniMapCam = GameObject.Find("MiniMapCamera")?.GetComponent<Camera>();
        if (miniMapCam != null && miniMapCam.targetTexture != null)
        {
            GUI.DrawTexture(new Rect(10, 10, 256, 256), miniMapCam.targetTexture, ScaleMode.ScaleToFit, false);
        }
        else
        {
            GUILayout.Label("MiniMapCamera chưa có TargetTexture");
        }
    }
}