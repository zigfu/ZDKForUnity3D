using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using OpenNI;


public class ZigInputOpenNI : IZigInputReader
{
	//-------------------------------------------------------------------------
	// IZigInputReader interface
	//-------------------------------------------------------------------------
	
	public void Init(ZigInputSettings settings) 
	{	
		//handList = new Dictionary<int, HandPoint>();
        UpdateDepth = settings.UpdateDepth;
        UpdateImage = settings.UpdateImage;
        UpdateLabelMap = settings.UpdateLabelMap;
        AlignDepthToRGB = settings.AlignDepthToRGB;
        Mirror = settings.OpenNISpecific.Mirror;
        LoadFromXML = settings.OpenNISpecific.UseXML;
        XMLFilename = settings.OpenNISpecific.XMLPath;

        try {
			ScriptNode sn;
            this.OpenNIContext = LoadFromXML ? Context.CreateFromXmlFile(XMLFilename, out sn) : new Context();
        } catch (Exception ex) {
            Debug.LogError(ex);
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

        this.Users = OpenNode(NodeType.User) as UserGenerator;
        this.Hands = OpenNode(NodeType.Hands) as HandsGenerator;
        this.Gestures = OpenNode(NodeType.Gesture) as GestureGenerator;
        this.userExitList = new List<int>();

        if (!LoadFromXML) {
            this.OpenNIContext.GlobalMirror = Mirror;
            mirrorState = Mirror;
        }

        // users stuff
        this.Users.SkeletonCapability.SetSkeletonProfile(SkeletonProfile.All);
        this.Users.NewUser += new EventHandler<NewUserEventArgs>(userGenerator_NewUser);
        this.Users.LostUser += new EventHandler<UserLostEventArgs>(userGenerator_LostUser);
        this.Users.UserExit += new EventHandler<UserExitEventArgs>(userGenerator_UserExit);
        this.Users.UserReEnter += new EventHandler<UserReEnterEventArgs>(userGenerator_UserReEnter);

        this.Users.PoseDetectionCapability.PoseDetected += new EventHandler<PoseDetectedEventArgs>(poseDetectionCapability_PoseDetected);
        this.Users.SkeletonCapability.CalibrationComplete += new EventHandler<CalibrationProgressEventArgs>(skeletonCapbility_CalibrationComplete);

        this.Depth = new ZigDepth(Depthmap.GetMetaData().XRes, Depthmap.GetMetaData().YRes);
        try {
            this.Imagemap = OpenNode(NodeType.Image) as ImageGenerator;
            this.Image = new ZigImage(Imagemap.GetMetaData().XRes, Imagemap.GetMetaData().YRes);
        }
        catch (OpenNI.GeneralException) {
            this.Imagemap = null;
            this.Image = new ZigImage(320, 240); //hard code the shit;
        }

        if ((Imagemap != null) && AlignDepthToRGB && (!LoadFromXML)) {
            Depthmap.AlternativeViewpointCapability.SetViewpoint(Imagemap);
        }

        this.LabelMap = new ZigLabelMap(Depth.xres, Depth.yres);
        rawImageMap = new byte[Image.xres * Image.yres * 3];
	}
	
	public void Update()
	{
		if (null == OpenNIContext) return;
		
        if ((!LoadFromXML) && (Mirror != mirrorState)) {
            OpenNIContext.GlobalMirror = Mirror;
            mirrorState = Mirror;
        }
		
		this.OpenNIContext.WaitNoneUpdateAll();
		
		if (lastDepthFrameId != Depthmap.FrameID) {
			lastDepthFrameId = Depthmap.FrameID;
			ProcessNewDepthFrame();
		}
        if (Imagemap != null) {
            if (lastImageFrameId != Imagemap.FrameID) {
                lastImageFrameId = Imagemap.FrameID;
                ProcessNewImageFrame();
            }
        }
        if (lastLabelMapFrameId != Users.FrameID) {
			lastLabelMapFrameId = Users.FrameID;
			ProcessNewLabelMapFrame();
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

    public ZigDepth Depth { get; private set; }
    public ZigImage Image { get; private set; }
    public ZigLabelMap LabelMap { get; private set; }

	public bool UpdateDepth { get; set; }
	public bool UpdateImage { get; set; }
    public bool UpdateLabelMap { get; set; }

    // RW <-> projective conversions
    public Vector3 ConvertWorldToImageSpace(Vector3 worldPosition)
    {
        Point3D pt = Depthmap.ConvertRealWorldToProjective(new Point3D(worldPosition.x, worldPosition.y, worldPosition.z));
        return new Vector3(pt.X, pt.Y, pt.Z);
    }
    public Vector3 ConvertImageToWorldSpace(Vector3 imagePosition)
    {
        Point3D pt = Depthmap.ConvertProjectiveToRealWorld(new Point3D(imagePosition.x, imagePosition.y, imagePosition.z));
        return new Vector3(pt.X, pt.Y, pt.Z);
    }

    //TODO: make this updateable in runtime
    public bool AlignDepthToRGB { get; set; }

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

    byte[] rawImageMap;

	int lastDepthFrameId;
	int lastImageFrameId;
    int lastLabelMapFrameId;

    List<int> userExitList;
    

    // Tries to get an existing node, or opening a new one
    // if we need to
	public ProductionNode OpenNode(NodeType nt)
	{
        if (null == OpenNIContext) return null;

		ProductionNode ret=null;
		try {
			ret = OpenNIContext.FindExistingNode(nt);
		} catch { // if exception, ret is still null
		}
        if (null == ret) {
            ret = OpenNIContext.CreateAnyProductionTree(nt, null);
            Generator g = ret as Generator;
            if (null != g) {
                g.StartGenerating();
            }
        }
        return ret;
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

    void userGenerator_UserExit(object sender, UserExitEventArgs e)
    {
        //stop checking for pose and stop tracking
      /*  if (Users.SkeletonCapability.DoesNeedPoseForCalibration)
        {
            Users.PoseDetectionCapability.StopPoseDetection(e.ID);            
        }
        else
        {
            Users.SkeletonCapability.StopTracking(e.ID);
        }*/
        userExitList.Add(e.ID);

    }

    void userGenerator_UserReEnter(object sender, UserReEnterEventArgs e)
    {
        //start Tracking again
      /*  if (Users.SkeletonCapability.DoesNeedPoseForCalibration)
        {
            Users.PoseDetectionCapability.StartPoseDetection(Users.SkeletonCapability.CalibrationPose, e.ID);
        }
        else
        {
            Users.SkeletonCapability.StartTracking(e.ID);
        }*/
        userExitList.Remove(e.ID);
    }
    
    void userGenerator_LostUser(object sender, UserLostEventArgs e)
    {
        userExitList.Remove(e.ID);
    }    

    void userGenerator_NewUser(object sender, NewUserEventArgs e)
    {
		if (Users.SkeletonCapability.DoesNeedPoseForCalibration) {
        	Users.PoseDetectionCapability.StartPoseDetection(Users.SkeletonCapability.CalibrationPose, e.ID);
		} else {
			Users.SkeletonCapability.StartTracking(e.ID);
		}
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

	private void ProcessNewDepthFrame() {
		if (UpdateDepth) {
            Marshal.Copy(Depthmap.DepthMapPtr, Depth.data, 0, Depth.data.Length);
		}
		
		// foreach user
		int[] userids = Users.GetUsers();
		List<ZigInputUser> users = new List<ZigInputUser>();
		foreach (int userid in userids) {
            if (!userExitList.Contains(userid))
            {
                // skeleton data
                List<ZigInputJoint> joints = new List<ZigInputJoint>();
                bool tracked = Users.SkeletonCapability.IsTracking(userid);
                if (tracked)
                {
                    SkeletonCapability skelCap = Users.SkeletonCapability;
                    SkeletonJointTransformation skelTrans;
                    // foreach joint
                    foreach (SkeletonJoint sj in Enum.GetValues(typeof(SkeletonJoint)))
                    {
                        if (skelCap.IsJointAvailable(sj))
                        {
                            skelTrans = skelCap.GetSkeletonJoint(userid, sj);

                            ZigInputJoint joint = new ZigInputJoint((ZigJointId)sj);
                            if (skelTrans.Orientation.Confidence > 0.5f)
                            {
                                joint.Rotation = OrientationToQuaternion(skelTrans.Orientation);
                                joint.GoodRotation = true;
                            }
                            if (skelTrans.Position.Confidence > 0.5f)
                            {
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
		}
		
		OnNewUsersFrame(users);
	}
	private void ProcessNewLabelMapFrame() {
        if (UpdateLabelMap)
        {
            SceneMetaData sceneMD = Users.GetUserPixels(0);
            Marshal.Copy(sceneMD.LabelMapPtr, LabelMap.data, 0, LabelMap.xres * LabelMap.yres);
        }
    }
	private void ProcessNewImageFrame()	{
        if (UpdateImage) {
            Marshal.Copy(Imagemap.ImageMapPtr, rawImageMap, 0, rawImageMap.Length);
            int rawi=0;
            for (int i = 0; i < Image.data.Length; i++, rawi+=3) {
                Image.data[i].r = rawImageMap[rawi];
                Image.data[i].g = rawImageMap[rawi+1];
                Image.data[i].b = rawImageMap[rawi+2];
            }
        }
	}
}



