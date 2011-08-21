using UnityEngine;
using System;
using System.Collections;
using OpenNI;

public class OpenNIContext : MonoBehaviour
{
    // singleton stuff
	static OpenNIContext instance;
	public static OpenNIContext Instance
	{
		get 
		{
			if (null == instance)
            {
                instance = FindObjectOfType(typeof(OpenNIContext)) as OpenNIContext;
                if (null == instance)
                {
                    GameObject container = new GameObject();
					DontDestroyOnLoad (container);
                    container.name = "OpenNIContextContainer";
                    instance = container.AddComponent<OpenNIContext>();
                }
				DontDestroyOnLoad(instance);
            }
			return instance;
		}
	}
	
	private Context context;
	public static Context Context 
	{
		get { return Instance.context; }
	}

    public DepthGenerator Depth { get; private set; }

	/*
    public bool Mirror
	{
		get { return mirrorCap.IsMirrored(); }
		set { if (!LoadFromRecording) mirrorCap.SetMirror(value); }
	}
	
	public static bool Mirror
	{
		get { return Instance.Mirror; }
		set { Instance.Mirror = value; }
	}
	*/

    private bool mirrorState;
    public bool Mirror;

	private MirrorCapability mirrorCap;
	
	public bool LoadFromRecording = false;
	public string RecordingFilename = "";
	public float RecordingFramerate = 30.0f;

    // Default key is NITE license from OpenNI.org
    public string LicenseKey = "0KOIk2JeIBYClPWVnMoRKn5cdY4=";
    public string LicenseVendor = "PrimeSense";

    public bool LoadFromXML = false;
    public string XMLFilename = ".\\OpenNI.xml";
	
	public OpenNIContext()
	{
	}

    // Tries to get an existing node, or opening a new one
    // if we need to
	private ProductionNode openNode(NodeType nt)
	{
        if (null == context) return null;

		ProductionNode ret=null;
		try
		{
			ret = context.FindExistingNode(nt);
		}
		catch
		{
			ret = context.CreateAnyProductionTree(nt, null);
			Generator g = ret as Generator;
			if (null != g)
			{
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
            this.context = LoadFromXML ? new Context(XMLFilename) : new Context();
        }
        catch (Exception ex) {
            Debug.LogError("Error opening OpenNI context: " + ex.Message);
            return;
        }

        // add license manually if not loading from XML
        if (!LoadFromXML) {
            License ll = new License();
            ll.Key = LicenseKey;
            ll.Vendor = LicenseVendor;
            context.AddLicense(ll);
        }

		if (LoadFromRecording)
		{
			context.OpenFileRecordingEx(RecordingFilename);
			Player player = openNode(NodeType.Player) as Player;
			player.PlaybackSpeed = 0.0;
			StartCoroutine(ReadNextFrameFromRecording(player));
		}
		
		this.Depth = openNode(NodeType.Depth) as DepthGenerator;
		this.mirrorCap = this.Depth.MirrorCapability;
        if (!LoadFromRecording) {
            this.mirrorCap.SetMirror(Mirror);
            mirrorState = Mirror;
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
	
	// (Since we add OpenNIContext singleton to a container GameObject, we get the MonoBehaviour functionality)
	public void Update () 
	{
        if (null == context) return;
        if (Mirror != mirrorState) {
            mirrorCap.SetMirror(Mirror);
            mirrorState = Mirror;
        }
		this.context.WaitNoneUpdateAll();
	}
	
	public void OnApplicationQuit()
	{
        if (null == context) return;

		if (!LoadFromRecording) 
		{
			context.StopGeneratingAll();
		}
		// shutdown is deprecated, but Release doesn't do the job
		context.Shutdown();
		context = null;
		OpenNIContext.instance = null;
	}
}
