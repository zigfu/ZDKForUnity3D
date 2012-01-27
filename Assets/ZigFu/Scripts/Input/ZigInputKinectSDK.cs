using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

/*
//-----------------------------------------------------------------------------

So why was the kinect reader written this way?!

1. The managed version of the Kinect SDK is .NET 4, and unity (or rather the 
   version of mono unity was built with) doesn't play with anything higher
   than 3.5
   
2. The NuiInitialize can only be called once per process. Calling it twice
   (even after calling NuiShutdown) causes a hang
   
3. Our singleton-monobehaviour is reinited every time the game is played in 
   the editor, which normally would cause NuiInit to be called multiple times
   (but PreventDoubleInit saves the day)

//-----------------------------------------------------------------------------
*/

class PreventDoubleInit
{ 
	[DllImport("kernel32.dll")]
	static extern IntPtr CreateEvent(IntPtr lpEventAttributes, bool bManualReset, bool bInitialState, string lpName);
	[DllImport("kernel32.dll")]
	static extern IntPtr OpenEvent(uint dwDesiredAccess, bool bInheritHandle, string lpName);
	
	const string eventName = "KinectReader_PreventDoubleInit";
	
	public static bool IsSafeToInit() {
		return (IntPtr.Zero == OpenEvent(2, false, eventName));
	}
	
	public static void MarkInited() {
		CreateEvent(IntPtr.Zero, false, false, eventName);
	}
}

public class NuiWrapper
{
    public enum NuiSkeletonTrackingState : uint
    {
        NotTracked = 0,
        PositionOnly = 1,
        Tracked = 2,
    }

    public enum NuiSkeletonPositionTrackingState : uint
    {
        NotTracked = 0,
        Inferred = 1,
        Tracked = 2,
    }
	
    public enum NuiSkeletonPositionIndex : uint
    {
        HipCenter = 0,
        Spine = 1,
        ShoulderCenter = 2,
        Head = 3,
        ShoulderLeft = 4,
        ElbowLeft = 5,
        WristLeft = 6,
        HandLeft = 7,
        ShoulderRight = 8,
        ElbowRight = 9,
        WristRight = 10,
        HandRight = 11,
        HipLeft = 12,
        KneeLeft = 13,
        AnkleLeft = 14,
        FootLeft = 15,
        HipRight = 16,
        KneeRight = 17,
        AnkleRight = 18,
        FootRight = 19,
		//Count = 20,
    }
	
	public enum NuiInitializeFlag : uint
	{
		UsesAudio 				= 0x10000000,
		UsesDepthAndPlayerIndex = 0x00000001,
		UsesColor 				= 0x00000002,
		UsesSkeleton			= 0x00000008,
		UsesDepth 				= 0x00000020,
	}
	
	[StructLayout(LayoutKind.Sequential)]
    public struct LARGE_INTEGER
    {
        public UInt32 LowPart;
        public UInt32 HighPart;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct Vector4
    {
        public float x;
        public float y;
        public float z;
        public float w;
    }
	
    [StructLayout(LayoutKind.Sequential)]
    public struct NuiSkeletonData
    {
        public NuiSkeletonTrackingState TrackingState;
        public UInt32 TrackingId;
        public UInt32 EnrollmentIndex;
        public UInt32 UserIndex;
        public Vector4 Position;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 20)]
        public Vector4[] SkeletonPositions;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 20)]
        public NuiSkeletonPositionTrackingState[] SkeletonPositionTrackingState;
        public UInt32 QualityFlags;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct NuiSkeletonFrame
    {
        public LARGE_INTEGER TimeStamp;
        public UInt32 FrameNumber;
        public UInt32 Flags;
        public Vector4 FloorClipPlane;
        public Vector4 NormalToGravity;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst=6)]
        public NuiSkeletonData[] SkeletonData;
    }
	
	//-------------------------------------------------------------------------
	
	[DllImport("kinect10.dll")]
	public static extern UInt32 NuiInitialize(UInt32 Flags);
	
	[DllImport("kinect10.dll")]
	public static extern UInt32 NuiSkeletonGetNextFrame(UInt32 Milliseconds, ref NuiSkeletonFrame skeletonFrame);
	
	[DllImport("kinect10.dll")]
	public static extern void NuiShutdown();
}

public class ZigInputKinectSDK : IZigInputReader
{
	NuiWrapper.NuiSkeletonFrame skeletonFrame = new NuiWrapper.NuiSkeletonFrame();
	
	//-------------------------------------------------------------------------
	// IZigInputReader interface
	//-------------------------------------------------------------------------
	
	public void Init()
	{
		UInt32 flags = 
			(uint)NuiWrapper.NuiInitializeFlag.UsesDepthAndPlayerIndex | 
		    (uint)NuiWrapper.NuiInitializeFlag.UsesSkeleton;
		
		if (PreventDoubleInit.IsSafeToInit()) {
			UInt32 hr = NuiWrapper.NuiInitialize(flags);
			if (0 != hr) {
				throw new Exception("Error initing Kinect SDK: " + hr);
			}

			PreventDoubleInit.MarkInited();
		}
	}
	
	public void Update() 
	{
		if (0 == NuiWrapper.NuiSkeletonGetNextFrame(0, ref skeletonFrame)) {
			ProcessNewSkeletonFrame();
		}
	}
	
	public void Shutdown() {
		// Unfortunately we cant call NuiShutdown because then NuiInitialize hangs
		// the next time its called (this happens in the editor, dll's aren't unloaded
		// and reloaded every time the game is launched). Oh well.
		
		//NuiWrapper.NuiShutdown()
	}
	
	
	public event EventHandler<NewUsersFrameEventArgs> NewUsersFrame;
	protected void OnNewUsersFrame(List<ZigInputUser> users) {
		if (null != NewUsersFrame) {
			NewUsersFrame.Invoke(this, new NewUsersFrameEventArgs(users));
		}
	}
	
	public Texture2D Depth { get; private set; }
	public Texture2D Image { get; private set; }
	public bool UpdateDepth { get; set; }
	public bool UpdateImage { get; set; }
	
	//-------------------------------------------------------------------------
	// Internal stuff
	//-------------------------------------------------------------------------
	
	Vector3 Vector4ToVector3(NuiWrapper.Vector4 pos)
	{
		return new Vector3(pos.x * 1000, pos.y * 1000, pos.z * -1000);
	}
	
	ZigJointId NuiToZig(NuiWrapper.NuiSkeletonPositionIndex nuiJoint) {
		switch (nuiJoint) {															
		case NuiWrapper.NuiSkeletonPositionIndex.HipCenter: return ZigJointId.Waist;										
		case NuiWrapper.NuiSkeletonPositionIndex.Spine: return ZigJointId.Torso;
		case NuiWrapper.NuiSkeletonPositionIndex.ShoulderCenter: return ZigJointId.Neck;
		case NuiWrapper.NuiSkeletonPositionIndex.Head: return ZigJointId.Head;
		case NuiWrapper.NuiSkeletonPositionIndex.ShoulderLeft: return ZigJointId.LeftShoulder;
		case NuiWrapper.NuiSkeletonPositionIndex.ElbowLeft: return ZigJointId.LeftElbow;
		case NuiWrapper.NuiSkeletonPositionIndex.WristLeft: return ZigJointId.LeftWrist;
		case NuiWrapper.NuiSkeletonPositionIndex.HandLeft: return ZigJointId.LeftHand;
		case NuiWrapper.NuiSkeletonPositionIndex.ShoulderRight: return ZigJointId.RightShoulder;
		case NuiWrapper.NuiSkeletonPositionIndex.ElbowRight: return ZigJointId.RightElbow;
		case NuiWrapper.NuiSkeletonPositionIndex.WristRight: return ZigJointId.RightWrist;
		case NuiWrapper.NuiSkeletonPositionIndex.HandRight: return ZigJointId.RightHand;
		case NuiWrapper.NuiSkeletonPositionIndex.HipLeft: return ZigJointId.LeftHip;
		case NuiWrapper.NuiSkeletonPositionIndex.KneeLeft: return ZigJointId.LeftKnee;
		case NuiWrapper.NuiSkeletonPositionIndex.AnkleLeft: return ZigJointId.LeftAnkle;
		case NuiWrapper.NuiSkeletonPositionIndex.FootLeft: return ZigJointId.LeftFoot;
		case NuiWrapper.NuiSkeletonPositionIndex.HipRight: return ZigJointId.RightHip;
		case NuiWrapper.NuiSkeletonPositionIndex.KneeRight: return ZigJointId.RightKnee;
		case NuiWrapper.NuiSkeletonPositionIndex.AnkleRight: return ZigJointId.RightAnkle;
		case NuiWrapper.NuiSkeletonPositionIndex.FootRight: return ZigJointId.RightFoot;
		}
		return 0;
	}
	
	void ProcessNewSkeletonFrame() {
		NuiWrapper.NuiSkeletonData skel;
		
		// foreach user
		List<ZigInputUser> users = new List<ZigInputUser>();
		foreach (var skeleton in skeletonFrame.SkeletonData) {
	
			if (skeleton.TrackingState == NuiWrapper.NuiSkeletonTrackingState.NotTracked) {
				continue;
			}
	
			// skeleton data
			List<ZigInputJoint> joints = new List<ZigInputJoint>();
			bool tracked = skeleton.TrackingState == NuiWrapper.NuiSkeletonTrackingState.Tracked;
			if (tracked) {
				// we need this if we want to use the skeleton as a ref arg
				skel = skeleton;

				foreach (NuiWrapper.NuiSkeletonPositionIndex j in Enum.GetValues(typeof(NuiWrapper.NuiSkeletonPositionIndex))) {
					// skip joints that aren't tracked
					if (skeleton.SkeletonPositionTrackingState[(int)j] == NuiWrapper.NuiSkeletonPositionTrackingState.NotTracked) {
						continue;
					}
					ZigInputJoint joint = new ZigInputJoint(NuiToZig(j));
					joint.Position = Vector4ToVector3(skeleton.SkeletonPositions[(int)j]);
					joint.Rotation = getJointOrientation(ref skel, j);
					joint.GoodRotation = true;
					joint.GoodPosition = true;
					joints.Add(joint);
				}
			}
			
			ZigInputUser user = new ZigInputUser((int)skeleton.TrackingId, Vector4ToVector3(skeleton.Position));
			user.Tracked = tracked;
			user.SkeletonData = joints;
			users.Add(user);
		}
		
		OnNewUsersFrame(users);

	}
	
	private Vector3 vec4to3(NuiWrapper.Vector4 v4)
	{
		return new Vector3(v4.x, v4.y, -v4.z);
	}

	private Vector3 jointPositionFromSkeleton(NuiWrapper.NuiSkeletonData skeleton, NuiWrapper.NuiSkeletonPositionIndex index)
	{
		//return Vector4ToVector3(skeleton.SkeletonPositions[(int)index]);
		return vec4to3(skeleton.SkeletonPositions[(int)index]);
	}

	private class Matrix3x3
	{
        public Matrix3x3()
        {
            col0 = Vector3.right;
            col1 = Vector3.up;
            col2 = Vector3.forward;
        }
		public Vector3 col0;
        public Vector3 col1;
        public Vector3 col2;
		
		public Quaternion ToQuaternion() 
		{
			//Vector3 worldY = new Vector3(col1.x, col1.y, -col1.z);
			//Vector3 worldZ = new Vector3(-col2.x, -col2.y, col2.z);
			//return Quaternion.LookRotation(worldZ, worldY);
			Quaternion rot = Quaternion.LookRotation(col2, col1);
			return rot;
		}
	}

	private Vector3 vectorBetweenNuiJoints(	ref NuiWrapper.NuiSkeletonData skeleton, 
													NuiWrapper.NuiSkeletonPositionIndex p1, 
													NuiWrapper.NuiSkeletonPositionIndex p2)
	{
		if (skeleton.SkeletonPositionTrackingState[(int)p1] == NuiWrapper.NuiSkeletonPositionTrackingState.NotTracked ||
			skeleton.SkeletonPositionTrackingState[(int)p2] == NuiWrapper.NuiSkeletonPositionTrackingState.NotTracked) 
		{
			return Vector3.zero;
		}

		return jointPositionFromSkeleton(skeleton, p2) - jointPositionFromSkeleton(skeleton, p1);
	}

	private Matrix3x3 orientationFromX(Vector3 v)
	{
		Matrix3x3 result = new Matrix3x3();
		result.col0 = v.normalized;
		result.col1 = (new Vector3(0.0f, v.z, -v.y)).normalized;
		result.col2 = Vector3.Cross(result.col0, result.col1);
		return result;
	}

	private Matrix3x3 orientationFromY(Vector3 v)
	{
		Matrix3x3 result = new Matrix3x3();
		result.col0 = (new Vector3(v.y,-v.x, 0.0f)).normalized;
		result.col1 = v.normalized;
		result.col2 = Vector3.Cross(result.col0, result.col1);
		return result;
	}

	private Matrix3x3 orientationFromZ(Vector3 v)
	{
		Matrix3x3 result = new Matrix3x3();
		result.col0 = (new Vector3(v.y, -v.x, 0.0f)).normalized;
		result.col1 = v.normalized;
		result.col2 = Vector3.Cross(result.col1, result.col0);
		return result;
	}

	private Matrix3x3 orientationFromXY(Vector3 vx, Vector3 vy)
	{
		Matrix3x3 result = new Matrix3x3();
		result.col0 = vx.normalized;
		result.col1 = Vector3.Cross(result.col0,vy.normalized).normalized;
		result.col2 = Vector3.Cross(result.col2, result.col0);
		return result;
	}

	private Matrix3x3 orientationFromYX(Vector3 vx, Vector3 vy)
	{
		Matrix3x3 result = new Matrix3x3();
		result.col1 = vy.normalized;
		result.col2 = Vector3.Cross(vx.normalized, result.col1.normalized);
		result.col0 = Vector3.Cross(result.col1, result.col2);
		return result;
	}

	private Matrix3x3 orientationFromYZ(Vector3 vy, Vector3 vz)
	{
		Matrix3x3 result = new Matrix3x3();
		result.col1 = vy.normalized;
		result.col0 = Vector3.Cross(result.col1, vz.normalized).normalized;
		result.col2 = Vector3.Cross(result.col0, result.col1);
		return result;
	}

	private Quaternion getJointOrientation(ref NuiWrapper.NuiSkeletonData skeleton, NuiWrapper.NuiSkeletonPositionIndex joint) 
	{
		Matrix3x3 result = new Matrix3x3();
		switch (joint) 
		{
	  		case NuiWrapper.NuiSkeletonPositionIndex.HipCenter:
				result = orientationFromYX(
					vectorBetweenNuiJoints(ref skeleton,NuiWrapper.NuiSkeletonPositionIndex.HipLeft,		NuiWrapper.NuiSkeletonPositionIndex.HipRight),
					vectorBetweenNuiJoints(ref skeleton,NuiWrapper.NuiSkeletonPositionIndex.HipCenter,		NuiWrapper.NuiSkeletonPositionIndex.Spine));
				break;
	   
			case NuiWrapper.NuiSkeletonPositionIndex.Spine:
				result = orientationFromYX(
					vectorBetweenNuiJoints(ref skeleton,NuiWrapper.NuiSkeletonPositionIndex.ShoulderLeft,	NuiWrapper.NuiSkeletonPositionIndex.ShoulderRight),
					vectorBetweenNuiJoints(ref skeleton,NuiWrapper.NuiSkeletonPositionIndex.Spine,			NuiWrapper.NuiSkeletonPositionIndex.ShoulderCenter));
				break;
	   
			case NuiWrapper.NuiSkeletonPositionIndex.ShoulderCenter:
				result = orientationFromYX(
					vectorBetweenNuiJoints(ref skeleton,NuiWrapper.NuiSkeletonPositionIndex.ShoulderLeft,	NuiWrapper.NuiSkeletonPositionIndex.ShoulderRight),
					vectorBetweenNuiJoints(ref skeleton,NuiWrapper.NuiSkeletonPositionIndex.ShoulderCenter,	NuiWrapper.NuiSkeletonPositionIndex.Head));
				break;

			case NuiWrapper.NuiSkeletonPositionIndex.Head:
				result = orientationFromY(
					vectorBetweenNuiJoints(ref skeleton,NuiWrapper.NuiSkeletonPositionIndex.ShoulderCenter,	NuiWrapper.NuiSkeletonPositionIndex.Head));
				break;
	   
			case NuiWrapper.NuiSkeletonPositionIndex.ShoulderLeft:
				result = orientationFromXY(
					vectorBetweenNuiJoints(ref skeleton,NuiWrapper.NuiSkeletonPositionIndex.ShoulderLeft,	NuiWrapper.NuiSkeletonPositionIndex.ElbowLeft),
			        //-vectorBetweenNuiJoints(ref skeleton,NuiWrapper.NuiSkeletonPositionIndex.ShoulderLeft,	NuiWrapper.NuiSkeletonPositionIndex.ElbowLeft),
					vectorBetweenNuiJoints(ref skeleton,NuiWrapper.NuiSkeletonPositionIndex.ElbowLeft,		NuiWrapper.NuiSkeletonPositionIndex.WristLeft));
				break;
	   
			case NuiWrapper.NuiSkeletonPositionIndex.ElbowLeft:
				result = orientationFromXY(
					-vectorBetweenNuiJoints(ref skeleton,NuiWrapper.NuiSkeletonPositionIndex.ElbowLeft,		NuiWrapper.NuiSkeletonPositionIndex.WristLeft),
					-vectorBetweenNuiJoints(ref skeleton,NuiWrapper.NuiSkeletonPositionIndex.ShoulderLeft,	NuiWrapper.NuiSkeletonPositionIndex.ElbowLeft));
				break;
	   
			case NuiWrapper.NuiSkeletonPositionIndex.WristLeft:
				result = orientationFromX(
					-vectorBetweenNuiJoints(ref skeleton,NuiWrapper.NuiSkeletonPositionIndex.WristLeft,		NuiWrapper.NuiSkeletonPositionIndex.HandLeft));
				break;
	   
			case NuiWrapper.NuiSkeletonPositionIndex.HandLeft:
				result = orientationFromX(
					-vectorBetweenNuiJoints(ref skeleton,NuiWrapper.NuiSkeletonPositionIndex.WristLeft,		NuiWrapper.NuiSkeletonPositionIndex.HandLeft));
				break;
	   
			case NuiWrapper.NuiSkeletonPositionIndex.HipLeft:
				result = orientationFromYX(
					vectorBetweenNuiJoints(ref skeleton,NuiWrapper.NuiSkeletonPositionIndex.HipLeft,		NuiWrapper.NuiSkeletonPositionIndex.HipRight),
					vectorBetweenNuiJoints(ref skeleton,NuiWrapper.NuiSkeletonPositionIndex.KneeLeft,		NuiWrapper.NuiSkeletonPositionIndex.HipLeft));
				break;
	   
			case NuiWrapper.NuiSkeletonPositionIndex.KneeLeft:
				result = orientationFromY(
					-vectorBetweenNuiJoints(ref skeleton,NuiWrapper.NuiSkeletonPositionIndex.KneeLeft,		NuiWrapper.NuiSkeletonPositionIndex.AnkleLeft));
				break;
	   
			case NuiWrapper.NuiSkeletonPositionIndex.AnkleLeft:
				result = orientationFromZ(
					vectorBetweenNuiJoints(ref skeleton,NuiWrapper.NuiSkeletonPositionIndex.FootLeft,		NuiWrapper.NuiSkeletonPositionIndex.AnkleLeft));
				break;
	   
			case NuiWrapper.NuiSkeletonPositionIndex.FootLeft:
				result = orientationFromZ(
					vectorBetweenNuiJoints(ref skeleton,NuiWrapper.NuiSkeletonPositionIndex.FootLeft,		NuiWrapper.NuiSkeletonPositionIndex.AnkleLeft));
				break;
	   
	   
			case NuiWrapper.NuiSkeletonPositionIndex.ShoulderRight:
				result = orientationFromXY(
					vectorBetweenNuiJoints(ref skeleton,NuiWrapper.NuiSkeletonPositionIndex.ShoulderRight,	NuiWrapper.NuiSkeletonPositionIndex.ElbowRight),
					vectorBetweenNuiJoints(ref skeleton,NuiWrapper.NuiSkeletonPositionIndex.ElbowRight,		NuiWrapper.NuiSkeletonPositionIndex.WristRight));
				break;
	   
			case NuiWrapper.NuiSkeletonPositionIndex.ElbowRight:
				result = orientationFromXY(
					vectorBetweenNuiJoints(ref skeleton,NuiWrapper.NuiSkeletonPositionIndex.ElbowRight,		NuiWrapper.NuiSkeletonPositionIndex.WristRight),
					-vectorBetweenNuiJoints(ref skeleton,NuiWrapper.NuiSkeletonPositionIndex.ShoulderRight,	NuiWrapper.NuiSkeletonPositionIndex.ElbowRight));
				break;
	   
			case NuiWrapper.NuiSkeletonPositionIndex.WristRight:
				result = orientationFromX(
					vectorBetweenNuiJoints(ref skeleton,NuiWrapper.NuiSkeletonPositionIndex.WristRight,		NuiWrapper.NuiSkeletonPositionIndex.HandRight));
				break;
	   
			case NuiWrapper.NuiSkeletonPositionIndex.HandRight:
				result = orientationFromX(
					vectorBetweenNuiJoints(ref skeleton,NuiWrapper.NuiSkeletonPositionIndex.WristRight,		NuiWrapper.NuiSkeletonPositionIndex.HandRight));
				break;
	   
			case NuiWrapper.NuiSkeletonPositionIndex.HipRight:
				result = orientationFromYX(
					vectorBetweenNuiJoints(ref skeleton,NuiWrapper.NuiSkeletonPositionIndex.HipLeft,		NuiWrapper.NuiSkeletonPositionIndex.HipRight),
					vectorBetweenNuiJoints(ref skeleton,NuiWrapper.NuiSkeletonPositionIndex.KneeRight,		NuiWrapper.NuiSkeletonPositionIndex.HipRight));
				break;
	   
			case NuiWrapper.NuiSkeletonPositionIndex.KneeRight:
				result = orientationFromYZ(
					-vectorBetweenNuiJoints(ref skeleton,NuiWrapper.NuiSkeletonPositionIndex.KneeRight,		NuiWrapper.NuiSkeletonPositionIndex.AnkleRight),
					-vectorBetweenNuiJoints(ref skeleton,NuiWrapper.NuiSkeletonPositionIndex.AnkleRight,	NuiWrapper.NuiSkeletonPositionIndex.FootRight));
				break;
	   
			case NuiWrapper.NuiSkeletonPositionIndex.AnkleRight:
				result = orientationFromZ(
					vectorBetweenNuiJoints(ref skeleton,NuiWrapper.NuiSkeletonPositionIndex.FootRight,		NuiWrapper.NuiSkeletonPositionIndex.AnkleRight));
				break;
	   
			case NuiWrapper.NuiSkeletonPositionIndex.FootRight:
				result = orientationFromZ(
					vectorBetweenNuiJoints(ref skeleton,NuiWrapper.NuiSkeletonPositionIndex.FootRight,		NuiWrapper.NuiSkeletonPositionIndex.AnkleRight));
				break;
		}
		
        // turn result into arraylist and pass it like a kidney stone
        return result.ToQuaternion();
	}
}

