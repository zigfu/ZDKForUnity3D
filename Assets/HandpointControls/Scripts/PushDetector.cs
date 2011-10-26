using UnityEngine;
using System.Collections;

[RequireComponent(typeof(HandPointControl))]
public class PushDetector : MonoBehaviour {
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
	
    Fader pushFader;

	void Start()
	{
        pushFader = gameObject.AddComponent<Fader>();
        pushFader.direction = -Vector3.forward;
	}
	
	void Hand_Create(Vector3 pos)
	{
        pushFader.size = size;
        pushFader.initialValue = initialValue;
        pushFader.MoveTo(pos, initialValue);

		Hand_Update(pos);
	}
	
	bool sent_push;
	void Hand_Update(Vector3 pos)
	{
		// move slider if hand is out of its bounds (that way it always feels responsive)
		pushFader.MoveToContain(pos);
        pushFader.Hand_Update(pos);
	
		
		// quick out if in cooldown
		if (OpenNISessionManager.Instance.CoolingDown) return;
		
		// click logic
		if (!IsClicked)
        {
            if (ClickProgress == 1.0f) {
				ClickPosition = pos;
                IsClicked = true;
				clickPushTime = Time.time;
				//SendMessage("PushDetector_Push", SendMessageOptions.DontRequireReceiver);
            }
        }
        else // clicked
        {
            if (ClickProgress < 0.5) {
                if (IsClick(clickPushTime, ClickPosition, Time.time, pos)) {
					SendMessage("PushDetector_Click",SendMessageOptions.DontRequireReceiver);
				}
				
				SendMessage("PushDetector_Release", SendMessageOptions.DontRequireReceiver);
                IsClicked = false;
				sent_push = false;
            }
			else
			{
				if (!sent_push && !IsClick(clickPushTime, ClickPosition, Time.time, pos))
				{
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
	
	void Hand_Destroy()
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

	void SessionManager_Visualize()
	{
		GUILayout.Label("- PushDetector");
		GUILayout.Toggle(IsClicked, "PUSH");
	}
	

}
