using UnityEngine;
using System.Collections;

public class itemLogi : MonoBehaviour {

	// Use this for initialization
	void Start () {

        renderer.material.color = origColor;
	}

    public Color origColor;
    public Color triggerColor;
    public mouse_event setTransform;
    void OnTriggerEnter(Collider other)
    {
        renderer.material.color = triggerColor;
        if (other.tag == "fingertip")
        {
            if (setTransform != null)

                if (setTransform.transformToFollow1 == null)
                {
                    if (setTransform.transformToFollow2 != null)
                    {
                        if (setTransform.transformToFollow2 != other.transform)
                        {
                            setTransform.transformToFollow1 = other.transform;
                            setTransform.FingersSet();
                        }
                    }
                    else 
                    {
                        setTransform.transformToFollow1 = other.transform;
                    }

                }
                else if ((setTransform.transformToFollow2 == null) && (setTransform.transformToFollow1 != other.transform))
                {
                    setTransform.transformToFollow2 = other.transform;
                    setTransform.FingersSet();
                
                }
        }
    }
    void OnTriggerExit()
    {
        renderer.material.color = origColor;
    }
    float stayTimer = 0f;
	public Transform moveMe;
	public Vector3 direction;
    void OnTriggerStay()
    {
        renderer.material.color = triggerColor;
        stayTimer = 0f;
		
		if (null != moveMe)
		{
			moveMe.transform.position += direction;
		}
    }
    public float timeOut = .1f;
	// Update is called once per frame
	void Update () {

        stayTimer += Time.deltaTime;
        if (stayTimer > timeOut)
        {
            renderer.material.color = origColor;
        }
	}
}
