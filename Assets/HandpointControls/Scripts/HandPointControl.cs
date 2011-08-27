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
            SessionManager.AddListener(this.gameObject);
            IsActive = true;
        }
        started = true;
    }

	void OnEnable()
	{
		// only add to session manager by default if not navigable
        // we test "started" since OnEnable is called before Start and we dont
        // want to cause openni to init just yet (it messes up the singleton)
        if (IsActive && started)
		{
			SessionManager.AddListener(this.gameObject);
            IsActive = true;
		}
	}
	
	void OnDisable()
	{
		SessionManager.RemoveListener(this.gameObject);
	}
	
	void Navigator_Activate()
	{
        IsActive = true;
		SessionManager.AddListener(this.gameObject);
	}

	void Navigator_Deactivate()
	{
        IsActive = false;
		SessionManager.RemoveListener(this.gameObject);
	}
	
	protected Vector3 FocusPoint { get { return SessionManager.FocusPoint; } }
}