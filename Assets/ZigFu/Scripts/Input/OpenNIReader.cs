using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using OpenNI;

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
		
        if (!LoadFromRecording) {
			this.OpenNIContext.GlobalMirror = Mirror;
            mirrorState = Mirror;
        }
		
		this.Users.SkeletonCapability.SetSkeletonProfile(SkeletonProfile.All);
        this.Users.NewUser += new EventHandler<NewUserEventArgs>(userGenerator_NewUser);
        this.Users.LostUser += new EventHandler<UserLostEventArgs>(userGenerator_LostUser);
        this.Users.PoseDetectionCapability.PoseDetected += new EventHandler<PoseDetectedEventArgs>(poseDetectionCapability_PoseDetected);
		this.Users.SkeletonCapability.CalibrationComplete += new EventHandler<CalibrationProgressEventArgs>(skeletonCapbility_CalibrationComplete);
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
	
	IEnumerator ReadNextFrameFromRecording(Player player)
	{
		while (true)
		{
			float waitTime = 1.0f / RecordingFramerate;
			yield return new WaitForSeconds (waitTime);
			player.ReadNext();
		}
	}
	
	
	Dictionary<NodeType, List<GameObject>> newFrameHandlers = new Dictionary<NodeType, List<GameObject>>();
	public void AddNewFrameHandler(NodeType type, GameObject target)
	{
		if (!newFrameHandlers.ContainsKey(type)) {
			newFrameHandlers[type] = new List<GameObject>();
		}
		newFrameHandlers[type].Add(target);
	}
	
	void Update () 
	{
        if (null == OpenNIContext) return;
        if (Mirror != mirrorState && !LoadFromRecording) {
            this.OpenNIContext.GlobalMirror = Mirror;
            mirrorState = Mirror;
        }
		this.OpenNIContext.WaitNoneUpdateAll();
		
		foreach (KeyValuePair<NodeType, List<GameObject>> listeners in newFrameHandlers) {
			foreach (GameObject go in listeners.Value) {
				//TODO: Take care of invalid gameobjects (remove from list)
				go.SendMessage("OpenNI_NewFrame", listeners.Key, SendMessageOptions.RequireReceiver);
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
