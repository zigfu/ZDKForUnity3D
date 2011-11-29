using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class tester : MonoBehaviour {
	
	public GameObject obj;
	Dictionary<int, GameObject> objects = new Dictionary<int, GameObject>();
	
	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
	
	}
	
	void Zig_NewUser(ZigTrackedUser user) 
	{
		GameObject o = Instantiate(obj) as GameObject;
		objects[user.UserId] = o;
		user.Controls.Listeners.Add(o);
	}
	
	void Zig_LostUser(ZigTrackedUser user)
	{
		Destroy(objects[user.UserId]);
		objects.Remove(user.UserId);
	}
}
