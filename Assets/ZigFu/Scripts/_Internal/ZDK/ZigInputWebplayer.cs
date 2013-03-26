using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

class ZigInputWebplayer : IZigInputReader
{

    int XRes;
    int YRes;
	// init/update/shutdown
	public void Init(ZigInputSettings settings)
	{
        UpdateDepth = settings.UpdateDepth;
        UpdateImage = settings.UpdateImage;
        UpdateLabelMap = settings.UpdateLabelMap;

		WebplayerReceiver receiver = WebplayerReceiver.Create();
        receiver.PluginSettingsEvent += new EventHandler<PluginSettingsEventArgs>(receiver_PluginSettingsEvent);
		receiver.NewDataEvent += HandleReceiverNewDataEvent;
        XRes = 160; // TODO: make better - the plugin exports the depth/image map resolutions (partially done in WebplayerReceiver side)
        YRes = 120;
        receiver.NewDepthEvent += HandleNewDepth;
        receiver.NewImageEvent += HandleNewImage;
        receiver.NewLabelMapEvent += HandleNewLabelMap;
        this.Depth = new ZigDepth(XRes, YRes);
        this.Image = new ZigImage(XRes, YRes);
        this.LabelMap = new ZigLabelMap(XRes, YRes);
    }

    // Image-space <-> World-space conversions
    float worldToImageXRatio = 1.11146f; // some sane defaults
    float worldToImageYRatio = 0.83359f;

    public Vector3 ConvertImageToWorldSpace(Vector3 imagePos)
    {
        float xCentered = imagePos.x - (Depth.xres / 2);
        float yCentered = imagePos.y - (Depth.yres / 2);
        return new Vector3(xCentered * imagePos.z * worldToImageXRatio,
                           yCentered * imagePos.z * worldToImageYRatio,
                           imagePos.z);
    }
    public Vector3 ConvertWorldToImageSpace(Vector3 worldPos)
    {
        if (Mathf.Approximately(worldPos.z, 0)) { 
            return new Vector3(Depth.xres / 2, Depth.yres / 2, worldPos.z);
        }

        return new Vector3((Depth.xres / 2) + (worldPos.x / (worldPos.z * worldToImageXRatio)),
                           (Depth.yres / 2) + (worldPos.y / (worldPos.z * worldToImageYRatio)),
                           worldPos.z);
    }
    void receiver_PluginSettingsEvent(object sender, PluginSettingsEventArgs e)
    {
        // implicitly assuming worldSpaceEdge and ImageSpaceEdge are of the same Z
        worldToImageXRatio = (e.WorldSpaceEdge.x / (e.ImageSpaceEdge.x / 2)) / e.WorldSpaceEdge.z;
        worldToImageYRatio = (e.WorldSpaceEdge.y / (e.ImageSpaceEdge.y / 2)) / e.WorldSpaceEdge.z;
        // quick sanity test
        //Vector3 pt1 = new Vector3(140, 30, 500);
        //Vector3 inworld = ConvertImageToWorldSpace(pt1);
        //Vector3 inimage = ConvertWorldToImageSpace(inworld);
        //WebplayerLogger.Log(string.Format("conversions: image({0},{1},{2})->world({3},{4},{5})->image({6},{7},{8})",
        //                                  pt1.x, pt1.y, pt1.z, inworld.x, inworld.y, inworld.z, inimage.x, inimage.y, inimage.z));
    }
   

    public ZigDepth Depth { get; private set; }
    public ZigImage Image { get; private set; }
    public ZigLabelMap LabelMap { get; private set; }

    public void Shutdown() 
    { 
    }
    public void Update() 
    { 
    }
	
	public event EventHandler<NewUsersFrameEventArgs> NewUsersFrame;
	protected void OnNewUsersFrame(List<ZigInputUser> users) {
		if (null != NewUsersFrame) {
			NewUsersFrame.Invoke(this, new NewUsersFrameEventArgs(users));
		}
	}
    private void HandleNewDepth(object sender, NewDataEventArgs e)
    {
        byte[] depthBytes = Convert.FromBase64String(e.JsonData);
        short[] output = Depth.data;
        for (int i = 0; i < output.Length; i++) {
            output[i] = (short)(depthBytes[i * 2] + (depthBytes[i * 2 + 1] << 8));
        }
    }


    private void HandleNewLabelMap(object sender, NewDataEventArgs e)
    {
        byte[] labelBytes = Convert.FromBase64String(e.JsonData);
        short[] output = LabelMap.data;
        for (int i = 0; i < output.Length; i++)
        {
            output[i] = (short)(labelBytes[i * 2] + (labelBytes[i * 2 + 1] << 8));
        }
    }




    private void HandleNewImage(object sender, NewDataEventArgs e)
    {
        byte[] imageBytes = Convert.FromBase64String(e.JsonData);
        Color32[] output = Image.data;
        for (int i = 0; i < output.Length; i++) {
            output[i].r = imageBytes[i * 3];
            output[i].g = imageBytes[i * 3 + 1];
            output[i].b = imageBytes[i * 3 + 2];
        }

    }

    bool updateDepth = false;
    public bool UpdateDepth {
        get { return updateDepth; }
        set
        {
            if (updateDepth != value) {
                updateDepth = value;
                WebplayerReceiver.SetStreamsToUpdate(updateDepth, updateImage, updateLabelMap);
            }
        }
    }
    bool updateImage = false;
    public bool UpdateImage {
        get { return updateImage; }
        set
        {
            if (updateImage != value) {
                updateImage = value;
                WebplayerReceiver.SetStreamsToUpdate(updateDepth, updateImage, updateLabelMap);
            }
        }
    }


    bool updateLabelMap = false;
    public bool UpdateLabelMap
    {
        get { return updateLabelMap; }
        set
        {
            if (updateLabelMap != value)
            {
                updateLabelMap = value;
                WebplayerReceiver.SetStreamsToUpdate(updateDepth, updateImage, updateLabelMap);
            }
        }
    }

    public bool AlignDepthToRGB { get; set; } // unsupported for now

	// needed for some disparity between the output of the JSON decoder and our zig input layer
	static void Intify(ArrayList list, string property)
	{
        foreach (Hashtable obj in list) {
        	obj[property] = int.Parse(obj[property].ToString());
		}
    }
	
	private void HandleReceiverNewDataEvent(object sender, NewDataEventArgs e) {
		Hashtable data = (Hashtable)JSON.JsonDecode(e.JsonData);
		
       	ArrayList jsonusers = (ArrayList)data["users"];
    	Intify(jsonusers, "id");
		
		List<ZigInputUser> users = new List<ZigInputUser>();
		
		foreach (Hashtable jsonuser in jsonusers) {
			int id = (int)jsonuser["id"];
			bool tracked = (double)jsonuser["tracked"] > 0;
			Vector3 position = PositionFromArrayList(jsonuser["centerofmass"] as ArrayList);
			
			ZigInputUser user = new ZigInputUser(id, position);
			List<ZigInputJoint> joints = new List<ZigInputJoint>();
			user.Tracked = tracked;
			if (tracked) {
				foreach (Hashtable jsonjoint in jsonuser["joints"] as ArrayList) {
					ZigInputJoint joint = new ZigInputJoint((ZigJointId)(double)jsonjoint["id"]);
					if ((double)jsonjoint["positionconfidence"] > 0) {
						joint.Position = PositionFromArrayList(jsonjoint["position"] as ArrayList);
						joint.GoodPosition = true;
					}
					if ((double)jsonjoint["rotationconfidence"] > 0) {
						joint.Rotation = RotationFromArrayList(jsonjoint["rotation"] as ArrayList);
						joint.GoodRotation = true;
					}
					joints.Add(joint);
				}
			}
			user.SkeletonData = joints;
			users.Add(user);
		}
		// now read depth/image/labelmap if there
        int index = JSON.ConsumedCharacters;
        string sourceString = e.JsonData;
        const int MAP_16BPP_ENCODED_SIZE = (160*120*2*4)/3; //TODO: un-hardcode
        const int MAP_24BPP_ENCODED_SIZE = (160*120*3*4)/3; //TODO: un-hardcode
        try {
            while (index < sourceString.Length) {
                char type = sourceString[index];
                index++;
                switch (type) {
                    case 'd':
                        HandleNewDepth(sender, new NewDataEventArgs(sourceString.Substring(index, MAP_16BPP_ENCODED_SIZE)));
                        index += MAP_16BPP_ENCODED_SIZE;
                        break;
                    case 'l':
                        HandleNewLabelMap(sender, new NewDataEventArgs(sourceString.Substring(index, MAP_16BPP_ENCODED_SIZE)));
                        index += MAP_16BPP_ENCODED_SIZE;
                        break;
                    case 'i':
                        HandleNewImage(sender, new NewDataEventArgs(sourceString.Substring(index, MAP_24BPP_ENCODED_SIZE)));
                        index += MAP_24BPP_ENCODED_SIZE;
                        break;
                    default: // just go on to next char
                        break;
                }
            }
        }
        catch (IndexOutOfRangeException) {
            // do nothing - it means data was broken. That's okay (we already have the skeleton data)
        }
    	OnNewUsersFrame(users);
	}
	
	Vector3 PositionFromArrayList(ArrayList fromJson)
	{
		return new Vector3((float)(double)fromJson[0],(float)(double)fromJson[1],-(float)(double)fromJson[2]);
	}
						
	Quaternion RotationFromArrayList(ArrayList fromJson)
	{
		float[] matrix = new float[] {
			(float)(double)fromJson[0],
			(float)(double)fromJson[1],
			(float)(double)fromJson[2],
			(float)(double)fromJson[3],
			(float)(double)fromJson[4],
			(float)(double)fromJson[5],
			(float)(double)fromJson[6],
			(float)(double)fromJson[7],
			(float)(double)fromJson[8] };
							
		// Z coordinate in OpenNI is opposite from Unity
		// Convert the OpenNI 3x3 rotation matrix to unity quaternion while reversing the Z axis
		Vector3 worldYVec = new Vector3((float)matrix[3], (float)matrix[4], -(float)matrix[5]);
		Vector3 worldZVec = new Vector3(-(float)matrix[6], -(float)matrix[7], (float)matrix[8]);
		return Quaternion.LookRotation(worldZVec, worldYVec);
	}
}