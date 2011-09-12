using UnityEngine;
using System.Collections;

public class ImagesFeed : MonoBehaviour {
    public ScrollingMenu menu;
    public ImageItem Item;
    public string Path = "Slides";
    public string SearchPattern = "*.png";

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
	void Start () {
        foreach (string filename in System.IO.Directory.GetFiles(Path, SearchPattern)) {
            ImageItem newItem = Instantiate(Item) as ImageItem;
            newItem.Init(filename);
            menu.Add(newItem.transform);
        }
	}
}
