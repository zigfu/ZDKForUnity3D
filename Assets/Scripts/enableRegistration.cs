using UnityEngine;
using System.Collections;
using OpenNI;

public class enableRegistration : MonoBehaviour {

	// Use this for initialization
	void Start () {
		DepthGenerator depth = OpenNIContext.OpenNode(NodeType.Depth) as DepthGenerator;
		ImageGenerator image = OpenNIContext.OpenNode(NodeType.Image) as ImageGenerator;
		depth.AlternativeViewpointCapability.SetViewpoint(image);
	}
}
