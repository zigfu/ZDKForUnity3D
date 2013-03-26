using UnityEngine;
using System.Collections;

public class FaceMover : MonoBehaviour 
{
	public float smooth = 10;
	public bool Mirror = true;

	void Update () 
	{
		if(ZigFaceTracker.FaceTransform.position != Vector3.zero)
		{
			Vector3 newposition = new Vector3(ZigFaceTracker.FaceTransform.position.x,
											  ZigFaceTracker.FaceTransform.position.y,
											  ZigFaceTracker.FaceTransform.position.z); 
			if(Mirror)
				newposition.x *= -1;
			transform.position    = Vector3.Lerp(transform.position,newposition, Time.deltaTime * smooth);
		}
		
		if(ZigFaceTracker.FaceTransform.eulerAngles != Vector3.zero)
			transform.eulerAngles = ZigFaceTracker.FaceTransform.eulerAngles;
	}
}
