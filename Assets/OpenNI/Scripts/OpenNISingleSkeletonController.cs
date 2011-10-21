using UnityEngine;
using System.Collections;
using OpenNI;

public class OpenNISingleSkeletonController : MonoBehaviour 
{
	public OpenNIUserTracker UserTracker;
	public OpenNISkeleton[] Skeletons;
	
	private int userId;
	private bool outOfFrame;
	
	public bool IsTracking {
		get { return userId != 0; }
	}
	
	// Use this for initialization
	
	void Start()
	{
        if (!UserTracker) {
            UserTracker = GetComponent<OpenNIUserTracker>();
		}
		if (!UserTracker) {
			UserTracker = GameObject.FindObjectOfType(typeof(OpenNIUserTracker)) as OpenNIUserTracker;
        }
        if (!UserTracker) {
            Debug.LogWarning("Missing a User Tracker. Adding...");
            UserTracker = gameObject.AddComponent<OpenNIUserTracker>();
        }
		
		if (UserTracker.MaxCalibratedUsers < 1) {
			UserTracker.MaxCalibratedUsers = 1;
		}
	}
	
	// Update is called once per frame
	void Update () 
	{
		// do we have a valid calibrated user?
		if (0 != userId)
		{
			// is the user still valid?
			if (!UserTracker.CalibratedUsers.Contains(userId))
			{
				userId = 0;
				foreach (OpenNISkeleton skel in Skeletons)
				{
					skel.RotateToCalibrationPose();
				}
			}
		}
		
		// look for a new userId if we dont have one
		if (0 == userId)
		{
			// just take the first calibrated user
			if (UserTracker.CalibratedUsers.Count > 0)
			{				
				userId = UserTracker.CalibratedUsers[0];
				outOfFrame = false;
			}
		}
		
		// we have a valid userId, lets use it for something!
		if (0 != userId)
		{
			// see if user is out o'frame
			Vector3 com = UserTracker.GetUserCenterOfMass(userId);
			if (outOfFrame != (com == Vector3.zero))
			{
				outOfFrame = (com == Vector3.zero);
				SendMessage("UserOutOfFrame", outOfFrame, SendMessageOptions.DontRequireReceiver);
			}
			
			// update our skeleton based on active user id	
			foreach (OpenNISkeleton skel in Skeletons)
			{
				UserTracker.UpdateSkeleton(userId, skel);
			}
		}
	}
	
	void OnGUI()
	{
		if (userId == 0)
		{
			if (UserTracker.CalibratingUsers.Count > 0)
			{
				// Calibrating
				GUILayout.Box(string.Format("Calibrating: {0}", UserTracker.CalibratingUsers[0]));
			}
			else
			{
				// Looking
				GUILayout.BeginArea (new Rect (Screen.width/2 - 150, Screen.height/2 - 150, 300, 300));
				GUILayout.Box("Waiting for single player to calibrate");
				GUILayout.EndArea();
			}
		}
		else
		{
			// Calibrated
			GUILayout.BeginVertical("box");
			GUILayout.Label(string.Format("Calibrated: {0}", userId));
			GUILayout.Label(string.Format("Out of frame: {0}", (outOfFrame) ? "TRUE" : "FALSE"));
			GUILayout.EndVertical();
		}
	}
}
