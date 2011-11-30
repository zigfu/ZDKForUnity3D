using UnityEngine;
using System.Collections;

public class ZigInputWebplayer : MonoBehaviour {
    public ZigUserTracker UserTracker;

    public string ZigJSElementID = "ZigJS";


    public GameObject MosheObject;
    public GameObject NewDataObject;
	// Use this for initialization
	void Start () {
        if (!Application.isWebPlayer) {
            print("Only web player supported by ZigInputWebplayer!");
            return;
        }

	}
	
	// Update is called once per frame
	void Update () {
	
	}

    // needed for some disparity between the output of the JSON decoder and our direct OpenNI layer
    static void Intify(ArrayList list, string property)
    {
        foreach (Hashtable obj in list) {
            obj[property] = int.Parse(obj[property].ToString());
        }
    }

    void NewData(string param)
    {
        NewDataObject.SetActiveRecursively(true);
        Logger.Log("Got new data: " + param);
        try {
            ArrayList data = (ArrayList)JSON.JsonDecode(param);
            ArrayList users = (ArrayList)data[0];
            ArrayList hands = (ArrayList)data[1];
            Intify(users, "id");
            Intify(hands, "id");
            Intify(hands, "userid");
            UserTracker.UpdateData(users, hands);
        }
        catch (System.Exception ex) {
            Logger.Log(ex.ToString());
        }
    }
}
