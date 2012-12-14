using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Leap;



public class LeapRender : MonoBehaviour {
    public Vector3 VtoV3(Vector v)
    {
        return new Vector3((float)v.x, (float)v.y, (float)v.z);
    }

	// Use this for initialization
	void Start () {
        LeapInput.OnHandFound += HandFoundHandler;
        LeapInput.OnHandUpdate += HandUpdateHandler;
        LeapInput.OnHandLost += HandLostHandler;

        LeapInput.OnFingerFound += FingerFoundHandler;
        LeapInput.OnFingerUpdate += FingerUpdateHandler;
        LeapInput.OnFingerLost += FingerLostHandler;
        handMap = new Dictionary<int,GameObject>();
        handrayMap = new Dictionary<int, GameObject>();
        fingerMap = new Dictionary<int,GameObject>();
	}

    public Transform handsParent;
    public Transform fingersParent;
    public GameObject handObject;
    public GameObject handrayObject;
    public GameObject fingerObject;
    public Vector3 scale;
    public Vector3 bias;
    
    
    Dictionary<int, GameObject> fingerMap;
    Dictionary<int, GameObject> handMap;
    Dictionary<int, GameObject> handrayMap;


    void HandFoundHandler(int ID)
    {
        Debug.Log("Hand found. Given ID: " + ID);
        GameObject newHand =  (GameObject)Instantiate(handObject);
        handMap.Add(ID, newHand);

        newHand.transform.parent = handsParent;
        GameObject newHandRay =  (GameObject)Instantiate(handrayObject);
        newHandRay.transform.parent = newHand.transform;
        handrayMap.Add(ID, newHandRay);
        
    }
       

    void HandUpdateHandler(int ID, Hand hand)
    {
        if (!handMap.ContainsKey(ID) || !handrayMap.ContainsKey(ID))
        {
            HandFoundHandler(ID);
        }
        if (hand == null)
        {
            Debug.LogWarning("Null Hand in update handler");
            return;
        }
        if (hand.palm() == null)
        {
            Debug.LogWarning("Null Hand Palm in update handler");
            return;
        }
        try
        {
            Vector v = hand.palm().position;
            Vector3 v3 = VtoV3(v);
            Vector3 pos = bias + Vector3.Scale(scale,v3);
            handMap[ID].transform.position = pos;
            Vector dir = hand.palm().direction;
            Vector3 dir3 = VtoV3(dir);
            Debug.DrawRay(Vector3.Scale(v3,scale), dir3);


           // handrayMap[ID].transform.position = pos + Vector3.Scale(scale, new Vector3((float)dir.x, (float)dir.y, (float)dir.z));
            handrayMap[ID].transform.position = pos + new Vector3((float)dir.x, (float)dir.y, -(float)dir.z);
            Vector normal = hand.normal();
            Debug.DrawRay(Vector3.Scale(v3,scale), VtoV3(normal));


            handMap[ID].transform.rotation = Quaternion.FromToRotation(v3, new Vector3((float)normal.x, (float)normal.y, -(float)normal.z));
            //handMap[ID].transform.LookAt(pos + Vector3.Scale(scale, new Vector3((float)dir.x, (float)dir.y, (float)dir.z)));
         
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("during hand:" + ID.ToString() + e.Message + e.StackTrace);
        }
    }
    void HandLostHandler(int ID)
    {
        Debug.Log("Hand Lost: " + ID);
        if (handMap.ContainsKey(ID))
        {

            GameObject handGO = handMap[ID];
            GameObject handrayGO = handrayMap[ID];
            handMap.Remove(ID);
            Destroy(handGO);
            handrayMap.Remove(ID);
            Destroy(handrayGO);
        }
    }
    void FingerFoundHandler(int ID, int handID)
    {
        if (!handMap.ContainsKey(handID))
        {
            HandFoundHandler(handID);
        }
        Debug.Log("Finger found. Given ID: " + ID);
        GameObject newFinger = (GameObject)Instantiate(fingerObject);
        newFinger.transform.parent = fingersParent;//handMap[handID].transform;
        if (fingerMap.ContainsKey(ID))
        {
            fingerMap[ID] = newFinger;
        }
        else
        {
            fingerMap.Add(ID, newFinger);
        }

    }
    void FingerUpdateHandler(int ID, int handID, Finger finger)
    {

        if (!fingerMap.ContainsKey(ID) || fingerMap[ID] == null)
        {
            FingerFoundHandler(ID, handID);
        }
        else
        {
            if (finger == null)
            {
                Debug.LogWarning("Null finger in update handler");
                return;
            }
            if (finger.tip() == null)
            {
                Debug.LogWarning("Null finger tip in update handler");
                return;
            }
            Leap.Ray r = finger.tip();
            Vector v = r.position;
            Vector dir = r.direction;
            Debug.DrawRay(Vector3.Scale(VtoV3(v), scale), VtoV3(dir));
            Vector3 pos = bias + Vector3.Scale(scale, new Vector3((float)v.x, (float)v.y, (float)v.z));
            fingerMap[ID].transform.position = pos;
            fingerMap[ID].transform.LookAt(pos + Vector3.Scale(scale, new Vector3((float)dir.x, (float)dir.y, (float)dir.z)));
        }
    }
    void FingerLostHandler(int ID)
    {
        Debug.Log("Finger Lost: " + ID);
        if (fingerMap.ContainsKey(ID))
        {
            GameObject go = fingerMap[ID];
            fingerMap.Remove(ID);
            Destroy(go);
        }

    }


	// Update is called once per frame
	void Update () {
       
	}
}
