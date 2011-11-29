using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using OpenNI;

public enum ZigInputType {
	OpenNI,
	Webplayer
}

public class ZigUserTracker : MonoBehaviour {
	
	ArrayList rawUsers;
	Dictionary<int, ZigTrackedUser> trackedUsers = new Dictionary<int, ZigTrackedUser>();
	Dictionary<int, int> trackedHands = new Dictionary<int, int>();
	GameObject trackedUsersContainer;
	
	
	// Use this for initialization
	void Start () {
		trackedUsersContainer = new GameObject("TrackedUsersContainer");
	}
	
	// Update is called once per frame
	void Update () {
	
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
			if (null == curruser) {
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

		// save raw data before updating the fullbody controls
		this.rawUsers = users;
		
		// update stuff
		foreach (Hashtable user in users) {
			int userid = (int)user["id"];
			trackedUsers[userid].UpdateUserData(user);
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
	
	void UpdateHands(ArrayList hands)
	{
		/*
		// get rid of old hands
		for (handid in this.trackedHands) {
			currhand = this.getItemById(hands, handid);
			if (undefined == currhand) {
				this.ProcessLostHand(handid);
			}
		}
		
		// add new hands
		for (handindex in hands) {
			hand = hands[handindex];
			if (undefined == this.trackedHands[hand.id]) {
				this.ProcessNewHand(hand.id, hand.userid);
			}
		}

		// save raw data before updating the handpoint controls
		this.rawHands = hands;
		
		// update hand points
		// go through list of users
		for (userid in this.trackedUsers) {
			// find hands belonging to this user
			currhands = [];
			for (handid in this.trackedHands) {
				if (this.trackedHands[handid] == userid) {
					currhands.push(this.getItemById(hands, handid));
				}
			}
			this.trackedUsers[userid].UpdateHands(currhands);
		}	*/
	}
	
	public void Update(ArrayList users, ArrayList hands)
	{
		UpdateUsers(users);
		UpdateHands(hands);
		
		foreach (KeyValuePair<int, ZigTrackedUser> user in trackedUsers) {
			user.Value.NotifyListeners();
		}
	}
}
