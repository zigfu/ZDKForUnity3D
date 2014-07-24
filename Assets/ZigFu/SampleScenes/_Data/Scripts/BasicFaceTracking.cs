using UnityEngine;
using System;
using System.Collections;

using Zigfu.FaceTracking;


public class BasicFaceTracking : MonoBehaviour
{

    public GameObject head;
    public bool mirrored = false;

    public float cameraFollowDistance = 6;

    public float translationMultiplier = 1;
    public float rotationMultiplier = 1;


    public ZigFaceTracker ZFT { 
        get { return ZigFaceTracker.Instance; } 
    }
    public ZigFaceTransform FaceTransform {
        get { return mirrored ? ZFT.FaceTransformMirrored : ZFT.FaceTransform; } 
    }


    Transform _cameraTransform;


	void Start () 
    {
        _cameraTransform = Camera.main.transform;

        ZigInput.Instance.AddListener(gameObject);
	}
	

    void Zig_Update(ZigInput input)
    {
        UpdateHead();
        UpdateCamera();
	}

    void UpdateHead()
    {
        ZigFaceTransform trans = FaceTransform;
        head.transform.localPosition    = trans.position * translationMultiplier;
        head.transform.localEulerAngles = trans.eulerAngles * rotationMultiplier;

        //print(trans);
    }

    void UpdateCamera()
    {
        Vector3 offset = Vector3.zero;
        offset.z = mirrored ? cameraFollowDistance : -cameraFollowDistance;
        _cameraTransform.position = head.transform.position + offset;

        Vector3 newRot = _cameraTransform.eulerAngles;
        newRot.y = mirrored ? 180 : 0;
        _cameraTransform.eulerAngles = newRot;  
    }

}
