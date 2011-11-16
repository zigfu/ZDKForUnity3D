using UnityEngine;
using System.Collections;

public class RotateWithHands : MonoBehaviour {

    public Transform LeftHand;
    public Transform RightHand;
    public Vector3 FirstLeft;
    public Vector3 FirstRight;
    public Vector3 Center;

    public Quaternion startRotation;//initial rotation upon first tracking
    public Quaternion targetRotation;
    public float targetZoom;

    public float turnRate = 0.2f;
    public float zoomRate = 0.2f;
    public float zoomFactor = 4e6f; // not sure this should really be over a million ... might wanna look into that
    public float initialDistance;

    public bool initialized { get; private set; }

	// Use this for initialization
	void Start () {
        initialized = false;
	}

    void initialize()
    {
        print("Initializing...");
        FirstLeft = LeftHand.position;
        FirstRight = RightHand.position;
        initialDistance = Vector3.Magnitude(FirstRight - FirstLeft);
        print("First left hand: "+FirstLeft);
        print("First right hand: " + FirstRight);
        Center = (FirstLeft + FirstRight) / 2.0f; // between the two hands
        startRotation = transform.rotation;

        initialized = true;
    }

	// Update is called once per frame
	void Update () {
        if (!initialized)
        {
            if  (    
                    Vector3.Magnitude(LeftHand.position) >= Vector3.kEpsilon && 
                    Vector3.Magnitude(RightHand.position) >= Vector3.kEpsilon &&
                    Vector3.Magnitude(RightHand.position - LeftHand.position) >= Vector3.kEpsilon
                )
            {
                initialize();
            }
            else
            {
                return;
            }
        }
        Vector3 leftPos = LeftHand.position;
        Vector3 rightPos = RightHand.position;
        Vector3 rightwards = Vector3.Normalize(rightPos - leftPos);
        Vector3 forwards = Vector3.Cross(rightwards, Vector3.up); // Vector3.Normalize(transform.position - Center);
        Vector3 upwards = Vector3.Cross(forwards,rightwards); // remember left-handed coordinate system
        targetRotation = Quaternion.Inverse(Quaternion.LookRotation(forwards,upwards));
        transform.rotation = Quaternion.Slerp(transform.rotation,targetRotation*targetRotation, turnRate); // notice the double rotation
        float distance = Vector3.Magnitude(rightPos - leftPos);
        float zoomScale = distance; // / initialDistance;
        float targetZoom = zoomScale * zoomScale * zoomFactor; // quadratic! exciting! but it doesn't have to be quadratic
        transform.localScale = Vector3.one * Mathf.Lerp(transform.localScale.x, targetZoom, zoomRate);
        //print("transform.localScale: " + transform.localScale.x + " targetZoom: "+ targetZoom);
	}
}
