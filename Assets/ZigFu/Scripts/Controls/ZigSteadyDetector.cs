using UnityEngine;
using System;
using System.Collections;
using Accord.Statistics.Analysis;

public class ZigSteadyDetector : MonoBehaviour {
	public float maxVariance = 50.0f;
	public float timedBufferSize = 0.5f;
	public float minSteadyTime = 0.1f;
	TimedBuffer<Vector3> points;
	
	// these really should be { get; private set; }
	// but this way they're visible in the inspector
	public bool IsSteady;
    public float Variance;
    public Vector3 steadyPoint;

    public event EventHandler Steady;
    protected virtual void OnSteady() {
        if (null != Steady) {
            Steady.Invoke(this, new EventArgs());
        }
        SendMessage("SteadyDetector_Steady", this, SendMessageOptions.DontRequireReceiver);
    }
	
	// Use this for initialization
	void Start () {
		points = new TimedBuffer<Vector3>(timedBufferSize);
	}
	
	Vector3 GetSingularValues()
	{
		var buffer = points.Buffer;
		if (buffer.Count < 4) {
			return Vector3.zero;
		}
		
		double[,] output = new double[buffer.Count, 3];
		int i = 0;
		foreach(var pt in buffer) {
			Vector3 pos = pt.obj;
			output[i,0] = pos.x;
			output[i,1] = pos.y;
			output[i,2] = pos.z;
			i++;
		}
		PrincipalComponentAnalysis anal = new PrincipalComponentAnalysis(output);
		anal.Compute();

		return new Vector3((float)anal.SingularValues[0], 
		                   (float)anal.SingularValues[1],
		                   (float)anal.SingularValues[2]);
	}
	
	public void Clear()
	{
		StopCoroutine("WaitForSteady");
		points.Clear();	
	}
	
	IEnumerator WaitForSteady()
	{
		yield return new WaitForSeconds(minSteadyTime);
        steadyPoint = points.Buffer[points.Buffer.Count - 1].obj;
        OnSteady();
	}

    void Session_Start(Vector3 focusPoint) {
        Clear();
        ProcessPoint(focusPoint);
    }

    void Session_Update(Vector3 handPoint) {
        ProcessPoint(handPoint);
    }

    void Session_End() {
        Clear();
    }

	void ProcessPoint(Vector3 position)
	{
		// add current point
		points.AddDataPoint(position);
		bool currentFrameSteady = GetSingularValues().x < maxVariance;
        Variance = GetSingularValues().x;
		if (!IsSteady && currentFrameSteady) {
			StartCoroutine("WaitForSteady");
		}
		if (IsSteady && !currentFrameSteady) {
			StopCoroutine("WaitForSteady");
		}
		IsSteady = currentFrameSteady;
	}

    void SteadyDetector_Steady(ZigSteadyDetector sender) {
        Debug.Log(gameObject.name + ": Steady");
    }
}
