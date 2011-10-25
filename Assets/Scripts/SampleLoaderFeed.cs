using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

[Serializable]
public class SampleLoaderEntry
{
	public string SceneName;
	public string Title;
	public string Description;
}

public class SampleLoaderFeed : MonoBehaviour {
	
	public ScrollingMenu menu;
	public List<SampleLoaderEntry> entries;
	public SampleLoaderItem itemPrefab;
	public TextMesh descriptionLabel;
	
	void Awake()
	{
		if (null == menu) {
			menu = GetComponent<ScrollingMenu>();
		}
	}
	
	void Start () {
		foreach (SampleLoaderEntry entry in entries) {
			SampleLoaderItem newItem = Instantiate(itemPrefab) as SampleLoaderItem;
			newItem.Init(entry);
			menu.Add(newItem.transform);
			newItem.transform.localRotation = Quaternion.identity;
		}
	}
	
	void Menu_Highlight(Transform item)
	{
		SampleLoaderEntry entry = item.gameObject.GetComponent<SampleLoaderItem>().entry;
		descriptionLabel.text = TextTools.WordWrap(entry.Description, 25);
	}
	
	void Menu_Unhighlight(Transform item)
	{
		descriptionLabel.text = "";
	}
	
	void Menu_Select(Transform item)
	{
		SampleLoaderEntry entry = item.gameObject.GetComponent<SampleLoaderItem>().entry;
		Application.LoadLevel(entry.SceneName);
	}
}
