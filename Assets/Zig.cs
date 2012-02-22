using UnityEngine;
using System.Collections;

public class Zig : MonoBehaviour {
    public ZigInputType inputType = ZigInputType.OpenNI;
    public bool UpdateDepthmapImage = false;
    public bool UpdateImagemapImage = false;

	void Awake () {
        #if UNITY_WEBPLAYER
        #if UNITY_EDITOR
        Debug.LogError("Depth camera input will not work in editor when target platform is Webplayer. Please change target platform to PC/Mac standalone.");
        return;
        #endif
        #endif

        ZigInput.InputType = inputType;
        ZigInput.UpdateDepth = UpdateDepthmapImage;
        ZigInput.UpdateImage = UpdateImagemapImage;
        ZigInput.Instance.AddListener(gameObject);
	}
}
