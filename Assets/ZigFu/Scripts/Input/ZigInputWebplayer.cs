using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

public class ZigInputWebplayer : IZigInputReader
{
    const int MaxDepth = 10000; // hard coded for now
    float[] depthHistogramMap;
    Color32[] depthMapPixels;
    ushort[] depthMapData;
    Color32[] imageMapPixels;
    int XRes;
    int YRes;
    int factor = 1;
	// init/update/shutdown
	public void Init()
	{
		WebplayerReceiver receiver = WebplayerReceiver.Create();
		receiver.NewDataEvent += HandleReceiverNewDataEvent;
        XRes = 160;
        YRes = 120;
        Depth = new Texture2D(XRes / factor, YRes / factor);
        depthMapPixels = new Color32[(XRes / factor) * (YRes / factor)];
        depthMapData = new ushort[(XRes / factor) * (YRes / factor)];
        depthHistogramMap = new float[MaxDepth];
        Image = new Texture2D(XRes / factor, YRes / factor);
        imageMapPixels = new Color32[(XRes / factor) * (YRes / factor)];
        receiver.NewDepthEvent += HandleNewDepth;
        receiver.NewImageEvent += HandleNewImage;
	}

	public void Update()
	{
	}
	
	public void Shutdown()
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
        string s = e.JsonData;
        int numOfPoints = 0;
        int outIndex = 0;
        Array.Clear(depthHistogramMap, 0, depthHistogramMap.Length);
        int i = 0;
        // calc histogram, do conversion to depthmap
        for(i = 0; i < s.Length; i+=2) {
            ushort pixel = (ushort)((s[i] & (~1)) | (s[i + 1] & 0x7f));
            depthMapData[outIndex] = pixel;
            if (pixel != 0) {
                depthHistogramMap[pixel]++;
                numOfPoints++;
            }
            outIndex++;
        }
        if (numOfPoints > 0) {
            for (i = 1; i < depthHistogramMap.Length; i++) {
                depthHistogramMap[i] += depthHistogramMap[i - 1];
            }
            for (i = 0; i < depthHistogramMap.Length; i++) {
                depthHistogramMap[i] = (1.0f - (depthHistogramMap[i] / numOfPoints)) * 255;
            }
        }
        depthHistogramMap[0] = 0;
        // flip the depthmap as we create the texture
        int YScaled = YRes / factor;
        int XScaled = XRes / factor;
        i = XScaled * YScaled - XScaled;
        int depthIndex = 0;
        for (int y = 0; y < YScaled; ++y, i -= XScaled) {
            for (int x = 0; x < XScaled; ++x, depthIndex += factor) {
                ushort pixel = depthMapData[depthIndex];
                if (pixel == 0) {
                    depthMapPixels[i + x] = Color.clear;
                }
                else {
                    Color32 c = new Color32((byte)depthHistogramMap[pixel], (byte)depthHistogramMap[pixel], 0, 255);
                    depthMapPixels[i + x] = c;
                }
            }
            // Skip lines
            depthIndex += (factor - 1) * XRes;
        }

        Depth.SetPixels32(depthMapPixels);
        Depth.Apply();
    }

    private void HandleNewImage(object sender, NewDataEventArgs e)
    {
        string s = e.JsonData;
        int outIndex = 0;
        for (int i = 0; i < s.Length; i += 3, outIndex++) {
            imageMapPixels[outIndex].r = (byte)(s[i] & (~1));
            imageMapPixels[outIndex].g = (byte)(s[i + 1] & (~1));
            imageMapPixels[outIndex].b = (byte)(s[i + 2] & (~1));
        }

        Image.SetPixels32(imageMapPixels);
        Image.Apply();
    }


    // textures
    Texture2D Image;
    Texture2D Depth;
    public Texture2D GetImage() { return Image; }
    public Texture2D GetDepth() { return Depth; }

    bool updateDepth = false;
    public bool UpdateDepth {
        get { return updateDepth; }
        set
        {
            if (updateDepth != value) {
                updateDepth = value;
                WebplayerReceiver.SetStreamsToUpdate(updateDepth, updateImage);
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
                WebplayerReceiver.SetStreamsToUpdate(updateDepth, updateImage);
            }
        }
    }
	
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