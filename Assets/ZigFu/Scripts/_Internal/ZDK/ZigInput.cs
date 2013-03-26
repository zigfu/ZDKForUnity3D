//#define WATERMARK_OMERCY

using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;

// Note: This enum is identical to the OpenNI SkeletonJoint enum
// It doesn't have to be, but this ensures backwards compatibility
// with some of our recorded data (not to mention less conversion code ;))

public enum ZigJointId
{
 	None = 0,
 	Head,
 	Neck,
 	Torso,
 	Waist,
 	LeftCollar,
 	LeftShoulder,
 	LeftElbow,
 	LeftWrist,
 	LeftHand,
 	LeftFingertip,
 	RightCollar,
 	RightShoulder,
 	RightElbow,
 	RightWrist,
 	RightHand,
 	RightFingertip,
 	LeftHip,
 	LeftKnee,
 	LeftAnkle,
 	LeftFoot,
 	RightHip,
 	RightKnee,
 	RightAnkle,
 	RightFoot
}

public class ZigInputJoint
{
	public ZigJointId Id { get; private set; }
	public Vector3 Position;
	public Quaternion Rotation;
	public bool GoodPosition;
	public bool GoodRotation;
    public bool Inferred;
	
	public ZigInputJoint(ZigJointId id) :
		this(id, Vector3.zero, Quaternion.identity, false) {
            GoodPosition = false;
            GoodRotation = false;
    }
	
	public ZigInputJoint(ZigJointId id, Vector3 position, Quaternion rotation, bool inferred) {
		Id = id;
		Position = position;
		Rotation = rotation;
        Inferred = inferred;
	}
}

public class ZigInputUser
{
	public int Id;
	public bool Tracked;
	public Vector3 CenterOfMass;
	public List<ZigInputJoint> SkeletonData;
	public ZigInputUser(int id, Vector3 com)
	{
		Id = id;
		CenterOfMass = com;
	}
}

public class NewUsersFrameEventArgs : EventArgs
{
	public NewUsersFrameEventArgs(List<ZigInputUser> users)
	{
		Users = users;
	}
	
	public List<ZigInputUser> Users { get; private set; }
}

public class ZigDepth {
    public int xres;
    public int yres;
    public short[] data;
    public ZigDepth(int x, int y) {
        xres = x;
        yres = y;
        data = new short[x * y];
    }
}

public class ZigImage
{
    public int xres { get; private set; }
    public int yres { get; private set; }
    public Color32[] data;
    public ZigImage(int x, int y) {
        xres = x;
        yres = y;
        data = new Color32[x * y];
    }
}

public class ZigLabelMap
{
    public int xres { get; private set; }
    public int yres { get; private set; }
    public short[] data;
    public ZigLabelMap(int x, int y)
    {
        xres = x;
        yres = y;
        data = new short[x * y];
    }
}

public interface IZigInputReader
{
	// init/update/shutdown
	void Init(ZigInputSettings settings);
	void Update();
	void Shutdown();
	
	// users & hands
	event EventHandler<NewUsersFrameEventArgs> NewUsersFrame;
	
    // streams
    //bool UpdateDepth { get; set; }
    //bool UpdateImage { get; set; }
    //bool UpdateLabelMap { get; set; }

    ZigDepth Depth { get; }
    ZigImage Image { get; }
    ZigLabelMap LabelMap { get; }

    // misc
    Vector3 ConvertWorldToImageSpace(Vector3 worldPosition);
    Vector3 ConvertImageToWorldSpace(Vector3 imagePosition);
    bool AlignDepthToRGB { get; set; }
}

public class ZigTrackedUser
{
	List<GameObject> listeners = new List<GameObject>();

    public int Id { get; private set; }
    public bool PositionTracked { get; private set; }
    public Vector3 Position { get; private set; }
    public bool SkeletonTracked { get; private set; }
    public ZigInputJoint[] Skeleton { get; private set; }

	public ZigTrackedUser(ZigInputUser userData) {
        Skeleton = new ZigInputJoint[Enum.GetValues(typeof(ZigJointId)).Length];
        for (int i=0; i<Skeleton.Length; i++) {
            Skeleton[i] = new ZigInputJoint((ZigJointId)i);
        }
		Update(userData);
	}
		
	public void AddListener(GameObject listener) {
		listeners.Add(listener);
        listener.SendMessage("Zig_Attach", this, SendMessageOptions.DontRequireReceiver);
	}

    public void RemoveListener(GameObject listener) {
        if (null == listener) {
            listeners.Clear();
        }
        else {
            listeners.Remove(listener);
            listener.SendMessage("Zig_Detach", this, SendMessageOptions.DontRequireReceiver);
        }
    }
	
	public void Update(ZigInputUser userData) {
		Id = userData.Id;
        PositionTracked = true;
        Position = userData.CenterOfMass;
        SkeletonTracked = userData.Tracked;
        foreach (ZigInputJoint j in userData.SkeletonData) {
            Skeleton[(int)j.Id] = j;
        }
		notifyListeners("Zig_UpdateUser", this);
	}

    void notifyListeners(string msgname, object arg) {
        for (int i = 0; i < listeners.Count; ) {
            GameObject go = listeners[i];
            if (go) {
                go.SendMessage(msgname, arg, SendMessageOptions.DontRequireReceiver);
                i++;
            }
            else {
                listeners.RemoveAt(i);
            }
        }
    }
}

public enum ZigInputType {
    Auto,
	OpenNI,    
	KinectSDK,
    OpenNI2,
}

[Serializable]
public class ZigInputSettings
{
    public bool UpdateDepth = true;
    public bool UpdateImage = false;
    public bool UpdateLabelMap = false;
    public bool AlignDepthToRGB = false;
    public ZigSettingsOpenNI OpenNISpecific;
    public ZigSettingsKinectSDK KinectSDKSpecific;
    public bool showWebPlayerLogger = false;
}

[Serializable]
public class ZigSettingsOpenNI
{
    public bool Mirror = true;
    public bool UseXML = false;
    public string XMLPath = "SampleConfig.xml";
}

[Serializable]
public class KinectSDKSmoothingParameters
{
    public float Smoothing = 0.5f;
    public float Correction = 0.5f;
    public float Prediction = 0.5f;
    public float JitterRadius = 0.05f;
    public float MaxDeviationRadius = 0.04f;
}

[Serializable]
public class ZigSettingsKinectSDK
{
    public bool UseSDKSmoothing = false;
    public bool SeatedMode = false;
    public bool NearMode = false;
    public bool TrackSkeletonInNearMode = false;
    public bool EnableFaceTracking = false;
    public KinectSDKSmoothingParameters SmoothingParameters = new KinectSDKSmoothingParameters();    
}



//-----------------------------------------------------------------------------
// ZigInput
//
// This Singleton/Monobehaviour makes sure we get input from a depth cam. The
// input can be from OpenNI, KinectSDK, or Webplayer.
//
// The Singleton part ensures only one instance that crosses scene boundries.
// The monobehaviour part ensures we get runtime via Update()
//
// A ZigInput component will be implicitly added to any scene that requires
// depth input. Add it explicitly to change paramters or switch the input
// type. The component will persist between scenes so make sure to only add it
// to the first scene that will use the sensor
//-----------------------------------------------------------------------------

public class ZigInput : MonoBehaviour {

	//-------------------------------------------------------------------------
	// Watermark stuff
	//-------------------------------------------------------------------------
	
	#if WATERMARK_OMERCY
	
	Texture2D watermarkTexture;
	
	Texture2D LoadTextureFromResource(string name) {
		// open resource stream
		Stream s = this.GetType().Assembly.GetManifestResourceStream(name);
		if (null == s) {
			return null;
		}
		
		// read & close
		byte[] data = new byte[s.Length];
		s.Read(data, 0, data.Length);
		s.Close();
		
		// load into texture
		Texture2D result = new Texture2D(1,1);
		result.LoadImage(data);
		return result;
	}
	
	void OnGUI() {
		GUI.DrawTexture(new Rect(10, Screen.height - 10 - watermarkTexture.height, watermarkTexture.width, watermarkTexture.height), watermarkTexture);
	}
	
	#endif
		
	//-------------------------------------------------------------------------
	// Singleton logic
	//-------------------------------------------------------------------------
		
    //public static bool UpdateDepth;
    //public static bool UpdateImage;
    //public static bool UpdateLabelMap;
    //public static bool AlignDepthToRGB;

    public static ZigInputSettings Settings;

	public static ZigInputType InputType = ZigInputType.Auto;    
	static ZigInput instance;
	public static ZigInput Instance
	{
		get {
			if (null == instance) {
                instance = FindObjectOfType(typeof(ZigInput)) as ZigInput;
                if (null == instance) {
                    GameObject container = new GameObject();
					DontDestroyOnLoad (container);
                    container.name = "ZigInputContainer";
                    instance = container.AddComponent<ZigInput>();
                }
				DontDestroyOnLoad(instance);
            }
			return instance;
		}
	}

    public static ZigDepth Depth { get; private set; }
    public static ZigImage Image { get; private set; }
    public static ZigLabelMap LabelMap { get; private set; }

	//-------------------------------------------------------------------------
	// MonoBehaviour logic
	//-------------------------------------------------------------------------
	
	public List<GameObject> listeners = new List<GameObject>();
    public IZigInputReader reader;
	public bool ReaderInited { get; private set; }
    public bool kinectSDK = false;

    public ZigInputOpenNI getOpenNI()
    {
        return reader as ZigInputOpenNI;
    }
    public OpenNI2.ZigInputOpenNI2 getOpenNI2()
    {
        return reader as OpenNI2.ZigInputOpenNI2;
    }
    public ZigInputKinectSDK getKinectSDK()
    {
        return reader as ZigInputKinectSDK;
    }

    public OpenNI.HandsGenerator GetHands()
    {
        return ((ZigInputOpenNI)reader).Hands;
    }
    public OpenNI.GestureGenerator GetGestures()
    {
        return ((ZigInputOpenNI)reader).Gestures;
    }

	public void SetNearMode(bool NearMode)
    {
        if (!kinectSDK)
            return;       
        ZigInputKinectSDK r = reader as ZigInputKinectSDK;
        r.SetNearMode(NearMode);        
    }  
    public void SetSkeletonTrackingSettings(bool SeatedMode, bool TrackSkeletonInNearMode)
    {
        if (!kinectSDK)
            return;
        ZigInputKinectSDK r = reader as ZigInputKinectSDK;
        r.SetSkeletonTrackingSettings(SeatedMode, TrackSkeletonInNearMode);     
    }
    public void UpdateMaps()
    {
        ZigInput.Depth = reader.Depth;
        ZigInput.Image = reader.Image;
        ZigInput.LabelMap = reader.LabelMap;
    }

	void Awake() {
		#if WATERMARK_OMERCY
		watermarkTexture = LoadTextureFromResource("ZDK.wm.png");
		#endif
		
		// reader factory
		if (Application.isWebPlayer) {
            WebplayerLogger.Instance.showLogger = Settings.showWebPlayerLogger;
			reader = (new ZigInputWebplayer()) as IZigInputReader;
            ReaderInited = StartReader();
		} else {



            if (ZigInput.InputType == ZigInputType.Auto)
            {
                    print("Trying to open Kinect sensor using MS Kinect SDK");
                   
					print("Note: Microsoft's Kinect SDK can only be used in one process at a time. Please quit any other processes using the Microsoft Kinect");
					print("This also means you must quit the Unity Editor before using any compiled product");
						

					reader = (new ZigInputKinectSDK()) as IZigInputReader;
					

                    if (StartReader())
                    {
                        ReaderInited = true; // KinectSDK
                        kinectSDK = true;
                    }
                    else
                    {
                        print("failed opening Kinect SDK sensor (if you intend to use the Microsoft Kinect SDK, please unplug the sensor, restart Unity and try again)");
                        print("Trying to open sensor using OpenNI");

                        reader = (new ZigInputOpenNI()) as IZigInputReader;
                        if (StartReader())
                        {
                            ReaderInited = true;
                        }
                        else
                        {
                            print("failed opening sensor using OpenNI version 1, attempting to open the sensor with OpenNI 2");
                            reader = (new OpenNI2.ZigInputOpenNI2()) as IZigInputReader;
                            if (StartReader())
                            {
                                ReaderInited = true;
                            }
                            else
                            {
                                print("failed opening sensor using OpenNI version 2.");
                                print("Note that OpenNI2 requires you to move the Redist directories from C:\\Program Files\\PrimeSense\\NiTE2\\Redist and C:\\Program Files\\OpenNI2\\Redist to your Unity project's root directory.");
                                Debug.LogError("Failed to load driver and middleware, review warnings above for specific exception messages from middleware");
                            }
                        }
                    }
            }
            else
            {
                if (ZigInput.InputType == ZigInputType.OpenNI)
                {
                    print("Trying to open sensor using OpenNI");
                    reader = (new ZigInputOpenNI()) as IZigInputReader;
                }
                else if (ZigInput.InputType == ZigInputType.OpenNI2)
                {
                    print("Trying to open sensor using OpenNI2");
                    reader = (new OpenNI2.ZigInputOpenNI2()) as IZigInputReader;
                }
                else
                {
                    print("Trying to open Kinect sensor using MS Kinect SDK");
                    reader = (new ZigInputKinectSDK()) as IZigInputReader;
                    kinectSDK = true;
                }

                if (StartReader())
                {
                    ReaderInited = true;
                }
                else
                {
                    print("Note that OpenNI2 requires you to move the Redist directories from C:\\Program Files\\PrimeSense\\NiTE2\\Redist and C:\\Program Files\\OpenNI2\\Redist to your Unity project's root directory.");
                    Debug.LogError("Failed to load driver and middleware, consider setting the Zig Input Type to Auto");
                    
                }

            }    
		}
	}

    private bool StartReader()
    {

        
        //reader.UpdateDepth = ZigInput.UpdateDepth;
        //reader.UpdateImage = ZigInput.UpdateImage;
        //reader.UpdateLabelMap = ZigInput.UpdateLabelMap;
        //reader.AlignDepthToRGB = ZigInput.AlignDepthToRGB;

        try {
            reader.Init(ZigInput.Settings);
			reader.NewUsersFrame += HandleReaderNewUsersFrame;
            ZigInput.Depth = reader.Depth;
            ZigInput.Image = reader.Image;
            ZigInput.LabelMap = reader.LabelMap;
            return true;
        }
        catch (Exception ex) {
			print("Exception while attempting to start reader:");
            Debug.LogWarning(ex.Message);
            return false;
        }
    }

    // Update is called once per frame
	void Update () {
		if (ReaderInited) {
			reader.Update();
		}
	}
	
	void OnApplicationQuit()
	{
		if (ReaderInited) {
			reader.Shutdown();	
			ReaderInited = false;
		}
	}
	
	public void AddListener(GameObject listener)
	{
		if (!listeners.Contains(listener)) {
			listeners.Add(listener);
		}

		foreach (ZigTrackedUser user in TrackedUsers.Values) {
            listener.SendMessage("Zig_UserFound", user, SendMessageOptions.DontRequireReceiver);
		}
	}

	
	Dictionary<int, ZigTrackedUser> trackedUsers = new Dictionary<int, ZigTrackedUser>();
	
	public Dictionary<int, ZigTrackedUser> TrackedUsers { 
		get {
			return trackedUsers;
		}
	}
	
	void HandleReaderNewUsersFrame(object sender, NewUsersFrameEventArgs e)
	{
		// get rid of old users
		List<int> idsToRemove = new List<int>(trackedUsers.Keys);
		foreach (ZigInputUser user in e.Users) {
			idsToRemove.Remove(user.Id);
		}
		foreach (int id in idsToRemove) {
			ZigTrackedUser user = trackedUsers[id];
			trackedUsers.Remove(id);
			notifyListeners("Zig_UserLost", user);
		}
			
		// add new & update existing users
		foreach (ZigInputUser user in e.Users) {
			if (!trackedUsers.ContainsKey(user.Id)) {
				ZigTrackedUser trackedUser = new ZigTrackedUser(user);
				trackedUsers.Add(user.Id, trackedUser);
                notifyListeners("Zig_UserFound", trackedUser);
			} else {
				trackedUsers[user.Id].Update(user);
			}
		}
		
		notifyListeners("Zig_Update", this);
	}
	
	void notifyListeners(string msgname, object arg)
	{
       for(int i = 0; i < listeners.Count; ) {
           GameObject go = listeners[i];
           if (go) {
               go.SendMessage(msgname, arg, SendMessageOptions.DontRequireReceiver);
               i++;
           }
           else {
               listeners.RemoveAt(i);
           }
		}
	}
    //-------------------------------------------------------------------------
    // World <-> Image space conversions
    //-------------------------------------------------------------------------
    public static Vector3 ConvertImageToWorldSpace(Vector3 imagePosition)
    {
        return Instance.reader.ConvertImageToWorldSpace(imagePosition);
    }

    public static Vector3 ConvertWorldToImageSpace(Vector3 worldPosition)
    {
        return Instance.reader.ConvertWorldToImageSpace(worldPosition);
    }
}
