using UnityEngine;
using System.Collections;
using OpenNI;

public class OpenNISplitScreenSkeletonController : MonoBehaviour 
{
	public OpenNIUserTracker UserTracker;
	public OpenNISkeleton Skeleton1;
	public OpenNISkeleton Skeleton2;
	
	private int userId1;
	private int userId2;
	
	// Use this for initialization
	void Start() 
	{
        if (null == UserTracker) {
            UserTracker = GetComponent<OpenNIUserTracker>();
        }
        if (null == UserTracker) {
            UserTracker = gameObject.AddComponent<OpenNIUserTracker>();
        }

		UserTracker.MaxCalibratedUsers = Mathf.Max(2, UserTracker.MaxCalibratedUsers);

        if (null != Skeleton1) {
			Skeleton1.RotateToCalibrationPose();
		}
		if (null != Skeleton2) {
			Skeleton2.RotateToCalibrationPose();
		}
	}
	
	// Update is called once per frame
	void Update () 
	{
		userId1 = UpdateUser(userId1, Skeleton1);
		userId2 = UpdateUser(userId2, Skeleton2);
	}
	
	int UpdateUser(int userId, OpenNISkeleton skeleton)
	{
		// valid users?
		if (0 != userId)
		{
			// is the user still valid?
			if (!UserTracker.CalibratedUsers.Contains(userId))
			{
				userId = 0;
				Skeleton1.RotateToCalibrationPose();
			}
		}
		
		// look for a new userId if we dont have one
		if (0 == userId)
		{
			foreach (int uId in UserTracker.CalibratedUsers)
			{
				if (!UserIdTaken(uId))
				{
					userId = uId;
					break;
				}
			}
		}
		
		// update our skeleton based on active user id
		if (0 != userId)
		{
			UserTracker.UpdateSkeleton(userId, skeleton);
		}
		
		return userId;
	}
	
	bool UserIdTaken(int userId)
	{
		return ((userId1 == userId) || (userId2 == userId));
	}
			
	void OnGUI()
	{
		int calibratingUsers = UserTracker.CalibratingUsers.Count;
		if (userId1 == 0)
		{
			if (calibratingUsers > 0)
			{
				--calibratingUsers;
				GUILayout.Box(string.Format("User 1: Calibrating a user"));
			}
			else
			{
				GUILayout.Box(string.Format("User 1: Searching for a user"));
			}
		}
		else 
		{
			GUILayout.Box(string.Format("User 1: Tracking"));
		}
		
		if (userId2 == 0)
		{
			if (calibratingUsers > 0)
			{
				GUILayout.Box(string.Format("User 2: Calibrating a user"));
			}
			else
			{
				GUILayout.Box(string.Format("User 2: Searching for a user"));
			}
		}
		else 
		{
			GUILayout.Box(string.Format("User 2: Tracking"));
		}
	}
}
