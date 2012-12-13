using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;


public class testOpenNI2 : MonoBehaviour {



    IntPtr pDevice;
    IntPtr pDepthStream;
    IntPtr pDeviceCBHandle;
    OpenNI2.OpenNI2Wrapper.OniDeviceCallbacks callbacks;
    Delegate Connected;
    Delegate Disconnected;
    Delegate StateChanged;
    IntPtr pCallbacks;
	// Use this for initialization
	void Start () {
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
        Debug.Log("opening from uri:" + info.uri);
        s = OpenNI2.OpenNI2Wrapper.oniDeviceOpen(uri, ref pDevice);
        Debug.Log("OPENNI2 Wrapper device open: " + s + " pDevice: " + pDevice);
    }
    s = OpenNI2.OpenNI2Wrapper.oniReleaseDeviceList(pDevices);
	Debug.Log("OpenNI2 Wrapper device list released : " + s + " pDevices " + pDevices);



    
    //callbacks.deviceConnected = new OpenNI2.OpenNI2Wrapper.OniDeviceInfoCallback(deviceConnected_handler);
    Connected =  new OpenNI2.OpenNI2Wrapper.OniDeviceInfoCallback(deviceConnected_handler);
    Disconnected = new OpenNI2.OpenNI2Wrapper.OniDeviceInfoCallback(deviceDisconnected_handler);
    StateChanged = new OpenNI2.OpenNI2Wrapper.OniDeviceStateCallback(deviceStateChanged_handler);
    callbacks.deviceConnected = Marshal.GetFunctionPointerForDelegate(Connected);
    callbacks.deviceDisconnected = Marshal.GetFunctionPointerForDelegate(Disconnected);
    callbacks.deviceStateChanged = Marshal.GetFunctionPointerForDelegate(StateChanged);

     //TODO: these callbacks have a weird behavior where they don't print the info the second time the callback occurs
    pCallbacks = Marshal.AllocHGlobal(Marshal.SizeOf(callbacks));
    Marshal.StructureToPtr(callbacks, pCallbacks, true);
    s = OpenNI2.OpenNI2Wrapper.oniRegisterDeviceCallbacks(pCallbacks, pDevice, out pDeviceCBHandle);
    Debug.Log("OpenNI2 Wrapper oniRegisterDeviceCallback called : " + s + "pDeviceCBHandle:" + pDeviceCBHandle);

    if (pDevice != IntPtr.Zero)
    {
        s = OpenNI2.OpenNI2Wrapper.oniDeviceCreateStream(pDevice, OpenNI2.OpenNI2Wrapper.OniSensorType.ONI_SENSOR_DEPTH, ref pDepthStream);
        Debug.Log("OpenNI2 Wrapper Stream created : " + s + " pDepthStream " + pDepthStream);

        s = OpenNI2.OpenNI2Wrapper.oniStreamStart(pDepthStream);
        Debug.Log("OpenNI2 Wrapper Stream started : " + s);
    }

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
        //Debug.Log("pInfo: " + pInfo);
        //OpenNI2.OpenNI2Wrapper.OniDeviceInfo info = new OpenNI2.OpenNI2Wrapper.OniDeviceInfo();
        //Marshal.PtrToStructure(pInfo, info);
        
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
        if (pDepthStream != IntPtr.Zero)
        {
            OpenNI2.OpenNI2Wrapper.oniStreamStop(pDepthStream);
            Debug.Log("OpenNI2 stream stopped");

            OpenNI2.OpenNI2Wrapper.oniStreamDestroy(pDepthStream);
            Debug.Log("OpenNI2 stream destroyed" + pDepthStream);

            pDepthStream = IntPtr.Zero;
            Debug.Log("OpenNI2 stream zeroed"); 
        }

        if (pDevice != IntPtr.Zero)
        {

            Debug.Log("Non-zero pDevice in disconnected " + pDevice);
            OpenNI2.OpenNI2Wrapper.oniDeviceClose(pDevice);
            Debug.Log("OpenNI2 device closed");
            pDevice = IntPtr.Zero;

        }
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
	// Update is called once per frame
	void Update () {
	
	}
	
	void OnDestroy()
	{
        if (pDepthStream != IntPtr.Zero)
        {
            OpenNI2.OpenNI2Wrapper.oniStreamStop(pDepthStream);
            Debug.Log("OpenNI2 stream stopped");

            OpenNI2.OpenNI2Wrapper.oniStreamDestroy(pDepthStream);
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
}
