using UnityEngine;
using System.Collections;

public class ItemSelector : MonoBehaviour {
	public Fader fader;
	public int numItems = 3;
	public float hysterisis = .1f;
    public float scrollRegion = .2f;
    public bool dontUpdateWhenPushed = true;

    // vars for scrolling repeat logic. 
    // TODO: possibly use an Input axis for this functionality
    public bool RepeatScrolling = true;
    public float InitialWaitTime = 0.6f;
    public float WaitTimeModifier = 0.75f;
    public float MinWaitTime = 0.15f;
    public float MaxWaitTime = 2.0f;

    public bool scrolling { get; private set; }

	int index;
	public int selectionIndex { 
		get {
			return index;
		}
		private set {
			index = Mathf.Clamp(value, 0, numItems-1);
			SendMessage("ItemSelector_Select", selectionIndex, SendMessageOptions.DontRequireReceiver);			
		}
	}
	
	public float minValue {
		get {
            return (selectionIndex == 0) ? scrollRegion : scrollRegion + (selectionIndex * itemWidth) - hysterisis;
		}
	}
	
	public float maxValue {
		get {
            return (selectionIndex == numItems - 1) ? 1.0f - scrollRegion : scrollRegion + ((selectionIndex + 1) * itemWidth) + hysterisis;
		}
	}
	
	public float itemWidth {
		get {
			return (1.0f - (2 * scrollRegion)) / (float)numItems;
		}
	}
	
	// push protection
    bool pushed;
	void PushDetector_Push()
	{
        if (dontUpdateWhenPushed) {
            pushed = true;
        }
	}

    void PushDetector_Release()
    {
        pushed = false;
    }

	void Start()
	{
		if (null == fader) {
			Debug.LogError("You need a fader!");
		}
	}
	
	void Hand_Update(Vector3 pos)
	{
            UpdateSelector(fader.value);
	}

    // scrolling logic

    void UpdateSelector(float value)
    {
        bool scrollingForwards = value > 1.0 - scrollRegion;
        bool scrollingBackwards = value < scrollRegion;
        bool scrollingInThisFrame = scrollingForwards || scrollingBackwards;

        if (!scrolling && scrollingInThisFrame) {
            scrolling = true;
            StartScrolling(scrollingForwards);
        } else if (scrolling && !scrollingInThisFrame) {
            StopScrolling();
            scrolling = false;
            selectionIndex = selectionIndex; // Force a resend of the select msg
        }

        if (!scrolling) {
            if (fader.value < minValue && !pushed) {
                selectionIndex--;
            } else if (fader.value > maxValue && !pushed) {
                selectionIndex++;
            }
        }
    }

    IEnumerator ScrollLoop(string eventName)
    {
        float waitTime = InitialWaitTime;
        while (scrolling) {
            SendMessage(eventName, SendMessageOptions.DontRequireReceiver);

            if (!RepeatScrolling) {
                break;
            }

            yield return new WaitForSeconds(waitTime);
            waitTime = Mathf.Clamp(waitTime * WaitTimeModifier, MinWaitTime, MaxWaitTime);
        }
    }

    void StartScrolling(bool forward)
    {
        SendMessage("ItemSelector_StartScrolling", forward, SendMessageOptions.DontRequireReceiver);
        StartCoroutine("ScrollLoop", (forward) ? "ItemSelector_Next" : "ItemSelector_Prev");
    }

    void StopScrolling()
    {
        StopCoroutine("ScrollLoop");
        SendMessage("ItemSelector_StopScrolling", SendMessageOptions.DontRequireReceiver);
    }

    void Hand_Destroy()
    {
        if (scrolling) {
            StopScrolling();
            scrolling = false;
        }
    }

    void SessionManager_Visualize()
    {
        GUILayout.Label("- Item Selector");
        if (scrolling) {
            GUILayout.Label("SCROLLING");
        } else {
            GUILayout.HorizontalSlider(selectionIndex, 0.0f, numItems - 1);
        }
    }
}
