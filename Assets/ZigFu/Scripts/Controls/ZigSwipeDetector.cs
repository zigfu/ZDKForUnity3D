using UnityEngine;
using System.Collections;

public enum ZigSwipeDetectorDirection
{
	Horizontal,
	Vertical,
}

public class ZigSwipeDetector : MonoBehaviour {
	public float size = 250.0f;
	public ZigSwipeDetectorDirection direction = ZigSwipeDetectorDirection.Horizontal;

    public ZigFader swipeFader { get; private set; }
	string dirStr;
	
	public bool IsSwiped { get; private set; }
	float swipedValue;
	
	// Use this for initialization
	void Awake () {
        swipeFader = gameObject.AddComponent<ZigFader>();
        swipeFader.direction = SwipeDirectionToVector(direction);
	}
	
	void Zig_OnSessionStart(ZigEventArgs args)
	{
        swipeFader.size = size;
        swipeFader.initialValue = 0.5f;
        swipeFader.MoveTo(args.FocusPoint, 0.5f);
	}
	
	void Zig_OnSessionUpdate(ZigEventArgs args)
	{
		Vector3 pos = args.HandPosition;
		
		// move fader if hand is out of its bounds (that way it always feels responsive)
		swipeFader.MoveToContain(pos);
		
        // force the fader to update its value, after its been moved
		swipeFader.ForceUpdate(pos);
	
		// swipe logic
		if (!IsSwiped) {
			
            if (Mathf.Approximately(swipeFader.value, 1.0f) || 
			    Mathf.Approximately(swipeFader.value, 0.0f)) {
				
				IsSwiped = true;
				swipedValue = swipeFader.value;
				SendMessage("SwipeDetector_Swipe", swipedValue, SendMessageOptions.DontRequireReceiver);
				dirStr = FaderValueToEventName(swipedValue, direction);
				SendMessage("SwipeDetector_" + dirStr, SendMessageOptions.DontRequireReceiver);
            }
        }
        else { // Swiped
            if (Mathf.Abs(swipedValue - swipeFader.value) >= 0.5f) {
				IsSwiped = false;
				SendMessage("SwipeDetector_Release", SendMessageOptions.DontRequireReceiver);
            }
        }
		
		// drift the slider to the initial position, if we aren't in a swipe
		if (!IsSwiped) {
			float delta = 0.5f - swipeFader.value;
            swipeFader.MoveTo(pos, swipeFader.value + (delta * 0.02f));
		}	
	}
	
	void Zig_OnSessionEnd()
	{
		if (IsSwiped) {
			SendMessage("SwipeDetector_Release", SendMessageOptions.DontRequireReceiver);
			IsSwiped = false;
		}
	}
	
	string FaderValueToEventName(float val, ZigSwipeDetectorDirection dir)
	{
		if (Mathf.Approximately(val, 1.0f)) {
			switch (dir) {
				case ZigSwipeDetectorDirection.Horizontal : return "Right";
				case ZigSwipeDetectorDirection.Vertical : return "Up";
			}
		}
			
		if (Mathf.Approximately(val, 0.0f)) {
			switch (dir) {
				case ZigSwipeDetectorDirection.Horizontal : return "Left";
				case ZigSwipeDetectorDirection.Vertical : return "Down";
			}
		}
		
		return "";
	}
	
	Vector3 SwipeDirectionToVector(ZigSwipeDetectorDirection dir)
	{
		switch (dir) {
			//case SwipeDetectorDirection.Horizontal: return (OpenNIContext.Instance.Mirror) ? Vector3.left : Vector3.right;
			case ZigSwipeDetectorDirection.Horizontal: return Vector3.left;
			case ZigSwipeDetectorDirection.Vertical: return Vector3.up;
		}
		return Vector3.zero;
	}
}
