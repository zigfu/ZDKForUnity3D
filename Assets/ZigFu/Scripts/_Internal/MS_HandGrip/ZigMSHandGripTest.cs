using UnityEngine;
using System.Collections;

using System.Runtime.InteropServices;
using System.Text;
using System;

public enum GripHandState { NONE, GRIP, RELEASED };
public enum GripHandKinectUpdateMode {FROM_UNITY, THREAD};

public class ZigMSHandGripTest : MonoBehaviour {

    public GripHandKinectUpdateMode m_UpdateMode = GripHandKinectUpdateMode.FROM_UNITY;
    public TextMesh m_MsgTxt;
    private StringBuilder m_SBTxt;
    
    public GripHandState m_RHandStat;
    public GripHandState m_LHandStat;


    [DllImport("KinectGrip170_CLR20", EntryPoint = "InitKinectInteraction")]
    public static extern int InitKinectInteraction(int a_Mode);

    [DllImport("KinectGrip170_CLR20", EntryPoint = "FinishKinectInteraction")]
    public static extern int FinishKinectInteraction();

    [DllImport("KinectGrip170_CLR20", EntryPoint = "GetLHandStat")]
    public static extern int GetLHandStat();

    [DllImport("KinectGrip170_CLR20", EntryPoint = "GetRHandStat")]
    public static extern int GetRHandStat();

    [DllImport("KinectGrip170_CLR20", EntryPoint = "UpdateKinectData")]
    public static extern int KinectDataProc();


    
    // Use this for initialization
	void Start () {
        // Initialize MS Kinect Interaction Stream
        InitKinectInteraction( (int)m_UpdateMode );

        m_SBTxt = new StringBuilder(100);
	}
	
	// Update is called once per frame
	void Update () {

        // Update MS Kinect Interaction Stream data    
        KinectDataProc();

        m_LHandStat = (GripHandState)Enum.ToObject(typeof(GripHandState), GetLHandStat());
        m_RHandStat = (GripHandState)Enum.ToObject(typeof(GripHandState), GetRHandStat());

        //if (m_LHandStat != GripHandState.NONE)
        {
            m_SBTxt.AppendFormat(" Left {0} ", m_LHandStat.ToString());
            m_MsgTxt.text = m_SBTxt.ToString();
        }

        //if (m_RHandStat != GripHandState.NONE)
        {
            m_SBTxt.AppendFormat(" : Right {0} ", m_RHandStat.ToString());
            m_MsgTxt.text = m_SBTxt.ToString();
        }

        m_SBTxt.Remove(0, m_SBTxt.Length);
	}

    void OnApplicationQuit()
    {
        // Close handles and release the Kinect Sensor
        FinishKinectInteraction();
    }
}
