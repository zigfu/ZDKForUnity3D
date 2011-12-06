using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ZigEventArgs
{
	public ZigControlList sender { get; private set; }
	public ZigTrackedUser user { get; private set; }
	public ZigEventArgs(ZigControlList sender, ZigTrackedUser user)
	{
		this.sender = sender;
		this.user = user;
	}
	
	public Vector3 FocusPoint { 
		get {
			return sender.FocusPoint;
		}
	}
	
	public Vector3 HandPosition {
		get {
			return user.Hands[user.PrimaryHand];
		}
	}
}

public class ZigControlList : MonoBehaviour {
	public List<GameObject> Listeners = new List<GameObject>();
	
	public bool IsInSession { get; private set; }
	public Vector3 FocusPoint { get; private set; }
	public ZigTrackedUser user { get; private set; }
	
	public void DoUpdate(ZigTrackedUser userData)
	{
		user = userData;
		ZigEventArgs zea = new ZigEventArgs(this, userData);
		
		// if we aren't in session, but should be
		if (!this.IsInSession && userData.Hands.Count > 0) {
			this.IsInSession = true;
			NotifyListeners("Zig_OnSessionStart", zea);
		}
		
		// if we are in session, but shouldn't be
		if (this.IsInSession && userData.Hands.Count == 0) {
			NotifyListeners("Zig_OnSessionEnd", zea);
			this.IsInSession = false;
		}
	
		// at this point we know if we are in a session or not,
		// and we sent the start/end notifications. all thats
		// left is updating the controls if we're in session
		if (this.IsInSession) {
			NotifyListeners("Zig_OnSessionUpdate", zea);
		}

		NotifyListeners("Zig_OnUserUpdate", zea);
	}
	
	// this allows nesting ZigControlList's
	void Zig_OnUserUpdate(ZigEventArgs args)
	{
		DoUpdate(args.user);
	}
	
	void NotifyListeners(string eventName, object arg) 
	{
		foreach (GameObject go in new List<GameObject>(Listeners)) {
			go.SendMessage(eventName, arg, SendMessageOptions.DontRequireReceiver);
		}
	}	
	
	public void AddControl(GameObject listener)
	{
		if (null != listener && !Listeners.Contains(listener)) {
			Listeners.Add(listener);
			if (IsInSession) {
				listener.SendMessage("Zig_OnSessionStart", 
				                     new ZigEventArgs(this, this.user), 
				                     SendMessageOptions.DontRequireReceiver);
			}
		}
	}
	
	public void RemoveControl(GameObject listener)
	{
		if (null != listener && Listeners.Contains(listener)) {
			Listeners.Remove(listener);
			if (IsInSession) {
				listener.SendMessage("Zig_OnSessionEnd", 
				                     new ZigEventArgs(this, this.user), 
				                     SendMessageOptions.DontRequireReceiver);
			}
		}
	}
	
	public GameObject StartingFocusedControl;
	public GameObject FocusedControl { get; private set; }
	public void SetFocus(GameObject target)
	{
		RemoveControl(FocusedControl);
		FocusedControl = target;
		AddControl(target);
	}
	
	void Start()
	{
		SetFocus(StartingFocusedControl);
	}
	
	Color colorInSession = Color.green;
	Color colorNotInSession = Color.red;
	Color colorObjectName = Color.white;
	Color colorHpcName = Color.grey;
	
	void Zig_Visualize()
	{
		Color original = GUI.color;
		
		GUILayout.BeginVertical("box");
		GUI.color = IsInSession ? colorInSession : colorNotInSession;
		GUILayout.Label("Controls List");
		
		if (Application.isWebPlayer) {
			GUI.color = Color.yellow;
			GUILayout.Label("[Web player]");
		}
		
		foreach (GameObject go in Listeners)
		{
            if (!go) continue;
			GUILayout.BeginVertical("box");
			GUI.color = colorObjectName;
			GUILayout.Label(go.name);
			GUI.color = colorHpcName;
			go.SendMessage("Zig_OnVisualize", SendMessageOptions.DontRequireReceiver);
			GUILayout.EndVertical();
		}
		GUILayout.EndVertical();
		GUI.color = original;
	}
}
