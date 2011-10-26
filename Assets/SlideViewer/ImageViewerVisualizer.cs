using UnityEngine;
using System.Collections;

public class ImageViewerVisualizer : MonoBehaviour {

    public Transform HandPositionIndicator;
    public float HandPositionIndicatorRange = 5.0f;
    public float rate = 5.0f;
    public bool EndSessionAfterScroll = true;

    public Color DefaultIndicatorColor;
    public Color ScrolledIndicatorColor;

    Fader f;
    ScrollingMenu s;
    Vector3 initialPosition;

	void Start () {
        initialPosition = HandPositionIndicator.localPosition;

        f = GetComponent<Fader>();
        s = GetComponent<ScrollingMenu>();
        if (!HandPositionIndicator) {
            Debug.LogError("No hand position indicator");
        }
        if (!f) {
            Debug.LogError("No fader");
        }
        if (!s) {
            Debug.LogError("No menu");
        }

        HandPositionIndicator.gameObject.GetComponent<SetColor>().color = DefaultIndicatorColor;
	}
	
	// Update is called once per frame
	void Update () {
        if (!HandPositionIndicator||!f||!s) return;

        Vector3 dest = initialPosition + (HandPositionIndicatorRange * s.direction.normalized * (f.value - 0.5f));
        HandPositionIndicator.localPosition = Vector3.Lerp(HandPositionIndicator.localPosition, dest, Time.deltaTime * rate);
	}

    IEnumerator ItemSelector_StartScrolling()
    {
        HandPositionIndicator.gameObject.GetComponent<SetColor>().color = ScrolledIndicatorColor;
        if (EndSessionAfterScroll) {
            yield return new WaitForSeconds(0.1f);
            OpenNISessionManager.Instance.EndSession();
        }
    }

    void ItemSelector_StopScrolling()
    {
        HandPositionIndicator.gameObject.GetComponent<SetColor>().color = DefaultIndicatorColor;
    }

    void OnDrawGizmos()
    {
        ScrollingMenu m = GetComponent<ScrollingMenu>();
        if (!m || !HandPositionIndicator) return;
        Gizmos.color = Color.blue;
        Bounds bbox = HandPositionIndicator.renderer.bounds;
        bbox.Expand(HandPositionIndicatorRange * m.direction.normalized);
        Vector3 pos = HandPositionIndicator.parent.position; //HandPositionIndicator.localToWorldMatrix.MultiplyPoint(new Vector3(0, 0, 0));
        Gizmos.DrawWireCube(pos, bbox.size);
    }
}
