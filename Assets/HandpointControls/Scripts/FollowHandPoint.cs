using UnityEngine;
using System;

public class FollowHandPoint : MonoBehaviour
{
	// default scale convert openni (millimeters) to unity (meters)
	public Vector3 Scale = new Vector3(0.02f, 0.02f, -0.02f);
	public Vector3 bias;
	public float damping = 5;

    public Vector3 bounds = new Vector3(10, 10, 10);

	private Vector3 desiredPos;
    public bool Absolute = false;
	
	void Start()
	{
		// make sure we get hand points
		if (null == GetComponent<HandPointControl>())
		{
			gameObject.AddComponent<HandPointControl>();
		}
		
		desiredPos = transform.localPosition;
	}
	
	void Update()
	{
		transform.localPosition = Vector3.Lerp(transform.localPosition,  desiredPos, damping * Time.deltaTime);
	}

    Vector3 ClampVector(Vector3 vec, Vector3 min, Vector3 max)
    {
        return new Vector3(Mathf.Clamp(vec.x, min.x, max.x),
                           Mathf.Clamp(vec.y, min.y, max.y),
                           Mathf.Clamp(vec.z, min.z, max.z));
    }

	Vector3 OpenNIToUnity (Vector3 pos)
	{
        Vector3 result = pos;
        if (!Absolute) {
            result -= OpenNISessionManager.FocusPoint;
        }
		result = Vector3.Scale(result, Scale) + bias;
        return ClampVector(result, -0.5f * bounds, 0.5f * bounds);
	}
	
	void Hand_Create(Vector3 pos)
	{
	
		desiredPos = OpenNIToUnity(pos);
	}
	
	void Hand_Update(Vector3 pos)
	{
		desiredPos = OpenNIToUnity(pos);
	}
	
	void Hand_Destroy()
	{
        desiredPos = Vector3.zero;
	}

    void OnDrawGizmos()
    {
        Gizmos.color = Color.blue;
        if (transform.parent) {
            Gizmos.DrawWireCube(transform.parent.position, bounds);
        }
        else {
            //TODO
        }
    }
}