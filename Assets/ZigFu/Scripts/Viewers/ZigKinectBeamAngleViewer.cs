using UnityEngine;
using System;
using Zigfu.KinectAudio;
using Zigfu.Utility;


public class ZigKinectBeamAngleViewer : MonoBehaviour {

    const string ClassName = "ZigKinectBeamAngleViewer";


    public Renderer targetRenderer;
    public int textureWidth = 360;            
    public int textureHeight = 40;
    public Color backgroundColor = Color.black;
    public Color beamAngleColor = Color.cyan;
    public Color sourceAngleColor = Color.green;

    public bool verbose = true;


    public int BeamAngle { get { return (int)_kinectAudioSource.BeamAngle; } }
    public int SourceAngle { get { return (int)_kinectAudioSource.SoundSourceAngle; } }
    public double SourceConfidence { get { return _kinectAudioSource.SoundSourceAngleConfidence; } }


    Color[] _blankCanvas;
    Texture2D _textureRef;

    // The areas of the texture that the indicators will be rendered within
    Rect _beamAngleIndicatorArea;
    Rect _soundSourceAngleIndicatorArea;

    ZigKinectAudioSource _kinectAudioSource;

    uint IndicatorWidth { get { return (uint)(textureWidth / 11); } }

    // Summary:
    //     This delegate method is responsible for returning the screen area to render to
    //
    public delegate Rect GetRenderingAreaDelegate(ZigKinectBeamAngleViewer bv);
    public GetRenderingAreaDelegate getRenderingArea_Handler;


    #region Init and Destroy

    void Start()
    {
        if (verbose) { print(ClassName + "::Start"); }

        _kinectAudioSource = ZigKinectAudioSource.Instance;
        AttachKinectAudioSourceEventHandlers(_kinectAudioSource);

        CreateBlankCanvas();
        _textureRef = InitTexture();
        InitTargetRendererWithTexture(_textureRef);
        InitIndicatorAreas();

        RenderBeamAngleIndicator(_kinectAudioSource.BeamAngle);
        RenderSoundSourceAngleIndicator(_kinectAudioSource.SoundSourceAngle, _kinectAudioSource.SoundSourceAngleConfidence);
    }

    void CreateBlankCanvas()
    {
        _blankCanvas = new Color[textureWidth * textureHeight];
        for (var i = 0; i < _blankCanvas.Length; i++)
        {
            _blankCanvas[i] = backgroundColor;
        }
    }

    Texture2D InitTexture()
    {
        _textureRef = new Texture2D(textureWidth, textureHeight);
        _textureRef.wrapMode = TextureWrapMode.Clamp;

        return _textureRef;
    }

    void InitTargetRendererWithTexture(Texture2D pTexture)
    {
        if (targetRenderer == null)
        {
            targetRenderer = renderer;
        }

        if (null != targetRenderer)
        {
            targetRenderer.material.mainTexture = pTexture;
        }
    }

    void InitIndicatorAreas()
    {
        float beamAreaHeight = textureHeight * 0.5f;
        float beamAreaTop = textureHeight - beamAreaHeight;
        _beamAngleIndicatorArea = new Rect(0, beamAreaTop, textureWidth, beamAreaHeight);

        float srcAreaHeight = textureHeight - beamAreaHeight;
        _soundSourceAngleIndicatorArea = new Rect(0, 0, textureWidth, srcAreaHeight);
    }

    void AttachKinectAudioSourceEventHandlers(ZigKinectAudioSource pKinectAudioSource)
    {
        pKinectAudioSource.BeamAngleChanged += BeamAngleChangedHandler;
        pKinectAudioSource.SoundSourceAngleChanged += SoundSourceAngleChangedHandler;
    }

    void OnDestroy()
    {
        if (verbose) { print(ClassName + " :: OnDestroy"); }

        if (_kinectAudioSource)
        {
            _kinectAudioSource.BeamAngleChanged -= BeamAngleChangedHandler;
            _kinectAudioSource.SoundSourceAngleChanged -= SoundSourceAngleChangedHandler;
        }
    }

    #endregion


    #region BeamAngle Event Handlers

    void BeamAngleChangedHandler(object sender, ZigKinectAudioSource.BeamAngleChanged_EventArgs e)
    {
        if (verbose) { print(ClassName + "::BeamAngleChangedHandler : Angle = " + (int)e.Angle); }

        RenderBeamAngleIndicator(e.Angle);
    }

    void SoundSourceAngleChangedHandler(object sender, ZigKinectAudioSource.SoundSourceAngleChanged_EventArgs e)
    {
        if (verbose) { print(ClassName + "::SoundSourceAngleChangedHandler : Angle = " + (int)e.Angle + ", Confidence = " + e.ConfidenceLevel.ToString("F2")); }

        RenderSoundSourceAngleIndicator(e.Angle, e.ConfidenceLevel);
    }

    #endregion


    #region Rendering

    void RenderBeamAngleIndicator(double beamAngle)
    {
        Rect bounds = _beamAngleIndicatorArea;

        int centerX = (int)ConvertBeamAngleToXPos(beamAngle);
        int centerY = (int)bounds.center.y;
        uint areaWidth = IndicatorWidth;
        uint areaHeight = (uint)bounds.height;

        ClearArea(bounds);
        RenderSolidRectangle(centerX, centerY, areaWidth, areaHeight, bounds, beamAngleColor);

        _textureRef.Apply();
    }

    void RenderSoundSourceAngleIndicator(double soundSourceAngle, double confidenceLevel)
    {
        Rect bounds = _soundSourceAngleIndicatorArea;

        int centerX = (int)ConvertBeamAngleToXPos(soundSourceAngle);
        int centerY = (int)bounds.center.y;
        uint areaWidth = IndicatorWidth;
        uint areaHeight = (uint)bounds.height;

        ClearArea(bounds);
        RenderSolidRectangle(centerX, centerY, areaWidth, areaHeight, bounds, sourceAngleColor);

        _textureRef.Apply();
    }

    void RenderSolidRectangle(int centerX, int centerY, uint width, uint height, Rect bounds, Color color)
    {
        int x = (int)(centerX - width * 0.5f);
        x = (int)Mathf.Clamp((float)x, bounds.xMin, bounds.xMax);

        int y = (int)(centerY - height * 0.5f);
        y = (int)Mathf.Clamp((float)y, bounds.yMin, bounds.yMax);

        Color[] colors = new Color[width * height];
        for (int i = 0; i < colors.Length; i++)
        {
            colors[i] = color;
        }

        _textureRef.SetPixels(x, y, (int)width, (int)height, colors);
    }

    void ClearArea(Rect r)
    {
        _textureRef.SetPixels((int)r.xMin, (int)r.yMin, (int)r.width, (int)r.height, _blankCanvas, 0);
    }

    #endregion


    #region OnGUI

    void OnGUI()
    {
        Rect r = GetRenderingArea();
        GUI.DrawTexture(r, _textureRef);
        GUI_Labels(r);
    }

    void GUI_Labels(Rect position)
    {
        Rect p = position;
        int halfHeight = (int)(0.5f * p.height);

        Rect r = new Rect(p.x, p.y, p.width, halfHeight);
        string text = String.Format(" {0,-8} {1,3}", "Beam:", BeamAngle);
        GUI.Label(r, text);

        r = new Rect(p.x, p.y + halfHeight, p.width, halfHeight);
        text = String.Format(" {0,-8} {1,3} {2,6}", "Source:", SourceAngle, "(" + SourceConfidence.ToString("##0%") + ")");
        GUI.Label(r, text);
    }

    Rect GetRenderingArea()
    {
        GetRenderingAreaDelegate handler = (null == getRenderingArea_Handler) ? GetRenderingArea_Default : getRenderingArea_Handler;
        return(handler(this));
    }

    Rect GetRenderingArea_Default(ZigKinectBeamAngleViewer bv)
    {
        float tW = bv.textureWidth;
        float tH = bv.textureHeight;
        float sW = Screen.width;
        float sH = Screen.height;

        // Define Bottom-Center Area
        float yPad = 10;
        float x = 0.5f * (sW - tW);
        float y = sH - tH - yPad;

        return new Rect(x, y, tW, tH);
    }

    #endregion


    #region Helper Methods

    float ConvertBeamAngleToXPos(double beamAngle)
    {
        float minAngle = (float)ZigKinectAudioSource.MinBeamAngle;
        float maxAngle = (float)ZigKinectAudioSource.MaxBeamAngle;
        return MathHelper.ConvertFromRangeToRange(minAngle, maxAngle, 0, _beamAngleIndicatorArea.width - 1, (float)beamAngle);
    }
    float ConvertSoundSourceAngleToXPos(double soundSourceAngle)
    {
        float minAngle = (float)ZigKinectAudioSource.MinSoundSourceAngle;
        float maxAngle = (float)ZigKinectAudioSource.MaxSoundSourceAngle;
        return MathHelper.ConvertFromRangeToRange(minAngle, maxAngle, 0, _soundSourceAngleIndicatorArea.width - 1, (float)soundSourceAngle);
    }

    #endregion
}
