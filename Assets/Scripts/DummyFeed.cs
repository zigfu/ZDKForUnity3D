using UnityEngine;
using System.Collections;

public class DummyFeed : MonoBehaviour {
    public ScrollingMenu menu;
    public Transform menuItem;
    public int count = 10;

    void Awake()
    {
        if (null == menu) {
            menu = GetComponent<ScrollingMenu>();
        }
        if (null == menu) {
            Debug.LogError("No menu attached to " + gameObject.name);
        }
    }

    // Use this for initialization
    void Start()
    {
        if (!menu) return;

        for (int i = 0; i < count; i++) {
            Transform newObj = Instantiate(menuItem) as Transform;
            TextMesh tm = newObj.GetComponentInChildren<TextMesh>();
            if (tm) {
                tm.text = "Testing " + i;
            }
            menu.Add(newObj);
        }
    }
}
