using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;


/*
FraceTrackData.dll and FaceTracklib.dll from then MS sdk need to be
included in the the root of the unity project, as well as 
ZigNativeFaceTracking.dll needs to be included in a unity plugins folder


These classes could be added to the top of ZigInputKinectSDK.cs 
like the nuiwraper if you wanted.
*/

public enum ZigFaceTrackingEvents { FaceDetected = 1, FaceLost = -1, ContinueTracking = 0}

public class ZigFaceTransform
{
	public Vector3 position = Vector3.zero;
	public Vector3 eulerAngles = Vector3.zero;
}


public static class ZigFaceTracker
{
	[StructLayout(LayoutKind.Sequential, Pack=1)]
	public struct FT_FaceTransform
	{
	    public float positionX;
	    public float positionY;
	    public float positionZ;
	    public float rotationX;
	    public float rotationY;
	    public float rotationZ;
	}

	public static IntPtr ImageBuffer = new IntPtr();
	public static IntPtr DepthBuffer = new IntPtr();

	private static UInt32 NativeFaceTracker;

	public static ZigFaceTransform FaceTransform = new ZigFaceTransform();

    [DllImport("ZigNativeFaceTracking")]
    public static extern UInt32 FT_CreateFaceTracker();

    [DllImport("ZigNativeFaceTracking")]
    public static extern UInt32 FT_InitFaceTracker();

    [DllImport("ZigNativeFaceTracking")]
    public static extern UInt32  FT_ProcessVideoFrame(IntPtr ImageFrame);

    [DllImport("ZigNativeFaceTracking")]
    public static extern UInt32  FT_ProcessDepthFrame(IntPtr ImageFrame);

	[DllImport("ZigNativeFaceTracking")]
    public static extern UInt32 FT_TrackFrame();    

	[DllImport("ZigNativeFaceTracking")]
    public static extern UInt32 FT_ShutDown(); 

    [DllImport("ZigNativeFaceTracking",CallingConvention = CallingConvention.StdCall)]
    public static extern FT_FaceTransform FT_GetFaceTransform();

    public static void UpdateFaceTransform()
    {
		ZigFaceTracker.FT_FaceTransform newPos = FT_GetFaceTransform();
    	ZigFaceTracker.FaceTransform.position = new Vector3(newPos.positionX*10.0f,newPos.positionY*10.0f,newPos.positionZ*-10.0f);
    	ZigFaceTracker.FaceTransform.eulerAngles = new Vector3(newPos.rotationX*-1.0f,newPos.rotationY,newPos.rotationZ*-1.0f);
    }
}


