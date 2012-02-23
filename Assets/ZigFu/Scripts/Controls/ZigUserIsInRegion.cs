using UnityEngine;
using System.Collections;

public class ZigUserIsInRegion : MonoBehaviour {
	
	public Bounds region;
	public bool IsInRegion;// { get; private set; }
	
	public bool isInRegionThisFrame;
	
	// Use this for initialization
	void Start () {
	}
	
	// Update is called once per frame
	void Update () {
	
	}
	
	void Reset() {
		IsInRegion = false;
	}
	

	void Zig_UpdateUser(ZigTrackedUser user)
	{
		isInRegionThisFrame = region.Contains(user.Position) && user.SkeletonTracked;
		if (!IsInRegion && isInRegionThisFrame) {
			SendMessage("UserIsInRegion", user);
		}
		if (IsInRegion && !isInRegionThisFrame) {
			SendMessage("UserIsNotInRegion", user);
		}
		IsInRegion = isInRegionThisFrame;
	}
}
