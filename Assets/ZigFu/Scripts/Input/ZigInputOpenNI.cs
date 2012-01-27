using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using OpenNI;

public class ZigInputOpenNI : IZigInputReader
{
	//-------------------------------------------------------------------------
	// IZigInputReader interface
	//-------------------------------------------------------------------------
	
	public void Init() 
	{	
		//handList = new Dictionary<int, HandPoint>();
		
        try {
			ScriptNode sn;
            this.OpenNIContext = LoadFromXML ? Context.CreateFromXmlFile(XMLFilename, out sn) : new Context();
        } catch (Exception ex) {
            throw new Exception("Error opening OpenNI context: " + ex.Message);
        }

        // add license manually if not loading from XML
        if (!LoadFromXML) {
            License ll = new License();
            ll.Key = LicenseKey;
            ll.Vendor = LicenseVendor;
            OpenNIContext.AddLicense(ll);
        }
		
		this.Depthmap = OpenNode(NodeType.Depth) as DepthGenerator;
		this.Imagemap = OpenNode(NodeType.Image) as ImageGenerator;
		this.Users = OpenNode(NodeType.User) as UserGenerator;
		this.Hands = OpenNode(NodeType.Hands) as HandsGenerator;
		this.Gestures = OpenNode(NodeType.Gesture) as GestureGenerator;
		
		this.OpenNIContext.GlobalMirror = Mirror;
        mirrorState = Mirror;
	
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
		
		// init textures
		Depth = new Texture2D(Depthmap.GetMetaData().XRes, Depthmap.GetMetaData().YRes);
		Image = new Texture2D(Imagemap.GetMetaData().XRes, Imagemap.GetMetaData().YRes);
	}
	
	public void Update()
	{
		if (null == OpenNIContext) return;
		
        if (Mirror != mirrorState) {
            OpenNIContext.GlobalMirror = Mirror;
            mirrorState = Mirror;
        }
		
		this.OpenNIContext.WaitNoneUpdateAll();
		
		if (lastDepthFrameId != Depthmap.FrameID) {
			lastDepthFrameId = Depthmap.FrameID;
			ProcessNewDepthFrame();
		}
		
		if (lastImageFrameId != Imagemap.FrameID) {
			lastImageFrameId = Imagemap.FrameID;
			ProcessNewImageFrame();
		}
	}
	
	public void Shutdown()
	{
		if (null == OpenNIContext) return;

		OpenNIContext.StopGeneratingAll();
		OpenNIContext.Release();
		OpenNIContext = null;
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
	
	public Context OpenNIContext { get; private set; }
    public DepthGenerator Depthmap { get; private set; }
	public ImageGenerator Imagemap { get; private set; }
	public UserGenerator Users { get; private set; }
	public HandsGenerator Hands { get; private set; }
	public GestureGenerator Gestures { get; private set; }

    // Default key is NITE license from OpenNI.org
    public string LicenseKey = "0KOIk2JeIBYClPWVnMoRKn5cdY4=";
    public string LicenseVendor = "PrimeSense";

    public bool LoadFromXML = false;
    public string XMLFilename = ".\\OpenNI.xml";
	
	public bool Mirror = true;
	bool mirrorState;
	
	int lastDepthFrameId;
	int lastImageFrameId;
	
    // Tries to get an existing node, or opening a new one
    // if we need to
	public ProductionNode OpenNode(NodeType nt)
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
	
	//public Dictionary<int, HandPoint> handList { get; private set; }
	void hands_HandCreate (object Sender, HandCreateEventArgs e)
	{
		//handList[e.UserID] = new HandPoint(e.UserID, WhichUserDoesThisPointBelongTo(e.Position), e.Position);
	}
	
	void hands_HandUpdate (object Sender, HandUpdateEventArgs e)
	{
		//handList[e.UserID].position = e.Position;
	}
	
	void hands_HandDestroy (object Sender, HandDestroyEventArgs e)
	{
		//handList.Remove(e.UserID);
	}
	
    public int WhichUserDoesThisPointBelongTo(Point3D point)
    {
        // get userID of user to whom the hand is attached
        Point3D ProjectiveHandPoint = Depthmap.ConvertRealWorldToProjective(point);
        SceneMetaData sceneMD = Users.GetUserPixels(0);
        return sceneMD[(int)ProjectiveHandPoint.X, (int)ProjectiveHandPoint.Y];
    }
	
	static Vector3 Point3DToVector3(Point3D pos)
	{
		return new Vector3(pos.X, pos.Y, -pos.Z);
	}

	static Quaternion OrientationToQuaternion(SkeletonJointOrientation ori)
	{
		// Z coordinate in OpenNI is opposite from Unity
		// Convert the OpenNI 3x3 rotation matrix to unity quaternion while reversing the Z axis
		Vector3 worldYVec = new Vector3(ori.Y1, ori.Y2, -ori.Y3);
		Vector3 worldZVec = new Vector3(-ori.Z1, -ori.Z2, ori.Z3);
		return Quaternion.LookRotation(worldZVec, worldYVec);
	}

	
	private void ProcessNewDepthFrame() 
	{
		if (UpdateDepth) {
			// TODO
		}
		
		// foreach user
		int[] userids = Users.GetUsers();
		List<ZigInputUser> users = new List<ZigInputUser>();
		foreach (int userid in userids) {
			// skeleton data
			List<ZigInputJoint> joints = new List<ZigInputJoint>();
			bool tracked = Users.SkeletonCapability.IsTracking(userid);
			if (tracked) {
				SkeletonCapability skelCap = Users.SkeletonCapability;
				SkeletonJointTransformation skelTrans;
				// foreach joint
				foreach (SkeletonJoint sj in Enum.GetValues(typeof(SkeletonJoint))) {
					if (skelCap.IsJointAvailable(sj)) {
						skelTrans = skelCap.GetSkeletonJoint(userid, sj);
						
						ZigInputJoint joint = new ZigInputJoint((ZigJointId)sj);
						if (skelTrans.Orientation.Confidence > 0.5f) {
							joint.Rotation = OrientationToQuaternion(skelTrans.Orientation);
							joint.GoodRotation = true;
						}
						if (skelTrans.Position.Confidence > 0.5f) {
							joint.Position = Point3DToVector3(skelTrans.Position.Position);
							joint.GoodPosition = true;
						}
						joints.Add(joint);
					}
				}
			}
			
			ZigInputUser user = new ZigInputUser(userid, Point3DToVector3(Users.GetCoM(userid)));
			user.Tracked = tracked;
			user.SkeletonData = joints;
			users.Add(user);
		}
		
		OnNewUsersFrame(users);
		
		/*
		ArrayList hands = new ArrayList();
		foreach (HandPoint hp in OpenNIReader.Instance.handList.Values) {
			Hashtable hand = new Hashtable();
			hand["id"] = hp.handId;
			hand["userid"] = hp.userId;
			hand["position"] = Point3DToArrayList(hp.position);
			hands.Add(hand);
		}*/
	}
	
	private void ProcessNewImageFrame()
	{
		
	}
}



