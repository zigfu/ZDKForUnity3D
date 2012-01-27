using UnityEngine;
using System.Collections;

public class ZigEngagementTrigger : MonoBehaviour {
	
	public bool RaiseHand;
	public bool StartSession;
	public bool SingleUserPosition;
	public bool SideBySideLeftPosition;
	public bool SideBySideRightPosition;
	
	
	ZigEngageSingleUser engager;
	ZigTrackedUser user;
	
	public void Init(ZigEngageSingleUser engager, ZigTrackedUser user)
	{
		this.engager = engager;
		this.user = user;
	}
	
	// Use this for initialization
	void Start () {
		if (RaiseHand) {
			if (null == GetComponent<ZigHandRaiseDetector>()) {
				gameObject.AddComponent<ZigHandRaiseDetector>();
			}
		}
		
		if (SingleUserPosition) {
			gameObject.AddComponent<ZigUserIsInRegion>().region = new Bounds(new Vector3(0,0,-2000), new Vector3(2000,4000,2000));
		}
			
	}
	
	// Update is called once per frame
	void Update () {
	
	}
	
	
	void HandRaiseDetector_HandRaised(ZigTrackedUser user)
	{
		print("Hand raised");
		if (RaiseHand) {
			engager.SendMessage("EngageTrigger", user);
		}
	}
	
	/*
	void Zig_OnSessionStart(ZigEventArgs args)
	{
		if (StartSession) {
			engager.SendMessage("EngageTrigger", user);
		}
	}
	
	void Zig_OnSessionEnd(ZigEventArgs args)
	{
		if (StartSession) {
			engager.SendMessage("DisengageTrigger", user);
		}
	}*/
	
	void UserIsInRegion(ZigTrackedUser user)
	{
		if (SingleUserPosition) {
			engager.SendMessage("EngageTrigger", user);
		}
	}
	
	void UserIsNotInRegion(ZigTrackedUser user)
	{
		if (SingleUserPosition) {
			engager.SendMessage("DisengageTrigger", user);
		}
	}
}
