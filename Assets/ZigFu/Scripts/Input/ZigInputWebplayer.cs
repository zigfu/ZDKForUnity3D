using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

public class ZigInputWebplayer : IZigInputReader
{
	// init/update/shutdown
	public void Init()
	{
		WebplayerReceiver receiver = WebplayerReceiver.Create();
		receiver.NewDataEvent += HandleReceiverNewDataEvent;
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
	
	// textures
	public Texture2D Depth { get; private set; }
	public Texture2D Image { get; private set; }
	public bool UpdateDepth { get; set; }
	public bool UpdateImage { get; set; }
	
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