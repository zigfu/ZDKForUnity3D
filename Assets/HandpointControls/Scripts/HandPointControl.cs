using UnityEngine;
using System;

public class HandPointControl : MonoBehaviour
{
	public bool ActiveOnStart = true;
	public bool IsActive { get; private set; }

    bool started = false;

	void Hand_Create(Vector3 pos)
	{
		
	}
	
	void Hand_Update(Vector3 pos)
	{
		
	}
	
	void Hand_Destroy()
	{
		
	}

    void Start()
    {
        if (ActiveOnStart) {
            OpenNISessionManager.AddListener(this.gameObject);
            IsActive = true;
        }
        started = true;
    }

	public void Activate()
	{
		IsActive = true;
		OpenNISessionManager.AddListener(this.gameObject);
	}
	
	public void Deactivate()
	{
        IsActive = false;
		OpenNISessionManager.RemoveListener(this.gameObject);
	}

	void OnEnable()
	{
		// only add to session manager by default if not navigable
        // we test "started" since OnEnable is called before Start and we dont
        // want to cause openni to init just yet (it messes up the singleton)
        if (IsActive && started)
		{
			OpenNISessionManager.AddListener(this.gameObject);
            IsActive = true;
		}
	}
	
	void OnDisable()
	{
		OpenNISessionManager.RemoveListener(this.gameObject);
	}
	
	void Navigator_Activate()
	{
		Activate();
	}

	void Navigator_Deactivate()
	{
		Deactivate();
	}
	
	protected Vector3 FocusPoint { get { return OpenNISessionManager.FocusPoint; } }
}