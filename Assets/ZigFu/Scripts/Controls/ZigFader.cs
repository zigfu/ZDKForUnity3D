using UnityEngine;
using System.Collections;

public class ZigFader : MonoBehaviour {
	public Vector3 direction = Vector3.right;
	public float size = 200;
    public float initialValue = 0.5f;

    public float value { get; private set; }

    Vector3 mirrorIndependentDirection {
        get {
            //return (OpenNIContext.Instance.Mirror) ? direction : Vector3.Scale(direction, new Vector3(-1, 1, 1));
			return direction; // assumes mirror is always on
        }
    }

    Vector3 start;
	
	// move the slider to contain pos within its bounds
	public void MoveToContain(Vector3 pos)
	{
		float dot = Vector3.Dot(mirrorIndependentDirection, pos - start);
		if (dot > size) {
			start += mirrorIndependentDirection * (dot - size);
		}
		if (dot < 0) {
			start += mirrorIndependentDirection * dot;
		}
	}
	
	// move the slider so that pos will be mapped to val (0-1)
	public void MoveTo(Vector3 pos, float val)
	{
        start = pos - (mirrorIndependentDirection * (val * size));
	}
	
	public void ForceUpdate(Vector3 pos)
	{
		value = GetValue(pos);
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
    void Zig_OnSessionStart(ZigEventArgs args)
    {
        MoveTo(args.sender.FocusPoint, initialValue);
        value = initialValue;
    }

    void Zig_OnSessionUpdate(ZigEventArgs args)
    {
        value = GetValue(args.HandPosition);
    }

    void Zig_OnSessionEnd()
    {
        value = initialValue;
    }

    void Start()
    {
        value = initialValue;
    }
	
	void Zig_Visualize()
	{
		
	}
}
