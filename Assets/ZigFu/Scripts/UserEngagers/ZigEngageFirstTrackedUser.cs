using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

public class ZigEngageFirstTrackedUser : MonoBehaviour {
	public GameObject EngagedUser;
	
	int engagedUserId = 0;
	
	void Start() 
	{
		// make sure we get zig events
		ZigInput.Instance.AddListener(gameObject);
	}
	
	void Zig_LostUser(ZigTrackedUser user)
	{
		if (user.UserData.Id == engagedUserId) {
			// lost user
			engagedUserId = 0;
		}
	}
	
	void Zig_Update(ZigInput zig)
	{
		if (engagedUserId == 0) {
			foreach (ZigTrackedUser trackedUser in zig.TrackedUsers.Values) {
				if (trackedUser.UserData.Tracked) {
					engagedUserId = trackedUser.UserData.Id;
					trackedUser.AddListener(EngagedUser);
					break;
				}
			}
		}
	}
}
