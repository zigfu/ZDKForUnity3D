using UnityEngine;
using System.Collections;

public class ZigPushDetector : MonoBehaviour {
	public float size = 150.0f;
	public float initialValue = 0.2f;
	public float driftSpeed = 0.05f;
	
	public float clickTimeFrame = 1.0f;
    public float clickMaxDistance = 100;

    public float clickPushTime { get; private set; }
	public bool IsClicked { get; private set; }
	public Vector3 ClickPosition { get; private set; }
    public float ClickProgress {
        get {
            return pushFader.value;
        }
    }
	
    ZigFader pushFader;

    void Start()
	{
        pushFader = gameObject.AddComponent<ZigFader>();
        pushFader.direction = Vector3.forward;
	}

	void Session_Start(Vector3 focusPosition) {
        pushFader.size = size;
        pushFader.initialValue = initialValue;
        pushFader.MoveTo(focusPosition, initialValue);
	}
	
	void Session_Update(Vector3 handPosition)
	{
		// move slider if hand is out of its bounds (that way it always feels responsive)
		pushFader.MoveToContain(handPosition);
        pushFader.UpdatePosition(handPosition);
	
		// click logic
		if (!IsClicked) {
            if (ClickProgress == 1.0f) {
				ClickPosition = handPosition;
                IsClicked = true;
				clickPushTime = Time.time;
				SendMessage("PushDetector_Push", SendMessageOptions.DontRequireReceiver);
            }
        }
        else { // clicked
           if (ClickProgress < 0.5) {
                if (IsClick(clickPushTime, ClickPosition, Time.time, handPosition)) {
					SendMessage("PushDetector_Click",SendMessageOptions.DontRequireReceiver);
				}
				
				SendMessage("PushDetector_Release", SendMessageOptions.DontRequireReceiver);
                IsClicked = false;
            }
        }
		
		// drift the slider to the initial position, if we aren't clicked
		if (!IsClicked) {
			float delta = initialValue - ClickProgress;
            pushFader.MoveTo(handPosition, ClickProgress + (delta * driftSpeed));
		}	
	}
	
	void Session_End() {
		if (IsClicked) {
			SendMessage("PushDetector_Release", SendMessageOptions.DontRequireReceiver);
			IsClicked = false;
		}
	}
	
    bool IsClick(float t1, Vector3 p1, float t2, Vector3 p2) {
        Vector3 delta = (p2 - p1);
        delta.z = 0;
        return ((t2 - t1 < clickTimeFrame) && (delta.magnitude < clickMaxDistance));
    }
}
