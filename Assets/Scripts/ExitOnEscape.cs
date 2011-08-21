using UnityEngine;
using System.Collections;

public class ExitOnEscape : MonoBehaviour {

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            print("Quitting");
            Application.Quit();
        }
	}
}
