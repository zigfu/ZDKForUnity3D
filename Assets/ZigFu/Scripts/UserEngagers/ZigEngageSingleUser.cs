using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ZigEngageSingleUser : MonoBehaviour {
	public ZigEngagementTrigger EngagementTrigger;
	public GameObject EngagedUser;
	
	Dictionary<int, GameObject> objects = new Dictionary<int, GameObject>();
	
	int engagedUserId = 0;
	
	void EngageTrigger(ZigTrackedUser user)
	{
		if (0 == engagedUserId) {
			engagedUserId = user.UserData.Id;
			user.AddListener(EngagedUser);
			
			// TODO: send message
		}
	}
	
	void DisengageTrigger(ZigTrackedUser user)
	{
		if (engagedUserId == user.UserData.Id) {
			engagedUserId = 0;
			// TODO: send message
			
			foreach (var i in objects) {
				i.Value.SendMessage("Reset", SendMessageOptions.DontRequireReceiver);
			}
		}
	}
	
	void Zig_NewUser(ZigTrackedUser user) 
	{
		ZigEngagementTrigger o = Instantiate(EngagementTrigger) as ZigEngagementTrigger;
		o.Init(this, user);
		o.transform.parent = gameObject.transform;
		objects[user.UserData.Id] = o.gameObject;
		user.AddListener(o.gameObject);
	}
	
	void Zig_LostUser(ZigTrackedUser user)
	{
		DisengageTrigger(user);
		Destroy(objects[user.UserData.Id]);
		objects.Remove(user.UserData.Id);
	}
}
