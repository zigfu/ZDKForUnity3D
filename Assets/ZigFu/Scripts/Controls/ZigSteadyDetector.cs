using UnityEngine;
using System.Collections;
using OpenNI;
using Accord.Statistics.Analysis;

public enum SteadyDetectorType
{
	HandSession,
	SkeletonJoint,
}

public class ZigSteadyDetector : MonoBehaviour {
	public SteadyDetectorType type = SteadyDetectorType.HandSession;
	public SkeletonJoint joint;
	public float maxVariance = 50.0f;
	public float timedBufferSize = 0.5f;
	public float minSteadyTime = 0.1f;
	TimedBuffer<Vector3> points;
	
	// this really should be { get; private set; }
	// but this way its visible in the inspector
	public bool IsSteady; 
	
	
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
	
	IEnumerator PleaseWaitForSteady()
	{
		yield return new WaitForSeconds(minSteadyTime);
		SendMessage("SteadyDetector_Steady", joint, SendMessageOptions.DontRequireReceiver);
	}
	/*
	void Zig_OnUserUpdate(ZigEventArgs args)
	{
		if (type != SteadyDetectorType.SkeletonJoint) {
			return;
		}
		
		// no skeleton? not interesting
		if (!args.user.SkeletonTracked) {
			IsSteady = false;
			return;
		}
		
		// add current point
		ProcessPoint(args.user.Joints[joint].position);
	}
	
	void Zig_OnSessionUpdate(ZigEventArgs args)
	{
		if (type != SteadyDetectorType.HandSession) {
			return;
		}
		
		ProcessPoint(args.HandPosition);
	}*/
	
	void ProcessPoint(Vector3 position)
	{
		// add current point
		points.AddDataPoint(position);
		bool currentFrameSteady = GetSingularValues().x < maxVariance;
		if (!IsSteady && currentFrameSteady) {
			StartCoroutine("PleaseWaitForSteady");
		}
		if (IsSteady && !currentFrameSteady) {
			StopCoroutine("PleaseWaitForSteady");
		}
		IsSteady = currentFrameSteady;
	}
}
