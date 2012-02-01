using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

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
	
	public ZigInputJoint(ZigJointId id) :
		this(id, Vector3.zero, Quaternion.identity) {}
	
	public ZigInputJoint(ZigJointId id, Vector3 position, Quaternion rotation) {
		Id = id;
		Position = position;
		Rotation = rotation;
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

public interface IZigInputReader
{
	// init/update/shutdown
	void Init();
	void Update();
	void Shutdown();
	
	// users & hands
	event EventHandler<NewUsersFrameEventArgs> NewUsersFrame;
	
	// textures
	Texture2D GetDepth();
	Texture2D GetImage();
	bool UpdateDepth { get; set; }
	bool UpdateImage { get; set; }

    //Texture2D ImageThing { get; }
}

public class ZigTrackedUser
{
	public ZigInputUser UserData {get; private set;}
	List<GameObject> listeners = new List<GameObject>();
	
	public ZigTrackedUser(ZigInputUser userData) {
		UserData = userData;
	}
		
	public void AddListener(GameObject listener) {
		listeners.Add(listener);
	}
	
	public void Update(ZigInputUser userData) {
		UserData = userData;
		notifyListeners("Zig_UpdateUser", this);
	}
	
	void notifyListeners(string msgname, object arg)
	{
		foreach (GameObject go in listeners) {
			go.SendMessage(msgname, arg, SendMessageOptions.DontRequireReceiver);
		}
	}
}

public enum ZigInputType {
	OpenNI,
	KinectSDK,
}

public class ZigInput : MonoBehaviour {
	
	//-------------------------------------------------------------------------
	// Singleton logic
	//-------------------------------------------------------------------------
		
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
	
	//-------------------------------------------------------------------------
	// MonoBehaviour logic
	//-------------------------------------------------------------------------
	
	//public ZigUserTracker userTracker;
	public ZigInputType inputType = ZigInputType.KinectSDK;
	public List<GameObject> listeners = new List<GameObject>();

    public IZigInputReader reader { get; private set; }
	public bool ReaderInited { get; private set; }
	
	void Awake() {

		#if UNITY_WEBPLAYER
		#if UNITY_EDITOR
		
		Debug.LogError("Depth camera input will not work in editor when target platform is Webplayer. Please change target platform to PC/Mac standalone.");	
		return;
		
		#endif
		#endif

		// reader factory
		if (Application.isWebPlayer) {
			reader = (new ZigInputWebplayer()) as IZigInputReader;
		} else {
			switch (inputType) {
			case ZigInputType.OpenNI:
				reader = (new ZigInputOpenNI()) as IZigInputReader;
				break;
			case ZigInputType.KinectSDK:
				reader = (new ZigInputKinectSDK()) as IZigInputReader;
				break;
			}
		}
		
		reader.NewUsersFrame += HandleReaderNewUsersFrame;
		reader.UpdateDepth = true;
        reader.UpdateImage = true;
		
		try {
			reader.Init();
			ReaderInited = true;
		} catch (Exception ex) {
			Debug.LogError(ex.Message);
			Logger.Log(ex.Message);
		}
	}
    public Texture2D Depth
    {
        get
        {
            if (!ReaderInited) return null;
            return reader.GetDepth();
        }
    }
    public Texture2D Image
    {
        get
        {
            if (!ReaderInited) return null;
            return reader.GetImage();
        }
    }
    public bool UpdateDepth
    {
        get
        {
            if (!ReaderInited) return false;
            return reader.UpdateDepth;
        }
        set
        {
            if (ReaderInited) reader.UpdateDepth = value;
        }
    }
    public bool UpdateImage
    {
        get
        {
            if (!ReaderInited) return false;
            return reader.UpdateImage;
        }
        set
        {
            if (ReaderInited) reader.UpdateImage = value;
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
			listener.SendMessage("Zig_NewUser", user);
		}
	}

	
	Dictionary<int, ZigTrackedUser> trackedUsers = new Dictionary<int, ZigTrackedUser>();
	// TODO: return a readonly IDictionary
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
			notifyListeners("Zig_LostUser", user);
		}
			
		// add new & update existing users
		foreach (ZigInputUser user in e.Users) {
			if (!trackedUsers.ContainsKey(user.Id)) {
				ZigTrackedUser trackedUser = new ZigTrackedUser(user);
				trackedUsers.Add(user.Id, trackedUser);
				notifyListeners("Zig_NewUser", user);
			} else {
				trackedUsers[user.Id].Update(user);
			}
		}
		
		notifyListeners("Zig_Update", this);
	}
	
	void notifyListeners(string msgname, object arg)
	{
		foreach (GameObject go in listeners) {
			go.SendMessage(msgname, arg, SendMessageOptions.DontRequireReceiver);
		}
	}
}
