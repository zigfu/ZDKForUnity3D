using UnityEngine;
using Zigfu.FaceTracking;


[RequireComponent (typeof(Camera))]
public class HeadTrackedCamera : MonoBehaviour
{
    public float smoothAmount = 0.7f;
    public bool disableZTilt = true;

    public float translationMultiplier = 1;
    public float rotationMultiplier = 1;

	
	void Start () 
    {
        ZigInput.Instance.AddListener(gameObject);
	}

    ZigFaceTransform oldTrans = new ZigFaceTransform();
    void Zig_Update(ZigInput input)
    {
        UpdateCamera();
	}

    void UpdateCamera()
    {
        ZigFaceTransform targetTrans = ZigFaceTracker.Instance.FaceTransform;

        if (disableZTilt) { targetTrans.eulerAngles.z = 0; }

        float lerpAmt = Mathf.Max(1 - smoothAmount, 0.1f);
        ZigFaceTransform newTrans = ZigFaceTransform.Lerp(oldTrans, targetTrans, lerpAmt);

        camera.transform.localPosition = newTrans.position * translationMultiplier;
        camera.transform.localEulerAngles = newTrans.eulerAngles * rotationMultiplier;

        oldTrans = newTrans;
    }
}
