using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using OpenNI;
public class objectPerUser : MonoBehaviour {
    public Vector3 scale = new Vector3(.001f, .001f, .001f);
	public GameObject userObject;
	public OpenNIUserTracker UserTracker;
	Dictionary<int,GameObject> userObjMap = new Dictionary<int, GameObject>();

    // Use this for initialization
	void Start () {
        if (!UserTracker) {
            UserTracker = GetComponent<OpenNIUserTracker>();
        }
        if (!UserTracker) {
            UserTracker = gameObject.AddComponent<OpenNIUserTracker>();
        }


	}
	
	// Update is called once per frame
	void Update () {
		foreach (KeyValuePair<int,GameObject> entry in userObjMap)
		{
			entry.Value.transform.localPosition = Vector3.Scale(scale,UserTracker.GetUserCenterOfMass(entry.Key));
		}
	}

    void UserDetected(NewUserEventArgs e)
	{
		Vector3 com = UserTracker.GetUserCenterOfMass(e.ID);
		GameObject clone = (GameObject)Instantiate(userObject,Vector3.Scale(scale,com),Quaternion.identity);		
		clone.transform.parent = transform;
		clone.transform.localPosition = Vector3.Scale(scale,com);
		userObjMap.Add(e.ID,clone);
	}
	void UserLost(UserLostEventArgs e)
	{
		
		GameObject obj = userObjMap[e.ID];
		userObjMap.Remove(e.ID);
		Destroy(obj);
	}
	
}
