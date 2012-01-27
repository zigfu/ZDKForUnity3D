using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using OpenNI;

// NOTE: Still uses openni for joint id's (will change)

public class ZigSkeleton : MonoBehaviour 
{
	public Transform Head;
	public Transform Neck;
	public Transform Torso;
	public Transform Waist;

	public Transform LeftCollar;
	public Transform LeftShoulder;
	public Transform LeftElbow;
	public Transform LeftWrist;
	public Transform LeftHand;
	public Transform LeftFingertip;

	public Transform RightCollar;
	public Transform RightShoulder;
	public Transform RightElbow;
	public Transform RightWrist;
	public Transform RightHand;
	public Transform RightFingertip;

	public Transform LeftHip;
	public Transform LeftKnee;
	public Transform LeftAnkle;
	public Transform LeftFoot;

	public Transform RightHip;
	public Transform RightKnee;
	public Transform RightAnkle;
	public Transform RightFoot;
	
	public bool UpdateJointPositions = false;
	public bool UpdateRootPosition = false;
	public bool UpdateOrientation = true;
	public bool RotateToPsiPose = false;
	public float RotationDamping = 30.0f;
	public Vector3 Scale = new Vector3(0.001f,0.001f,0.001f); 
	
	public Vector3 PositionBias = Vector3.zero;
	
	private Transform[] transforms;
	private Quaternion[] initialRotations;
	private Vector3 rootPosition;

	public void Awake()
	{
		int jointCount = Enum.GetNames(typeof(SkeletonJoint)).Length + 1; // Enum starts at 1
		
		transforms = new Transform[jointCount];
		initialRotations = new Quaternion[jointCount];
		
		transforms[(int)SkeletonJoint.Head] = Head;
		transforms[(int)SkeletonJoint.Neck] = Neck;
		transforms[(int)SkeletonJoint.Torso] = Torso;
		transforms[(int)SkeletonJoint.Waist] = Waist;
		transforms[(int)SkeletonJoint.LeftCollar] = LeftCollar;
		transforms[(int)SkeletonJoint.LeftShoulder] = LeftShoulder;
		transforms[(int)SkeletonJoint.LeftElbow] = LeftElbow;
		transforms[(int)SkeletonJoint.LeftWrist] = LeftWrist;
		transforms[(int)SkeletonJoint.LeftHand] = LeftHand;
		transforms[(int)SkeletonJoint.LeftFingertip] = LeftFingertip;
		transforms[(int)SkeletonJoint.RightCollar] = RightCollar;
		transforms[(int)SkeletonJoint.RightShoulder] = RightShoulder;
		transforms[(int)SkeletonJoint.RightElbow] = RightElbow;
		transforms[(int)SkeletonJoint.RightWrist] = RightWrist;
		transforms[(int)SkeletonJoint.RightHand] = RightHand;
		transforms[(int)SkeletonJoint.RightFingertip] = RightFingertip;
		transforms[(int)SkeletonJoint.LeftHip] = LeftHip;
		transforms[(int)SkeletonJoint.LeftKnee] = LeftKnee;
		transforms[(int)SkeletonJoint.LeftAnkle] = LeftAnkle;
		transforms[(int)SkeletonJoint.LeftFoot] = LeftFoot;
		transforms[(int)SkeletonJoint.RightHip] = RightHip;
		transforms[(int)SkeletonJoint.RightKnee] = RightKnee;
	    transforms[(int)SkeletonJoint.RightAnkle] = RightAnkle;
		transforms[(int)SkeletonJoint.RightFoot] = RightFoot;
		
		// save all initial rotations
		// NOTE: Assumes skeleton model is in "T" pose since all rotations are relative to that pose
		foreach (SkeletonJoint j in Enum.GetValues(typeof(SkeletonJoint))) {
			if (transforms[(int)j])	{
				// we will store the relative rotation of each joint from the gameobject rotation
				// we need this since we will be setting the joint's rotation (not localRotation) but we 
				// still want the rotations to be relative to our game object
				initialRotations[(int)j] = Quaternion.Inverse(transform.rotation) * transforms[(int)j].rotation;
			}
		}
    }

    void Start() 
    {
		// start out in calibration pose
		if (RotateToPsiPose) {
			RotateToCalibrationPose();
		}
	}
	
	void UpdateRoot(Vector3 skelRoot)
	{
        // +Z is backwards in OpenNI coordinates, so reverse it
		rootPosition = Vector3.Scale(new Vector3(skelRoot.x, skelRoot.y, skelRoot.z), Scale) + PositionBias;
		if (UpdateRootPosition) {
			transform.localPosition = (transform.rotation * rootPosition);
		}
	}
	
	void UpdateRotation(ZigJointId joint, Quaternion orientation)
	{
        // make sure something is hooked up to this joint
        if (!transforms[(int)joint]) {
            return;
        }

        if (UpdateOrientation) {
			Quaternion newRotation = transform.rotation * orientation * initialRotations[(int)joint];
			transforms[(int)joint].rotation = Quaternion.Slerp(transforms[(int)joint].rotation, newRotation, Time.deltaTime * RotationDamping);
        }
	}
	
	void UpdatePosition(ZigJointId joint, Vector3 position)
	{
		// make sure something is hooked up to this joint
		if (!transforms[(int)joint]) {
			return;
		}
		
		if (UpdateJointPositions) {
			Vector3 dest = Vector3.Scale(position, Scale) - rootPosition;
			transforms[(int)joint].localPosition = Vector3.Lerp(transforms[(int)joint].localPosition, dest, Time.deltaTime * RotationDamping);
		}
	}

	public void RotateToCalibrationPose()
	{
		foreach (SkeletonJoint j in Enum.GetValues(typeof(SkeletonJoint))) {
			if (null != transforms[(int)j])	{
				transforms[(int)j].rotation = transform.rotation * initialRotations[(int)j];
			}
		}
		
		// calibration pose is skeleton base pose ("T") with both elbows bent in 90 degrees
		if (null != RightElbow) {
			RightElbow.rotation = transform.rotation * Quaternion.Euler(0, -90, 90) * initialRotations[(int)SkeletonJoint.RightElbow];
		}
		if (null != LeftElbow) {
        	LeftElbow.rotation = transform.rotation * Quaternion.Euler(0, 90, -90) * initialRotations[(int)SkeletonJoint.LeftElbow];
		}
	}
	
	public void SetRootPositionBias()
	{
		this.PositionBias = -rootPosition;
	}
	
	public void SetRootPositionBias(Vector3 bias)
	{
		this.PositionBias = bias;	
	}
	
	void Zig_UpdateUser(ZigTrackedUser user)
	{
		UpdateRoot(user.UserData.CenterOfMass);
		if (user.UserData.Tracked) {
			foreach (ZigInputJoint joint in user.UserData.SkeletonData) {
				if (joint.GoodPosition) UpdatePosition(joint.Id, joint.Position);
				if (joint.GoodRotation) UpdateRotation(joint.Id, joint.Rotation);
			}
		}
	}
}
