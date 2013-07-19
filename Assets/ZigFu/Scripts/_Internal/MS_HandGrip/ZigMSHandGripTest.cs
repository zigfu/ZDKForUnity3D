using UnityEngine;
using System.Collections;

using System.Runtime.InteropServices;
using System.Text;

public class ZigMSHandGripTest : MonoBehaviour {

    public TextMesh m_MsgTxt;
    public StringBuilder m_sbTxt;

    [DllImport("KinectGrip170_CLR20", EntryPoint = "TestFunc")]
    public static extern int TestFunc();


    [DllImport("KinectGrip170_CLR20", EntryPoint = "InitKinectInteraction")]
    public static extern int InitKinectInteraction();

    [DllImport("KinectGrip170_CLR20", EntryPoint = "FinishKinectInteraction")]
    public static extern int FinishKinectInteraction();

    [DllImport("KinectGrip170_CLR20", EntryPoint = "GetLHandStat")]
    public static extern int GetLHandStat();

    [DllImport("KinectGrip170_CLR20", EntryPoint = "GetRHandStat")]
    public static extern int GetRHandStat();

    [DllImport("KinectGrip170_CLR20", EntryPoint = "KinectDataProc")]
    public static extern int KinectDataProc();

    // Use this for initialization
	void Start () {
        
        print(" KinectGrip TestFunc = " + TestFunc());

        InitKinectInteraction();
        m_sbTxt = new StringBuilder(1000);
	}
	
	// Update is called once per frame
	void Update () {
        
        KinectDataProc();

        if (GetRHandStat() != 0)
        {
            m_sbTxt.AppendFormat(" R {0} ", GetRHandStat());
            print(m_sbTxt.ToString());
            m_MsgTxt.text = m_sbTxt.ToString();
        }

        if (GetLHandStat() != 0)
        {
            m_sbTxt.AppendFormat(": L {0} ", GetLHandStat());
            print(m_sbTxt.ToString());
            m_MsgTxt.text = m_sbTxt.ToString();
        }

        m_sbTxt.Remove(0, m_sbTxt.Length);
	}

    void OnApplicationQuit()
    {
        FinishKinectInteraction();
    }
}
