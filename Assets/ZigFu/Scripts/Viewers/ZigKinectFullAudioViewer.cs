using UnityEngine;
using System.Collections;

[RequireComponent(typeof(ZigKinectAudioViewer))]
[RequireComponent(typeof(ZigKinectBeamAngleViewer))]

public class ZigKinectFullAudioViewer : MonoBehaviour {

    ZigKinectAudioViewer _audioViewer;
    ZigKinectBeamAngleViewer _beamAngleViewer;


    void Start()
    {
        _audioViewer = GetComponent<ZigKinectAudioViewer>();
        _audioViewer.getRenderingArea_Handler = GetAudioRenderingArea;

        _beamAngleViewer = GetComponent<ZigKinectBeamAngleViewer>();
        _beamAngleViewer.getRenderingArea_Handler = GetBeamAngleRenderingArea;
    }


    #region RenderingArea Delegate Handlers

    Rect GetAudioRenderingArea(ZigKinectAudioViewer av)
    {
        float tW = av.textureWidth;
        float tH = av.textureHeight;
        float sW = Screen.width;
        float sH = Screen.height;

        // Define Bottom-Center Area
        float bvHeight = _beamAngleViewer.textureHeight;
        float yPad = 20;
        float x = 0.5f * (sW - tW);
        float y = sH - tH - yPad - bvHeight;

        return new Rect(x, y, tW, tH);
    }

    Rect GetBeamAngleRenderingArea(ZigKinectBeamAngleViewer bv)
    {
        Rect wfArea = GetAudioRenderingArea(_audioViewer);

        // Render the beamAngleViewer directly beneath the audioViewer
        float tW = bv.textureWidth;
        float tH = bv.textureHeight;

        float yPad = 10;
        float x = wfArea.x;
        float y = wfArea.yMax + yPad;

        return new Rect(x, y, tW, tH);
    }

    #endregion

}
