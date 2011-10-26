using UnityEngine;
using System.Collections;
using System.Reflection;

public class SessionManagerVisualizer : MonoBehaviour {
	public bool Visible;
	void OnGUI()
	{
		if (Event.current.Equals(Event.KeyboardEvent("f1"))) {
			Visible = !Visible;
			Event.current.Use();
		}
		
		if (Visible) {
			OpenNISessionManager.Instance.DebugDrawListeners();
		}
	}
}
