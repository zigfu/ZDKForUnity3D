using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ZigEngageAllUsers : MonoBehaviour {
	
	public GameObject InstantiatePerUser;
	Dictionary<int, GameObject> objects = new Dictionary<int, GameObject>();
	
	void Zig_NewUser(ZigTrackedUser user) 
	{
		GameObject o = Instantiate(InstantiatePerUser) as GameObject;
		objects[user.UserId] = o;
		user.Controls.Listeners.Add(o);
	}
	
	void Zig_LostUser(ZigTrackedUser user)
	{
		Destroy(objects[user.UserId]);
		objects.Remove(user.UserId);
	}
}
