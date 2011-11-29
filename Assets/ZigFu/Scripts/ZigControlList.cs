using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ZigControlList : MonoBehaviour {
	public List<GameObject> Listeners = new List<GameObject>();
	
	public bool IsInSession { get; private set; }
	public Vector3 FocusPoint { get; private set; }
	
	public ZigTrackedUser TrackedUser { get; private set; }
	
	public void DoUpdate(ZigTrackedUser userData)
	{
		this.TrackedUser = userData;
		
		// if we aren't in session, but should be
		if (!this.IsInSession && userData.Hands.Count > 0) {
			NotifyListeners("Zig_OnSessionStart", this);
			this.IsInSession = true;
		}
		
		// if we are in session, but shouldn't be
		if (this.IsInSession && userData.Hands.Count == 0) {
			NotifyListeners("Zig_OnSessionEnd", this);
			this.IsInSession = false;
		}
	
		// at this point we know if we are in a session or not,
		// and we sent the start/end notifications. all thats
		// left is updating the controls if we're in session
		if (this.IsInSession) {
			NotifyListeners("Zig_OnSessionUpdate", this);
		}

		NotifyListeners("Zig_OnUpdate", userData);
	}
	
	// this allows nesting ZigControlList's
	void Zig_OnUpdate(ZigTrackedUser userData)
	{
		DoUpdate(userData);
	}
	
	void NotifyListeners(string eventName, object arg) 
	{
		foreach (GameObject go in Listeners) {
			go.SendMessage(eventName, arg, SendMessageOptions.DontRequireReceiver);
		}
	}	
	
	public GameObject StartingFocusedControl;
	public GameObject FocusedControl { get; private set; }
	public void SetFocus(GameObject target)
	{
		if (null != FocusedControl) {
			if (IsInSession) {
				FocusedControl.SendMessage("Zig_OnSessionEnd", this, SendMessageOptions.DontRequireReceiver);
			}
			Listeners.Remove(FocusedControl);
		}
		FocusedControl = target;
		if (null != target) {
			if (IsInSession) {
				target.SendMessage("Zig_OnSessionStart", this, SendMessageOptions.DontRequireReceiver);
			}
			if (!Listeners.Contains(FocusedControl)) {
				Listeners.Add(FocusedControl);
			}
		}
	}
	
	void Start()
	{
		SetFocus(StartingFocusedControl);
	}
}
