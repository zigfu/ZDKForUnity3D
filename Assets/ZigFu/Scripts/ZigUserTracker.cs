using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using OpenNI;

public class ZigUserTracker : MonoBehaviour {
	
	public bool allowHandsForUntrackedUsers = true;
	
	Dictionary<int, ZigTrackedUser> trackedUsers = new Dictionary<int, ZigTrackedUser>();
	Dictionary<int, int> trackedHands = new Dictionary<int, int>();
	GameObject trackedUsersContainer;
	
	public Dictionary<int, ZigTrackedUser> TrackedUsers { get { return trackedUsers; } }
	
	// Use this for initialization
	void Start () {
		trackedUsersContainer = new GameObject("TrackedUsersContainer");
		
		// make sure we have some input
		if (Application.isWebPlayer) {
			if (null == GetComponent<ZigInputWebplayer>()) {
				gameObject.AddComponent<ZigInputWebplayer>().UserTracker = this;
			}
		} else {
			if (null == GetComponent<ZigInputOpenNI>()) {
				gameObject.AddComponent<ZigInputOpenNI>().userTracker = this;
			}
		}
	}
	
	void ProcessNewUser(int userid)
	{
		ZigTrackedUser user = trackedUsersContainer.AddComponent<ZigTrackedUser>();
		user.init(userid);
		this.trackedUsers[userid] = user;
		SendMessage("Zig_NewUser", user, SendMessageOptions.DontRequireReceiver);
		print("New user: " + userid);
	}
	
	void ProcessLostUser(int userid)
	{
		if (!this.trackedUsers.ContainsKey(userid)) {
			Debug.LogError("Attempting to remove non existing user " + userid);
			return;
		}
		
		ZigTrackedUser user = this.trackedUsers[userid];
		this.trackedUsers.Remove(userid);
		Destroy(user);
		SendMessage("Zig_LostUser", user, SendMessageOptions.DontRequireReceiver);
		print("Lost user: " + userid);
	}
	
	void UpdateUsers(ArrayList users)
	{
		// get rid of old users
		List<int> ids = new List<int>(trackedUsers.Keys);
		foreach (int userid in ids) {
			Hashtable curruser = this.getById(users, userid) as Hashtable;
			if (null == curruser && isRealUser(userid)) {
				this.ProcessLostUser(userid);
			}
		}
		
		// add new users
		foreach (Hashtable user in users) {
			//int userid = int.Parse((string)user["id"]);
			int userid = (int)user["id"];
			if (!this.isUserTracked(userid)) {
				this.ProcessNewUser(userid);
			}
		}
	
		// update stuff
		foreach (Hashtable user in users) {
			int userid = (int)user["id"];
			trackedUsers[userid].UpdateUserData(user);
		}
	}
	
	void ProcessNewHand(int handid, int userid)
	{
		// no user id
		if (userid <= 0) {
			// get out if we dont allow such hands
			if (!this.allowHandsForUntrackedUsers) return;
			
			// otherwise allocate a "fake" user id and use it
			userid = this.getFakeUserId();
		}
	
		// add the user if neccessary
		if (!this.isUserTracked(userid)) {
			this.ProcessNewUser(userid);
		}
		
		// associate this hand with the user
		this.trackedHands[handid] = userid;
	}
	
	void ProcessLostHand(int handid)
	{
		// remove the hand->user association
		int userid = this.trackedHands[handid];
		this.trackedHands.Remove(handid);
		
		// if this user is "fake" (created for this specific 
		// hand point) then get rid of it
		if (!this.isRealUser(userid)) {
			this.ProcessLostUser(userid);
		}
	}
	
	void UpdateHands(ArrayList hands)
	{
		// get rid of old hands
		List<int> handids = new List<int>(this.trackedHands.Keys);
		foreach (int handid in handids) {
			if (null == this.getById(hands, handid)) {
				this.ProcessLostHand(handid);
			}
		}
		
		// add new hands
		foreach (Hashtable hand in hands) {
			if (!this.trackedHands.ContainsKey((int)hand["id"])) {
				this.ProcessNewHand((int)hand["id"], (int)hand["userid"]);
			}
		}

		// update hand points
		// go through list of users
		foreach (ZigTrackedUser user in this.trackedUsers.Values) {
			// find hands belonging to this user
			ArrayList currhands = new ArrayList();
			foreach (KeyValuePair<int,int> hand in this.trackedHands) {
				if (hand.Value == user.UserId) {
					currhands.Add(this.getById(hands, hand.Key));
				}
			}
			user.UpdateHands(currhands);
		}	
	}
	
	public void UpdateData(ArrayList users, ArrayList hands)
	{
		UpdateUsers(users);
		UpdateHands(hands);
		
		SendMessage("Zig_Update", this, SendMessageOptions.DontRequireReceiver);
		
		foreach (KeyValuePair<int, ZigTrackedUser> user in trackedUsers) {
			user.Value.NotifyListeners();
		}
	}
	
	object getById(ArrayList list, int id)
	{
		foreach (Hashtable item in list) {
			if ((int)item["id"] == id) {
				return item;
			}
		}
		return null;
	}
	
	bool isUserTracked(int userid) 
	{
		return this.trackedUsers.ContainsKey(userid);
	}
	
	// "Fake" users are kinda hacky right now
	
	const int FakeUserIdBase = 1337;
	int nextFakeUserId = FakeUserIdBase;
	int getFakeUserId()
	{
		int ret = nextFakeUserId;
		nextFakeUserId++;
		return ret;
	}

	bool isRealUser(int userid)
	{
		return (userid < FakeUserIdBase);
	}
}
