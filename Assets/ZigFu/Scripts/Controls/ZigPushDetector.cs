using UnityEngine;
using System.Collections;

public class ZigPushDetector : MonoBehaviour {
	public float size = 150.0f;
	public float initialValue = 0.2f;
	public float driftSpeed = 0.05f;
	
	public float clickTimeFrame = 1.0f;
    public float clickMaxDistance = 100; //??

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
	
	void Zig_OnSessionStart(ZigEventArgs args)
	{
        pushFader.size = size;
        pushFader.initialValue = initialValue;
        pushFader.MoveTo(args.FocusPoint, initialValue);
	}
	
	bool sent_push;
	void Zig_OnSessionUpdate(ZigEventArgs args)
	{
		Vector3 pos = args.HandPosition;
		
		// move slider if hand is out of its bounds (that way it always feels responsive)
		pushFader.MoveToContain(pos);
        pushFader.ForceUpdate(pos);
	
		// click logic
		if (!IsClicked) {
            if (ClickProgress == 1.0f) {
				ClickPosition = pos;
                IsClicked = true;
				clickPushTime = Time.time;
				//SendMessage("PushDetector_Push", SendMessageOptions.DontRequireReceiver);
            }
        }
        else { // clicked
           if (ClickProgress < 0.5) {
                if (IsClick(clickPushTime, ClickPosition, Time.time, pos)) {
					SendMessage("PushDetector_Click",SendMessageOptions.DontRequireReceiver);
				}
				
				SendMessage("PushDetector_Release", SendMessageOptions.DontRequireReceiver);
                IsClicked = false;
				sent_push = false;
            } else {
				if (!sent_push && !IsClick(clickPushTime, ClickPosition, Time.time, pos)) {
					SendMessage("PushDetector_Push", SendMessageOptions.DontRequireReceiver);
					sent_push = true;
				}
			}
        }
		
		// drift the slider to the initial position, if we aren't clicked
		if (!IsClicked) {
			float delta = initialValue - ClickProgress;
            pushFader.MoveTo(pos, ClickProgress + (delta * driftSpeed));
		}	
	}
	
	void Zig_OnSessionEnd()
	{
		if (IsClicked) {
			SendMessage("PushDetector_Release", SendMessageOptions.DontRequireReceiver);
			IsClicked = false;
		}
	}
	
    bool IsClick(float t1, Vector3 p1, float t2, Vector3 p2)
    {
        Vector3 delta = (p2 - p1);
        delta.z = 0;
        return ((t2 - t1 < clickTimeFrame) && (delta.magnitude < clickMaxDistance));
    }
}
