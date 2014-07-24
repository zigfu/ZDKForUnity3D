using UnityEngine;
using System;
using System.Collections;

using Zigfu.FaceTracking;


public class MaskedImageViewer : MonoBehaviour
{

    public ZigImageViewer imageViewer;
    public GameObject mask;
    public float maskScale = 1;
    public bool mirrored = false;

    Transform _maskTrans;

    float _xMultiplier = 0.5f;
    float _yMultiplier = 0.5f;


    public ZigFaceTracker ZFT
    {
        get { return ZigFaceTracker.Instance; }
    }
    public ZigFaceTransform FaceTransform
    {
        get { return mirrored ? ZFT.FaceTransformMirrored : ZFT.FaceTransform; }
    }


    void Start()
    {
        _maskTrans = mask.transform;

        ZigInput.Instance.AddListener(gameObject);
    }


    void Zig_Update(ZigInput input)
    {
        imageViewer.mirrored = this.mirrored;

        UpdateMask();
    }

    void UpdateMask()
    {
        ZigFaceTransform trans = FaceTransform;

        Vector3 newPos = trans.position;
        newPos.x *= _xMultiplier;
        newPos.y *= _yMultiplier;
        _maskTrans.localPosition = newPos;

        Quaternion newRot = Quaternion.Euler(trans.eulerAngles);
        _maskTrans.localRotation = newRot;

        _maskTrans.localScale = new Vector3(maskScale, maskScale, maskScale);
    }

}
