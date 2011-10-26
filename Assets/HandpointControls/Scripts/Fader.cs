using UnityEngine;
using System.Collections;

[RequireComponent(typeof(HandPointControl))]
public class Fader : MonoBehaviour {
	public Vector3 direction = Vector3.right;
	public float size = 200;
    public float initialValue = 0.5f;

    public float value { get; private set; }

    Vector3 mirrorIndependentDirection {
        get {
            return (OpenNIContext.Instance.Mirror) ? direction : Vector3.Scale(direction, new Vector3(-1, 1, 1));
        }
    }

    Vector3 start;
	
	// move the slider to contain pos within its bounds
	public void MoveToContain(Vector3 pos)
	{
		float dot = Vector3.Dot(mirrorIndependentDirection, pos - start);
		if (dot > size)
		{
			start += mirrorIndependentDirection * (dot - size);
		}
		if (dot < 0)
		{
			start += mirrorIndependentDirection * dot;
		}
	}
	
	// move the slider so that pos will be mapped to val (0-1)
	public void MoveTo(Vector3 pos, float val)
	{
        start = pos - (mirrorIndependentDirection * (val * size));
	}
	
	public float GetValue(Vector3 pos)
	{
		float dot = Vector3.Dot(mirrorIndependentDirection, pos - start);
        float val = Mathf.Clamp01(dot / size);
        return val;
    }
	
	public Vector3 GetPosition(float val)
	{
        return start + (mirrorIndependentDirection * (val * size));
	}

    // hand point control messages
    void Hand_Create(Vector3 pos)
    {
        MoveTo(OpenNISessionManager.FocusPoint, initialValue);
        value = initialValue;
    }

    public void Hand_Update(Vector3 pos)
    {
        value = GetValue(pos);
    }

    void Hand_Destroy()
    {
        value = initialValue;
    }

    void Start()
    {
        value = initialValue;
    }

    void SessionManager_Visualize()
    {
        GUILayout.Label("- Fader");
        GUILayout.HorizontalSlider(value, 0.0f, 1.0f);
    }
}
