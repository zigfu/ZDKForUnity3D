using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using OpenNI;

public class HandPoint
{
	public int handId { get; private set; }
	public int userId { get; private set; }
	public Point3D position;
	
	public HandPoint(int handId, int userId, Point3D position)
	{
		this.handId = handId;
		this.userId = userId;
		this.position = position;
	}
}

class NewFrameHandlerContext
{
	public Generator node;
	public List<GameObject> listeners = new List<GameObject>();
	public int lastFrameId;
}

public class OpenNIReader : MonoBehaviour
{
    // singleton stuff
	static OpenNIReader instance;
	public static OpenNIReader Instance
	{
		get {
			if (null == instance) {
                instance = FindObjectOfType(typeof(OpenNIReader)) as OpenNIReader;
                if (null == instance) {
                    GameObject container = new GameObject();
					DontDestroyOnLoad (container);
                    container.name = "OpenNIReaderContainer";
                    instance = container.AddComponent<OpenNIReader>();
                }
				DontDestroyOnLoad(instance);
            }
			return instance;
		}
	}
	
	public Context OpenNIContext { get; private set; }
    public DepthGenerator Depth { get; private set; }
	public ImageGenerator Image { get; private set; }
	public UserGenerator Users { get; private set; }
	public HandsGenerator Hands { get; private set; }
	public GestureGenerator Gestures { get; private set; }

	public bool LoadFromRecording = false;
	public string RecordingFilename = "";
	public float RecordingFramerate = 30.0f;

    // Default key is NITE license from OpenNI.org
    public string LicenseKey = "0KOIk2JeIBYClPWVnMoRKn5cdY4=";
    public string LicenseVendor = "PrimeSense";

    public bool LoadFromXML = false;
    public string XMLFilename = ".\\OpenNI.xml";
	
	public bool Mirror = true;
	bool mirrorState;
	
    // Tries to get an existing node, or opening a new one
    // if we need to
	private ProductionNode openNode(NodeType nt)
	{
        if (null == OpenNIContext) return null;

		ProductionNode ret=null;
		try {
			ret = OpenNIContext.FindExistingNode(nt);
		} catch {
			ret = OpenNIContext.CreateAnyProductionTree(nt, null);
			Generator g = ret as Generator;
			if (null != g) {
				g.StartGenerating();
			}
		}
		return ret;
	}
	
	public static ProductionNode OpenNode(NodeType nt)
	{
		return Instance.openNode(nt);
	}
	
	public void Awake()
	{
		handList = new Dictionary<int, HandPoint>();
		
        Debug.Log("Initing OpenNI" + (LoadFromXML ? "(" + XMLFilename + ")" : ""));
        try {
			ScriptNode sn;
            this.OpenNIContext = LoadFromXML ? Context.CreateFromXmlFile(XMLFilename, out sn) : new Context();
        } catch (Exception ex) {
            Debug.LogError("Error opening OpenNI context: " + ex.Message);
            return;
        }

        // add license manually if not loading from XML
        if (!LoadFromXML) {
            License ll = new License();
            ll.Key = LicenseKey;
            ll.Vendor = LicenseVendor;
            OpenNIContext.AddLicense(ll);
        }

		if (LoadFromRecording)
		{
			OpenNIContext.OpenFileRecordingEx(RecordingFilename);
			Player player = openNode(NodeType.Player) as Player;
			player.PlaybackSpeed = 0.0;
			StartCoroutine(ReadNextFrameFromRecording(player));
		}
		
		this.Depth = openNode(NodeType.Depth) as DepthGenerator;
		this.Image = openNode(NodeType.Image) as ImageGenerator;
		this.Users = openNode(NodeType.User) as UserGenerator;
		this.Hands = openNode(NodeType.Hands) as HandsGenerator;
		this.Gestures = openNode(NodeType.Gesture) as GestureGenerator;
		
        if (!LoadFromRecording) {
			this.OpenNIContext.GlobalMirror = Mirror;
            mirrorState = Mirror;
        }
		
		// users stuff
		this.Users.SkeletonCapability.SetSkeletonProfile(SkeletonProfile.All);
        this.Users.NewUser += new EventHandler<NewUserEventArgs>(userGenerator_NewUser);
        this.Users.LostUser += new EventHandler<UserLostEventArgs>(userGenerator_LostUser);
        this.Users.PoseDetectionCapability.PoseDetected += new EventHandler<PoseDetectedEventArgs>(poseDetectionCapability_PoseDetected);
		this.Users.SkeletonCapability.CalibrationComplete += new EventHandler<CalibrationProgressEventArgs>(skeletonCapbility_CalibrationComplete);
		
		// hands stuff
		this.Hands.HandCreate += new EventHandler<HandCreateEventArgs>(hands_HandCreate);
		this.Hands.HandUpdate += new EventHandler<HandUpdateEventArgs>(hands_HandUpdate);
		this.Hands.HandDestroy += new EventHandler<HandDestroyEventArgs>(hands_HandDestroy);
		
		this.Gestures.AddGesture("Wave");
		this.Gestures.AddGesture("Click");
        //this.gestures.AddGesture("RaiseHand");
		this.Gestures.GestureRecognized += new EventHandler<GestureRecognizedEventArgs> (gestures_GestureRecognized);
	}
	
	void gestures_GestureRecognized (object Sender, GestureRecognizedEventArgs e)
	{
        if (e.Gesture == "RaiseHand") {
            int user = WhichUserDoesThisPointBelongTo(e.IdentifiedPosition);
            if (0 == user) {
                // false positive if no one raised their hand, miss detect if user
                // isn't on usermap (at this during this frame at the gesture position)
                return;
            }

            // TODO: make sure point is in a good position relative to the CoM of the user
            // TODO: possibly take top user point into account?
            //Vector3 CoM = Point3DToVector3(userGenerator.GetCoM(user));
            //Vector3 gesturePoint = Point3DToVector3(e.IdentifiedPosition);
            this.Hands.StartTracking(e.IdentifiedPosition);
        }
	
		// so called "External hand tracker"
		if (e.Gesture == "Wave" || e.Gesture == "Click") {
			this.Hands.StartTracking (e.IdentifiedPosition);
		}
	}

	
	void skeletonCapbility_CalibrationComplete(object sender, CalibrationProgressEventArgs e)
    {
        if (e.Status == 0) {
            Users.SkeletonCapability.StartTracking(e.ID);
        } else {
            Users.PoseDetectionCapability.StartPoseDetection(Users.SkeletonCapability.CalibrationPose, e.ID);
        }
    }

    void poseDetectionCapability_PoseDetected(object sender, PoseDetectedEventArgs e)
    {
        Users.PoseDetectionCapability.StopPoseDetection(e.ID);
        Users.SkeletonCapability.RequestCalibration(e.ID, true);
    }

    void userGenerator_LostUser(object sender, UserLostEventArgs e)
    {
    }

    void userGenerator_NewUser(object sender, NewUserEventArgs e)
    {
		if (Users.SkeletonCapability.DoesNeedPoseForCalibration) {
        	Users.PoseDetectionCapability.StartPoseDetection(Users.SkeletonCapability.CalibrationPose, e.ID);
		} else {
			Users.SkeletonCapability.StartTracking(e.ID);
		}
    }
	
	public Dictionary<int, HandPoint> handList { get; private set; }
	void hands_HandCreate (object Sender, HandCreateEventArgs e)
	{
		handList[e.UserID] = new HandPoint(e.UserID, WhichUserDoesThisPointBelongTo(e.Position), e.Position);
	}
	
	void hands_HandUpdate (object Sender, HandUpdateEventArgs e)
	{
		handList[e.UserID].position = e.Position;
	}
	
	void hands_HandDestroy (object Sender, HandDestroyEventArgs e)
	{
		handList.Remove(e.UserID);
	}
	
    public int WhichUserDoesThisPointBelongTo(Point3D point)
    {
        // get userID of user to whom the hand is attached
        Point3D ProjectiveHandPoint = Depth.ConvertRealWorldToProjective(point);
        SceneMetaData sceneMD = Users.GetUserPixels(0);
        return sceneMD[(int)ProjectiveHandPoint.X, (int)ProjectiveHandPoint.Y];
    }

	
	IEnumerator ReadNextFrameFromRecording(Player player)
	{
		while (true) {
			float waitTime = 1.0f / RecordingFramerate;
			yield return new WaitForSeconds (waitTime);
			player.ReadNext();
		}
	}
	
	
	Dictionary<NodeType, NewFrameHandlerContext> newFrameHandlers = new Dictionary<NodeType, NewFrameHandlerContext>();
	
	public void AddNewFrameHandler(NodeType type, GameObject target)
	{
		if (!newFrameHandlers.ContainsKey(type)) {
			newFrameHandlers[type] = new NewFrameHandlerContext();
			newFrameHandlers[type].node = openNode(type) as Generator;
		}
		newFrameHandlers[type].listeners.Add(target);
	}
	
	void Update () 
	{
        if (null == OpenNIContext) return;
        if (Mirror != mirrorState && !LoadFromRecording) {
            this.OpenNIContext.GlobalMirror = Mirror;
            mirrorState = Mirror;
        }
		this.OpenNIContext.WaitNoneUpdateAll();
		
		foreach (KeyValuePair<NodeType, NewFrameHandlerContext> handlers in newFrameHandlers) {
			if (handlers.Value.lastFrameId != handlers.Value.node.FrameID) {
				handlers.Value.lastFrameId = handlers.Value.node.FrameID;
				foreach (GameObject go in handlers.Value.listeners) {
					//TODO: Take care of invalid gameobjects (remove from list)
					go.SendMessage("OpenNI_NewFrame", handlers.Key, SendMessageOptions.RequireReceiver);
				}
			}
		}
	}
	
	public void OnApplicationQuit()
	{
        if (null == OpenNIContext) return;

		if (!LoadFromRecording) 
		{
			OpenNIContext.StopGeneratingAll();
		}
		// shutdown is deprecated, but Release doesn't do the job
		OpenNIContext.Shutdown();
		OpenNIContext = null;
		OpenNIReader.instance = null;
		Debug.Log("OpenNI shut down");
	}
}
