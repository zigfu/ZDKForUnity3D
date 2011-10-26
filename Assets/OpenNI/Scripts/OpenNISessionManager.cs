using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using OpenNI;

public class OpenNISessionManager : MonoBehaviour {
	// singleton
    static OpenNISessionManager instance;
    public static OpenNISessionManager Instance
    {
        get
        {
            if (null == instance) {
                instance = FindObjectOfType(typeof(OpenNISessionManager)) as OpenNISessionManager;
                if (null == instance) {
                    GameObject container = new GameObject();
                    DontDestroyOnLoad(container);
                    container.name = "SessionManagerContainer";
                    instance = container.AddComponent<OpenNISessionManager>();
                }
                DontDestroyOnLoad(instance);
            }
            return instance;
        }
    }


    public Vector3 focusPoint { get; private set; }
    public Point3D lastRawPoint { get; private set; }
    public bool inSession { get; private set; }

	public static Vector3 FocusPoint
	{
		get { return Instance.focusPoint; }
	}

    public static Point3D LastRawPoint
    {
        get { return Instance.lastRawPoint; }
    }

    public static Vector3 HandPosition
    {
        get { return Point3DToVector3(Instance.handPos); }
    }

	public static bool InSession
	{
		get { return (instance != null && Instance.inSession); }
	}

	public bool DetectWave = true;
	public bool DetectPush = true;
    public bool ExperimentalGestureless = false;
    public bool StealOnWave = true;

    public bool RotateToUser = true;
    public bool SessionBoundingBox = true;
    public Vector3 SessionBounds = new Vector3(1000, 700, 1000);

    private Bounds currentSessionBounds;
    private List<GameObject> Listeners = new List<GameObject>();

	private int handId = -1;
	private Point3D handPos = new Point3D(0,0,0);
	
	private HandsGenerator hands { get { return OpenNIContext.OpenNode(NodeType.Hands) as HandsGenerator; }}
	private GestureGenerator gestures { get { return OpenNIContext.OpenNode(NodeType.Gesture) as GestureGenerator; }}
	
	void Start()
	{
		this.hands.HandCreate += new EventHandler<HandCreateEventArgs>(hands_HandCreate);
		this.hands.HandUpdate += new EventHandler<HandUpdateEventArgs>(hands_HandUpdate);
		this.hands.HandDestroy += new EventHandler<HandDestroyEventArgs>(hands_HandDestroy);
		
		if (DetectWave) {
			this.gestures.AddGesture ("Wave");
		}
		if (DetectPush) {
			this.gestures.AddGesture ("Click");
		}

        if (ExperimentalGestureless) {
            this.gestures.AddGesture("RaiseHand");
        }

		this.gestures.GestureRecognized += new EventHandler<GestureRecognizedEventArgs> (gestures_GestureRecognized);
		
		if (RotateToUser) {
			if (null == userGenerator) {
				userGenerator = OpenNIContext.OpenNode(NodeType.User) as UserGenerator;
			}
		}
	}
		
	void gestures_GestureRecognized (object Sender, GestureRecognizedEventArgs e)
	{
        // wave stealing
        if (handId != -1 && e.Gesture == "Wave" && StealOnWave) {
            EndSession();
        }

		if (handId == -1) {
            if (e.Gesture == "RaiseHand" && ExperimentalGestureless) {
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
                this.hands.StartTracking(e.IdentifiedPosition);
            }

			if ( (e.Gesture == "Wave" && DetectWave) ||
			     (e.Gesture == "Click" && DetectPush) ) {
				this.hands.StartTracking (e.IdentifiedPosition);
			}
		}
	}
	
	void hands_HandCreate (object Sender, HandCreateEventArgs e)
	{
		// Only support one hand at the moment
		if (handId != -1 && e.UserID != handId) return;
		handId = e.UserID;

        lastRawPoint = e.Position;
		if (RotateToUser) {
			handPos = RotateHandPoint(e.Position);
		} else {
			handPos = e.Position;
		}

        currentSessionBounds = new Bounds(Point3DToVector3(handPos), SessionBounds);
		OnSessionStarted(handPos);

		foreach (GameObject obj in new List<GameObject>(Listeners))
        {
            if (!obj) continue;
			NotifyHandCreate(obj, handPos);
		}
	}
	
	void hands_HandUpdate (object Sender, HandUpdateEventArgs e)
	{
        lastRawPoint = e.Position;
		if (RotateToUser) {
			handPos = RotateHandPoint(e.Position);
		} else {
			handPos = e.Position;
		}

        // see if we're out of our session bounds
        if (SessionBoundingBox && !currentSessionBounds.Contains(Point3DToVector3(handPos))) {
            OpenNISessionManager.Instance.EndSession();
			Debug.Log("Session Ended by out of Bounds");
            return;
        }
		
        foreach (GameObject obj in new List<GameObject>(Listeners))
        {
            if (!obj) continue;
			NotifyHandUpdate(obj, handPos);
		}
	}
	
	void hands_HandDestroy (object Sender, HandDestroyEventArgs e)
	{
		handId = -1;
		
		foreach (GameObject obj in new List<GameObject>(Listeners)) {
            if (!obj) continue;
			NotifyHandDestroy(obj);
		}
		OnSessionEnded();
		this.gestures.StartGenerating();
	}

	public static void AddListener(GameObject obj)
	{
		if (!OpenNISessionManager.Instance.Listeners.Contains(obj)) {
			OpenNISessionManager.Instance.Listeners.Add(obj);
		}
		
		if (OpenNISessionManager.InSession) {
			OpenNISessionManager.Instance.NotifyHandCreate(obj, OpenNISessionManager.Instance.handPos);
		}
	}
	
	public static void RemoveListener(GameObject obj)
	{
		if (null == OpenNISessionManager.instance) return;

		if (OpenNISessionManager.Instance.Listeners.Contains(obj))	{
			OpenNISessionManager.Instance.Listeners.Remove(obj);
		}
		
		if (OpenNISessionManager.InSession) {
			OpenNISessionManager.Instance.NotifyHandDestroy(obj);
		}
	}
	
	public void StartSession(Point3D pos)
	{
		EndSession();
		// doesn't guarantee session will start
		this.hands.StartTracking(pos);
	}
	
	public void EndSession()
	{
		if (!InSession) return;
		
		// lose the current point
		if (-1 != handId) {
			this.hands.StopTracking(handId);
            handId = -1;
		}
	}
	
	void OnSessionStarted(Point3D pos)
	{
		Debug.Log("Hand point session started");
		inSession = true;
		focusPoint = Point3DToVector3(pos);
		
		foreach (GameObject obj in new List<GameObject>(Listeners)) {
            if (!obj) continue;
			NotifySessionStart(obj);
			NotifyHandCreate(obj, pos);
		}
	}
	
	void OnSessionEnded()
	{
		Debug.Log("Hand point Session ended");
		inSession = false;
		foreach (GameObject obj in new List<GameObject>(Listeners)) {
            if (!obj) continue;
			NotifyHandDestroy(obj);
			NotifySessionEnd(obj);
		}
	}
	
	void NotifyHandCreate(GameObject obj, Point3D pos)
	{
		obj.SendMessage("Hand_Create", Point3DToVector3(pos), SendMessageOptions.DontRequireReceiver);
	}

	void NotifyHandUpdate(GameObject obj, Point3D pos)
	{
		obj.SendMessage("Hand_Update", Point3DToVector3(pos), SendMessageOptions.DontRequireReceiver);
	}

	void NotifyHandDestroy(GameObject obj)
	{
		obj.SendMessage("Hand_Destroy", SendMessageOptions.DontRequireReceiver);
	}
	
	void NotifySessionStart(GameObject obj)
	{
		obj.SendMessage("Session_Start", SendMessageOptions.DontRequireReceiver);
	}
	
	void NotifySessionEnd(GameObject obj)
	{
		obj.SendMessage("Session_End", SendMessageOptions.DontRequireReceiver);
	}
	
	public static Vector3 Point3DToVector3(Point3D pos)
	{
		return new Vector3(pos.X, pos.Y, pos.Z);
	}

    public static Point3D Vector3ToPoint3D(Vector3 pos)
    {
        return new Point3D(pos.x, pos.y, pos.z);
    }
	
	Color colorInSession = Color.green;
	Color colorNotInSession = Color.red;
	Color colorObjectName = Color.white;
	Color colorHpcName = Color.grey;
	
	public void DebugDrawListeners()
	{
		Color original = GUI.color;
		
		GUILayout.BeginVertical("box");
		GUI.color = InSession ? colorInSession : colorNotInSession;
		GUILayout.Label("Session Manager");
		foreach (GameObject go in Listeners)
		{
            if (!go) continue;
			GUILayout.BeginVertical("box");
			GUI.color = colorObjectName;
			GUILayout.Label(go.name);
			GUI.color = colorHpcName;
			go.SendMessage("SessionManager_Visualize", SendMessageOptions.DontRequireReceiver);
			GUILayout.EndVertical();
		}
		GUILayout.EndVertical();
		GUI.color = original;
	}
	
	private UserGenerator userGenerator;
	private DepthGenerator depthGenerator { get { return OpenNIContext.OpenNode(NodeType.Depth) as DepthGenerator; } }

	public Point3D RotateHandPoint(Point3D HandPoint)
	{
		//TODO: handle user getting/losing CoM better.
		//TODO: Smoothing on CoM (so sudden CoM changes won't mess with the hand
		//      point too much)
		
		// get userID of user to whom the hand is attached
        int userID = WhichUserDoesThisPointBelongTo(HandPoint);
		if (userID == 0) {
			// no CoM, do nothing
			return HandPoint;
		}
		Vector3 user = Point3DToVector3(userGenerator.GetCoM(userID));
		
		// use line between com and sensor as Z
		Quaternion newOrientation = Quaternion.FromToRotation(user.normalized, Vector3.forward);
		Vector3 newHandPoint = newOrientation * Point3DToVector3(HandPoint);
		return new Point3D(newHandPoint.x, newHandPoint.y, newHandPoint.z);
	}

    public int WhichUserDoesThisPointBelongTo(Vector3 point)
    {
        return WhichUserDoesThisPointBelongTo(Vector3ToPoint3D(point));
    }

    public int WhichUserDoesThisPointBelongTo(Point3D point)
    {
        // get userID of user to whom the hand is attached
        Point3D ProjectiveHandPoint = depthGenerator.ConvertRealWorldToProjective(point);
        SceneMetaData sceneMD = userGenerator.GetUserPixels(0);
        return sceneMD[(int)ProjectiveHandPoint.X, (int)ProjectiveHandPoint.Y];
    }


    private int Cooldowns = 0;
    public bool CoolingDown
    {
        get
        {
            return Cooldowns > 0;
        }
    }
    public void StartCooldown(float seconds)
    {
        Cooldowns++; // make sure cooldown starts the moment we call the function
                     // (I'm unsure of whether the code before the first yield is
                     // executed or not before scheduling the rest of the coroutine)
        StartCoroutine(DoCooldown(seconds));
    }
    IEnumerator DoCooldown(float seconds)
    {
        yield return new WaitForSeconds(seconds);
        Cooldowns--;
    }
}
