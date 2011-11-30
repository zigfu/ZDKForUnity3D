using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using OpenNI;

public class ZigJoint
{
	public Vector3 position;
	public Quaternion rotation;
}

public class ZigTrackedUser : MonoBehaviour
{
	public ZigControlList Controls { get; private set; }
	public int UserId { get; private set; }
	public Vector3 Position { get; private set; }
	public bool SkeletonTracked { get; private set; }
	public Dictionary<SkeletonJoint, ZigJoint> Joints { get; private set; }
	public Dictionary<int, Vector3> Hands { get; private set; }
	public int PrimaryHand { get; private set; }
	
	public void init(int userid) 
	{
		this.UserId = userid;
	}	
	
	void Awake()
	{
		this.Controls = this.gameObject.AddComponent<ZigControlList>();
		this.Joints = new Dictionary<SkeletonJoint, ZigJoint>();
		this.Hands = new Dictionary<int, Vector3>();
	}
	
	void OnDestroy()
	{
		Destroy(this.Controls);	
	}
	
	public void UpdateUserData(Hashtable userData)
	{
		// unpack
		SkeletonTracked = (bool)userData["tracked"];
		Position = PositionFromArrayList(userData["centerofmass"] as ArrayList);
		
		// skeleton data
		if (SkeletonTracked) {
			foreach (Hashtable joint in userData["joints"] as ArrayList) {
				SkeletonJoint sj = (SkeletonJoint)joint["id"];
				if (!Joints.ContainsKey(sj)) {
					Joints[sj] = new ZigJoint();
				}
				
				if ((double)joint["positionconfidence"] > 0) {
					Joints[sj].position = PositionFromArrayList(joint["position"] as ArrayList);
				}
				if ((double)joint["rotationconfidence"] > 0) {
					Joints[sj].rotation = RotationFromArrayList(joint["rotation"] as ArrayList);
				}
			}
		}
	}
	
	public void UpdateHands(ArrayList hands)
	{
		// TODO: Think of a better way to keep hands updated
		// TODO: Think of a better way to choose primary hand
		Hands.Clear();
		foreach (Hashtable hand in hands) {
			Hands[(int)hand["id"]] = PositionFromArrayList(hand["position"] as ArrayList);
		}
		PrimaryHand = (hands.Count > 0) ? (int)((hands[0] as Hashtable)["id"]) : 0;
	}
	
	public void NotifyListeners()
	{
		Controls.DoUpdate(this);
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
