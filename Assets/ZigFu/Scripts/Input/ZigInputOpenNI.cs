using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using OpenNI;

public class ZigInputOpenNI : MonoBehaviour
{
	public ZigUserTracker userTracker;
	
	void Start()
	{
		// get notifications for new users
		OpenNIReader.Instance.AddNewFrameHandler(NodeType.User, this.gameObject);
	}
	
	ArrayList Point3DToArrayList(Point3D pos)
	{
		return new ArrayList(new double[] {pos.X, pos.Y, pos.Z});
	}
	
	ArrayList OrientationToArrayList(SkeletonJointOrientation ori)
	{
		return new ArrayList(new double[] {ori.X1, ori.X2, ori.X3, ori.Y1, ori.Y2, ori.Y3, ori.Z1, ori.Z2, ori.Z3 });
	}
	
	void OpenNI_NewFrame(NodeType type) {
		if (type != NodeType.User) return;
		
		UserGenerator Users = OpenNIReader.Instance.Users;
		
		// foreach user
		int[] userids = Users.GetUsers();
		ArrayList users = new ArrayList();
		foreach (int userid in userids) {
			// center of mass (position)
			Point3D com = Users.GetCoM(userid);
			
			// skeleton data
			ArrayList joints = new ArrayList();
			bool tracked = Users.SkeletonCapability.IsTracking(userid);
			if (tracked) {
				SkeletonCapability skelCap = Users.SkeletonCapability;
				SkeletonJointTransformation skelTrans;
				foreach (SkeletonJoint sj in Enum.GetValues(typeof(SkeletonJoint))) {
					if (skelCap.IsJointAvailable(sj)) {
						skelTrans = skelCap.GetSkeletonJoint(userid, sj);
						Hashtable joint = new Hashtable();
						joint["id"] = (int)sj;
						joint["position"] = Point3DToArrayList(skelTrans.Position.Position);
						joint["rotation"] = OrientationToArrayList(skelTrans.Orientation);
						joint["positionconfidence"] = skelTrans.Position.Confidence;
						joint["rotationconfidence"] = skelTrans.Orientation.Confidence;
						joints.Add(joint);
					}
				}
			}
			
			Hashtable user = new Hashtable();
			user["id"] = userid;
			user["centerofmass"] = Point3DToArrayList(com);
			user["tracked"] = tracked;
			user["joints"] = joints;
			users.Add(user);
		}
		
		ArrayList hands = new ArrayList();
		
		// update the usertracker
		userTracker.Update(users, hands);
	}
}
