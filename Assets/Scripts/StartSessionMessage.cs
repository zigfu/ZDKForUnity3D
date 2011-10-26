using UnityEngine;
using System.Collections;

public class StartSessionMessage : MonoBehaviour {
    void OnGUI()
    {
        if (!OpenNISessionManager.InSession) {
            GUILayout.BeginArea(new Rect(Screen.width / 2 - 150, 50, 300, 300));
            GUILayout.Box("Please perform a focus gesture to start the session");
            GUILayout.EndArea();
        }
    }
}
