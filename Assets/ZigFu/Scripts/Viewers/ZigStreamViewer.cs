using UnityEngine;
using System;
using System.Collections.Generic;

public enum ZigStreamType
{
    Depth,
    Image,
}

public class ZigStreamViewer : MonoBehaviour
{
    public ZigStreamType type = ZigStreamType.Depth;
    public Renderer target;

    Texture2D targetTexture;

    void Start()
    {
        if (null == target) {
            target = GetComponent<Renderer>();
        }

        if (ZigInput.Instance.ReaderInited) {
            switch (type) {
                case ZigStreamType.Depth: targetTexture = ZigInput.Instance.reader.GetDepth(); break;
                case ZigStreamType.Image: targetTexture = ZigInput.Instance.reader.GetImage(); break;
            }
        }

        if (null != target) {
            target.material.mainTexture = targetTexture;
        }
    }

    void OnGUI()
    {
        if (null == target && ZigInput.Instance.ReaderInited) {
            GUI.Box(new Rect(Screen.width - 128 - 10, Screen.height - 96 - 10, 128, 96), targetTexture);
        }
    }
}
