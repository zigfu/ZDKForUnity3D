using UnityEngine;
using System.Collections;
using OpenNI;

public class ZigHandRaiseDetector : MonoBehaviour {
	
	public bool IsHandRaised { get; private set; }
	
	void Zig_OnUserUpdate(ZigEventArgs args)
	{
		if (!args.user.SkeletonTracked) {
			return;
		}
		
		Vector3 leftHand = args.user.Joints[SkeletonJoint.LeftHand].position;
		Vector3 rightHand = args.user.Joints[SkeletonJoint.RightHand].position;
		Vector3 head = args.user.Joints[SkeletonJoint.Head].position;

		bool shouldBeRaised = (leftHand.y > head.y || rightHand.y > head.y);
		if (!IsHandRaised && shouldBeRaised) {
			SendMessage("Zig_OnHandRaised", args, SendMessageOptions.DontRequireReceiver);
		}
		if (IsHandRaised && !shouldBeRaised) {
			SendMessage("Zig_OnHandLowered", args, SendMessageOptions.DontRequireReceiver);
		}
		IsHandRaised = shouldBeRaised;
	}
}
