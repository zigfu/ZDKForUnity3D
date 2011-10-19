using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(Fader))]
public class ScrollingMenu : MonoBehaviour
{
    public Fader fader;
    public Vector3 direction = Vector3.down;
    public int WindowSize;
    public float damping = 5.0f;
    public float scrollRegionSize = 0.15f;
    public bool RepositionBasedOnBounds = false;
	public bool RepositionItemsOnUpdate;
    public bool SelectOnPush;
	public bool PushSlide;
	public float pushSliderSize = 200;
	public float pushSliderSensitiviy = 10.0f;
	
    List<Transform> items = new List<Transform>();
    ItemSelector selector;
    GameObject itemsContainer;
    Vector3 itemsContainerTargetPos;
	
	Fader pushslideFader;
	bool pushSliding;
	float centerIndexBase;

    float centerIndex = 0;
    public float CenterIndex
    {
        get { return centerIndex; }
        private set
        {
            if (items.Count == 0) {
                centerIndex = 0;
                return;
            }

            // make sure we're not out o'range
            centerIndex = Mathf.Clamp(value, 0, items.Count - 1);
            int centerIndexFloored = Mathf.FloorToInt(centerIndex);

            // find the target position. this is the position we want to be at the center of the menu
            Vector3 target;
            Vector3 omercy1 = items[centerIndexFloored].position;
            if (centerIndexFloored + 1 < items.Count - 1) {
                Vector3 omercy2 = items[centerIndexFloored + 1].position;
                target = Vector3.Lerp(omercy1, omercy2, centerIndex - centerIndexFloored);
            } else {
                target = omercy1;
            }

            // convert the world target to local position (relative to items container)
            target = itemsContainer.transform.InverseTransformPoint(target);

            // only use the component of target in our menu's direction
            target = direction.normalized * Mathf.Abs(Vector3.Dot(direction.normalized, target));

            // position the items container so that target will be at local 0,0,0
            itemsContainerTargetPos = -target;
        }
    }

    int firstOnScreenIndex = 0;
    int lastActiveItemIndex = 0;
    public int activeItemIndex = -1;
    public int ActiveItemIndex
    {
        get { return activeItemIndex; }
        set
        {
            if (items.Count == 0) {
                return;
            }

            // clamp
            int clamped = Mathf.Clamp(value, 0, items.Count - 1);

            // send activate/deactivate messages
            if (clamped != activeItemIndex) {
                Unhighlight();
                HighlightItem(clamped);
            }
			
			// OOB
			if (clamped != value) {
				SendMessage("Menu_OutOfBounds", (value > 0), SendMessageOptions.DontRequireReceiver);
			}
			
            // see if we should scroll our sliding window
            if (activeItemIndex < firstOnScreenIndex) {
                firstOnScreenIndex = activeItemIndex;
            }
            if (activeItemIndex > firstOnScreenIndex + (WindowSize - 1)) {
                firstOnScreenIndex = activeItemIndex - (WindowSize - 1);
            }

            // reposition items container to reflect the newly highlighted item
            CenterIndex = (float)firstOnScreenIndex + (((float)WindowSize - 1) / 2.0f);
        }
    }

	public bool CanScrollForward {
		get {
			return  (items.Count > 0 && ActiveItemIndex < items.Count - 1);
		}
	}
	
	public bool CanScrollBack {
		get {
			return  (items.Count > 0 && ActiveItemIndex > 0);
		}
	}

    void Awake()
    {
        // init the items container
        Transform t = transform.Find("ItemsContainer");
        if (t) {
            itemsContainer = t.gameObject;
        } else {
            itemsContainer = new GameObject("ItemsContainer");
        }
        itemsContainer.transform.parent = transform;
        itemsContainer.transform.localPosition = Vector3.zero;
        itemsContainer.transform.localRotation = Quaternion.identity;
        itemsContainerTargetPos = itemsContainer.transform.localPosition;

        // fader
        if (!fader) {
            fader = GetComponent<Fader>();
        }
        if (!fader) {
            Debug.LogError("Please add a fader to " + gameObject.name);
            return;
        }

        // init the itemselector
        selector = gameObject.AddComponent<ItemSelector>();
        selector.fader = fader;
    
        // push detector, if we need one
        if (SelectOnPush || PushSlide) {
            if (null == GetComponent<PushDetector>()) {
                gameObject.AddComponent<PushDetector>();
            }
        }
		
		// fader for the pushslider, if we need one
		if (PushSlide) {
			pushslideFader = gameObject.AddComponent<Fader>() as Fader;
			pushslideFader.direction = fader.direction;
			pushslideFader.size = pushSliderSize;
		}
    }

   	// Update is called once per frame
    void Update()
    {
		// push sliding
		if (pushSliding) {
			// convert 0-1 slider to -1 to 1 slider
			float pushedOffset = (pushslideFader.value - 0.5f) * 2;
			CenterIndex = centerIndexBase + (pushedOffset * pushSliderSensitiviy);
		}
		
		// move itemscontainer
       	itemsContainer.transform.localPosition = Vector3.Lerp(itemsContainer.transform.localPosition, itemsContainerTargetPos, Time.deltaTime * damping);
		
		if (RepositionItemsOnUpdate) {
			RepositionItems();
		}
    }
	
	// Backwards compatibility
	void Menu_Add(Transform item)
	{
		Add(item);
	}
	
	void Menu_Reposition()
	{
		RepositionItems();
	}
	
    public void Add(Transform item)
    {
        item.parent = itemsContainer.transform;
        items.Add(item);
        RepositionItems();

        // first item
        if (items.Count == 1) {
            HighlightItem(0);
        }
		
		ActiveItemIndex = ActiveItemIndex;
    }
	
	public void Clear()
	{
		foreach (Transform item in items)
		{
			Destroy(item.gameObject);
		}
		items.Clear();
    }

    void RepositionItems()
    {
        // update window size & itemselector
        WindowSize = Mathf.Max(WindowSize, 1);
        selector.numItems = WindowSize;
        selector.scrollRegion = scrollRegionSize;

        // position of next item
        Vector3 current = Vector3.zero;
        Bounds currentBounds = new Bounds();

        foreach (Transform item in items) {
            // get the bounds for this item BEFORE moving item. The bounds includes both
            // the old & new positions if we change localPosition first
            if (RepositionBasedOnBounds) {
                currentBounds = GetBoundingBox(item.gameObject);
            }

            // reposition this item
            item.localPosition = current;

            // update "current" for next item
            if (RepositionBasedOnBounds) {
                current += Mathf.Abs(Vector3.Dot(direction.normalized, currentBounds.size)) * direction.normalized;
            }
            current += direction;
        }
    }

    Bounds GetBoundingBox(GameObject go)
    {
        Bounds newBounds = new Bounds(go.transform.position, Vector3.zero);
        foreach (Renderer child in go.GetComponentsInChildren<Renderer>()) {
            newBounds.Encapsulate(child.bounds);
        }
        return newBounds;
    }

    void SelectActive()
    {
        items[ActiveItemIndex].SendMessage("MenuItem_Select", SendMessageOptions.DontRequireReceiver);
        SendMessage("Menu_Select", items[ActiveItemIndex], SendMessageOptions.DontRequireReceiver);
    }

    //-------------------------------------------------------------------------
    // ItemSelector messages
    //-------------------------------------------------------------------------

    void ItemSelector_Select(int index)
    {
		if (pushSliding) return;
        ActiveItemIndex = firstOnScreenIndex + index;
    }

    void ItemSelector_Next()
    {
		if (pushSliding) return;
        ActiveItemIndex++;
    }

    void ItemSelector_Prev()
    {
		if (pushSliding) return;
        ActiveItemIndex--;
    }

    //-------------------------------------------------------------------------
    // PushDetector messages
    //-------------------------------------------------------------------------

	void PushDetector_Push()
	{
		if (PushSlide) {
			pushslideFader.MoveTo(gameObject.GetComponent<PushDetector>().ClickPosition, 0.5f);
			centerIndexBase = CenterIndex;
			pushSliding = true;
		}
	}
	
	void PushDetector_Release()
	{
		if (PushSlide) {
			pushSliding = false;
			ActiveItemIndex = Mathf.FloorToInt(CenterIndex);
		}
	}
	
    void PushDetector_Click()
    {
        if (SelectOnPush) {
            SelectActive();
        }
    }

    //-------------------------------------------------------------------------
    // Hand point messages
    //-------------------------------------------------------------------------

    void Hand_Create(Vector3 pos)
    {
        HighlightItem(lastActiveItemIndex);
    }

    void Hand_Update(Vector3 pos)
    {
    }

    void Hand_Destroy()
    {
        Unhighlight();
    }

    //-------------------------------------------------------------------------
    // Highlighting/Unhighlighting items
    //-------------------------------------------------------------------------

    void HighlightItem(int index)
    {
        if (items.Count == 0) {
            return;
        }

        activeItemIndex = index;
        items[index].SendMessage("MenuItem_Highlight", SendMessageOptions.DontRequireReceiver);
        SendMessage("Menu_Highlight", items[index], SendMessageOptions.DontRequireReceiver);
    }

    void Unhighlight()
    {
        if (ActiveItemIndex == -1 || items.Count == 0) {
            return;
        }

        items[ActiveItemIndex].SendMessage("MenuItem_Unhighlight", SendMessageOptions.DontRequireReceiver);
        SendMessage("Menu_Unhighlight", items[activeItemIndex], SendMessageOptions.DontRequireReceiver);
        lastActiveItemIndex = ActiveItemIndex;
        activeItemIndex = -1;
    }

    //-------------------------------------------------------------------------
    // Keyboard support
    //-------------------------------------------------------------------------

    void OnGUI()
    {
        if (Event.current.type == EventType.KeyDown) {
            if (!GetComponent<HandPointControl>().IsActive) { return; }

            if (Event.current.keyCode == KeyCode.UpArrow ||
                Event.current.keyCode == KeyCode.LeftArrow) {
                ActiveItemIndex--;
                Event.current.Use();
            }

            if (Event.current.keyCode == KeyCode.DownArrow ||
                Event.current.keyCode == KeyCode.RightArrow) {
                ActiveItemIndex++;
                Event.current.Use();
            }

            if (Event.current.keyCode == KeyCode.Return) {
                SelectActive();
                Event.current.Use();
            }
        }
    }

    //-------------------------------------------------------------------------
    // Debug visualization
    //-------------------------------------------------------------------------

    void OnDrawGizmos()
    {
        // draw bounding box for each item
        int i = 0;
        foreach (Transform item in items) {
            Gizmos.color = (i == ActiveItemIndex) ? Color.red : Color.white;
            Bounds bbox = GetBoundingBox(item.gameObject);
            Gizmos.DrawWireCube(bbox.center, bbox.size);
            i++;
        }
    }

}
