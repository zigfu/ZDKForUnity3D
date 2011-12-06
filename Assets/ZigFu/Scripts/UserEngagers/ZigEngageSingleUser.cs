using UnityEngine;
using System.Collections;

public class ZigEngageSingleUser : MonoBehaviour {
	public GameObject engagedUser;
	int engagedUsedId;
	
	// Messages from user tracker
	
	void Zig_NewUser(ZigTrackedUser user)
	{
		user.Controls.AddControl(gameObject);
	}

	void Zig_LostUser(ZigTrackedUser user)
	{
		user.Controls.RemoveControl(gameObject);
	}
	
	// Messages from control list
	
	void Zig_OnSessionStart(ZigEventArgs args)
	{
		// no user engaged yet
		if (0 == engagedUsedId) {
			engagedUsedId = args.user.UserId;
			args.user.Controls.AddControl(engagedUser);
		}
	}
	
	void Zig_OnSessionEnd(ZigEventArgs args)
	{
		// engaged user was us
		if (args.user.UserId == engagedUsedId) {
			engagedUsedId = 0;
			args.user.Controls.RemoveControl(engagedUser);
		}
	}
}
	