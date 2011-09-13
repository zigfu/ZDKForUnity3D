using UnityEngine;
using System.Collections;

public class SetColor : MonoBehaviour
{
    public Color color;
    public bool Smooth = true;
    public float rate = 5.0f;

	

    void Update()
    {
        if (Smooth) {
            renderer.material.color = Color.Lerp(renderer.material.color, color, Time.deltaTime * rate);
        } else {
            renderer.material.color = color;
        }
    }
}
