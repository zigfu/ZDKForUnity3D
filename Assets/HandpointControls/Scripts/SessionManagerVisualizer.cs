using UnityEngine;
using System.Collections;
using System.Reflection;

public class SessionManagerVisualizer : MonoBehaviour {
	void OnGUI()
	{
		SessionManager.Instance.DebugDrawListeners();
	}
}
