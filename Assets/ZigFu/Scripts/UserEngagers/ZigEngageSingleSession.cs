using UnityEngine;
using System;
using System.Collections.Generic;

class ZigEngageSingleSession : MonoBehaviour {
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

    void DisengageUser(ZigTrackedUser user) {
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

        ZigHandSessionDetector hsd = go.AddComponent<ZigHandSessionDetector>();
        hsd.SessionStart += delegate {
            EngageUser(user);
        };
        hsd.SessionEnd += delegate {
            DisengageUser(user);
        };

        user.AddListener(go);
    }

    void Zig_UserLost(ZigTrackedUser user) {
        DisengageUser(user);
        Destroy(objects[user.Id]);
        objects.Remove(user.Id);
    }
}