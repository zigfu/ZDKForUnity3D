using UnityEngine;
using System.Collections;

public enum SwipeDetectorDirection
{
	Horizontal,
	Vertical,
}

[RequireComponent(typeof(HandPointControl))]
public class SwipeDetector : MonoBehaviour {
	public float size = 250.0f;
	public SwipeDetectorDirection direction = SwipeDetectorDirection.Horizontal;

    public Fader swipeFader { get; private set; }
	string dirStr;
	
	public bool IsSwiped { get; private set; }
	float swipedValue;
	
	// Use this for initialization
	void Awake () {
        swipeFader = gameObject.AddComponent<Fader>();
        swipeFader.direction = SwipeDirectionToVector(direction);
	}
	
	// Update is called once per frame
	void Update () {
	
	}
	
	void Hand_Create(Vector3 pos)
	{
        swipeFader.size = size;
        swipeFader.initialValue = 0.5f;
        swipeFader.MoveTo(pos, 0.5f);

		Hand_Update(pos);
	}
	
	void Hand_Update(Vector3 pos)
	{
		// move fader if hand is out of its bounds (that way it always feels responsive)
		swipeFader.MoveToContain(pos);
        swipeFader.Hand_Update(pos);
	
		// quick out if in cooldown
		if (OpenNISessionManager.Instance.CoolingDown) return;
		
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
	
	void Hand_Destroy()
	{
		if (IsSwiped) {
			SendMessage("SwipeDetector_Release", SendMessageOptions.DontRequireReceiver);
			IsSwiped = false;
		}
	}
	
	string FaderValueToEventName(float val, SwipeDetectorDirection dir)
	{
		if (Mathf.Approximately(val, 1.0f)) {
			switch (dir) {
				case SwipeDetectorDirection.Horizontal : return "Right";
				case SwipeDetectorDirection.Vertical : return "Up";
			}
		}
			
		if (Mathf.Approximately(val, 0.0f)) {
			switch (dir) {
				case SwipeDetectorDirection.Horizontal : return "Left";
				case SwipeDetectorDirection.Vertical : return "Down";
			}
		}
		
		return "";
	}
	
	Vector3 SwipeDirectionToVector(SwipeDetectorDirection dir)
	{
		switch (dir) {
			case SwipeDetectorDirection.Horizontal: return (OpenNIContext.Instance.Mirror) ? Vector3.left : Vector3.right;
			case SwipeDetectorDirection.Vertical: return Vector3.up;
		}
		return Vector3.zero;
	}
	
	void SessionManager_Visualize()
	{
		GUILayout.Label("- Swipe Detector");
        if (IsSwiped) {
            GUILayout.Label(dirStr);
        } else {
            GUILayout.HorizontalSlider(swipeFader.value, 0.0f, 1.0f);
        }
	}
}
