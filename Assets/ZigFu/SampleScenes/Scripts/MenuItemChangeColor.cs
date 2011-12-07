using UnityEngine;
using System.Collections;

public class MenuItemChangeColor : MonoBehaviour
{
    public Color defaultColor = Color.white;
    public Color highlightColor = Color.green;
    public Color selectColor = Color.blue;
    public Renderer target;
    public float damping = 5.0f;
    Color targetColor;

    // Use this for initialization
    void Awake()
    {
        if (null == target) {
            target = GetComponent<Renderer>();
        }
        if (null == target) {
            Debug.LogError("Missing a renderer for MenuItemChangeColor");
            return;
        }
        targetColor = defaultColor;
        target.material.color = defaultColor;
    }

    void Update()
    {
        if (!target) return;

        target.material.color = Color.Lerp(target.material.color, targetColor, Time.deltaTime * damping);
    }

    void MenuItem_Highlight()
    {
        targetColor = highlightColor;
    }

    void MenuItem_Unhighlight()
    {
        targetColor = defaultColor;
    }

    void MenuItem_Select()
    {
        targetColor = selectColor;
    }
}
