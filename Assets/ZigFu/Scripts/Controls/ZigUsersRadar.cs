using UnityEngine;
using System.Collections;

public class ZigUsersRadar : MonoBehaviour {
	public Vector2 RadarRealWorldDimensions = new Vector2(4000, 4000);
	public int PixelsPerMeter = 35;
	
	void Start()
	{
		
	}
	
	void OnGUI () 
	{
		if (!ZigInput.Instance.ReaderInited) return; 
		
		int width = (int)((float)PixelsPerMeter * (RadarRealWorldDimensions.x / 1000.0f));
		int height = (int)((float)PixelsPerMeter * (RadarRealWorldDimensions.y / 1000.0f));
		
		GUI.BeginGroup (new Rect (Screen.width - width - 20, 20, width, height));
		GUI.Box(new Rect(0, 0, width, height), "Users Radar");

		foreach (ZigTrackedUser currentUser in ZigInput.Instance.TrackedUsers.Values)
		{
			// normalize the center of mass to radar dimensions
			Vector3 com = currentUser.UserData.CenterOfMass;
			Vector2 radarPosition = new Vector2(com.x / RadarRealWorldDimensions.x, -com.z / RadarRealWorldDimensions.y);
			
			// X axis: 0 in real world is actually 0.5 in radar units (middle of field of view)
			radarPosition.x += 0.5f;
			
			// clamp
			radarPosition.x = Mathf.Clamp(radarPosition.x, 0.0f, 1.0f);
			radarPosition.y = Mathf.Clamp(radarPosition.y, 0.0f, 1.0f);

			// draw
			GUI.Box(new Rect(radarPosition.x * width - 10, radarPosition.y * height - 10, 20, 20), currentUser.UserData.Id.ToString());
		}
		GUI.EndGroup();
	}
}
