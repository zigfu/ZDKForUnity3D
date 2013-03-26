


using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace OpenNI2
{
    using OniStreamHandle = System.IntPtr;
    using OniDeviceHandle = System.IntPtr;
    using OniRecorderHandle = System.IntPtr;
    using OniDepthPixel = System.UInt16;
    using NiteUserTrackerHandle = System.IntPtr;
    using NiteHandTrackerHandle = System.IntPtr;
    using NiteUserId = System.Int16;
    using NiteHandId = System.Int16;
    public class OpenNI2Wrapper 
    {

        public static int ONI_MAX_STR = 256;
        [Flags]
        public enum OniStatus : uint
        {

            ONI_STATUS_OK = 0,
            ONI_STATUS_ERROR = 1,
            ONI_STATUS_NOT_IMPLEMENTED = 2,
            ONI_STATUS_NOT_SUPPORTED = 3,
            ONI_STATUS_BAD_PARAMETER = 4,
            ONI_STATUS_OUT_OF_FLOW = 5,
            ONI_STATUS_NO_DEVICE = 6,
            ONI_STATUS_TIME_OUT = 102,
        }
        public enum OniSensorType : uint            
        {
	        ONI_SENSOR_IR = 1,
	        ONI_SENSOR_COLOR = 2,
	        ONI_SENSOR_DEPTH = 3,
        }

    public enum OniPixelFormat : uint
    {
	    // Depth
	    ONI_PIXEL_FORMAT_DEPTH_1_MM = 100,
	    ONI_PIXEL_FORMAT_DEPTH_100_UM = 101,
	    ONI_PIXEL_FORMAT_SHIFT_9_2 = 102,
	    ONI_PIXEL_FORMAT_SHIFT_9_3 = 103,

	    // Color
	    ONI_PIXEL_FORMAT_RGB888 = 200,
	    ONI_PIXEL_FORMAT_YUV422 = 201,
	    ONI_PIXEL_FORMAT_GRAY8 = 202,
	    ONI_PIXEL_FORMAT_GRAY16 = 203,
	    ONI_PIXEL_FORMAT_JPEG = 204,
    } 

    public enum OniDeviceState : uint
    {
	    ONI_DEVICE_STATE_OK 		= 0,
	    ONI_DEVICE_STATE_ERROR		= 1,
	    ONI_DEVICE_STATE_NOT_READY 	= 2,
	    ONI_DEVICE_STATE_EOF 		= 3
    }

        
    public enum OniImageRegistrationMode : uint {
	    ONI_IMAGE_REGISTRATION_OFF				= 0,
	    ONI_IMAGE_REGISTRATION_DEPTH_TO_COLOR	= 1,
    }

    public struct OniVideoMode
    {
	    OniPixelFormat pixelFormat;
	    int resolutionX;
	    int resolutionY;
	    int fps;
    } 

    [StructLayout(LayoutKind.Sequential)]
    public struct OniSensorInfo
    {
    	OniSensorType sensorType;
    	int numSupportedVideoModes;        
        IntPtr pSupportedVideoModes; //OniVideoMode* pSupportedVideoModes;
    } 

    [StructLayout(LayoutKind.Sequential)]
    public struct OniDeviceInfo
    {        
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 256)]
	    public char [] uri;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 256)]
	    public char [] vendor;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 256)]
	    public char [] name;
	    UInt16 usbVendorId;
	    UInt16 usbProductId;
    }


    [StructLayout(LayoutKind.Sequential)] 
    public struct OniVersion
    {		      																			
																								
	    /** Major version number, incremented for major API restructuring. */						
	    public int major;																					
	    /** Minor version number, incremented when signficant new features added. */
        public int minor;																					
	    /** Mainenance build number, incremented for new releases that primarily provide minor bug fixes. */
        public int maintenance;																			
	    /** Build number. Incremented for each new API build. Generally not shown on the installer and download site. */
        public int build;																					
    }

        
    [StructLayout(LayoutKind.Sequential)] 
    public struct OniFrame  
    {
	    public int dataSize;
	    public IntPtr data;

        public OniSensorType sensorType;
        public UInt64 timestamp;
        public int frameIndex;

        public int width;
        public int height;

        public OniVideoMode videoMode;
        public bool croppingEnabled;
        public int cropOriginX;
        public int cropOriginY;

        public int stride;
    }

    public delegate void OniNewFrameCallback(IntPtr stream, IntPtr pCookie);
    public delegate void OniGeneralCallback(IntPtr pCookie);
    public delegate void OniDeviceInfoCallback(ref OniDeviceInfo info, IntPtr pCookie);
    public delegate void OniDeviceStateCallback(ref OniDeviceInfo info, OniDeviceState deviceState, IntPtr pCookie);

    //[MarshalAs(UnmanagedType.FunctionPtr)]
    //public OniDeviceInfoCallback		deviceConnected;
    //[MarshalAs(UnmanagedType.FunctionPtr)]
    //public OniDeviceInfoCallback		deviceDisconnected;
    //[MarshalAs(UnmanagedType.FunctionPtr)]
    //public OniDeviceStateCallback		deviceStateChanged;

    [StructLayout(LayoutKind.Sequential)]
    public struct OniFloatPoint3D
    {
        public float x;
        public float y;
        public float z;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct OniDeviceCallbacks
    {
        public IntPtr deviceConnected;
        public IntPtr deviceDisconnected;
        public IntPtr deviceStateChanged;
    } 
//Bindings to OpenNI2 based on OniCAPI.h

        /**  Initialize OpenNI2. Use ONI_API_VERSION as the version. */
        //ONI_C_API OniStatus oniInitialize(int apiVersion);
        [DllImport("OpenNI2.dll")]
        public static extern OniStatus oniInitialize(int apiVersion);
        
        /**  Shutdown OpenNI2 */
        //ONI_C_API void oniShutdown();
        [DllImport("OpenNI2.dll")]
        public static extern void oniShutdown();
   
         /**
         * Get the list of currently connected device.
         * Each device is represented by its OniDeviceInfo.
         * pDevices will be allocated inside.
         */
        //ONI_C_API OniStatus oniGetDeviceList(OniDeviceInfo** pDevices, int* pNumDevices);
        [DllImport("OpenNI2.dll")]
        public static extern OniStatus oniGetDeviceList(ref IntPtr pDevices, ref int pNumDevices);


        /** Release previously allocated device list */
        //ONI_C_API OniStatus oniReleaseDeviceList(OniDeviceInfo* pDevices);
        [DllImport("OpenNI2.dll")]
        public static extern OniStatus oniReleaseDeviceList(IntPtr pDevices);

        
        
        //ONI_C_API OniStatus oniRegisterDeviceCallbacks(OniDeviceCallbacks* pCallbacks, void* pCookie, OniCallbackHandle* pHandle);
        [DllImport("OpenNI2.dll")]
        public static extern OniStatus oniRegisterDeviceCallbacks(IntPtr callbacks, IntPtr pCookie, out IntPtr pHandle);

        //ONI_C_API void oniUnregisterDeviceCallbacks(OniCallbackHandle handle);
        [DllImport("OpenNI2.dll")]
        public static extern void oniUnregisterDeviceCallbacks(IntPtr pHandle);

        ///** Wait for any of the streams to have a new frame */
        //ONI_C_API OniStatus oniWaitForAnyStream(OniStreamHandle* pStreams, int numStreams, int* pStreamIndex, int timeout);
        [DllImport("OpenNI2.dll")]
        public static extern OniStatus oniWaitForAnyStream(ref IntPtr pHandle, int numStreams, out int pStreamIndex, int timeout); //TODO properly handle 1st arg stream array

        ///** Get the current version of OpenNI2 */
        //ONI_C_API OniVersion oniGetVersion();
        [DllImport("OpenNI2.dll")]
        public static extern OniVersion oniGetVersion();

        ///** Translate from format to number of bytes per pixel. Will return 0 for formats in which the number of bytes per pixel isn't fixed. */
        //ONI_C_API int oniFormatBytesPerPixel(OniPixelFormat format);
        [DllImport("OpenNI2.dll")]
        public static extern int oniFormatBytesPerPixel(OniPixelFormat format);

        ///** Get internal error */
        //ONI_C_API const char* oniGetExtendedError();
        [DllImport("OpenNI2.dll")]
        [return : MarshalAs(UnmanagedType.LPStr)]
        public static extern string oniGetExtendedError();

        /** Open a device. Uri can be taken from the matching OniDeviceInfo. */
        //ONI_C_API OniStatus oniDeviceOpen(const char* uri, OniDeviceHandle* pDevice);
        [DllImport("OpenNI2.dll")]
        public static extern OniStatus oniDeviceOpen( string uri, ref OniDeviceHandle pDevice);

        /** Close a device */
        [DllImport("OpenNI2.dll")]
        public static extern OniStatus oniDeviceClose(OniDeviceHandle device);

        /** Get the possible configurations available for a specific source, or NULL if the source does not exist. */
        //ONI_C_API const OniSensorInfo* oniDeviceGetSensorInfo(OniDeviceHandle device, OniSensorType sensorType);
        [DllImport("OpenNI2.dll")]
        public static extern IntPtr oniDeviceGetSensorInfo(OniDeviceHandle device, OniSensorType sensorType);

        ///** Get the OniDeviceInfo of a certain device. */
        //ONI_C_API OniStatus oniDeviceGetInfo(OniDeviceHandle device, OniDeviceInfo* pInfo);
        [DllImport("OpenNI2.dll")]
        public static extern OniStatus oniDeviceGetInfo(OniDeviceHandle device, ref OniDeviceInfo pInfo);   

        /** Create a new stream in the device. The stream will originate from the source. */
        //ONI_C_API OniStatus oniDeviceCreateStream(OniDeviceHandle device, OniSensorType sensorType, OniStreamHandle* pStream);
        [DllImport("OpenNI2.dll")]
        public static extern OniStatus oniDeviceCreateStream(OniDeviceHandle device, OniSensorType sensorType, ref IntPtr pStream);
   
        //ONI_C_API OniStatus oniDeviceEnableDepthColorSync(OniDeviceHandle device);
        [DllImport("OpenNI2.dll")]
        public static extern OniStatus oniDeviceEnableDepthColorSync(OniDeviceHandle device);   
        //ONI_C_API void oniDeviceDisableDepthColorSync(OniDeviceHandle device);
        [DllImport("OpenNI2.dll")]
        public static extern void oniDeviceDisableDepthColorSync(OniDeviceHandle device);

/** Set property in the device. Use the properties listed in OniTypes.h: ONI_DEVICE_PROPERTY_..., or specific ones supplied by the device. */
//ONI_C_API OniStatus oniDeviceSetProperty(OniDeviceHandle device, int propertyId, const void* data, int dataSize);
        [DllImport("OpenNI2.dll")]
        public static extern OniStatus oniDeviceSetProperty(OniDeviceHandle device, int propertyId, IntPtr data, int dataSize);

///** Get property in the device. Use the properties listed in OniTypes.h: ONI_DEVICE_PROPERTY_..., or specific ones supplied by the device. */
//ONI_C_API OniStatus oniDeviceGetProperty(OniDeviceHandle device, int propertyId, void* data, int* pDataSize);
        [DllImport("OpenNI2.dll")]
        public static extern OniStatus oniDeviceGetProperty(OniDeviceHandle device, int propertyId, out IntPtr data, out int dataSize);


///** Check if the property is supported by the device. Use the properties listed in OniTypes.h: ONI_DEVICE_PROPERTY_..., or specific ones supplied by the device. */
//ONI_C_API OniBool oniDeviceIsPropertySupported(OniDeviceHandle device, int propertyId);
        [DllImport("OpenNI2.dll")]
        public static extern bool oniDeviceIsPropertySupported(OniDeviceHandle device, int propertyId);

///** Invoke an internal functionality of the device. */
//ONI_C_API OniStatus oniDeviceInvoke(OniDeviceHandle device, int commandId, const void* data, int dataSize);
        [DllImport("OpenNI2.dll")]
        public static extern OniStatus oniDeviceInvoke(OniDeviceHandle device, int commandId, IntPtr data, int dataSize);


///** Check if a command is supported, for invoke */
//ONI_C_API OniBool oniDeviceIsCommandSupported(OniDeviceHandle device, int commandId);
        [DllImport("OpenNI2.dll")]
        public static extern bool oniDeviceIsCommandSupported(OniDeviceHandle device, int commandId);

//ONI_C_API OniBool oniDeviceIsImageRegistrationModeSupported(OniDeviceHandle device, OniImageRegistrationMode mode);
        [DllImport("OpenNI2.dll")]
        public static extern bool oniDeviceIsImageRegistrationModeSupported(OniDeviceHandle device, OniImageRegistrationMode mode);

        /** Destroy an existing stream */
        [DllImport("OpenNI2.dll")]
        public static extern void oniStreamDestroy(OniStreamHandle stream);
        
        /** Get the OniSourceInfo of the certain stream. */
        //ONI_C_API const OniSensorInfo* oniStreamGetSensorInfo(OniStreamHandle stream);
        [DllImport("OpenNI2.dll")]
        public static extern IntPtr oniStreamGetSensorInfo(OniStreamHandle stream);

        /** Start generating data from the stream. */
        //ONI_C_API OniStatus oniStreamStart(OniStreamHandle stream);
        [DllImport("OpenNI2.dll")]
        public static extern OniStatus oniStreamStart(OniStreamHandle stream);

        /** Stop generating data from the stream. */
        //ONI_C_API void oniStreamStop(OniStreamHandle stream);        
        [DllImport("OpenNI2.dll")]
        public static extern void oniStreamStop(OniStreamHandle stream);



        ///** Get the next frame from the stream. This function is blocking until there is a new frame from the stream. For timeout, use oniWaitForStreams() first */
        //ONI_C_API OniStatus oniStreamReadFrame(OniStreamHandle stream, OniFrame** pFrame);
        [DllImport("OpenNI2.dll")]
        public static extern OniStatus oniStreamReadFrame(OniStreamHandle stream, ref IntPtr pFrame);

        ///** Register a callback to when the stream has a new frame. */
        //ONI_C_API OniStatus oniStreamRegisterNewFrameCallback(OniStreamHandle stream, OniNewFrameCallback handler, void* pCookie, OniCallbackHandle* pHandle);
        [DllImport("OpenNI2.dll")]
        public static extern OniStatus oniStreamRegisterNewFrameCallback(OniStreamHandle stream, IntPtr handler, IntPtr pCookie, out IntPtr pHandle);

///** Unregister a previously registered callback to when the stream has a new frame. */
//ONI_C_API void oniStreamUnregisterNewFrameCallback(OniStreamHandle stream, OniCallbackHandle handle);
        [DllImport("OpenNI2.dll")]
        public static extern void oniStreamUnregisterNewFrameCallback(OniStreamHandle stream, IntPtr handle);

///** Set property in the stream. Use the properties listed in OniTypes.h: ONI_STREAM_PROPERTY_..., or specific ones supplied by the device for its streams. */
//ONI_C_API OniStatus oniStreamSetProperty(OniStreamHandle stream, int propertyId, const void* data, int dataSize);
        [DllImport("OpenNI2.dll")]
        public static extern OniStatus oniStreamSetProperty(OniStreamHandle stream, int propertId, IntPtr data, int dataSize);


///** Get property in the stream. Use the properties listed in OniTypes.h: ONI_STREAM_PROPERTY_..., or specific ones supplied by the device for its streams. */
//ONI_C_API OniStatus oniStreamGetProperty(OniStreamHandle stream, int propertyId, void* data, int* pDataSize);
        [DllImport("OpenNI2.dll")]
        public static extern OniStatus oniStreamGetProperty(OniStreamHandle stream, int propertId, out IntPtr data, out int dataSize);

///** Check if the property is supported the stream. Use the properties listed in OniTypes.h: ONI_STREAM_PROPERTY_..., or specific ones supplied by the device for its streams. */
//ONI_C_API OniBool oniStreamIsPropertySupported(OniStreamHandle stream, int propertyId);
        [DllImport("OpenNI2.dll")]
        public static extern bool oniStreamIsPropertySupported(OniStreamHandle stream, int propertyId);

///** Invoke an internal functionality of the stream. */
//ONI_C_API OniStatus oniStreamInvoke(OniStreamHandle stream, int commandId, const void* data, int dataSize);
        [DllImport("OpenNI2.dll")]
        public static extern OniStatus oniStreamInvoke(OniStreamHandle stream, int commandId, IntPtr data, int dataSize);


///** Check if a command is supported, for invoke */
//ONI_C_API OniBool oniStreamIsCommandSupported(OniStreamHandle stream, int commandId);
        [DllImport("OpenNI2.dll")]
        public static extern bool oniStreamIsCommandSupported(OniStreamHandle stream, int commandId);


//// handle registration of pixel

//////
///** Mark another user of the frame. */
//ONI_C_API void oniFrameAddRef(OniFrame* pFrame);
        [DllImport("OpenNI2.dll")]
        public static extern void oniFrameAddRef(IntPtr pFrame);

///** Mark that the frame is no longer needed.  */
//ONI_C_API void oniFrameRelease(OniFrame* pFrame);
        [DllImport("OpenNI2.dll")]
        public static extern void oniFrameRelease(IntPtr pFrame);



//// ONI_C_API OniStatus oniConvertRealWorldToProjective(OniStreamHandle stream, OniFloatPoint3D* pRealWorldPoint, OniFloatPoint3D* pProjectivePoint);
        [DllImport("OpenNI2.dll")]
        public static extern OniStatus oniConvertRealWorldToProjective(OniStreamHandle stream, ref OniFloatPoint3D pRealWorldPoint, ref OniFloatPoint3D pProjectivePoint);

//// ONI_C_API OniStatus oniConvertProjectiveToRealWorld(OniStreamHandle stream, OniFloatPoint3D* pProjectivePoint, OniFloatPoint3D* pRealWorldPoint);
        [DllImport("OpenNI2.dll")]
        public static extern OniStatus oniConvertProjectiveToRealWorld(OniStreamHandle stream, ref OniFloatPoint3D pProjectivePoint, ref OniFloatPoint3D pRealWorldPoint);


///**
// * Creates a recorder that records to a file.
// * @param	[in]	fileName	The name of the file that will contain the recording.
// * @param	[out]	pRecorder	Points to the handle to the newly created recorder.
// * @retval ONI_STATUS_OK Upon successful completion.
// * @retval ONI_STATUS_ERROR Upon any kind of failure.
// */
//ONI_C_API OniStatus oniCreateRecorder(const char* fileName, OniRecorderHandle* pRecorder);
        [DllImport("OpenNI2.dll")]
        public static extern OniStatus oniCreateRecorder(string fileName, OniRecorderHandle pRecorder);


///**
// * Attaches a stream to a recorder. The amount of attached streams is virtually
// * infinite. You cannot attach a stream after you have started a recording, if
// * you do: an error will be returned by oniRecorderAttachStream.
// * @param	[in]	recorder				The handle to the recorder.
// * @param	[in]	stream					The handle to the stream.
// * @param	[in]	allowLossyCompression	Allows/denies lossy compression
// * @retval ONI_STATUS_OK Upon successful completion.
// * @retval ONI_STATUS_ERROR Upon any kind of failure.
// */
//ONI_C_API OniStatus oniRecorderAttachStream(
//        OniRecorderHandle   recorder, 
//        OniStreamHandle     stream, 
//        OniBool             allowLossyCompression);
        [DllImport("OpenNI2.dll")]
        public static extern OniStatus oniRecorderAttachStream(OniRecorderHandle recorder, OniStreamHandle stream, bool allowLossyCompression);


///**
// * Starts recording. There must be at least one stream attached to the recorder,
// * if not: oniRecorderStart will return an error.
// * @param[in] recorder The handle to the recorder.
// * @retval ONI_STATUS_OK Upon successful completion.
// * @retval ONI_STATUS_ERROR Upon any kind of failure.
// */
//ONI_C_API OniStatus oniRecorderStart(OniRecorderHandle recorder);
        [DllImport("OpenNI2.dll")]
        public static extern OniStatus oniRecorderStart(OniRecorderHandle recorder);

        //TODO: tell primesense about void typed function despite retval in doc
///**
// * Stops recording. You can resume recording via oniRecorderStart.
// * @param[in] recorder The handle to the recorder.
// * @retval ONI_STATUS_OK Upon successful completion.
// * @retval ONI_STATUS_ERROR Upon any kind of failure.
// */
//ONI_C_API void oniRecorderStop(OniRecorderHandle recorder);
        [DllImport("OpenNI2.dll")]
        public static extern void oniRecorderStop(OniRecorderHandle recorder);


///**
// * Stops recording if needed, and destroys a recorder.
// * @param	[in,out]	recorder	The handle to the recorder, the handle will be
// *									invalidated (nullified) when the function returns.
// * @retval ONI_STATUS_OK Upon successful completion.
// * @retval ONI_STATUS_ERROR Upon any kind of failure.
// */
//ONI_C_API OniStatus oniRecorderDestroy(OniRecorderHandle* pRecorder);
        [DllImport("OpenNI2.dll")]
        public static extern OniStatus oniRecorderDestroy(ref OniRecorderHandle recorder);


//ONI_C_API OniStatus oniCoordinateConverterDepthToWorld(OniStreamHandle depthStream, float depthX, float depthY, float depthZ, float* pWorldX, float* pWorldY, float* pWorldZ);
        [DllImport("OpenNI2.dll")]
        public static extern OniStatus oniCoordinateConverterDepthToWorld(OniStreamHandle depthStream, float depthX, float depthY, float depthZ, out float pWorldX, out float pWorldY, out float pWorldZ);

//ONI_C_API OniStatus oniCoordinateConverterWorldToDepth(OniStreamHandle depthStream, float worldX, float worldY, float worldZ, float* pDepthX, float* pDepthY, float* pDepthZ);
        [DllImport("OpenNI2.dll")]
        public static extern OniStatus oniCoordinateConverterWorldToDepth(OniStreamHandle depthStream, float worldX, float worldY, float worldZ, out float pDepthX, out float pDepthY, out float pDepthZ);

//ONI_C_API OniStatus oniCoordinateConverterDepthToColor(OniStreamHandle depthStream, OniStreamHandle colorStream, int depthX, int depthY, OniDepthPixel depthZ, int* pColorX, int* pColorY);
        [DllImport("OpenNI2.dll")]
        public static extern OniStatus oniCoordinateConverterDepthToColor(OniStreamHandle depthStream, OniStreamHandle colorStream, int depthX, int depthY, float depthZ, out float pWorldX, out float pWorldY, out float pWorldZ);




    }


    public class NITE2Wrapper
    {
        
        
        [Flags]        
        public enum NiteJointType : uint
        {
	        NITE_JOINT_HEAD = 0,
	        NITE_JOINT_NECK = 1,

	        NITE_JOINT_LEFT_SHOULDER = 2,
	        NITE_JOINT_RIGHT_SHOULDER = 3,
	        NITE_JOINT_LEFT_ELBOW = 4,
	        NITE_JOINT_RIGHT_ELBOW = 5,
	        NITE_JOINT_LEFT_HAND = 6,
	        NITE_JOINT_RIGHT_HAND = 7,

	        NITE_JOINT_TORSO = 8,

	        NITE_JOINT_LEFT_HIP = 9,
	        NITE_JOINT_RIGHT_HIP = 10,
	        NITE_JOINT_LEFT_KNEE = 11,
	        NITE_JOINT_RIGHT_KNEE = 12,
	        NITE_JOINT_LEFT_FOOT = 13,
	        NITE_JOINT_RIGHT_FOOT = 14
        } 
        public static ZigJointId NiteJointToZig(NiteJointType niteJointType)
        {
            switch (niteJointType)
            {
                case NiteJointType.NITE_JOINT_HEAD: return ZigJointId.Head;
	            case NiteJointType.NITE_JOINT_NECK: return ZigJointId.Neck;
	            case NiteJointType.NITE_JOINT_LEFT_SHOULDER: return ZigJointId.LeftShoulder;
	            case NiteJointType.NITE_JOINT_RIGHT_SHOULDER: return ZigJointId.RightShoulder;
	            case NiteJointType.NITE_JOINT_LEFT_ELBOW: return ZigJointId.LeftElbow;
	            case NiteJointType.NITE_JOINT_RIGHT_ELBOW: return ZigJointId.RightElbow;
	            case NiteJointType.NITE_JOINT_LEFT_HAND: return ZigJointId.LeftHand;
	            case NiteJointType.NITE_JOINT_RIGHT_HAND: return ZigJointId.RightHand;

	            case NiteJointType.NITE_JOINT_TORSO: return ZigJointId.Torso;

	            case NiteJointType.NITE_JOINT_LEFT_HIP: return ZigJointId.LeftHip;
	            case NiteJointType.NITE_JOINT_RIGHT_HIP: return ZigJointId.RightHip;
	            case NiteJointType.NITE_JOINT_LEFT_KNEE: return ZigJointId.LeftKnee;
	            case NiteJointType.NITE_JOINT_RIGHT_KNEE: return ZigJointId.RightKnee;
	            case NiteJointType.NITE_JOINT_LEFT_FOOT: return ZigJointId.LeftFoot;
                case NiteJointType.NITE_JOINT_RIGHT_FOOT: return ZigJointId.RightFoot;
                default: return ZigJointId.Head;
            }
        }
        /** Possible states of the skeleton */
        public enum NiteSkeletonState : uint
        {
	        /** No skeleton - skeleton was not requested */
	        NITE_SKELETON_NONE = 0,
	        /** Skeleton requested, but still unavailable */
	        NITE_SKELETON_CALIBRATING = 1,
	        /** Skeleton available */
	        NITE_SKELETON_TRACKED = 2,

	        /** Possible reasons as to why skeleton is unavailable */
	        NITE_SKELETON_CALIBRATION_ERROR_NOT_IN_POSE = 3,
	        NITE_SKELETON_CALIBRATION_ERROR_HANDS = 4,
	        NITE_SKELETON_CALIBRATION_ERROR_HEAD = 5,
	        NITE_SKELETON_CALIBRATION_ERROR_LEGS = 6,
	        NITE_SKELETON_CALIBRATION_ERROR_TORSO = 7
        } 
        public enum NiteUserState : uint
        {
	        /** User is visible and already known */
	        NITE_USER_STATE_VISIBLE = 1,
	        /** User is new - this is the first time the user is available */
	        NITE_USER_STATE_NEW = 2,
	        /** User is lost. This is the last time this user will be seen */
	        NITE_USER_STATE_LOST = 4,
        } 

        public enum NiteStatus : uint
        {
	        NITE_STATUS_OK = 0,
	        NITE_STATUS_ERROR = 1,
	        NITE_STATUS_BAD_USER_ID = 2
        }

        public enum NitePoseType : uint
        {
	        NITE_POSE_PSI = 0,
	        NITE_POSE_CROSSED_HANDS = 1
        } 

        public enum NitePoseState : uint
        {
	        NITE_POSE_STATE_DETECTING = 1,
	        NITE_POSE_STATE_IN_POSE = 2,
	        NITE_POSE_STATE_ENTER = 4,
	        NITE_POSE_STATE_EXIT = 8

        }

        public enum NiteGestureType : uint
        {
	        NITE_GESTURE_WAVE = 0,
	        NITE_GESTURE_CLICK = 1,
	        NITE_GESTURE_HAND_RAISE = 2
        }

/** Possible state of a gesture. Currently only 'Complete' is used. */
        public enum NiteGestureState : uint
        {
	        NITE_GESTURE_STATE_NEW = 1,				// Future
	        NITE_GESTURE_STATE_IN_PROGRESS = 2,		// Future
	        NITE_GESTURE_STATE_COMPLETED = 4
        } 

/** Possible state of a hand */
        public enum NiteHandState : uint
        {
 	        /** This hand was lost. It is the last frame in which it will be provided */
	        NITE_HAND_STATE_LOST = 0,
	        /** This is a new hand - it is the first frame in which it is available*/
	        NITE_HAND_STATE_NEW = 1,
 	        /** This is a known hand */
	        NITE_HAND_STATE_TRACKED = 2,
 	        /** This is a known hand, and in this frame it's very near the edge of the field of view */
	        NITE_HAND_STATE_TOUCHING_FOV = 4,
        }



        [StructLayout(LayoutKind.Sequential)]
        public struct NiteUserTrackerCallbacks
        {
            public IntPtr readyForNextFrame;      
        }
        
        [StructLayout(LayoutKind.Sequential)]
        public struct NiteHandTrackerCallbacks
        {
            public IntPtr readyForNextFrame;      
        } 
        

        [StructLayout(LayoutKind.Sequential)]
        public struct NitePoint3f
        {
            public float x;
            public float y;
            public float z;
            static public implicit operator Vector3(NitePoint3f p) { return new Vector3(p.x,p.y,-p.z); }
        }
        

        [StructLayout(LayoutKind.Sequential)]
        public struct NiteQuaternion
        {    
	        public float x;
            public float y;
            public float z;
            public float w;
            static public implicit operator Quaternion(NiteQuaternion q) { return new Quaternion(q.x, q.y, -q.z, -q.w); }
        } 

        [StructLayout(LayoutKind.Sequential)]
        public struct NiteSkeletonJoint
        {
	        /** Type of the joint */
	        public NiteJointType jointType;

	        /** Position of the joint - in real world coordinates */
	        public NitePoint3f position;
	        public float positionConfidence;

	        /** Orientation of the joint */
	        public NiteQuaternion orientation;
	        public float orientationConfidence;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct NiteBoundingBox
        {
	        public NitePoint3f min;
	        public NitePoint3f max;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct NitePoseData
        {
	        public NitePoseType type;
	        public int state;
        } 
        
        [StructLayout(LayoutKind.Sequential)]
        public struct NiteSkeleton 
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 15)] //NITE_JOINT_COUNT
	        public NiteSkeletonJoint [] joints;
	        public NiteSkeletonState state;
        }

        /** Snapshot of a specific user */
        [StructLayout(LayoutKind.Sequential)]
        public struct NiteUserData
        {
	        public NiteUserId id;
	        public NiteBoundingBox boundingBox;
	        public NitePoint3f centerOfMass;

            public NiteUserState state;

	        public NiteSkeleton skeleton;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)] //NITE_POSE_COUNT
            public NitePoseData [] poses;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct NiteUserMap
        {
            public IntPtr pixels; //NiteUserId* pixels;

	        public int width;
	        public int height;

	        public int stride;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct NitePlane
        {
	        public NitePoint3f point;
	        public NitePoint3f normal;
        } 
        
        [StructLayout(LayoutKind.Sequential)]
        public struct NiteUserTrackerFrame
        {
	        /** Number of users */
	        public int userCount;
	        /** List of users */
	        public IntPtr pUser;//NiteUserData* pUser;

	        /** Scene segmentation map */
	        public NiteUserMap userMap;
	        /** The depth frame from which this data was learned */
	        public IntPtr pDepthFrame; //OniFrame* pDepthFrame;

	        public UInt64 timestamp;
	        public int frameIndex;

	        /** Confidence of the floor plane */
	        public float floorConfidence;
	        /** Floor plane */
	        public NitePlane floor;
        } 

        /** A snapshot of a specific hand */
        [StructLayout(LayoutKind.Sequential)]
        public struct NiteHandData
        {
	        public NiteHandId id;
	        public NitePoint3f position;
	        public int state;
        }

/** A snapshot of a specific gesture */
        [StructLayout(LayoutKind.Sequential)]
        public struct NiteGestureData
        {
	        public NiteGestureType type;
	        public NitePoint3f currentPosition;
	        public int state;
        } 

    /** Output snapshot of the Hand Tracker algorithm */
        [StructLayout(LayoutKind.Sequential)]
        public struct NiteHandTrackerFrame
        {
	        /** Number of hands */
	        public int handCount;
	        /** List of hands */
            public IntPtr pHands; //NiteHandData* pHands;

	        /** Number of gestures */
	        public int gestureCount;
	        /** List of gestures */
            public IntPtr pGestures;//NiteGestureData* pGestures;

	        /** The depth frame from which this data was learned */
	        public IntPtr pDepthFrame; //OniFrame* pDepthFrame;

	        public UInt64 timestamp;
	        public int frameIndex;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct NiteVersion
        {

            /** Major version number, incremented for major API restructuring. */
            public int major;
            /** Minor version number, incremented when signficant new features added. */
            public int minor;
            /** Mainenance build number, incremented for new releases that primarily provide minor bug fixes. */
            public int maintenance;
            /** Build number. Incremented for each new API build. Generally not shown on the installer and download site. */
            public int build;
        }

        /**  Initialize OpenNI2. Use ONI_API_VERSION as the version. */
        //ONI_C_API OniStatus oniInitialize(int apiVersion);
        [DllImport("NiTE2.dll")]
        public static extern NiteStatus niteInitialize();

        
        /**  Shutdown OpenNI2 */
        //ONI_C_API void oniShutdown();
        [DllImport("NiTE2.dll")]
        public static extern void niteShutdown();


//        NITE_API NiteVersion niteGetVersion();
        [DllImport("NiTE2.dll")]
        public static extern NiteVersion niteGetVersion();

//// UserTracker
//NITE_API NiteStatus niteInitializeUserTracker(NiteUserTrackerHandle*);
        [DllImport("NiTE2.dll")]
        public static extern NiteStatus niteInitializeUserTracker(out NiteUserTrackerHandle pHandle);


//NITE_API NiteStatus niteInitializeUserTrackerByDevice(void*, NiteUserTrackerHandle*);
        [DllImport("NiTE2.dll")]
        public static extern NiteStatus niteInitializeUserTrackerByDevice(OniDeviceHandle pDevice, out NiteUserTrackerHandle pHandle);



//NITE_API NiteStatus niteShutdownUserTracker(NiteUserTrackerHandle);
        [DllImport("NiTE2.dll")]
        public static extern NiteStatus niteShutdownUserTracker(NiteUserTrackerHandle pHandle);


//NITE_API NiteStatus niteStartSkeletonTracking(NiteUserTrackerHandle, NiteUserId);
        [DllImport("NiTE2.dll")]
        public static extern NiteStatus niteStartSkeletonTracking(NiteUserTrackerHandle pHandle, NiteUserId userId);


//NITE_API void niteStopSkeletonTracking(NiteUserTrackerHandle, NiteUserId);
        [DllImport("NiTE2.dll")]
        public static extern void niteStopSkeletonTracking(NiteUserTrackerHandle pHandle, NiteUserId userId);

//NITE_API bool niteIsSkeletonTracking(NiteUserTrackerHandle, NiteUserId);
        [DllImport("NiTE2.dll")]
        public static extern bool niteIsSkeletonTracking(NiteUserTrackerHandle pHandle, NiteUserId userId);


//NITE_API NiteStatus niteSetSkeletonSmoothing(NiteUserTrackerHandle, float);
        [DllImport("NiTE2.dll")]
        public static extern NiteStatus niteSetSkeletonSmoothing(NiteUserTrackerHandle pHandle, float smoothing);

//NITE_API NiteStatus niteGetSkeletonSmoothing(NiteUserTrackerHandle, float*);
        [DllImport("NiTE2.dll")]
        public static extern NiteStatus niteGetSkeletonSmoothing(NiteUserTrackerHandle pHandle, out float smoothing);

//NITE_API NiteStatus niteStartPoseDetection(NiteUserTrackerHandle, NiteUserId, NitePoseType);
        [DllImport("NiTE2.dll")]
        public static extern NiteStatus niteStartPoseDetection(NiteUserTrackerHandle pHandle, NiteUserId userId, NitePoseType poseType);

        //NITE_API void niteStopPoseDetection(NiteUserTrackerHandle, NiteUserId, NitePoseType);
        [DllImport("NiTE2.dll")]
        public static extern void niteStopPoseDetection(NiteUserTrackerHandle pHandle, NiteUserId userId, NitePoseType poseType);

//NITE_API void niteStopAllPoseDetection(NiteUserTrackerHandle, NiteUserId);
        [DllImport("NiTE2.dll")]
        public static extern void niteStopAllPoseDetection(NiteUserTrackerHandle pHandle, NiteUserId userId);


//NITE_API NiteStatus niteRegisterUserTrackerCallbacks(NiteUserTrackerHandle, NiteUserTrackerCallbacks*, void*);
        [DllImport("NiTE2.dll")]
        public static extern NiteStatus niteRegisterUserTrackerCallbacks(NiteUserTrackerHandle pHandle, IntPtr pCallbacks, IntPtr pCookie);

//NITE_API void niteUnregisterUserTrackerCallbacks(NiteUserTrackerHandle, NiteUserTrackerCallbacks*);
        [DllImport("NiTE2.dll")]
        public static extern void niteUnregisterUserTrackerCallbacks(NiteUserTrackerHandle pHandle, IntPtr pCallbacks);

//NITE_API NiteStatus niteReadUserTrackerFrame(NiteUserTrackerHandle, NiteUserTrackerFrame**);
        [DllImport("NiTE2.dll")]
        public static extern NiteStatus niteReadUserTrackerFrame(NiteUserTrackerHandle pHandle, ref IntPtr pFrame);

//NITE_API NiteStatus niteUserTrackerFrameAddRef(NiteUserTrackerHandle, NiteUserTrackerFrame*);
        [DllImport("NiTE2.dll")]
        public static extern NiteStatus niteUserTrackerFrameAddRef(NiteUserTrackerHandle pHandle, IntPtr pFrame);


//NITE_API NiteStatus niteUserTrackerFrameRelease(NiteUserTrackerHandle, NiteUserTrackerFrame*);
        [DllImport("NiTE2.dll")]
        public static extern NiteStatus niteUserTrackerFrameRelease(NiteUserTrackerHandle pHandle, IntPtr pFrame);


//// HandTracker
//NITE_API NiteStatus niteInitializeHandTracker(NiteHandTrackerHandle*);
        [DllImport("NiTE2.dll")]
        public static extern NiteStatus niteInitializeHandTracker(out NiteHandTrackerHandle pHandle);


//NITE_API NiteStatus niteInitializeHandTrackerByDevice(void*, NiteHandTrackerHandle*);
        [DllImport("NiTE2.dll")]
        public static extern NiteStatus niteInitializeHandTrackerByDevice(IntPtr pDevice, out NiteHandTrackerHandle pHandle);


//NITE_API NiteStatus niteShutdownHandTracker(NiteHandTrackerHandle);
        [DllImport("NiTE2.dll")]
        public static extern NiteStatus niteShutdownHandTracker(NiteHandTrackerHandle pHandle);


//NITE_API NiteStatus niteStartHandTracking(NiteHandTrackerHandle, const NitePoint3f*, NiteHandId* pNewHandId);
        [DllImport("NiTE2.dll")]
        public static extern NiteStatus niteStartHandTracking(NiteHandTrackerHandle pHandle, ref NitePoint3f point, out NiteHandId pNewHandId);


//NITE_API void niteStopHandTracking(NiteHandTrackerHandle, NiteHandId);
        [DllImport("NiTE2.dll")]
        public static extern void niteStopHandTracking(NiteHandTrackerHandle pHandle, NiteHandId handId);

//NITE_API void niteStopAllHandTracking(NiteHandTrackerHandle);
        [DllImport("NiTE2.dll")]
        public static extern void niteStopAllHandTracking(NiteHandTrackerHandle pHandle);

//NITE_API NiteStatus niteSetHandSmoothingFactor(NiteHandTrackerHandle, float);
        [DllImport("NiTE2.dll")]
        public static extern NiteStatus niteSetHandSmoothingFactor(NiteHandTrackerHandle pHandle, float smoothing);

//NITE_API NiteStatus niteGetHandSmoothingFactor(NiteHandTrackerHandle, float*);
        [DllImport("NiTE2.dll")]
        public static extern NiteStatus niteSetHandSmoothingFactor(NiteHandTrackerHandle pHandle, out float smoothing);


//NITE_API NiteStatus niteRegisterHandTrackerCallbacks(NiteHandTrackerHandle, NiteHandTrackerCallbacks*, void*);
        [DllImport("NiTE2.dll")]
        public static extern NiteStatus niteRegisterHandTrackerCallbacks(NiteHandTrackerHandle pHandle, IntPtr pCallbacks, IntPtr pCookie);

//NITE_API void niteUnregisterHandTrackerCallbacks(NiteHandTrackerHandle, NiteHandTrackerCallbacks*);
        [DllImport("NiTE2.dll")]
        public static extern NiteStatus niteUnregisterHandTrackerCallbacks(NiteHandTrackerHandle pHandle, IntPtr pCallbacks);


//NITE_API NiteStatus niteReadHandTrackerFrame(NiteHandTrackerHandle, NiteHandTrackerFrame**);
        [DllImport("NiTE2.dll")]
        public static extern NiteStatus niteReadHandTrackerFrame(NiteHandTrackerHandle pHandle, ref IntPtr pFrame);


//NITE_API NiteStatus niteHandTrackerFrameAddRef(NiteHandTrackerHandle, NiteHandTrackerFrame*);
        [DllImport("NiTE2.dll")]
        public static extern NiteStatus niteHandTrackerFrameAddRef(NiteHandTrackerHandle pHandle, IntPtr pFrame);

//NITE_API NiteStatus niteHandTrackerFrameRelease(NiteHandTrackerHandle, NiteHandTrackerFrame*);
        [DllImport("NiTE2.dll")]
        public static extern NiteStatus niteHandTrackerFrameRelease(NiteHandTrackerHandle pHandle, IntPtr pFrame);


//NITE_API NiteStatus niteStartGestureDetection(NiteHandTrackerHandle, NiteGestureType);
        [DllImport("NiTE2.dll")]
        public static extern NiteStatus niteStartGestureDetection(NiteHandTrackerHandle pHandle, NiteGestureType gestureType);

//NITE_API void niteStopGestureDetection(NiteHandTrackerHandle, NiteGestureType);
        [DllImport("NiTE2.dll")]
        public static extern NiteStatus niteStopGestureDetection(NiteHandTrackerHandle pHandle, NiteGestureType gestureType);

//NITE_API void niteStopAllGestureDetection(NiteHandTrackerHandle);
        [DllImport("NiTE2.dll")]
        public static extern NiteStatus niteStopAllGestureDetection(NiteHandTrackerHandle pHandle);
 
//NITE_API NiteStatus niteConvertJointCoordinatesToDepth(NiteUserTrackerHandle userTracker, float x, float y, float z, float* pX, float* pY);
        [DllImport("NiTE2.dll")]
        public static extern NiteStatus niteConvertJointCoordinatesToDepth(NiteUserTrackerHandle userTracker, float x, float y, float z, out float pX, out float pY);

//NITE_API NiteStatus niteConvertDepthCoordinatesToJoint(NiteUserTrackerHandle userTracker, int x, int y, int z, float* pX, float* pY);
        [DllImport("NiTE2.dll")]
        public static extern NiteStatus niteConvertDepthCoordinatesToJoint(NiteUserTrackerHandle userTracker, int x, int y, int z, out float pX, out float pY);

//NITE_API NiteStatus niteConvertHandCoordinatesToDepth(NiteHandTrackerHandle handTracker, float x, float y, float z, float* pX, float* pY);
        [DllImport("NiTE2.dll")]
        public static extern NiteStatus niteConvertHandCoordinatesToDepth(NiteUserTrackerHandle userTracker,  float x, float y, float z, out float pX, out float pY);

//NITE_API NiteStatus niteConvertDepthCoordinatesToHand(NiteHandTrackerHandle handTracker, int x, int y, int z, float* pX, float* pY);
        [DllImport("NiTE2.dll")]
        public static extern NiteStatus niteConvertDepthCoordinatesToHand(NiteUserTrackerHandle userTracker, int x, int y, int z, out float pX, out float pY);

    }





    public class ZigInputOpenNI2 : IZigInputReader
    {

        OniDeviceHandle pDevice;
        OniStreamHandle pDepthStream;
        OniStreamHandle pImageStream;        
        IntPtr pDeviceCBHandle;
        IntPtr pNewFrameCBHandle;
        OpenNI2.OpenNI2Wrapper.OniDeviceCallbacks callbacks;        
        Delegate Connected;
        Delegate Disconnected;
        Delegate StateChanged;
        Delegate NewFrame;
        IntPtr pCallbacks;
        byte[] rawImageMap;

        NiteUserTrackerHandle pUserTracker;

        // init/update/shutdown
	    public void Init(ZigInputSettings settings)
        {
            users = new List<ZigInputUser>();
            UpdateDepth = settings.UpdateDepth;
            UpdateImage = settings.UpdateImage;
            UpdateLabelMap = settings.UpdateLabelMap;

            Debug.Log("ZigInputOpenNI2 Init");
            OpenNI2.OpenNI2Wrapper.OniVersion v = OpenNI2.OpenNI2Wrapper.oniGetVersion();

            Debug.Log("OpenNI2 version: " + v.major + "." + v.minor + "." + v.maintenance + "." + v.build);

            OpenNI2.OpenNI2Wrapper.OniStatus s = OpenNI2.OpenNI2Wrapper.oniInitialize(2000);
            Debug.Log("OpenNI2 Wrapper Started : " + s);


            OpenNI2.NITE2Wrapper.NiteStatus ns = OpenNI2.NITE2Wrapper.niteInitialize();
            Debug.Log("NITE2 Wrapper Started : " + ns);

            IntPtr pDevices = IntPtr.Zero;
            int numDevices = 0;
            s = OpenNI2.OpenNI2Wrapper.oniGetDeviceList(ref pDevices, ref numDevices);
            Debug.Log("OpenNI2 Wrapper device list gotten : " + s + " num devices " + numDevices + " pDevices" + pDevices);
            OpenNI2.OpenNI2Wrapper.OniDeviceInfo info = new OpenNI2.OpenNI2Wrapper.OniDeviceInfo();
            if (numDevices == 0)
            {
                Debug.Log("OpenNI2 Error: No device connected");
            }
            else
            {
                //TODO: test with multiple devices.
                for (int i = 0; i < numDevices; i++)
                {
                    IntPtr ptr = new IntPtr(pDevices.ToInt32() + i);
                    //IntPtr obj = Marshal.ReadIntPtr(pDevices);
                    info = (OpenNI2.OpenNI2Wrapper.OniDeviceInfo)Marshal.PtrToStructure(ptr, typeof(OpenNI2.OpenNI2Wrapper.OniDeviceInfo));
                    Debug.Log("uri:" + new String(info.uri));
                    Debug.Log("vendor:" + new String(info.vendor));
                    Debug.Log("name:" + new String(info.name));

                }

                string uri = new String(info.uri);
                //Debug.Log("opening from uri:" + info.uri);
                s = OpenNI2.OpenNI2Wrapper.oniDeviceOpen(uri, ref pDevice);
                Debug.Log("OpenNI2 Wrapper device open: " + s);
            }
            s = OpenNI2.OpenNI2Wrapper.oniReleaseDeviceList(pDevices);
            //Debug.Log("OpenNI2 Wrapper device list released : " + s + " pDevices " + pDevices);


            
            //Unity will not let these callbacks be accessed from a different thread

            //callbacks.deviceConnected = new OpenNI2.OpenNI2Wrapper.OniDeviceInfoCallback(deviceConnected_handler);
        /*    Connected = new OpenNI2.OpenNI2Wrapper.OniDeviceInfoCallback(deviceConnected_handler);
            Disconnected = new OpenNI2.OpenNI2Wrapper.OniDeviceInfoCallback(deviceDisconnected_handler);
            StateChanged = new OpenNI2.OpenNI2Wrapper.OniDeviceStateCallback(deviceStateChanged_handler);
            callbacks.deviceConnected = Marshal.GetFunctionPointerForDelegate(Connected);
            callbacks.deviceDisconnected = Marshal.GetFunctionPointerForDelegate(Disconnected);
            callbacks.deviceStateChanged = Marshal.GetFunctionPointerForDelegate(StateChanged);

            //TODO: these callbacks have a weird behavior where they don't print the info the second time the callback occurs
            pCallbacks = Marshal.AllocHGlobal(Marshal.SizeOf(callbacks));
            Marshal.StructureToPtr(callbacks, pCallbacks, true);
            s = OpenNI2.OpenNI2Wrapper.oniRegisterDeviceCallbacks(pCallbacks, pDevice, out pDeviceCBHandle);
            //Debug.Log("OpenNI2 Wrapper oniRegisterDeviceCallback called : " + s + "pDeviceCBHandle:" + pDeviceCBHandle);
         */
            if (pDevice != IntPtr.Zero)
            {


                

                s = OpenNI2.OpenNI2Wrapper.oniDeviceCreateStream(pDevice, OpenNI2.OpenNI2Wrapper.OniSensorType.ONI_SENSOR_DEPTH, ref pDepthStream);
                Debug.Log("OpenNI2 Wrapper Depth Stream created : " + s);

                s = OpenNI2.OpenNI2Wrapper.oniStreamStart(pDepthStream);
                Debug.Log("OpenNI2 Wrapper Depth Stream started : " + s);

                s = OpenNI2.OpenNI2Wrapper.oniDeviceCreateStream(pDevice, OpenNI2.OpenNI2Wrapper.OniSensorType.ONI_SENSOR_COLOR, ref pImageStream);
                Debug.Log("OpenNI2 Wrapper Image Stream created : " + s);

                s = OpenNI2.OpenNI2Wrapper.oniStreamStart(pImageStream);
                Debug.Log("OpenNI2 Wrapper Image Stream started : " + s);
              
                //NewFrame = new OpenNI2.OpenNI2Wrapper.OniNewFrameCallback(newFrame_handler);
                //s = OpenNI2.OpenNI2Wrapper.oniStreamRegisterNewFrameCallback(pDepthStream, Marshal.GetFunctionPointerForDelegate(NewFrame), pDevice, out pNewFrameCBHandle);
                //Debug.Log("OpenNI2 oniStreamRegisterNewFrameCallback " + s + " pNewFrameCBHandle: " + pNewFrameCBHandle);

        //TODO: should get properties of the stream and set it to resolution
                Depth = new ZigDepth(320, 240);
                Image = new ZigImage(320, 240);
                LabelMap = new ZigLabelMap(320, 240);
                rawImageMap = new byte[Image.xres * Image.yres * 3];
                frame = new OpenNI2.OpenNI2Wrapper.OniFrame();




                ns = OpenNI2.NITE2Wrapper.niteInitializeUserTracker(out pUserTracker);
                Debug.Log("OpenNI2 Wrapper User Tracker Initialized : " + ns);

            }

        }


        void newFrame_handler(IntPtr stream, IntPtr pCookie)
        {
            //OpenNI2.OpenNI2Wrapper.oniStreamUnregisterNewFrameCallback(pDepthStream, pNewFrameCBHandle);
            Debug.Log("EVENT: new frame: " + stream + " pCookie " + pCookie);

            //Warning: unity canot access monobehavior methods from the handler thread

        }
        void deviceConnected_handler(ref OpenNI2.OpenNI2Wrapper.OniDeviceInfo info, IntPtr pCookie)
        {
            Debug.Log("Device Connection Callback");
            //Debug.Log("pInfo: " + pInfo);

            //OpenNI2.OpenNI2Wrapper.OniDeviceInfo info = new OpenNI2.OpenNI2Wrapper.OniDeviceInfo();
            //Marshal.PtrToStructure(pInfo, info);

            try
            {//TODO: fails to print the second time a connected or disconnectec callback is called
                Debug.Log("printing info in Connection handler");
                Debug.Log("uri:" + new String(info.uri));
                Debug.Log("vendor:" + new String(info.vendor));
                Debug.Log("name:" + new String(info.name));
            }
            catch (Exception e)
            {
                Debug.LogWarning("info Connection Exception : " + e.Message);
            }
        }
        void deviceDisconnected_handler(ref OpenNI2.OpenNI2Wrapper.OniDeviceInfo info, IntPtr pCookie)
        {
            Debug.Log("Device Disconnection Callback");

            try
            {
                Debug.Log("uri:" + new String(info.uri));
                Debug.Log("vendor:" + new String(info.vendor));
                Debug.Log("name:" + new String(info.name));
            }
            catch (Exception e)
            {
                Debug.LogWarning("info Disconnection Exception : " + e.Message);
            }
            //if (pDepthStream != IntPtr.Zero)
            //{
            //    OpenNI2.OpenNI2Wrapper.oniStreamStop(pDepthStream);
            //    Debug.Log("OpenNI2 stream stopped");

            //    OpenNI2.OpenNI2Wrapper.oniStreamDestroy(pDepthStream);
            //    Debug.Log("OpenNI2 stream destroyed" + pDepthStream);

            //    pDepthStream = IntPtr.Zero;
            //    Debug.Log("OpenNI2 stream zeroed");
            //}

            //if (pDevice != IntPtr.Zero)
            //{

            //    Debug.Log("Non-zero pDevice in disconnected " + pDevice);
            //    OpenNI2.OpenNI2Wrapper.oniDeviceClose(pDevice);
            //    Debug.Log("OpenNI2 device closed");
            //    pDevice = IntPtr.Zero;

            //}
        }

        void deviceStateChanged_handler(ref OpenNI2.OpenNI2Wrapper.OniDeviceInfo info, OpenNI2.OpenNI2Wrapper.OniDeviceState deviceState, IntPtr pCookie)
        {
            Debug.Log("Device State Change Callback");

            //OpenNI2.OpenNI2Wrapper.OniDeviceInfo info = new OpenNI2.OpenNI2Wrapper.OniDeviceInfo();
            //Marshal.PtrToStructure(pInfo, info);
            /*
            OpenNI2.OpenNI2Wrapper.OniDeviceState deviceState = new OpenNI2.OpenNI2Wrapper.OniDeviceState();
            Marshal.PtrToStructure(pDeviceState, deviceState);
        
             */
            Debug.Log("uri:" + new String(info.uri));
            Debug.Log("vendor:" + new String(info.vendor));
            Debug.Log("name:" + new String(info.name));
            Debug.Log("State:" + deviceState);

        }
        List<ZigInputUser> users;
	       

            
        public void Shutdown()
        {
            Debug.Log("Shutdown called for OpenNI2");


            //if (pNewFrameCBHandle != IntPtr.Zero)
            //{
            //    OpenNI2.OpenNI2Wrapper.oniStreamUnregisterNewFrameCallback(pDepthStream, pNewFrameCBHandle);
            //    Debug.Log("OpenNI2 NewFrame unregistered");
            //}

            if (pUserTracker != IntPtr.Zero)
            {
                OpenNI2.NITE2Wrapper.niteShutdownUserTracker(pUserTracker);
            }
            if (pDepthStream != IntPtr.Zero)
            {
                OpenNI2.OpenNI2Wrapper.oniStreamStop(pDepthStream);
                Debug.Log("OpenNI2 stream stopped");

                OpenNI2.OpenNI2Wrapper.oniStreamDestroy(pDepthStream);
                Debug.Log("OpenNI2 stream destroyed");
            }
            if (pImageStream != IntPtr.Zero)
            {
                OpenNI2.OpenNI2Wrapper.oniStreamStop(pImageStream);
                Debug.Log("OpenNI2 stream stopped");

                OpenNI2.OpenNI2Wrapper.oniStreamDestroy(pImageStream);
                Debug.Log("OpenNI2 stream destroyed");
            }
            if (pDeviceCBHandle != IntPtr.Zero)
            {
                OpenNI2.OpenNI2Wrapper.oniUnregisterDeviceCallbacks(pDeviceCBHandle);
                Debug.Log("OPENNI2 unregistered callbacks");
            }

            if (pDevice != IntPtr.Zero)
            {
                OpenNI2.OpenNI2Wrapper.oniDeviceClose(pDevice);
                Debug.Log("OpenNI2 Device Close");
            }
		    OpenNI2.OpenNI2Wrapper.oniShutdown();
		
		    Debug.Log("OpenNI2 Shutdown");
		    OpenNI2.NITE2Wrapper.niteShutdown();
		    Debug.Log("NITE2 Shutdown");
	    
        }
	
	    // users & hands
	    public event EventHandler<NewUsersFrameEventArgs> NewUsersFrame;
        protected void OnNewUsersFrame(List<ZigInputUser> users)
        {
            if (null != NewUsersFrame)
            {
                NewUsersFrame.Invoke(this, new NewUsersFrameEventArgs(users));
            }
        }
        
        
        // streams
        public bool UpdateDepth { get; set; }
        public bool UpdateImage { get; set; }
        public bool UpdateLabelMap { get; set; }

        OpenNI2.OpenNI2Wrapper.OniFrame frame;
        IntPtr pFrame;
        int oldUserCount;
        public void Update()
        {
            try
            {
           //     keepTrying = false;

                //(OniStreamHandle* pStreams, int numStreams, int* pStreamIndex, int timeout);
//        [DllImport("OpenNI2.dll")]
  //      public static extern OniStatus oniWaitForAnyStream(ref IntPtr pHandle, int numStreams, ref int pStreamIndex, int timeout);

                int index;
                OpenNI2.OpenNI2Wrapper.OniStatus s = OpenNI2.OpenNI2Wrapper.oniWaitForAnyStream(ref pDepthStream, 1, out index, -1);//timeout forever -1,
         //       Debug.Log("Status after wait: " + s + " pDepthStream " + pDepthStream + "index: " + index);


                if (s == OpenNI2.OpenNI2Wrapper.OniStatus.ONI_STATUS_TIME_OUT)
                {
                    Debug.Log("TIMEOUT!!!");
                    return;
                    
                }
                if (s != OpenNI2.OpenNI2Wrapper.OniStatus.ONI_STATUS_OK)
                {
                    
                    return;
                }

                if (UpdateDepth)
                {
                    IntPtr pDepthFrame = IntPtr.Zero;
                    s = OpenNI2.OpenNI2Wrapper.oniStreamReadFrame(pDepthStream, ref pDepthFrame);
                    //         Debug.Log("Read depthStream Frame: " + s + "\t\t\t pFRAME\t" + pFrame);

                    //OpenNI2.OpenNI2Wrapper.oniFrameAddRef(pFrame);

                    //Debug.Log("pFrame: \t\t\t pFRAME\t" + pFrame);

                    frame = (OpenNI2.OpenNI2Wrapper.OniFrame)Marshal.PtrToStructure(pDepthFrame, typeof(OpenNI2.OpenNI2Wrapper.OniFrame));

                    if (Depth.data.Length == 0)
                    {
                        Depth.xres = frame.width;
                        Depth.yres = frame.height;
                        Depth.data = new short[frame.width * frame.height];
                    }

                    //Marshal Depth.data in from frame.data
                    Marshal.Copy(frame.data, Depth.data, 0, Depth.data.Length);
                    OpenNI2.OpenNI2Wrapper.oniFrameRelease(pDepthFrame);
                  
                }




                if (UpdateImage)
                {
                    IntPtr pImageFrame = IntPtr.Zero;
                    s = OpenNI2.OpenNI2Wrapper.oniStreamReadFrame(pImageStream, ref pImageFrame);
                    frame = (OpenNI2.OpenNI2Wrapper.OniFrame)Marshal.PtrToStructure(pImageFrame, typeof(OpenNI2.OpenNI2Wrapper.OniFrame));
                    //  Debug.Log("Image frame: " + frame.width + " " + frame.height);
                    Marshal.Copy(frame.data, rawImageMap, 0, rawImageMap.Length);

                    int rawi = 0;
                    for (int i = 0; i < Image.data.Length; i++, rawi += 3)
                    {
                        Image.data[i].r = rawImageMap[rawi];
                        Image.data[i].g = rawImageMap[rawi + 1];
                        Image.data[i].b = rawImageMap[rawi + 2];
                    };
                    OpenNI2.OpenNI2Wrapper.oniFrameRelease(pImageFrame);

                }


                IntPtr pUserFrame = IntPtr.Zero;
                OpenNI2.NITE2Wrapper.NiteStatus ns = OpenNI2.NITE2Wrapper.niteReadUserTrackerFrame(pUserTracker, ref pUserFrame);
                //Debug.Log("read user tracker frame: " + s + " pFrame: " + pFrame);

                OpenNI2.NITE2Wrapper.NiteUserTrackerFrame userFrame = (OpenNI2.NITE2Wrapper.NiteUserTrackerFrame)Marshal.PtrToStructure(pUserFrame, typeof(OpenNI2.NITE2Wrapper.NiteUserTrackerFrame));
                //Debug.Log("User Frame to Ptr, userCount: " + userFrame.userCount);
                OpenNI2.NITE2Wrapper.NiteUserData[] niteUsers = new OpenNI2.NITE2Wrapper.NiteUserData[userFrame.userCount];

                int sizeInBytes = Marshal.SizeOf(typeof(OpenNI2.NITE2Wrapper.NiteUserData));
                

                users = new List<ZigInputUser>();
                for(int i = 0; i<userFrame.userCount; i++)
                {
                    IntPtr p = new IntPtr((userFrame.pUser.ToInt32() + i * sizeInBytes));
                    niteUsers[i] = (OpenNI2.NITE2Wrapper.NiteUserData)Marshal.PtrToStructure( p,typeof(OpenNI2.NITE2Wrapper.NiteUserData));
                    ZigInputUser user = new ZigInputUser(niteUsers[i].id, niteUsers[i].centerOfMass);

                    OpenNI2.NITE2Wrapper.NiteSkeleton skeleton = niteUsers[i].skeleton;
                                                                                                         
                    user.Tracked =  skeleton.state == NITE2Wrapper.NiteSkeletonState.NITE_SKELETON_TRACKED;

                    List<ZigInputJoint> joints = new List<ZigInputJoint>();
                    if (user.Tracked)
                    {
                        
                        foreach (OpenNI2.NITE2Wrapper.NiteSkeletonJoint joint in skeleton.joints)
                        {
                            ZigInputJoint zigJoint = new ZigInputJoint(OpenNI2.NITE2Wrapper.NiteJointToZig(joint.jointType));
                            zigJoint.GoodPosition = joint.positionConfidence > .5f;
                            zigJoint.Position = joint.position;

                            zigJoint.GoodRotation = joint.orientationConfidence > .5f;
                            zigJoint.Rotation = joint.orientation;
                            joints.Add(zigJoint);
                        }
                       
                    }
                    else
                    {
                        ns = OpenNI2.NITE2Wrapper.niteStartSkeletonTracking(pUserTracker, (short)user.Id);
                    }

                    user.SkeletonData = joints;
                    users.Add(user);
                                 
                }
                if (UpdateLabelMap)
                {

                    Marshal.Copy(userFrame.userMap.pixels, LabelMap.data, 0, userFrame.userMap.width * userFrame.userMap.height);
                }
                OpenNI2.NITE2Wrapper.niteUserTrackerFrameRelease(pUserTracker, pUserFrame);
                
            }
            catch (Exception e)
            {
                Debug.LogWarning("Exception in ZigInputOpenNI2.Update: " + e.Message);
            }

            OnNewUsersFrame(users);
        }

        public ZigDepth Depth { get; private set; }
        public ZigImage Image { get; private set; }
        public ZigLabelMap LabelMap { get; private set; }

        // misc
        public Vector3 ConvertWorldToImageSpace(Vector3 worldPosition)
        {
            float x, y, z;

            OpenNI2.OpenNI2Wrapper.OniStatus s = OpenNI2.OpenNI2Wrapper.oniCoordinateConverterWorldToDepth(pDepthStream, worldPosition.x, worldPosition.y, -worldPosition.z, out x, out y, out z);
            //Debug.Log("Convert status: " + s);
            
            return new Vector3(x, y, z);
        }
        public Vector3 ConvertImageToWorldSpace(Vector3 imagePosition)
        {
            float x, y, z;
            OpenNI2.OpenNI2Wrapper.OniStatus s = OpenNI2.OpenNI2Wrapper.oniCoordinateConverterDepthToWorld(pDepthStream, imagePosition.x, imagePosition.y, imagePosition.z, out x, out y, out z);
            
            //Debug.Log("Convert status: " + s);
         
            return new Vector3(x,y,-z);
        }
        public bool AlignDepthToRGB { get; set; }
    }
}
