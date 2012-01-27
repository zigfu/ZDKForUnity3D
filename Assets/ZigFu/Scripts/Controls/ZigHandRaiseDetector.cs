using UnityEngine;
using System.Collections;
using OpenNI; // OpenNI dep for SkeletonJoint, will go soon

public class ZigHandRaiseDetector : MonoBehaviour {
	
	ZigSteadyDetector leftHandSteady;
	ZigSteadyDetector rightHandSteady;
	//ZigTrackedUser user;
	
	// Use this for initialization
	void Start () {
		leftHandSteady = gameObject.AddComponent<ZigSteadyDetector>();
		rightHandSteady = gameObject.AddComponent<ZigSteadyDetector>();
		
		leftHandSteady.type = SteadyDetectorType.SkeletonJoint;
		leftHandSteady.joint = SkeletonJoint.LeftHand;
		
		rightHandSteady.type = SteadyDetectorType.SkeletonJoint;
		rightHandSteady.joint = SkeletonJoint.RightHand;
	}
	
	/*
	void Zig_OnUserUpdate(ZigEventArgs args)
	{
		user = args.user;
	}*/
	/*
	void SteadyDetector_Steady(SkeletonJoint joint)
	{
		// if the steady point is a hand, and its higher than the head
		if (SkeletonJoint.LeftHand == joint || SkeletonJoint.RightHand == joint) {
			Vector3 head = user.Joints[SkeletonJoint.Head].position;
			if (user.Joints[joint].position.y > head.y) {
				SendMessage("HandRaiseDetector_HandRaised", user, SendMessageOptions.DontRequireReceiver);
			}
		}
	}*/
}
