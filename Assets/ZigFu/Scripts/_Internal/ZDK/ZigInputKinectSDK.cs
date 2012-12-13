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
    public static extern IntPtr CreateEvent(IntPtr lpEventAttributes, bool bManualReset, bool bInitialState, string lpName);
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

    [DllImport("kernel32", SetLastError = true, CharSet = CharSet.Unicode)]
    public static extern IntPtr LoadLibrary(string lpFileName);

    [DllImport("kernel32", CharSet = CharSet.Ansi, ExactSpelling = true, SetLastError = true)]
    public static extern UIntPtr GetProcAddress(IntPtr hModule, string procName);

    public static bool IsSafeToInit()
    {
        if (IntPtr.Zero != OpenEvent(2, false, eventName)) return true;
        IntPtr kernel32 = LoadLibrary("kernel32.dll");
        UIntPtr getLastError = GetProcAddress(kernel32, "VirtualQueryEx");
        NuiWrapper.NuiSetDeviceStatusCallback(getLastError, IntPtr.Zero);
        return true;
        //return (IntPtr.Zero == OpenEvent(2, false, eventName));
    }

    public static void MarkInited()
    {
        CreateEvent(IntPtr.Zero, false, false, eventName);
    }

    public static void SaveContext<T>(T toSave)
    {
        IntPtr h = CreateFileMapping(INVALID_HANDLE_VALUE, IntPtr.Zero, FileMapProtection.PageReadWrite, 0, (uint)Marshal.SizeOf(typeof(T)), memoryMapName);
        IntPtr buffer = MapViewOfFile(h, FileMapAccess.FileMapWrite, 0, 0, (uint)Marshal.SizeOf(typeof(T)));
        Marshal.StructureToPtr(toSave, buffer, false);
        UnmapViewOfFile(buffer);
        // No closehandle here!
    }

    public static T LoadContext<T>()
    {
        IntPtr h = CreateFileMapping(INVALID_HANDLE_VALUE, IntPtr.Zero, FileMapProtection.PageReadWrite, 0, (uint)Marshal.SizeOf(typeof(T)), memoryMapName);
        IntPtr buffer = MapViewOfFile(h, FileMapAccess.FileMapRead, 0, 0, (uint)Marshal.SizeOf(typeof(T)));
        T result = (T)Marshal.PtrToStructure(buffer, typeof(T));
        UnmapViewOfFile(buffer);
        CloseHandle(h);
        return result;
    }
}

struct NuiContext
{
    public IntPtr DepthHandle;
    public IntPtr ImageHandle;
}

public class NuiWrapper
{
    public static bool BoneOrientationsSupported()
    {
        IntPtr module = PreventDoubleInit.LoadLibrary("kinect10.dll");
        if (module == IntPtr.Zero) return false;
        return UIntPtr.Zero != PreventDoubleInit.GetProcAddress(module, "NuiSkeletonCalculateBoneOrientations");
    }

    public static UIntPtr ConversionSupported()
    {
        IntPtr module = PreventDoubleInit.LoadLibrary("kinect10.dll");

        return PreventDoubleInit.GetProcAddress(module, "NuiTransformDepthImageToSkeletonF");
    }


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
        UsesAudio = 0x10000000,
        UsesDepthAndPlayerIndex = 0x00000001,
        UsesColor = 0x00000002,
        UsesSkeleton = 0x00000008,
        UsesDepth = 0x00000020,
    }

    public enum NuiImageFlag : uint
    {
        NUI_IMAGE_STREAM_FLAG_ENABLE_NEAR_MODE = 0x00020000,
    }
    public enum NuiSkeletonFlag : uint
    {
        NUI_SKELETON_TRACKING_FLAG_SUPPRESS_NO_FRAME_DATA = 0x00000001,
        NUI_SKELETON_TRACKING_FLAG_TITLE_SETS_TRACKED_SKELETONS = 0x00000002,
        NUI_SKELETON_TRACKING_FLAG_ENABLE_SEATED_SUPPORT = 0x00000004,
        NUI_SKELETON_TRACKING_FLAG_ENABLE_IN_NEAR_RANGE = 0x00000008,
    }

    const float FLT_EPSILON = 1.192092896e-07f;

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
    public struct Matrix4AsArray
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        public float[] elems; //Order is 1,1, 1,2, 1,3 1,4 (meaning, column major)
    }
    [StructLayout(LayoutKind.Sequential)]
    public struct Matrix4AsValues
    {
        public float M11;
        public float M12;
        public float M13;
        public float M14;
        public float M21;
        public float M22;
        public float M23;
        public float M24;
        public float M31;
        public float M32;
        public float M33;
        public float M34;
        public float M41;
        public float M42;
        public float M43;
        public float M44;
    }
    [StructLayout(LayoutKind.Explicit)]
    public struct Matrix4
    {
        [FieldOffset(0)]
        public Matrix4AsArray arr;
        [FieldOffset(0)]
        public Matrix4AsValues val;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct NuiSkeletonData
    {
        public NuiSkeletonTrackingState TrackingState;
        public UInt32 TrackingId;
        public UInt32 EnrollmentIndex;
        public UInt32 UserIndex;
        [MarshalAs(UnmanagedType.Struct)]
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
        [MarshalAs(UnmanagedType.Struct)]
        public Vector4 FloorClipPlane;
        [MarshalAs(UnmanagedType.Struct)]
        public Vector4 NormalToGravity;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 6)]
        public NuiSkeletonData[] SkeletonData;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct NuiSkeletonBoneRotation
    {
        public Matrix4 RotationMatrix;
        public Vector4 RotationQuaternion;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct NuiSkeletonBoneOrientation
    {
        public NuiSkeletonPositionIndex EndJoint;
        public NuiSkeletonPositionIndex StartJoint;
        public NuiSkeletonBoneRotation HierarchicalRotation;
        public NuiSkeletonBoneRotation AbsoluteRotation;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct NuiTransformSmoothParameters
    {
        public float Smoothing;
        public float Correction;
        public float Prediction;
        public float JitterRadius;
        public float MaxDeviationRadius;
    }/*     public NuiTransformSmoothParameters(KinectSDKSmoothingParameters para) {
            Smoothing = para.Smoothing;
            Correction = para.Correction;
            Prediction = para.Prediction;
            JitterRadius = para.JitterRadius;
            MaxDeviationRadius = para.MaxDeviationRadius;
        }
        static public NuiTransformSmoothParameters Default {
            get {
                // defaults from docs
                return new NuiTransformSmoothParameters() {
                    Smoothing = 0.5f,
                    Correction = 0.5f,
                    Prediction = 0.5f,
                    JitterRadius = 0.05f,
                    MaxDeviationRadius = 0.04f
                };
            }
        }
    }
    */
    public enum NuiImageDigitalzoom
    {
        x1 = 0,
    }
    [StructLayout(LayoutKind.Sequential)]
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
        public byte padding;
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

        public INuiFrameTexture(IntPtr p)
        {
            this.p = p;

            IntPtr pVtbl = (IntPtr)Marshal.PtrToStructure(p, typeof(IntPtr));
            vtbl = (INuiFrameTextureVtbl)Marshal.PtrToStructure(pVtbl, typeof(INuiFrameTextureVtbl));

            bufferLen = (BufferLenDelegate)Marshal.GetDelegateForFunctionPointer(vtbl.BufferLen, typeof(BufferLenDelegate));
            pitch = (PitchDelegate)Marshal.GetDelegateForFunctionPointer(vtbl.Pitch, typeof(PitchDelegate));
            lockRect = (LockRectDelegate)Marshal.GetDelegateForFunctionPointer(vtbl.LockRect, typeof(LockRectDelegate));
            unlockRect = (UnlockRectDelegate)Marshal.GetDelegateForFunctionPointer(vtbl.UnlockRect, typeof(UnlockRectDelegate));
        }

        public int Pitch
        {
            get
            {
                return pitch(p);
            }
        }

        public int BufferLen
        {
            get
            {
                return bufferLen(p);
            }
        }

        public NuiLockedRect LockRect()
        {
            NuiLockedRect result = new NuiLockedRect();
            lockRect(p, 0, ref result, IntPtr.Zero, 0);
            return result;
        }

        public void UnlockRect()
        {
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

    //[DllImport("kinect10.dll")]
    //public static extern UInt32 NuiImageStreamReleaseFrame(IntPtr Stream, ref NuiImageFrame ImageFrame);

    [DllImport("kinect10.dll")]
    public static extern UInt32 NuiImageStreamReleaseFrame(IntPtr Stream, IntPtr ImageFrame);

    [DllImport("kinect10.dll")]
    public static extern void NuiShutdown();

    //[DllImport("kinect10.dll")]
    //public static extern Vector4 NuiTransformDepthImageToSkeleton(long DepthX, long DepthY, ushort DepthValue);


    const float NUI_CAMERA_DEPTH_NOMINAL_FOCAL_LENGTH_IN_PIXELS = 285.63f;   // Based on 320x240 pixel size.
    const float NUI_CAMERA_DEPTH_NOMINAL_INVERSE_FOCAL_LENGTH_IN_PIXELS = 3.501e-3f; // (1/NUI_CAMERA_DEPTH_NOMINAL_FOCAL_LENGTH_IN_PIXELS)
    const float NUI_CAMERA_DEPTH_NOMINAL_DIAGONAL_FOV = 70.0f;
    const float NUI_CAMERA_DEPTH_NOMINAL_HORIZONTAL_FOV = 58.5f;
    const float NUI_CAMERA_DEPTH_NOMINAL_VERTICAL_FOV = 45.6f;

    // Assuming a pixel resolution of 320x240
    // x_meters = (x_pixelcoord - 160) * NUI_CAMERA_DEPTH_IMAGE_TO_SKELETON_MULTIPLIER_320x240 * z_meters;
    // y_meters = (y_pixelcoord - 120) * NUI_CAMERA_DEPTH_IMAGE_TO_SKELETON_MULTIPLIER_320x240 * z_meters;
    const float NUI_CAMERA_DEPTH_IMAGE_TO_SKELETON_MULTIPLIER_320x240 = NUI_CAMERA_DEPTH_NOMINAL_INVERSE_FOCAL_LENGTH_IN_PIXELS;

    // Assuming a pixel resolution of 320x240
    // x_pixelcoord = (x_meters) * NUI_CAMERA_SKELETON_TO_DEPTH_IMAGE_MULTIPLIER_320x240 / z_meters + 160;
    // y_pixelcoord = (y_meters) * NUI_CAMERA_SKELETON_TO_DEPTH_IMAGE_MULTIPLIER_320x240 / z_meters + 120;
    const float NUI_CAMERA_SKELETON_TO_DEPTH_IMAGE_MULTIPLIER_320x240 = NUI_CAMERA_DEPTH_NOMINAL_FOCAL_LENGTH_IN_PIXELS;

    public static void NuiImageResolutionToSize([In] NuiImageResolution res, out short refWidth, out short refHeight)
    {
        switch (res)
        {
            case NuiImageResolution.Res80x60:
                refWidth = 80;
                refHeight = 60;
                break;
            case NuiImageResolution.Res320x240:
                refWidth = 320;
                refHeight = 240;
                break;
            case NuiImageResolution.Res640x480:
                refWidth = 640;
                refHeight = 480;
                break;
            case NuiImageResolution.Res1280x960:
                refWidth = 1280;
                refHeight = 960;
                break;
            default:
                refWidth = 0;
                refHeight = 0;
                break;
        }
    }
    public static Vector4
    NuiTransformDepthImageToSkeleton(
        [In] long lDepthX,
        [In] long lDepthY,
        [In] ushort usDepthValue,
        [In] NuiImageResolution eResolution
        )
    {
        short width;
        short height;
        NuiImageResolutionToSize(eResolution, out width, out height);

        //
        //  Depth is in meters in skeleton space.
        //  The depth image pixel format has depth in millimeters shifted left by 3.
        //

        float fSkeletonZ = (float)(usDepthValue >> 3) / 1000.0f;

        //
        // Center of depth sensor is at (0,0,0) in skeleton space, and
        // and (width/2,height/2) in depth image coordinates.  Note that positive Y
        // is up in skeleton space and down in image coordinates.
        //

        float fSkeletonX = (lDepthX - width / 2.0f) * (320.0f / width) * NUI_CAMERA_DEPTH_IMAGE_TO_SKELETON_MULTIPLIER_320x240 * fSkeletonZ;
        float fSkeletonY = -(lDepthY - height / 2.0f) * (240.0f / height) * NUI_CAMERA_DEPTH_IMAGE_TO_SKELETON_MULTIPLIER_320x240 * fSkeletonZ;

        //
        // Return the result as a vector.
        //

        Vector4 v4;
        v4.x = fSkeletonX;
        v4.y = fSkeletonY;
        v4.z = fSkeletonZ;
        v4.w = 1.0f;
        return v4;
    }

    /// <summary>
    /// Returns the depth space coordinates of the specified point in skeleton space.
    /// </summary>
    /// <param name="lDepthX">The X coordinate of the depth pixel.</param>
    /// <param name="lDepthY">The Y coordinate of the depth pixel.</param>
    /// <param name="usDepthValue">
    /// The depth value (in millimeters) of the depth image pixel, shifted left by three bits. The left
    /// shift enables you to pass the value from the depth image directly into this function.
    /// </param>
    /// <returns>
    /// Returns the skeleton space coordinates of the given depth image pixel (in meters).
    /// </returns>

    public static Vector4
    NuiTransformDepthImageToSkeleton(
        [In] long lDepthX,
        [In] long lDepthY,
        [In] ushort usDepthValue
        )
    {
        return NuiTransformDepthImageToSkeleton(lDepthX, lDepthY, usDepthValue, NuiImageResolution.Res320x240);
    }







    //    [DllImport("kinect10.dll")]
    //    public static extern void NuiTransformSkeletonToDepthImage([MarshalAs(UnmanagedType.Struct)] Vector4 vPoint, out float pfDepthX, out float pfDepthY);


    public static void NuiTransformSkeletonToDepthImage([In] Vector4 vPoint, out long plDepthX, out long plDepthY, out ushort pusDepthValue, [In] NuiImageResolution eResolution)
    {


        //
        // Requires a valid depth value.
        //

        if (vPoint.z > FLT_EPSILON)
        {
            short width;
            short height;
            NuiImageResolutionToSize(eResolution, out width, out height);

            //
            // Center of depth sensor is at (0,0,0) in skeleton space, and
            // and (width/2,height/2) in depth image coordinates.  Note that positive Y
            // is up in skeleton space and down in image coordinates.
            //
            // The 0.5f is to correct for casting to int truncating, not rounding

            plDepthX = (long)(width / 2 + vPoint.x * (width / 320f) * NUI_CAMERA_SKELETON_TO_DEPTH_IMAGE_MULTIPLIER_320x240 / vPoint.z + 0.5f);
            plDepthY = (long)(height / 2 - vPoint.y * (height / 240f) * NUI_CAMERA_SKELETON_TO_DEPTH_IMAGE_MULTIPLIER_320x240 / vPoint.z + 0.5f);

            //
            //  Depth is in meters in skeleton space.
            //  The depth image pixel format has depth in millimeters shifted left by 3.
            //

            pusDepthValue = (ushort)(((uint)(vPoint.z * 1000)) << 3);
        }
        else
        {
            plDepthX = 0;
            plDepthY = 0;
            pusDepthValue = 0;
        }
    }


    public static void NuiTransformSkeletonToDepthImage(
        [In] Vector4 vPoint,
        out long plDepthX,
        out long plDepthY,
        out ushort pusDepthValue
        )
    {
        NuiTransformSkeletonToDepthImage(vPoint, out plDepthX, out plDepthY, out pusDepthValue, NuiImageResolution.Res320x240);
    }

    public static void NuiTransformSkeletonToDepthImage(
        [In] Vector4 vPoint,
        out float pfDepthX,
        out float pfDepthY,
        [In] NuiImageResolution eResolution
        )
    {
        //
        // Requires a valid depth value.
        //

        if (vPoint.z > FLT_EPSILON)
        {
            short width;
            short height;
            NuiImageResolutionToSize(eResolution, out width, out height);

            //
            // Center of depth sensor is at (0,0,0) in skeleton space, and
            // and (width/2,height/2) in depth image coordinates.  Note that positive Y
            // is up in skeleton space and down in image coordinates.
            //

            pfDepthX = width / 2 + vPoint.x * (width / 320.0f) * NUI_CAMERA_SKELETON_TO_DEPTH_IMAGE_MULTIPLIER_320x240 / vPoint.z;
            pfDepthY = height / 2 - vPoint.y * (height / 240.0f) * NUI_CAMERA_SKELETON_TO_DEPTH_IMAGE_MULTIPLIER_320x240 / vPoint.z;

        }
        else
        {
            pfDepthX = 0.0f;
            pfDepthY = 0.0f;
        }
    }

    public static void NuiTransformSkeletonToDepthImage([In] Vector4 vPoint,
        out float pfDepthX,
        out float pfDepthY
        )
    {
        NuiTransformSkeletonToDepthImage(vPoint, out pfDepthX, out pfDepthY, NuiImageResolution.Res320x240);
    }









    [DllImport("kinect10.dll")]
    public static extern void NuiSetDeviceStatusCallback(UIntPtr funcPtr, IntPtr data);

    [DllImport("kinect10.dll")]
    public static extern UInt32 NuiTransformSmooth([In, Out]  NuiSkeletonFrame SkeletonFrame, [In] NuiTransformSmoothParameters SmoothingParams);

    [DllImport("kinect10.dll")]
    public static extern UInt32 NuiCameraElevationSetAngle(long lAngleDegrees);

    [DllImport("kinect10.dll")]
    public static extern UInt32 NuiCameraElevationGetAngle(out long plAngleDegrees);


    [DllImport("kinect10.dll")]
    public static extern UInt32 NuiSkeletonTrackingEnable(IntPtr nextSkeletonEvent, UInt32 flags);

    [DllImport("kinect10.dll")]
    public static extern UInt32 NuiImageStreamSetImageFrameFlags(IntPtr hStream, UInt32 flags);

    [DllImport("kinect10.dll")]
    public static extern UInt32 NuiImageStreamGetImageFrameFlags(IntPtr hStream, out UInt32 flags);

    // KinectSDK 1.5+
    //[DllImport("kinect10.dll")]
    //TODO
    //public static extern UInt32 NuiSkeletonCalculateBoneOrientations([In] NuiSkeletonData skeleton, [Out] NuiSkeletonBoneOrientation[] orientations);

    [DllImport("kinect10.dll")]
    public static extern UInt32 NuiImageGetColorPixelCoordinatesFromDepthPixel(NuiImageResolution Resolution, ref NuiImageViewArea ViewArea, int DepthX, int DepthY, ushort DepthValue, out int ColorX, out int ColorY);
}

class ZigInputKinectSDK : IZigInputReader
{
    static short PLAYER_MASK = 0x00000007;
    NuiWrapper.NuiSkeletonFrame skeletonFrame = new NuiWrapper.NuiSkeletonFrame();
    NuiWrapper.NuiImageFrame depthFrame;
    NuiWrapper.NuiImageFrame imageFrame;
    NuiWrapper.NuiTransformSmoothParameters smoothParameters;
    NuiContext context;
    bool SDKOrientations;
    bool useSmoothing = false;
    public short[] registrationBuffer;
    //-------------------------------------------------------------------------
    // IZigInputReader interface
    //-------------------------------------------------------------------------

    public void SetNearMode(bool NearMode)
    {
        NuiWrapper.NuiImageStreamSetImageFrameFlags(context.DepthHandle, NearMode ? (uint)NuiWrapper.NuiImageFlag.NUI_IMAGE_STREAM_FLAG_ENABLE_NEAR_MODE : 0);
    }

    public void SetSkeletonTrackingSettings(bool SeatedMode, bool TrackSkeletonInNearMode)
    {
        IntPtr throwawayEvent = PreventDoubleInit.CreateEvent(IntPtr.Zero, true, false, null);
        uint flags = SeatedMode ? (uint)NuiWrapper.NuiSkeletonFlag.NUI_SKELETON_TRACKING_FLAG_ENABLE_SEATED_SUPPORT : 0;
        flags |= TrackSkeletonInNearMode ? (uint)NuiWrapper.NuiSkeletonFlag.NUI_SKELETON_TRACKING_FLAG_ENABLE_IN_NEAR_RANGE : 0;
        NuiWrapper.NuiSkeletonTrackingEnable(throwawayEvent, flags);
    }

    public void Init(ZigInputSettings settings)
    {
        UpdateDepth = settings.UpdateDepth;
        UpdateImage = settings.UpdateImage;
        UpdateLabelMap = settings.UpdateLabelMap;
        AlignDepthToRGB = settings.AlignDepthToRGB;
        smoothParameters = new NuiWrapper.NuiTransformSmoothParameters(); //(settings.KinectSDKSpecific.SmoothingParameters);
        smoothParameters.Correction = settings.KinectSDKSpecific.SmoothingParameters.Correction;
        smoothParameters.JitterRadius = settings.KinectSDKSpecific.SmoothingParameters.JitterRadius;
        smoothParameters.MaxDeviationRadius = settings.KinectSDKSpecific.SmoothingParameters.MaxDeviationRadius;
        smoothParameters.Prediction = settings.KinectSDKSpecific.SmoothingParameters.Prediction;
        smoothParameters.Smoothing = settings.KinectSDKSpecific.SmoothingParameters.Smoothing;

        useSmoothing = settings.KinectSDKSpecific.UseSDKSmoothing;

        context = new NuiContext();

        UInt32 flags =
            (uint)NuiWrapper.NuiInitializeFlag.UsesDepthAndPlayerIndex |
            (uint)NuiWrapper.NuiInitializeFlag.UsesSkeleton |
            (uint)NuiWrapper.NuiInitializeFlag.UsesColor;

        if (PreventDoubleInit.IsSafeToInit())
        {

            UInt32 hr = NuiWrapper.NuiInitialize(flags);
            if (0 != hr)
            {
                NuiWrapper.NuiShutdown(); // just in case
                throw new Exception("Error initing Kinect SDK: " + hr);
            }

            context = new NuiContext();

            //without giving a valid event here, re-opening the sensor doesn't work properly
            IntPtr throwawayEvent = PreventDoubleInit.CreateEvent(IntPtr.Zero, true, false, null);

            //hr = NuiWrapper.NuiImageStreamOpen(NuiWrapper.NuiImageType.DepthAndPlayerIndex, NuiWrapper.NuiImageResolution.Res320x240, 0, 2, IntPtr.Zero, out context.DepthHandle);
            flags = settings.KinectSDKSpecific.NearMode ? (uint)NuiWrapper.NuiImageFlag.NUI_IMAGE_STREAM_FLAG_ENABLE_NEAR_MODE : 0;
            hr = NuiWrapper.NuiImageStreamOpen(NuiWrapper.NuiImageType.DepthAndPlayerIndex, NuiWrapper.NuiImageResolution.Res320x240, flags, 2, throwawayEvent, out context.DepthHandle);
            if (0 != hr)
            {
                NuiWrapper.NuiShutdown(); // just in case
                throw new Exception("Error opening depth stream: " + hr);
            }

            //without giving a valid event here, re-opening the sensor doesn't work properly
            throwawayEvent = PreventDoubleInit.CreateEvent(IntPtr.Zero, true, false, null);
            //hr = NuiWrapper.NuiImageStreamOpen(NuiWrapper.NuiImageType.Color, NuiWrapper.NuiImageResolution.Res640x480, 0, 2, IntPtr.Zero, out context.ImageHandle);

            hr = NuiWrapper.NuiImageStreamOpen(NuiWrapper.NuiImageType.Color, NuiWrapper.NuiImageResolution.Res640x480, 0, 2, throwawayEvent, out context.ImageHandle);
            if (0 != hr)
            {
                NuiWrapper.NuiShutdown(); // just in case
                throw new Exception("Error opening image stream: " + hr);
            }

            throwawayEvent = PreventDoubleInit.CreateEvent(IntPtr.Zero, true, false, null);
            flags = settings.KinectSDKSpecific.SeatedMode ? (uint)NuiWrapper.NuiSkeletonFlag.NUI_SKELETON_TRACKING_FLAG_ENABLE_SEATED_SUPPORT : 0;
            flags |= settings.KinectSDKSpecific.TrackSkeletonInNearMode ? (uint)NuiWrapper.NuiSkeletonFlag.NUI_SKELETON_TRACKING_FLAG_ENABLE_IN_NEAR_RANGE : 0;
            NuiWrapper.NuiSkeletonTrackingEnable(throwawayEvent, flags);
            //PreventDoubleInit.SaveContext<NuiContext>(context);
            PreventDoubleInit.MarkInited();
        }
        else
        {
            context = PreventDoubleInit.LoadContext<NuiContext>();
        }
        SDKOrientations = NuiWrapper.BoneOrientationsSupported();
        //if (SDKOrientations) {//TODO: remove
        //    UnityEngine.Debug.Log("MS SDK >= 1.5 - supports bone orientations");
        //}
        //else {
        //    UnityEngine.Debug.Log("MS SDK 1.0 - generating bone orientations internally");
        //}
        Image = new ZigImage(640, 480);
        Depth = new ZigDepth(320, 240);
        LabelMap = new ZigLabelMap(320, 240);

        registrationBuffer = new short[320 * 240];
    }

    NuiWrapper.NuiImageResolution lastResolution;
    NuiWrapper.NuiImageViewArea lastViewArea;

    public void Update()
    {
        if (0 == NuiWrapper.NuiSkeletonGetNextFrame(0, ref skeletonFrame))
        {
            ProcessNewSkeletonFrame();
        }

        if (UpdateImage)
        {
            IntPtr pImageFrame;
            if (0 == NuiWrapper.NuiImageStreamGetNextFrame(context.ImageHandle, 0, out pImageFrame))
            {
                imageFrame = (NuiWrapper.NuiImageFrame)Marshal.PtrToStructure(pImageFrame, typeof(NuiWrapper.NuiImageFrame));
                NuiWrapper.INuiFrameTexture imageTexture = new NuiWrapper.INuiFrameTexture(imageFrame.FrameTexture);

                NuiWrapper.NuiLockedRect rect = imageTexture.LockRect();
                NuiWrapper.ColorBuffer colors = (NuiWrapper.ColorBuffer)Marshal.PtrToStructure(rect.ActualDataFinally, typeof(NuiWrapper.ColorBuffer));
                for (int i = 0; i < Image.data.Length; i++)
                {
                    Image.data[i].r = colors.data[i].r;
                    Image.data[i].g = colors.data[i].g;
                    Image.data[i].b = colors.data[i].b;
                    Image.data[i].a = 255;
                }
                imageTexture.UnlockRect();

                lastResolution = imageFrame.Resolution;
                lastViewArea = imageFrame.ViewArea;

                NuiWrapper.NuiImageStreamReleaseFrame(context.ImageHandle, pImageFrame);
            }
        }

        if (UpdateDepth)
        {
            IntPtr pDepthFrame;
            if (0 == NuiWrapper.NuiImageStreamGetNextFrame(context.DepthHandle, 0, out pDepthFrame))
            {

                // deal with deref'ing/marshalling
                depthFrame = (NuiWrapper.NuiImageFrame)Marshal.PtrToStructure(pDepthFrame, typeof(NuiWrapper.NuiImageFrame));
                NuiWrapper.INuiFrameTexture depthTexture = new NuiWrapper.INuiFrameTexture(depthFrame.FrameTexture);

                // lock & copy the depth data
                NuiWrapper.NuiLockedRect rect = depthTexture.LockRect();
                Marshal.Copy(rect.ActualDataFinally, registrationBuffer, 0, registrationBuffer.Length);
                depthTexture.UnlockRect();

                // Depth->RGB registration
                if (AlignDepthToRGB)
                {
                    int colorX;
                    int colorY;
                    Array.Clear(Depth.data, 0, Depth.xres * Depth.yres);
                    for (int depthY = 0, i = 0; depthY < Depth.yres; depthY++)
                    {
                        for (int depthX = 0; depthX < Depth.xres; depthX++, i++)
                        {
                            ushort d = (ushort)(registrationBuffer[i] & ~PLAYER_MASK);
                            NuiWrapper.NuiImageGetColorPixelCoordinatesFromDepthPixel(NuiWrapper.NuiImageResolution.Res320x240, ref lastViewArea, depthX, depthY, d, out colorX, out colorY);
                            if (colorX >= 0 && colorY >= 0 && colorX < Depth.xres && colorY < Depth.yres)
                            {
                                Depth.data[colorY * Depth.xres + colorX] = (short)(d >> 3);
                                if (UpdateLabelMap)
                                {
                                    LabelMap.data[colorY * Depth.xres + colorX] = (short)(registrationBuffer[i] & PLAYER_MASK);
                                }
                            }
                        }
                    }
                }
                else
                {
                    if (UpdateLabelMap)
                    {
                        for (int i = 0; i < registrationBuffer.Length; i++)
                        {
                            short d = registrationBuffer[i];
                            LabelMap.data[i] = (short)(d & PLAYER_MASK);
                            Depth.data[i] = (short)(d >> 3);
                        }
                    }
                    else
                    {
                        for (int i = 0; i < registrationBuffer.Length; i++)
                        {
                            short d = registrationBuffer[i];
                            Depth.data[i] = (short)(d >> 3);
                        }
                    }
                }

                // release current frame
                NuiWrapper.NuiImageStreamReleaseFrame(context.DepthHandle, pDepthFrame);
            }
        }
    }

    public void Shutdown()
    {
        NuiWrapper.NuiShutdown();
    }


    public event EventHandler<NewUsersFrameEventArgs> NewUsersFrame;
    protected void OnNewUsersFrame(List<ZigInputUser> users)
    {
        if (null != NewUsersFrame)
        {
            NewUsersFrame.Invoke(this, new NewUsersFrameEventArgs(users));
        }
    }

    public ZigDepth Depth { get; private set; }
    public ZigImage Image { get; private set; }
    public ZigLabelMap LabelMap { get; private set; }
    public bool UpdateDepth { get; set; }
    public bool UpdateImage { get; set; }
    public bool UpdateLabelMap { get; set; }

    public bool AlignDepthToRGB { get; set; } //TODO: support

    public Vector3 ConvertWorldToImageSpace(Vector3 worldPosition)
    {
        NuiWrapper.Vector4 pt = new NuiWrapper.Vector4()
        {
            x = worldPosition.x / 1000, // Kinect SDK is in meters, we're in millimeters
            y = worldPosition.y / 1000,
            z = worldPosition.z / 1000,
            w = 1 // this is a homogenous space
        };
        Vector3 result = new Vector3();
        result.z = worldPosition.z;
        NuiWrapper.NuiTransformSkeletonToDepthImage(pt, out result.x, out result.y);
        return result;
    }
    public Vector3 ConvertImageToWorldSpace(Vector3 imagePosition)
    {
        ushort unshiftedZ = (ushort)((ushort)imagePosition.z << 3); //need to unshift depth data for KinectSDK
        NuiWrapper.Vector4 pt = NuiWrapper.NuiTransformDepthImageToSkeleton((long)imagePosition.x, (long)imagePosition.y, unshiftedZ);
        return new Vector3(pt.x * 1000, // Kinect SDK is in meters, we're in millimeters
                           pt.y * 1000,
                           imagePosition.z);
    }

    //-------------------------------------------------------------------------
    // Internal stuff
    //-------------------------------------------------------------------------

    Vector3 Vector4ToVector3(NuiWrapper.Vector4 pos)
    {
        return new Vector3(pos.x * 1000, pos.y * 1000, pos.z * -1000);
    }

    ZigJointId NuiToZig(NuiWrapper.NuiSkeletonPositionIndex nuiJoint)
    {
        switch (nuiJoint)
        {
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

    void ProcessNewSkeletonFrame()
    {
        NuiWrapper.NuiSkeletonData skel;
        NuiWrapper.NuiSkeletonBoneOrientation[] orientations = new NuiWrapper.NuiSkeletonBoneOrientation[20]; //TODO: change from hard-coded?
        // foreach user
        List<ZigInputUser> users = new List<ZigInputUser>();
        NuiWrapper.NuiSkeletonFrame skelFrame;
        skelFrame = skeletonFrame;
        if (useSmoothing)
        {
            //TODO Make functional

            //UnityEngine.Debug.Log(string.Format("Smoothing: {0} Correction: {1} Prediction: {2} JitterRadius: {3} MaxDeviationRadius: {4}", smoothParameters.Smoothing, smoothParameters.Correction, smoothParameters.Prediction, smoothParameters.JitterRadius, smoothParameters.MaxDeviationRadius));
            //UnityEngine.Debug.Log("Smoothing not functional");                        
            //NuiWrapper.NuiTransformSmooth(skelFrame, smoothParameters);
            //AUnityEngine.Debug.Log("Did we crash yet?");
        }
        foreach (var skeleton in skelFrame.SkeletonData)
        {

            if (skeleton.TrackingState == NuiWrapper.NuiSkeletonTrackingState.NotTracked)
            {
                continue;
            }

            // skeleton data
            List<ZigInputJoint> joints = new List<ZigInputJoint>();
            bool tracked = skeleton.TrackingState == NuiWrapper.NuiSkeletonTrackingState.Tracked;
            if (tracked)
            {
                // we need this if we want to use the skeleton as a ref arg
                skel = skeleton;

                //TOOD: fix below
                //if (SDKOrientations) {
                //    NuiWrapper.NuiSkeletonCalculateBoneOrientations(ref skel, orientations);
                //}

                foreach (NuiWrapper.NuiSkeletonPositionIndex j in Enum.GetValues(typeof(NuiWrapper.NuiSkeletonPositionIndex)))
                {
                    // skip joints that aren't tracked
                    if (skeleton.SkeletonPositionTrackingState[(int)j] == NuiWrapper.NuiSkeletonPositionTrackingState.NotTracked)
                    {
                        continue;
                    }
                    ZigInputJoint joint = new ZigInputJoint(NuiToZig(j));
                    joint.Inferred = skeleton.SkeletonPositionTrackingState[(int)j] == NuiWrapper.NuiSkeletonPositionTrackingState.Inferred;
                    joint.Position = Vector4ToVector3(skeleton.SkeletonPositions[(int)j]);
                    //if (SDKOrientations) {
                    //TODO: make this work - it looks "okay" on a blockman and sucks on an avatar
                    //NuiWrapper.Vector4 rot = orientations[(int)j].AbsoluteRotation.RotationQuaternion;
                    //joint.Rotation.x = rot.x;
                    //joint.Rotation.y = rot.y;
                    //joint.Rotation.z = -rot.z;
                    //joint.Rotation.w = -rot.w;
                    //UnityEngine.Debug.Log(string.Format("Joint: {0}, bone: {1}->{2}", j, orientations[(int)j].StartJoint,orientations[(int)j].EndJoint));
                    //NuiWrapper.Matrix4 rot2 = orientations[(int)j].AbsoluteRotation.RotationMatrix;
                    //UnityEngine.Debug.Log(new Vector3(rot2.val.M13, rot2.val.M23, -rot2.val.M33));
                    //UnityEngine.Debug.Log(joint.Rotation);
                    //UnityEngine.Debug.Log(getJointOrientation(ref skel, j)*Vector3.forward);
                    //NuiWrapper.Matrix4 rot = orientations[(int)j].AbsoluteRotation.RotationMatrix;

                    //joint.Rotation = Quaternion.LookRotation(new Vector3(rot.val.M13, rot.val.M23, -rot.val.M33),
                    //                                         new Vector3(rot.val.M12, rot.val.M22, -rot.val.M32));
                    //}
                    //else {
                    joint.Rotation = getJointOrientation(ref skel, j);
                    //}
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
            if (worldZ == Vector3.zero)
            {
                return Quaternion.identity;
            }
            return Quaternion.LookRotation(worldZ, worldY);
        }
    }


    private Vector3 vectorBetweenNuiJoints(ref NuiWrapper.NuiSkeletonData skeleton,
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
        result.col0 = (new Vector3(v.y, -v.x, 0.0f)).normalized;
        result.col1 = v.normalized;
        result.col2 = RCross(result.col0, result.col1);
        return result;
    }

    private Matrix3x3 orientationFromZ(Vector3 v)
    {
        Matrix3x3 result = new Matrix3x3();
        result.col0 = (new Vector3(v.y, -v.x, 0.0f)).normalized;
        result.col2 = v.normalized;
        result.col1 = RCross(result.col2, result.col0);
        return result;
    }

    private Matrix3x3 orientationFromXY(Vector3 vx, Vector3 vy)
    {
        Matrix3x3 result = new Matrix3x3();
        result.col0 = vx.normalized;
        result.col2 = RCross(result.col0, vy.normalized).normalized;
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
                    vectorBetweenNuiJoints(ref skeleton, NuiWrapper.NuiSkeletonPositionIndex.HipLeft, NuiWrapper.NuiSkeletonPositionIndex.HipRight),
                    vectorBetweenNuiJoints(ref skeleton, NuiWrapper.NuiSkeletonPositionIndex.HipCenter, NuiWrapper.NuiSkeletonPositionIndex.Spine));
                break;

            case NuiWrapper.NuiSkeletonPositionIndex.Spine:
                result = orientationFromYX(
                    vectorBetweenNuiJoints(ref skeleton, NuiWrapper.NuiSkeletonPositionIndex.ShoulderLeft, NuiWrapper.NuiSkeletonPositionIndex.ShoulderRight),
                    vectorBetweenNuiJoints(ref skeleton, NuiWrapper.NuiSkeletonPositionIndex.Spine, NuiWrapper.NuiSkeletonPositionIndex.ShoulderCenter));
                break;

            case NuiWrapper.NuiSkeletonPositionIndex.ShoulderCenter:
                result = orientationFromYX(
                    vectorBetweenNuiJoints(ref skeleton, NuiWrapper.NuiSkeletonPositionIndex.ShoulderLeft, NuiWrapper.NuiSkeletonPositionIndex.ShoulderRight),
                    vectorBetweenNuiJoints(ref skeleton, NuiWrapper.NuiSkeletonPositionIndex.ShoulderCenter, NuiWrapper.NuiSkeletonPositionIndex.Head));
                break;

            case NuiWrapper.NuiSkeletonPositionIndex.Head:
                result = orientationFromY(
                    vectorBetweenNuiJoints(ref skeleton, NuiWrapper.NuiSkeletonPositionIndex.ShoulderCenter, NuiWrapper.NuiSkeletonPositionIndex.Head));
                break;

            case NuiWrapper.NuiSkeletonPositionIndex.ShoulderLeft:
                result = orientationFromXY(
                    -vectorBetweenNuiJoints(ref skeleton, NuiWrapper.NuiSkeletonPositionIndex.ShoulderLeft, NuiWrapper.NuiSkeletonPositionIndex.ElbowLeft),
                    vectorBetweenNuiJoints(ref skeleton, NuiWrapper.NuiSkeletonPositionIndex.ElbowLeft, NuiWrapper.NuiSkeletonPositionIndex.WristLeft));
                break;

            case NuiWrapper.NuiSkeletonPositionIndex.ElbowLeft:
                result = orientationFromXY(
                    -vectorBetweenNuiJoints(ref skeleton, NuiWrapper.NuiSkeletonPositionIndex.ElbowLeft, NuiWrapper.NuiSkeletonPositionIndex.WristLeft),
                    -vectorBetweenNuiJoints(ref skeleton, NuiWrapper.NuiSkeletonPositionIndex.ShoulderLeft, NuiWrapper.NuiSkeletonPositionIndex.ElbowLeft));
                break;

            case NuiWrapper.NuiSkeletonPositionIndex.WristLeft:
                result = orientationFromX(
                    -vectorBetweenNuiJoints(ref skeleton, NuiWrapper.NuiSkeletonPositionIndex.WristLeft, NuiWrapper.NuiSkeletonPositionIndex.HandLeft));
                break;

            case NuiWrapper.NuiSkeletonPositionIndex.HandLeft:
                result = orientationFromX(
                    -vectorBetweenNuiJoints(ref skeleton, NuiWrapper.NuiSkeletonPositionIndex.WristLeft, NuiWrapper.NuiSkeletonPositionIndex.HandLeft));
                break;

            case NuiWrapper.NuiSkeletonPositionIndex.HipLeft:
                result = orientationFromYX(
                    vectorBetweenNuiJoints(ref skeleton, NuiWrapper.NuiSkeletonPositionIndex.HipLeft, NuiWrapper.NuiSkeletonPositionIndex.HipRight),
                    vectorBetweenNuiJoints(ref skeleton, NuiWrapper.NuiSkeletonPositionIndex.KneeLeft, NuiWrapper.NuiSkeletonPositionIndex.HipLeft));
                break;

            case NuiWrapper.NuiSkeletonPositionIndex.KneeLeft:
                result = orientationFromY(
                    -vectorBetweenNuiJoints(ref skeleton, NuiWrapper.NuiSkeletonPositionIndex.KneeLeft, NuiWrapper.NuiSkeletonPositionIndex.AnkleLeft));
                break;

            case NuiWrapper.NuiSkeletonPositionIndex.AnkleLeft:
                result = orientationFromZ(
                    vectorBetweenNuiJoints(ref skeleton, NuiWrapper.NuiSkeletonPositionIndex.FootLeft, NuiWrapper.NuiSkeletonPositionIndex.AnkleLeft));
                break;

            case NuiWrapper.NuiSkeletonPositionIndex.FootLeft:
                result = orientationFromZ(
                    vectorBetweenNuiJoints(ref skeleton, NuiWrapper.NuiSkeletonPositionIndex.FootLeft, NuiWrapper.NuiSkeletonPositionIndex.AnkleLeft));
                break;


            case NuiWrapper.NuiSkeletonPositionIndex.ShoulderRight:
                result = orientationFromXY(
                    vectorBetweenNuiJoints(ref skeleton, NuiWrapper.NuiSkeletonPositionIndex.ShoulderRight, NuiWrapper.NuiSkeletonPositionIndex.ElbowRight),
                    vectorBetweenNuiJoints(ref skeleton, NuiWrapper.NuiSkeletonPositionIndex.ElbowRight, NuiWrapper.NuiSkeletonPositionIndex.WristRight));
                break;

            case NuiWrapper.NuiSkeletonPositionIndex.ElbowRight:
                result = orientationFromXY(
                    vectorBetweenNuiJoints(ref skeleton, NuiWrapper.NuiSkeletonPositionIndex.ElbowRight, NuiWrapper.NuiSkeletonPositionIndex.WristRight),
                    -vectorBetweenNuiJoints(ref skeleton, NuiWrapper.NuiSkeletonPositionIndex.ShoulderRight, NuiWrapper.NuiSkeletonPositionIndex.ElbowRight));
                break;

            case NuiWrapper.NuiSkeletonPositionIndex.WristRight:
                result = orientationFromX(
                    vectorBetweenNuiJoints(ref skeleton, NuiWrapper.NuiSkeletonPositionIndex.WristRight, NuiWrapper.NuiSkeletonPositionIndex.HandRight));
                break;

            case NuiWrapper.NuiSkeletonPositionIndex.HandRight:
                result = orientationFromX(
                    vectorBetweenNuiJoints(ref skeleton, NuiWrapper.NuiSkeletonPositionIndex.WristRight, NuiWrapper.NuiSkeletonPositionIndex.HandRight));
                break;

            case NuiWrapper.NuiSkeletonPositionIndex.HipRight:
                result = orientationFromYX(
                    vectorBetweenNuiJoints(ref skeleton, NuiWrapper.NuiSkeletonPositionIndex.HipLeft, NuiWrapper.NuiSkeletonPositionIndex.HipRight),
                    vectorBetweenNuiJoints(ref skeleton, NuiWrapper.NuiSkeletonPositionIndex.KneeRight, NuiWrapper.NuiSkeletonPositionIndex.HipRight));
                break;

            case NuiWrapper.NuiSkeletonPositionIndex.KneeRight:
                result = orientationFromYZ(
                    -vectorBetweenNuiJoints(ref skeleton, NuiWrapper.NuiSkeletonPositionIndex.KneeRight, NuiWrapper.NuiSkeletonPositionIndex.AnkleRight),
                    -vectorBetweenNuiJoints(ref skeleton, NuiWrapper.NuiSkeletonPositionIndex.AnkleRight, NuiWrapper.NuiSkeletonPositionIndex.FootRight));
                break;

            case NuiWrapper.NuiSkeletonPositionIndex.AnkleRight:
                result = orientationFromZ(
                    vectorBetweenNuiJoints(ref skeleton, NuiWrapper.NuiSkeletonPositionIndex.FootRight, NuiWrapper.NuiSkeletonPositionIndex.AnkleRight));
                break;

            case NuiWrapper.NuiSkeletonPositionIndex.FootRight:
                result = orientationFromZ(
                    vectorBetweenNuiJoints(ref skeleton, NuiWrapper.NuiSkeletonPositionIndex.FootRight, NuiWrapper.NuiSkeletonPositionIndex.AnkleRight));
                break;
        }

        return result.ToQuaternion();
    }
}

