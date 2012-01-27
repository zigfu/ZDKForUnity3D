using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ZigEngageAllUsers : MonoBehaviour {
	
	public GameObject InstantiatePerUser;
	Dictionary<int, GameObject> objects = new Dictionary<int, GameObject>();
	
	void Zig_NewUser(ZigTrackedUser user) 
	{
		GameObject o = Instantiate(InstantiatePerUser) as GameObject;
		objects[user.UserData.Id] = o;
		user.AddListener(o);
	}
	
	void Zig_LostUser(ZigTrackedUser user)
	{
		Destroy(objects[user.UserData.Id]);
		objects.Remove(user.UserData.Id);
	}
}
