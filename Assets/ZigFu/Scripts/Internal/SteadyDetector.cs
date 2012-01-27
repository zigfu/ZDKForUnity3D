using UnityEngine;
using System.Collections;
using Accord.Statistics.Analysis;

public class SteadyDetector : MonoBehaviour {
	public Transform target;
	public float maxVariance = 50;
	public float timedBufferSize = 0.5f;
	public float minSteadyTime = 0.3f;
	TimedBuffer<Vector3> points;

	public bool IsSteady { get; private set; }
	
	// Use this for initialization
	void Start () {
		points = new TimedBuffer<Vector3>(timedBufferSize);
	}
	
	void FixedUpdate () {
		if (target) {
			points.AddDataPoint(transform.position);
			bool currentFrameSteady = GetSingularValues().x < maxVariance;
			if (!IsSteady && currentFrameSteady) {
				StartCoroutine("PleaseWaitForSteady");
			}
			if (IsSteady && !currentFrameSteady) {
				StopCoroutine("PleaseWaitForSteady");
			}
			IsSteady = currentFrameSteady;
		} else {
			IsSteady = false;
		}

	}
	
	void AddPoint(Vector3 point)
	{
		points.AddDataPoint(point);
	}
	
	public void Clear()
	{
		StopCoroutine("WaitForSteady");
		target = null;
		points.Clear();	
	}
	
	IEnumerator PleaseWaitForSteady()
	{
		yield return new WaitForSeconds(minSteadyTime);
		if (null != target) {
			SendMessage("SteadyDetector_Steady", target, SendMessageOptions.DontRequireReceiver);
		}
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
}
