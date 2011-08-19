using UnityEngine;
using System.Collections;
using OpenNI;

public class OpenNISinglePlayerMultiSkeletonController : MonoBehaviour 
{
	public OpenNIUserTracker UserTracker;
	public OpenNISkeleton [] Skeletons;
	
	private int userId;
	
	// Use this for initialization
	void OnEnable () 
	{
		if (null == UserTracker) return;
		if (!UserTracker.enabled) UserTracker.enabled = true;
		UserTracker.MaxCalibratedUsers = 1;
	}
	
	void Start()
	{
	}
	bool firstTime = true;
	//public GameManager2 gm;
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
				foreach (OpenNISkeleton Skeleton in Skeletons)
				{
					Skeleton.RotateToCalibrationPose();
			
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
			}
		}
		
		// update our skeleton based on active user id
		if (0 != userId)
		{
			foreach (OpenNISkeleton Skeleton in Skeletons)
			{
				UserTracker.UpdateSkeleton(userId, Skeleton);
		
			}
			
				if (firstTime)
			{
				firstTime = false;
			//	gm.ResetGame();
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
			GUILayout.Box(string.Format("Calibrated: {0}", userId));
		}
	}
}
