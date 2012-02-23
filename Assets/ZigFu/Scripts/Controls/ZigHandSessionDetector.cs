using UnityEngine;
using System;
using System.Collections.Generic;

public class ZigHandSessionDetector : MonoBehaviour {
    public List<GameObject> listeners = new List<GameObject>();
    GameObject leftHandDetector;
    GameObject rightHandDetector;
    ZigJointId jointInSession;
    bool InSession;
    
    void Awake() {
        leftHandDetector = new GameObject("LeftHandDetector");
        leftHandDetector.transform.parent = gameObject.transform;
        ZigMapJointToSession leftMap = leftHandDetector.AddComponent<ZigMapJointToSession>();
        leftMap.joint = ZigJointId.LeftHand;
        leftHandDetector.AddComponent<ZigSteadyDetector>();
        //leftHandDetector.AddComponent<ZigWaveDetector>();

        rightHandDetector = new GameObject("RightHandDetector");
        rightHandDetector.transform.parent = gameObject.transform;
        ZigMapJointToSession rightMap = leftHandDetector.AddComponent<ZigMapJointToSession>();
        rightMap.joint = ZigJointId.RightHand;
        rightHandDetector.AddComponent<ZigSteadyDetector>();
        //rightHandDetector.AddComponent<ZigWaveDetector>();

    }

    void Zig_Attach(ZigTrackedUser user) {
        user.AddListener(leftHandDetector);
        user.AddListener(rightHandDetector);
    }

    void Zig_OnUserUpdate(ZigTrackedUser user) {
        if (InSession) {
            // rotate point
            // check if point is in bounds
            notifyListeners("Session_Update", user.Skeleton[(int)jointInSession]);
        }
    }

    void Zig_Detach(ZigTrackedUser user) {
        user.RemoveListener(leftHandDetector);
        user.RemoveListener(rightHandDetector);
    }

    void notifyListeners(string msgname, object arg) {
        for (int i = 0; i < listeners.Count; ) {
            GameObject go = listeners[i];
            if (go) {
                go.SendMessage(msgname, arg, SendMessageOptions.DontRequireReceiver);
                i++;
            }
            else {
                listeners.RemoveAt(i);
            }
        }
    }
}
