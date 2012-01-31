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
    [DllImport("kernel32.dll")]
    static extern IntPtr CreateFileMapping(IntPtr hFile, IntPtr lpFileMappingAttributes, FileMapProtection flProtect, uint dwMaximumSizeHigh, uint dwMaximumSizeLow, [MarshalAs(UnmanagedType.LPTStr)] string lpName);
    [DllImport("kernel32.dll", SetLastError = true)]
    static extern IntPtr MapViewOfFile(IntPtr hFileMappingObject, FileMapAccess dwDesiredAccess, uint dwFileOffsetHigh, uint dwFileOffsetLow, uint dwNumberOfBytesToMap);
    [DllImport("Kernel32.dll")]
    static extern bool UnmapViewOfFile(IntPtr map);
    [DllImport("kernel32.dll")]
    static extern int CloseHandle(IntPtr hObject);

    enum FileMapProtection : uint
    {
        PageReadonly = 0x02,
        PageReadWrite = 0x04,
        PageWriteCopy = 0x08,
        PageExecuteRead = 0x20,
        PageExecuteReadWrite = 0x40,
        SectionCommit = 0x8000000,
        SectionImage = 0x1000000,
        SectionNoCache = 0x10000000,
        SectionReserve = 0x4000000,
    }

    [Flags]
    public enum FileMapAccess : uint
    {
        FileMapCopy = 0x0001,
        FileMapWrite = 0x0002,
        FileMapRead = 0x0004,
        FileMapAllAccess = 0x001f,
        FileMapExecute = 0x0020,
    }

    static IntPtr INVALID_HANDLE_VALUE = new IntPtr(-1);
	
    const string eventName = "KinectReader_PreventDoubleInit";
    const string memoryMapName = "KinectReader_PreventDoubleInitMemoryMap";

	public static bool IsSafeToInit() {
		return (IntPtr.Zero == OpenEvent(2, false, eventName));
	}
	
	public static void MarkInited() {
		CreateEvent(IntPtr.Zero, false, false, eventName);
	}

    public static void SaveContext<T>(T toSave) {
        IntPtr h = CreateFileMapping(INVALID_HANDLE_VALUE, IntPtr.Zero, FileMapProtection.PageReadWrite, 0, (uint)Marshal.SizeOf(typeof(T)), memoryMapName);
        IntPtr buffer = MapViewOfFile(h, FileMapAccess.FileMapWrite, 0, 0, (uint)Marshal.SizeOf(typeof(T)));
        Marshal.StructureToPtr(toSave, buffer, false);
        UnmapViewOfFile(buffer);
        // No closehandle here!
    }

    public static T LoadContext<T>() {
        IntPtr h = CreateFileMapping(INVALID_HANDLE_VALUE, IntPtr.Zero, FileMapProtection.PageReadWrite, 0, (uint)Marshal.SizeOf(typeof(T)), memoryMapName);
        IntPtr buffer = MapViewOfFile(h, FileMapAccess.FileMapRead, 0, 0, (uint)Marshal.SizeOf(typeof(T)));
        T result = (T) Marshal.PtrToStructure(buffer, typeof(T));
        UnmapViewOfFile(buffer);
        CloseHandle(h);
        return result;
    }
}

public struct NuiContext
{
    public IntPtr DepthHandle;
    public IntPtr ImageHandle;
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

    public enum NuiImageType
    {
        DepthAndPlayerIndex = 0,
        Color,
        ColorYuv,
        ColorRawYuv,
        Depth
    }

    public enum NuiImageResolution
    {
        Invalid = -1,
        Res80x60 = 0,
        Res320x240,
        Res640x480,
        Res1280x960,
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

    public enum NuiImageDigitalzoom
    {
        x1 = 0,
    } 

    public struct NuiImageViewArea 
    {
        NuiImageDigitalzoom DigitalZoom;
        UInt32 CenterX;
        UInt32 CenterY;
    };

    [StructLayout(LayoutKind.Sequential)]
    public struct NuiImageFrame 
    {
        public LARGE_INTEGER TimeStamp;
        public UInt32 FrameNumber;
        public NuiImageType ImageType;
        public NuiImageResolution Resolution;
        public IntPtr FrameTexture;
        public UInt32 FrameFlags;
        public NuiImageViewArea ViewArea;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct NuiLockedRect 
    {
        public int Pitch;
        public int Size;
        public IntPtr ActualDataFinally;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct ColorPixel
    {
        public byte b;
        public byte g;
        public byte r;
        public byte a;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct ColorBuffer
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 640 * 480)]
        public ColorPixel[] data;
    }

    public class INuiFrameTexture
    {
        [StructLayout(LayoutKind.Sequential)]
        public struct INuiFrameTextureVtbl
        {
            public IntPtr QueryInterface;
            public IntPtr AddRef;
            public IntPtr Release;

            public IntPtr BufferLen;
            public IntPtr Pitch;

            public IntPtr LockRect;
            public IntPtr GetLevelDesc;
            public IntPtr UnlockRect;
        }

        INuiFrameTextureVtbl vtbl;
        IntPtr p;

        delegate int BufferLenDelegate(IntPtr This);
        delegate int PitchDelegate(IntPtr This);
        delegate UInt32 LockRectDelegate(IntPtr This, uint level, ref NuiLockedRect rect, IntPtr pRect, UInt32 Flags);
        delegate UInt32 UnlockRectDelegate(IntPtr This, uint level);

        BufferLenDelegate bufferLen;
        PitchDelegate pitch;
        LockRectDelegate lockRect;
        UnlockRectDelegate unlockRect;

        public INuiFrameTexture(IntPtr p) {
            this.p = p;

            IntPtr pVtbl = (IntPtr)Marshal.PtrToStructure(p, typeof(IntPtr));
            vtbl = (INuiFrameTextureVtbl)Marshal.PtrToStructure(pVtbl, typeof(INuiFrameTextureVtbl));

            bufferLen = (BufferLenDelegate)Marshal.GetDelegateForFunctionPointer(vtbl.BufferLen, typeof(BufferLenDelegate));
            pitch = (PitchDelegate)Marshal.GetDelegateForFunctionPointer(vtbl.Pitch, typeof(PitchDelegate));
            lockRect = (LockRectDelegate)Marshal.GetDelegateForFunctionPointer(vtbl.LockRect, typeof(LockRectDelegate));
            unlockRect = (UnlockRectDelegate)Marshal.GetDelegateForFunctionPointer(vtbl.UnlockRect, typeof(UnlockRectDelegate));
        }

        public int Pitch {
            get {
                return pitch(p);
            }
        }

        public int BufferLen {
            get {
                return bufferLen(p);
            }
        }

        public NuiLockedRect LockRect() {
            NuiLockedRect result = new NuiLockedRect();
            lockRect(p, 0, ref result, IntPtr.Zero, 0);
            return result;
        }

        public void UnlockRect() {
            unlockRect(p, 0);
        }
    }

	//-------------------------------------------------------------------------
	
	[DllImport("kinect10.dll")]
	public static extern UInt32 NuiInitialize(UInt32 Flags);

	[DllImport("kinect10.dll")]
	public static extern UInt32 NuiSkeletonGetNextFrame(UInt32 Milliseconds, ref NuiSkeletonFrame skeletonFrame);
	
    [DllImport("kinect10.dll")]
	public static extern UInt32 NuiImageStreamOpen(NuiImageType ImageType, NuiImageResolution Resolution, UInt32 ImageFrameFlags, UInt32 FrameLimit, IntPtr NextFrameEvent, out IntPtr StreamHandle);

    [DllImport("kinect10.dll")]
    public static extern UInt32 NuiImageStreamGetNextFrame(IntPtr Stream, UInt32 MillisecondsToWait, out IntPtr ImageFrame);

    [DllImport("kinect10.dll")]
    public static extern UInt32 NuiImageStreamReleaseFrame(IntPtr Stream, ref NuiImageFrame ImageFrame);

	[DllImport("kinect10.dll")]
	public static extern void NuiShutdown();
}

public class ZigInputKinectSDK : IZigInputReader
{
	NuiWrapper.NuiSkeletonFrame skeletonFrame = new NuiWrapper.NuiSkeletonFrame();
    NuiWrapper.NuiImageFrame depthFrame;
    NuiWrapper.NuiImageFrame imageFrame;
    NuiContext context;

    short[] rawDepthMap;
    float[] depthHistogramMap;
    Color32[] depthMapPixels;
    int XRes;
    int YRes;
    int factor;

    int ImageXRes;
    int ImageYRes;
    Color32[] rawImageMap;

    int lastDepthFrameId;
    int lastImageFrameId;

	//-------------------------------------------------------------------------
	// IZigInputReader interface
	//-------------------------------------------------------------------------

	public void Init()
	{
		UInt32 flags = 
			(uint)NuiWrapper.NuiInitializeFlag.UsesDepthAndPlayerIndex | 
		    (uint)NuiWrapper.NuiInitializeFlag.UsesSkeleton;// |
            //(uint)NuiWrapper.NuiInitializeFlag.UsesColor;
        
        if (PreventDoubleInit.IsSafeToInit()) {

            UInt32 hr = NuiWrapper.NuiInitialize(flags);
            if (0 != hr) {
                throw new Exception("Error initing Kinect SDK: " + hr);
            }

            context = new NuiContext();
            hr = NuiWrapper.NuiImageStreamOpen(NuiWrapper.NuiImageType.DepthAndPlayerIndex, NuiWrapper.NuiImageResolution.Res320x240, 0, 2, IntPtr.Zero, out context.DepthHandle);
            if (0 != hr) {
                throw new Exception("Error opening depth stream: " + hr);
            }

            //hr = NuiWrapper.NuiImageStreamOpen(NuiWrapper.NuiImageType.Color, NuiWrapper.NuiImageResolution.Res640x480, 0, 2, IntPtr.Zero, out context.ImageHandle);
            //if (0 != hr) {
            //    throw new Exception("Error opening image stream: " + hr);
            //}
            
            PreventDoubleInit.SaveContext<NuiContext>(context);
            PreventDoubleInit.MarkInited();
        }
        else {
            context = PreventDoubleInit.LoadContext<NuiContext>();
        }

        // init textures
        factor = 1;
        XRes = 320 / factor;
        YRes = 240 / factor;
        Depth = new Texture2D(XRes, YRes);

        //ImageXRes = 640 / factor;
        //ImageYRes = 480 / factor;
        //Image = new Texture2D(ImageXRes, ImageYRes);
        //rawImageMap = new Color32[ImageXRes * ImageYRes];

        // depthmap data
        rawDepthMap = new short[XRes * YRes];
        depthMapPixels = new Color32[(XRes / factor) * (YRes / factor)];

        // histogram stuff
        int maxDepth = 4000;
        depthHistogramMap = new float[maxDepth];

	}
	
	public void Update() 
	{
		if (0 == NuiWrapper.NuiSkeletonGetNextFrame(0, ref skeletonFrame)) {
			ProcessNewSkeletonFrame();
		}
        IntPtr pDepthFrame;
        if (0 == NuiWrapper.NuiImageStreamGetNextFrame(context.DepthHandle, 0, out pDepthFrame)) {
            // deal with deref'ing/marshalling
            depthFrame = (NuiWrapper.NuiImageFrame) Marshal.PtrToStructure(pDepthFrame, typeof(NuiWrapper.NuiImageFrame));
            NuiWrapper.INuiFrameTexture depthTexture = new NuiWrapper.INuiFrameTexture(depthFrame.FrameTexture);

            // lock & copy the depth data
            NuiWrapper.NuiLockedRect rect = depthTexture.LockRect();
            Marshal.Copy(rect.ActualDataFinally, rawDepthMap, 0, rawDepthMap.Length);
            depthTexture.UnlockRect();

            // process it
            UpdateHistogram();
            UpdateDepthmapTexture();
            
            // release current frame
            NuiWrapper.NuiImageStreamReleaseFrame(context.DepthHandle, ref depthFrame);
        }

        //IntPtr pImageFrame;
        //if (0 == NuiWrapper.NuiImageStreamGetNextFrame(context.ImageHandle, 0, out pImageFrame)) {
        //    imageFrame = (NuiWrapper.NuiImageFrame)Marshal.PtrToStructure(pImageFrame, typeof(NuiWrapper.NuiImageFrame));
        //    Debug.Log("Image frame: " + imageFrame.FrameNumber);
            //NuiWrapper.INuiFrameTexture imageTexture = new NuiWrapper.INuiFrameTexture(imageFrame.FrameTexture);
            /*
            NuiWrapper.NuiLockedRect rect = imageTexture.LockRect();
            NuiWrapper.ColorBuffer colors = (NuiWrapper.ColorBuffer)Marshal.PtrToStructure(rect.ActualDataFinally, typeof(NuiWrapper.ColorBuffer));
            for (int i = 0; i < rect.Size; i++) {
                rawImageMap[i].r = colors.data[i].r;
                rawImageMap[i].g = colors.data[i].g;
                rawImageMap[i].b = colors.data[i].b;
                rawImageMap[i].a = colors.data[i].a;
            }
            imageTexture.UnlockRect();*/

       //     NuiWrapper.NuiImageStreamReleaseFrame(context.ImageHandle, ref imageFrame);
        //}
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
		return new Vector3(v4.x, v4.y, v4.z);
	}

	private Vector3 jointPositionFromSkeleton(NuiWrapper.NuiSkeletonData skeleton, NuiWrapper.NuiSkeletonPositionIndex index)
	{
		return vec4to3(skeleton.SkeletonPositions[(int)index]);
	}

    public Vector3 RCross(Vector3 v1, Vector3 v2)
    {
        Vector3 result;
        result.x = v1.y * v2.z - v1.z * v2.y;
        result.y = v1.z * v2.x - v1.x * v2.z;
        result.z = v1.x * v2.y - v1.y * v2.x;
        return result;
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
			Vector3 worldY = new Vector3(col1.x, col1.y, -col1.z);
            Vector3 worldZ = new Vector3(-col2.x, -col2.y, col2.z);
			return Quaternion.LookRotation(worldZ, worldY);
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
		result.col2 = RCross(result.col0, result.col1);
		return result;
	}

	private Matrix3x3 orientationFromY(Vector3 v)
	{
		Matrix3x3 result = new Matrix3x3();
		result.col0 = (new Vector3(v.y,-v.x, 0.0f)).normalized;
		result.col1 = v.normalized;
		result.col2 = RCross(result.col0, result.col1);
		return result;
	}

	private Matrix3x3 orientationFromZ(Vector3 v)
	{
		Matrix3x3 result = new Matrix3x3();
		result.col0 = (new Vector3(v.y, -v.x, 0.0f)).normalized;
		result.col1 = v.normalized;
		result.col2 = RCross(result.col1, result.col0);
		return result;
	}

	private Matrix3x3 orientationFromXY(Vector3 vx, Vector3 vy)
	{
		Matrix3x3 result = new Matrix3x3();
		result.col0 = vx.normalized;
		result.col2 = RCross(result.col0,vy.normalized).normalized;
		result.col1 = RCross(result.col2, result.col0);
		return result;
	}

	private Matrix3x3 orientationFromYX(Vector3 vx, Vector3 vy)
	{
		Matrix3x3 result = new Matrix3x3();
		result.col1 = vy.normalized;
		result.col2 = RCross(vx.normalized, result.col1.normalized);
		result.col0 = RCross(result.col1, result.col2);
		return result;
	}

	private Matrix3x3 orientationFromYZ(Vector3 vy, Vector3 vz)
	{
		Matrix3x3 result = new Matrix3x3();
		result.col1 = vy.normalized;
		result.col0 = RCross(result.col1, vz.normalized).normalized;
		result.col2 = RCross(result.col0, result.col1);
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
			        -vectorBetweenNuiJoints(ref skeleton,NuiWrapper.NuiSkeletonPositionIndex.ShoulderLeft,	NuiWrapper.NuiSkeletonPositionIndex.ElbowLeft),
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
		
        return result.ToQuaternion();
	}

    void UpdateHistogram() {
        int i, numOfPoints = 0;

        Array.Clear(depthHistogramMap, 0, depthHistogramMap.Length);

        short curr;
        for (i = 0; i < rawDepthMap.Length; i++) {
            // only calculate for valid depth
            curr = (short)(rawDepthMap[i] >> 3);
            if (curr != 0) {
                depthHistogramMap[curr]++;
                numOfPoints++;
            }
        }

        if (numOfPoints > 0) {
            for (i = 1; i < depthHistogramMap.Length; i++) {
                depthHistogramMap[i] += depthHistogramMap[i - 1];
            }
            for (i = 0; i < depthHistogramMap.Length; i++) {
                depthHistogramMap[i] = (1.0f - (depthHistogramMap[i] / numOfPoints)) * 255;
            }
        }
        depthHistogramMap[0] = 0;
    }

    void UpdateDepthmapTexture() {
        // flip the depthmap as we create the texture
        int YScaled = YRes / factor;
        int XScaled = XRes / factor;
        int i = XScaled * YScaled - XScaled;
        int depthIndex = 0;
        for (int y = 0; y < YScaled; ++y, i -= XScaled) {
            for (int x = 0; x < XScaled; ++x, depthIndex += factor) {
                short pixel = (short)(rawDepthMap[depthIndex] >> 3);
                if (pixel == 0) {
                    depthMapPixels[i + x] = Color.clear;
                }
                else {
                    Color32 c = new Color32((byte)depthHistogramMap[pixel], (byte)depthHistogramMap[pixel], 0, 255);
                    depthMapPixels[i + x] = c;
                }
            }
            // Skip lines
            depthIndex += (factor - 1) * XRes;
        }

        Depth.SetPixels32(depthMapPixels);
        Depth.Apply();
    }
}

