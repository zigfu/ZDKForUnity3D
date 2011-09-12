using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Fader))]
public class StaticMenu : MonoBehaviour {
    public Transform[] items;
    public bool SelectOnPush;
    public Fader fader;

    ItemSelector selector;

    int lastActiveItemIndex = 0;
    int activeItemIndex = -1;
    public int ActiveItemIndex
    {
        get { return activeItemIndex; }
        set
        {
            if (items.Length == 0) {
                return;
            }

            // clamp
            int clamped = Mathf.Clamp(value, 0, items.Length - 1);

            // send activate/deactivate messages
            if (clamped != activeItemIndex) {
                Unhighlight();
                HighlightItem(clamped);
            }
        }
    }


	// Use this for initialization
	void Start () {
        if (!fader) {
            fader = GetComponent<Fader>();
        }
        if (!fader) {
            Debug.LogError("Please add a fader to " + gameObject.name);
            return;
        }

        selector = gameObject.AddComponent<ItemSelector>();
        selector.numItems = items.Length;
        selector.scrollRegion = 0.0f;
        selector.fader = fader;

        if (SelectOnPush) {
            if (null == GetComponent<PushDetector>()) {
                gameObject.AddComponent<PushDetector>();
            }
        }
	}

    void ItemSelector_Select(int index)
    {
        ActiveItemIndex = index;
    }

    void PushDetector_Click()
    {
        if (SelectOnPush) {
            items[ActiveItemIndex].SendMessage("MenuItem_Select", SendMessageOptions.DontRequireReceiver);
            SendMessage("Menu_Select", items[ActiveItemIndex], SendMessageOptions.DontRequireReceiver);
        }
    }

    //-------------------------------------------------------------------------
    // Highlighting/Unhighlighting items
    //-------------------------------------------------------------------------

    void HighlightItem(int index)
    {
        if (items.Length == 0) {
            return;
        }

        activeItemIndex = index;
        items[index].SendMessage("MenuItem_Highlight", SendMessageOptions.DontRequireReceiver);
        SendMessage("Menu_Highlight", items[index], SendMessageOptions.DontRequireReceiver);
    }

    void Unhighlight()
    {
        if (ActiveItemIndex == -1 || items.Length == 0) {
            return;
        }

        items[ActiveItemIndex].SendMessage("MenuItem_Unhighlight", SendMessageOptions.DontRequireReceiver);
        SendMessage("Menu_Unhighlight", items[activeItemIndex], SendMessageOptions.DontRequireReceiver);
        lastActiveItemIndex = ActiveItemIndex;
        activeItemIndex = -1;
    }

    //-------------------------------------------------------------------------
    // Hand point events
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
}
