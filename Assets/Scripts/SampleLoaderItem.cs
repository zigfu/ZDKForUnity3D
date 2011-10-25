using UnityEngine;
using System.Collections;

public class SampleLoaderItem : MonoBehaviour {
	
	public TextMesh titleLabel;
	public SampleLoaderEntry entry { get; private set; }
	
	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
	
	}
	
	public void Init(SampleLoaderEntry entry)
	{
		this.entry = entry;
		titleLabel.text = entry.Title;
	}
}
