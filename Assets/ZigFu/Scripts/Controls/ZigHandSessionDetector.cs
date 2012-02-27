using UnityEngine;
using System;
using System.Collections.Generic;
/*
TODO:
 * sane userlost behavior
 */

public class SessionStartEventArgs : EventArgs
{
    public Vector3 FocusPoint { get; private set; }
    public SessionStartEventArgs(Vector3 fp) {
        FocusPoint = fp;
    }
}

public class SessionUpdateEventArgs : EventArgs
{
    public Vector3 HandPoint { get; private set; }
    public SessionUpdateEventArgs(Vector3 hp) {
        HandPoint = hp;
    }
}

public class ZigHandSessionDetector : MonoBehaviour {
    public bool StartOnSteady = true;
    public bool StartOnWave = true;
    public bool RotateToUser = true;
    
    public List<GameObject> listeners = new List<GameObject>();

    public Vector3 SessionBoundsOffset = new Vector3(0, 250, -300);
    public Vector3 SessionBounds = new Vector3(1000, 700, 1000);

    GameObject leftHandDetector;
    GameObject rightHandDetector;
    ZigJointId jointInSession;
    Vector3 focusPoint;
    ZigTrackedUser trackedUser;
    bool InSession;
    Bounds currentSessionBounds;

    public event EventHandler<SessionStartEventArgs> SessionStart;
    public event EventHandler<SessionUpdateEventArgs> SessionUpdate;
    public event EventHandler SessionEnd;

    protected virtual void OnSessionStart(Vector3 focusPoint) {
        notifyListeners("Session_Start", focusPoint);
        if (null != SessionStart) {
            SessionStart.Invoke(this, new SessionStartEventArgs(focusPoint));
        }
    }

    protected virtual void OnSessionUpdate(Vector3 handPoint) {
        notifyListeners("Session_Update", handPoint);
        if (null != SessionUpdate) {
            SessionUpdate.Invoke(this, new SessionUpdateEventArgs(handPoint));
        }
    }

    protected virtual void OnSessionEnd() {
        notifyListeners("Session_End", null);
        if (null != SessionEnd) {
            SessionEnd.Invoke(this, new EventArgs());
        }
    }

    void Awake() {
        leftHandDetector = new GameObject("LeftHandDetector");
        leftHandDetector.transform.parent = gameObject.transform;
        ZigMapJointToSession leftMap = leftHandDetector.AddComponent<ZigMapJointToSession>();
        leftMap.joint = ZigJointId.LeftHand;

        rightHandDetector = new GameObject("RightHandDetector");
        rightHandDetector.transform.parent = gameObject.transform;
        ZigMapJointToSession rightMap = rightHandDetector.AddComponent<ZigMapJointToSession>();
        rightMap.joint = ZigJointId.RightHand;

        if (StartOnSteady) {
            ZigSteadyDetector steadyLeft = leftHandDetector.AddComponent<ZigSteadyDetector>();
            steadyLeft.Steady += delegate(object sender, EventArgs ea) {
                CheckSessionStart((sender as ZigSteadyDetector).steadyPoint, ZigJointId.LeftHand);
            };

            ZigSteadyDetector steadyRight = rightHandDetector.AddComponent<ZigSteadyDetector>();
            steadyRight.Steady += delegate(object sender, EventArgs ea) {
                CheckSessionStart((sender as ZigSteadyDetector).steadyPoint, ZigJointId.RightHand);
            };
        }

        if (StartOnWave) {
            ZigWaveDetector waveLeft = leftHandDetector.AddComponent<ZigWaveDetector>();
            waveLeft.Wave += delegate(object sender, EventArgs ea) {
                CheckSessionStart((sender as ZigWaveDetector).wavePoint, ZigJointId.LeftHand);
            };

            ZigWaveDetector waveRight = rightHandDetector.AddComponent<ZigWaveDetector>();
            waveRight.Wave += delegate(object sender, EventArgs ea) {
                CheckSessionStart((sender as ZigWaveDetector).wavePoint, ZigJointId.RightHand);
            };
        }
    }

    void Zig_Attach(ZigTrackedUser user) {
        user.AddListener(leftHandDetector);
        user.AddListener(rightHandDetector);
        trackedUser = user;
    }

    void Zig_UpdateUser(ZigTrackedUser user) {
        if (InSession) {
            // get hand point for this frame, rotate if neccessary
            Vector3 hp = user.Skeleton[(int)jointInSession].Position;
            if (RotateToUser) hp = RotateHandPoint(hp);
            // make sure hand point is still within session bounds
            currentSessionBounds.center = (RotateToUser) ? RotateHandPoint(trackedUser.Position) : trackedUser.Position;
            if (!currentSessionBounds.Contains(hp)) {
                InSession = false;
                OnSessionEnd();
                return;
            }
            OnSessionUpdate(hp);
        }
    }

    void Zig_Detach(ZigTrackedUser user) {
        user.RemoveListener(leftHandDetector);
        user.RemoveListener(rightHandDetector);
        if (InSession) {
            InSession = false;
            OnSessionEnd();
        }
        trackedUser = null;
    }

    void CheckSessionStart(Vector3 point, ZigJointId joint) {
        if (InSession) return;

        Vector3 boundsCenter = (RotateToUser) ? RotateHandPoint(trackedUser.Position) : trackedUser.Position;
        currentSessionBounds = new Bounds(boundsCenter, SessionBounds);
        Vector3 fp = (RotateToUser) ? RotateHandPoint(point) : point;
        if (currentSessionBounds.Contains(fp)) {
            focusPoint = fp;
            jointInSession = joint;
            InSession = true;
            OnSessionStart(fp);
        }
    }

    void notifyListeners(string msgname, object arg) {
        SendMessage(msgname, arg, SendMessageOptions.DontRequireReceiver);
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

    Vector3 RotateHandPoint(Vector3 handPoint) {
        //TODO: Smoothing on CoM (so sudden CoM changes won't mess with the hand
        //      point too much)
        Vector3 rotateTarget = trackedUser.Position.normalized;

        // use line between com and sensor as Z
        Quaternion newOrientation = Quaternion.FromToRotation(rotateTarget, Vector3.forward);
        return newOrientation * handPoint;
    }

    void Session_Start(Vector3 focusPoint) {
        Debug.Log("HandSessionDetection: Session start");
    }

    void Session_Update(Vector3 handPoint) {
        Debug.Log("HandSessionDetection: Session update");
    }

    void Session_End() {
        Debug.Log("HandSessionDetection: Session end");
    }
}
