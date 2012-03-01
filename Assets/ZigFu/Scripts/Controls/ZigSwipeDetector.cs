using UnityEngine;
using System.Collections;

public class ZigSwipeDetector : MonoBehaviour {
    public Vector2 size = new Vector2(300, 250);
    public ZigFader horizFader { get; private set; }
    public ZigFader vertFader { get; private set; }
	
	// Use this for initialization
	void Awake () {
        horizFader = gameObject.AddComponent<ZigFader>();
        horizFader.direction = Vector3.right;
        horizFader.driftAmount = 15;
        horizFader.Edge += delegate {
            if (Mathf.Approximately(horizFader.value, 0)) {
                DoSwipe("Left");
            }
            else {
                DoSwipe("Right");
            }
        };

        vertFader = gameObject.AddComponent<ZigFader>();
        vertFader.direction = Vector3.up;
        vertFader.driftAmount = 10;
        vertFader.Edge += delegate {
            if (Mathf.Approximately(horizFader.value, 0)) {
                DoSwipe("Down");
            }
            else {
                DoSwipe("Up");
            }
        };
	}

    void DoSwipe(string direction) {
        SendMessage("SwipeDetector_" + direction, this, SendMessageOptions.DontRequireReceiver);
        SendMessage("SwipeDetector_Swipe", direction, SendMessageOptions.DontRequireReceiver);
    }
}
