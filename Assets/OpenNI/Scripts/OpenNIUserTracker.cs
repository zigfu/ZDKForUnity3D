using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using OpenNI;

public class OpenNIUserTracker : MonoBehaviour 
{
	public float UserTooCloseDistance = 1.60f;
	
	public int MaxCalibratedUsers; 
	
	private UserGenerator userGenerator;
	private SkeletonCapability skeletonCapbility;
	private PoseDetectionCapability poseDetectionCapability;
	private string calibPose;

	private List<int> allUsers;
	private List<int> calibratedUsers;
	private List<int> calibratingUsers;
	
	public IList<int> AllUsers
	{
		get { return allUsers.AsReadOnly(); }
	}
	public IList<int> CalibratedUsers
	{
		get { return calibratedUsers.AsReadOnly(); }
	}
	public IList<int> CalibratingUsers
	{
		get {return calibratingUsers.AsReadOnly(); }
	}
	private Dictionary<int,Vector3> userCalibrationPosition = new Dictionary<int, Vector3>();
	public Dictionary<int,Vector3> UserCalibrationPosition
	{
		get {return userCalibrationPosition; }
	}
		
	bool AttemptToCalibrate
	{
		get { return calibratedUsers.Count < MaxCalibratedUsers; }
	}
	
	void Start() 
	{
		calibratedUsers = new List<int>();
		calibratingUsers = new List<int>();
		allUsers = new List<int>();

		this.userGenerator = OpenNIContext.OpenNode(NodeType.User) as UserGenerator; //new UserGenerator(this.Context.context);
		this.skeletonCapbility = this.userGenerator.SkeletonCapability;
		this.poseDetectionCapability = this.userGenerator.PoseDetectionCapability;
		this.calibPose = this.skeletonCapbility.CalibrationPose;
		this.skeletonCapbility.SetSkeletonProfile(SkeletonProfile.All);

        this.userGenerator.NewUser += new EventHandler<NewUserEventArgs>(userGenerator_NewUser);
        this.userGenerator.LostUser += new EventHandler<UserLostEventArgs>(userGenerator_LostUser);
        this.poseDetectionCapability.PoseDetected += new EventHandler<PoseDetectedEventArgs>(poseDetectionCapability_PoseDetected);
        this.skeletonCapbility.CalibrationEnd += new EventHandler<CalibrationEndEventArgs>(skeletonCapbility_CalibrationEnd);
		
		foreach (int userId in userGenerator.GetUsers())
		{
			allUsers.Add(userId);
		}
		AttemptCalibrationForAllUsers();
	}

	
	private bool isTooClose = true;
	private bool calibratedUserTooClose = true;
	
	void Update()
	{
		bool foundTooClose = false;
		bool foundCalibrated = false;
		foreach(int userId in allUsers) {
			Vector3 com = GetUserCenterOfMass(userId);
			float distance = -com.z / 1000f;
			if (distance < UserTooCloseDistance) {
				if (!isTooClose) {
					SendMessage("UserTooClose",SendMessageOptions.DontRequireReceiver);
					isTooClose = true;
				}
				foundTooClose = true;
				if (calibratedUsers.Contains(userId)) {
					if (!calibratedUserTooClose) {
						SendMessage("CalibratedUserTooClose",SendMessageOptions.DontRequireReceiver);
						calibratedUserTooClose = true;
					}
					foundCalibrated = true;
				}
			}
		}
		
		if (!foundTooClose) {
			if (isTooClose) {
				SendMessage("UserNotTooClose",SendMessageOptions.DontRequireReceiver);
			}
			isTooClose = false;
		}
		if (!foundCalibrated) {
			if (calibratedUserTooClose) {
				SendMessage("CalibratedUserNotTooClose", SendMessageOptions.DontRequireReceiver);
			}
			calibratedUserTooClose = false;
		}
	}
	
    void skeletonCapbility_CalibrationEnd(object sender, CalibrationEndEventArgs e)
    {
        if (e.Success)
        {
            if (AttemptToCalibrate)
            {
				SendMessage("CalibrationComplete", e, SendMessageOptions.DontRequireReceiver);
                this.skeletonCapbility.StartTracking(e.ID);
				
				SkeletonJointTransformation skelTrans = new SkeletonJointTransformation();
				skelTrans = skeletonCapbility.GetSkeletonJoint(e.ID, SkeletonJoint.Torso);
				Point3D pos = skelTrans.Position.Position;
				userCalibrationPosition[e.ID] = new Vector3(pos.X,pos.Y,-pos.Z);
				
                calibratedUsers.Add(e.ID);
            }
        }
        else
        {
			SendMessage("CalibrationFailed", e, SendMessageOptions.DontRequireReceiver);
            if (AttemptToCalibrate)
            {
                this.poseDetectionCapability.StartPoseDetection(calibPose, e.ID);
            }
        }
		
		calibratingUsers.Remove(e.ID);
		
	
    }

    void poseDetectionCapability_PoseDetected(object sender, PoseDetectedEventArgs e)
    {
        this.poseDetectionCapability.StopPoseDetection(e.ID);
		SendMessage("PoseDetected", e, SendMessageOptions.DontRequireReceiver);
        if (AttemptToCalibrate)
        {
            this.skeletonCapbility.RequestCalibration(e.ID, true);
			calibratingUsers.Add(e.ID);
        }
    }

    void userGenerator_LostUser(object sender, UserLostEventArgs e)
    {
        allUsers.Remove(e.ID);
		SendMessage("UserLost", e, SendMessageOptions.DontRequireReceiver);
		
        if (calibratedUsers.Contains(e.ID))
        {
            calibratedUsers.Remove(e.ID);
			if (calibratedUsers.Count == 0)
			{
				SendMessage("CalibratedUserLost", e, SendMessageOptions.DontRequireReceiver);
			}
        }
		if (calibratingUsers.Contains(e.ID))
		{
			calibratingUsers.Remove(e.ID);
		}

		if (allUsers.Count == 0)
		{			
			SendMessage("AllUsersLost", e, SendMessageOptions.DontRequireReceiver);
		}


        if (AttemptToCalibrate)
        {
            AttemptCalibrationForAllUsers();
        }
    }

    void userGenerator_NewUser(object sender, NewUserEventArgs e)
    {
        allUsers.Add(e.ID);
		SendMessage("UserDetected", e, SendMessageOptions.DontRequireReceiver);
        if (AttemptToCalibrate)
        {
            this.poseDetectionCapability.StartPoseDetection(this.calibPose, e.ID);
        }
    }
	
	void AttemptCalibrationForAllUsers()
	{
		foreach (int id in userGenerator.GetUsers())
		{
			if (!skeletonCapbility.IsCalibrating(id) && !skeletonCapbility.IsTracking(id))
			{
				this.poseDetectionCapability.StartPoseDetection(this.calibPose, id);
				
				
			}
		}
	}
	
	void LoseAllUsers()
	{
		foreach (int id in CalibratedUsers) {
			skeletonCapbility.StopTracking(id);	
		}
		calibratedUsers.Clear();
	}
	
	public void ResetTracker()
	{
		LoseAllUsers();
		AttemptCalibrationForAllUsers();
	}
			
	public void UpdateSkeleton(int userId, OpenNISkeleton skeleton)
	{
		// make sure we have skeleton data for this user
		if (!skeletonCapbility.IsTracking(userId))
		{
			return;
		}
		
		// Use torso as root
		SkeletonJointTransformation skelTrans = new SkeletonJointTransformation();
		skelTrans = skeletonCapbility.GetSkeletonJoint(userId, SkeletonJoint.Torso);
		if (skeleton.absolute)
		{		
			Point3D pos = skelTrans.Position.Position;
			skeleton.UpdateRoot(new Vector3(pos.X,pos.Y,pos.Z));
		}
		else
		{
			Point3D pos = skelTrans.Position.Position;
			Vector3 v3dpos = new Vector3(pos.X, pos.Y, -pos.Z);
			Vector3 calPos = userCalibrationPosition[userId];
			skeleton.UpdateRoot(calPos - v3dpos); 
		}
		
		// update each joint with data from OpenNI
		foreach (SkeletonJoint joint in Enum.GetValues(typeof(SkeletonJoint)))
		{
			if (skeletonCapbility.IsJointAvailable(joint))
			{
				skelTrans = skeletonCapbility.GetSkeletonJoint(userId, joint);
				skeleton.UpdateJoint(joint, skelTrans);
			}
		}
	}
	
	public Vector3 GetUserCenterOfMass(int userId)
	{
		Point3D com = userGenerator.GetCoM(userId);
		return new Vector3(com.X, com.Y, -com.Z);
	}
}
