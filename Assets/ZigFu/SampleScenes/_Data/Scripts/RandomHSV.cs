using UnityEngine;
using System.Collections;

public class RandomHSV : MonoBehaviour {

	// Use this for initialization
	void Start () {
		HSBColor color;
		color = new HSBColor(Random.Range(0.0f, 1.0f), 1f, 1f);
		Color col;
		col =color.ToColor();
		//this.gameObject.renderer.material.color = col;

		foreach (Renderer r in transform.GetComponentsInChildren<Renderer>())
		{
			Debug.Log("foo");
			r.material.color = col;
		}

	}

}
