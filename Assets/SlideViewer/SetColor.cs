using UnityEngine;
using System.Collections;

public class SetColor : MonoBehaviour
{
    public Color color;
	public Renderer target;

	void Awake()
	{
		if (null == target) {
			target = GetComponent<Renderer>();
		}
	}
	
    void Start()
    {
        target.material.color = color;
    }
}
