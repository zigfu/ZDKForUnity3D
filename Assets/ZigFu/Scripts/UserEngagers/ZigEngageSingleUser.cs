using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ZigEngageSingleUser : MonoBehaviour {
    public bool SkeletonTracked = true;
    public bool RaiseHand;
    public bool Wave;
    public bool SingleUserPosition;

	public GameObject EngagedUser;
	
	Dictionary<int, GameObject> objects = new Dictionary<int, GameObject>();

    ZigTrackedUser engagedTrackedUser;

    void Start() {
        // make sure we get zig events
        ZigInput.Instance.AddListener(gameObject);
    }

	void EngageUser(ZigTrackedUser user) {
		if (null == engagedTrackedUser) {
            engagedTrackedUser = user;
			if (null != EngagedUser) user.AddListener(EngagedUser);
            SendMessage("UserEngaged", this, SendMessageOptions.DontRequireReceiver);
		}
	}
	
	void DisengageUser(ZigTrackedUser user)	{
        if (user == engagedTrackedUser) {
            if (null != EngagedUser) user.RemoveListener(EngagedUser);
            engagedTrackedUser = null;
            SendMessage("UserDisengaged", this, SendMessageOptions.DontRequireReceiver);
        }
	}
	
	void Zig_UserFound(ZigTrackedUser user) {
        // create gameobject to listen for events for this user
        GameObject go = new GameObject("WaitForEngagement" + user.Id);
        go.transform.parent = transform;
		objects[user.Id] = go;

        // add various detectors & events

        if (Wave) {
            ZigWaveDetector wd = go.AddComponent<ZigWaveDetector>();
            wd.Wave += delegate {
                EngageUser(user);
            };
        }
        if (RaiseHand) {
            ZigHandRaiseDetector hrd = go.AddComponent<ZigHandRaiseDetector>();
            hrd.HandRaise += delegate {
                EngageUser(user);
            };
        }

        // attach the new object to the new user
		user.AddListener(go);
	}
	
	void Zig_UserLost(ZigTrackedUser user) {
        DisengageUser(user);
		Destroy(objects[user.Id]);
		objects.Remove(user.Id);
	}

    void Zig_Update(ZigInput zig) {
        if (SkeletonTracked && null == engagedTrackedUser) {
            foreach (ZigTrackedUser trackedUser in zig.TrackedUsers.Values) {
                if (trackedUser.SkeletonTracked) {
                    EngageUser(trackedUser);
                }
            }
        }
    }

    public void Reset() {
        if (null != engagedTrackedUser) {
            DisengageUser(engagedTrackedUser);
        }
    }
}
