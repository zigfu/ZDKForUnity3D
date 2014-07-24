using UnityEngine;
using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Text;
using Zigfu.KinectAudio;
using Zigfu.Utility;


public class ZigKinectAudioViewer : MonoBehaviour
{
    const string ClassName = "ZigKinectAudioViewer";

    const int AudioPollingInterval_MS = 50;


    public bool verbose = true;

    public Renderer targetRenderer;
    public int textureWidth = 360;
    public int textureHeight = 70;
    public Color backgroundColor = Color.black;
    public Color waveformColor = Color.white;

    public enum WaveRenderStyle
    {
        Dots,
        VerticalLines
    }
    public WaveRenderStyle renderStyle = WaveRenderStyle.VerticalLines;


    ZigKinectAudioSource _kinectAudioSource;

    Stream _audioStream;
    byte[] _audioBuffer;
    float[] _energyBuffer;
    uint _energyBufferStartIndex;

    Color[] _blankCanvas;
    Texture2D _textureRef;


    // Summary:
    //     This delegate method is responsible for returning the screen area in which to render
    //
    public delegate Rect GetRenderingAreaDelegate(ZigKinectAudioViewer av);
    public GetRenderingAreaDelegate getRenderingArea_Handler;


    #region Init and Destroy

    void Start()
    {
        if (verbose) { print(ClassName + " :: Start"); }

        _kinectAudioSource = ZigKinectAudioSource.Instance;

        InitAudioAndEnergyBuffers();

        CreateBlankCanvas();
        _textureRef = InitTexture();
        InitTargetRendererWithTexture(_textureRef);

        // Receive Notifications when the ZigKinectAudioSource starts/stops audio capturing so we can start/stop the rendering of its AudioStream.
        _kinectAudioSource.MutableAudioCapturingStarted += MutableAudioCapturingStarted_Handler;
        _kinectAudioSource.MutableAudioCapturingStopped += MutableAudioCapturingStopped_Handler;

        StartUpdating();
    }

    void InitAudioAndEnergyBuffers()
    {
        ZigKinectAudioSource.WaveFormat wf = _kinectAudioSource.GetKinectWaveFormat();
        UInt32 audioBufferSize = (UInt32)(AudioPollingInterval_MS * wf.AudioAverageBytesPerSecond * 0.001f);
        _audioBuffer = new byte[audioBufferSize];

        UInt32 energyBufferSize = (UInt32)(textureWidth);
        _energyBuffer = new float[energyBufferSize];
        ClearEnergyBufferValues(energyBufferSize);
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

    void OnDestroy()
    {
        if (verbose) { print(ClassName + " :: OnDestroy"); }

        StopUpdating();

        if (_kinectAudioSource)
        {
            _kinectAudioSource.MutableAudioCapturingStarted -= MutableAudioCapturingStarted_Handler;
            _kinectAudioSource.MutableAudioCapturingStopped -= MutableAudioCapturingStopped_Handler;
        }
    }

    #endregion


    #region Update

    bool _updatingHasStarted = false;
    public void StartUpdating()
    {
        if (verbose) { print(ClassName + " :: StartUpdating"); }

        if (_updatingHasStarted)
        {
            return;
        }

        _updatingHasStarted = true;
        if (!TryStartCapturingAudio(out _audioStream))
        {
            _updatingHasStarted = false;
            return;
        }

        StartCoroutine(Update_Coroutine_MethodName);
    }
    Boolean TryStartCapturingAudio(out Stream audioStream)
    {
        audioStream = null;
        try
        {
            var audioProcessingIntent = ZigKinectAudioSource.ZigAudioProcessingIntent.CaptureMutableAudio;
            audioStream = _kinectAudioSource.StartCapturingAudio(audioProcessingIntent);
        }
        catch(Exception e)
        {
            UnityEngine.Debug.LogException(e);
            return false;
        }
        return true;
    }
    public void StopUpdating()
    {
        if (verbose) { print(ClassName + " :: StopUpdating"); }

        if (!_updatingHasStarted)
        {
            return;
        }

        StopCoroutine(Update_Coroutine_MethodName);

        _updatingHasStarted = false;
    }

    const string Update_Coroutine_MethodName = "Update_Coroutine";
    IEnumerator Update_Coroutine()
    {
        while (true)
        {
            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();
            {
                if (_updatingHasStarted)
                {
                    Update_Tick();
                }
            }
            stopWatch.Stop();

            int elapsedTime = stopWatch.Elapsed.Milliseconds;
            float waitTime = 0.001f * Mathf.Max(0, AudioPollingInterval_MS - elapsedTime);
            yield return new WaitForSeconds(waitTime);
        }
    }

    void Update_Tick()
    {
        GetLatestAudioAsEnergy();

        if (renderStyle == WaveRenderStyle.Dots)
        {
            RenderWaveformAsDots();
        }
        else if (renderStyle == WaveRenderStyle.VerticalLines)
        {
            RenderWaveformAsVerticalLines();
        }
    }

    void GetLatestAudioAsEnergy()
    {
        int readCount = _audioStream.Read(_audioBuffer, 0, _audioBuffer.Length);

        uint numEnergySamplesCreated = AudioToEnergy.Convert(_audioBuffer, readCount, _energyBuffer, _energyBufferStartIndex);
        _energyBufferStartIndex = (_energyBufferStartIndex + numEnergySamplesCreated) % (uint)_energyBuffer.Length;
    }

    #endregion


    #region Render

    void ClearTexture()
    {
        _textureRef.SetPixels(_blankCanvas, 0);
    }

    void RenderWaveformAsDots()
    {
        ClearTexture();

        int numSamples = _energyBuffer.Length;
        float widthRatio = (float)textureWidth / numSamples;
        
        for (var i = 0; i < numSamples; i++)
        {
            float sample = _energyBuffer[(_energyBufferStartIndex + i) % numSamples];
            int x = (int)(widthRatio * i);
            int y = (int)MathHelper.ConvertFromRangeToRange(AudioToEnergy.MinEnergyValue, AudioToEnergy.MaxEnergyValue, 0, textureHeight, sample);
            _textureRef.SetPixel(x, y, waveformColor);
        }

        _textureRef.Apply();
    }

    void RenderWaveformAsVerticalLines()
    {
        ClearTexture();

        int numSamples = _energyBuffer.Length;
        float halfHeight = 0.5f * textureHeight;
        float widthRatio = (float)textureWidth / numSamples;

        Color[] waveformColors = new Color[textureHeight];
        for (int i = 0; i < waveformColors.Length; i++)
        {
            waveformColors[i] = waveformColor;
        }

        for (var i = 0; i < numSamples; i++)
        {
            float sample = _energyBuffer[(_energyBufferStartIndex + i) % numSamples];
            uint x = (uint)(widthRatio * i);

            float amp = Mathf.Abs(MathHelper.ConvertFromRangeToRange(AudioToEnergy.MinEnergyValue, AudioToEnergy.MaxEnergyValue, -halfHeight, halfHeight, sample));
            uint lineStartY = (uint)(halfHeight - amp);
            uint lineHeight = (uint)(2 * amp);

            _textureRef.SetPixels((int)x, (int)lineStartY, 1, (int)lineHeight, waveformColors);
        }

        _textureRef.Apply();
    }

    #endregion


    #region GUI

    void OnGUI()
    {
        GUI.DrawTexture(GetRenderingArea(), _textureRef);
    }

    Rect GetRenderingArea()
    {
        GetRenderingAreaDelegate handler = (null == getRenderingArea_Handler) ? GetRenderingArea_Default : getRenderingArea_Handler;
        return (handler(this));
    }

    Rect GetRenderingArea_Default(ZigKinectAudioViewer av)
    {
        float tW = av.textureWidth;
        float tH = av.textureHeight;
        float sW = Screen.width;
        float sH = Screen.height;

        // Define Bottom-Center Area
        float yPad = 10;
        float x = 0.5f * (sW - tW);
        float y = sH - tH - yPad;

        return new Rect(x, y, tW, tH);
    }

    #endregion


    #region ZigKinectAudioSource Event Handlers

    void MutableAudioCapturingStarted_Handler(object sender, ZigKinectAudioSource.MutableAudioCapturingStarted_EventArgs e)
    {
        if (verbose) { print(ClassName + " :: AudioCapturingStarted_Handler"); }

        _audioStream = e.AudioStream;
        if (!_updatingHasStarted)
        {
            StartUpdating();
        }
    }

    void MutableAudioCapturingStopped_Handler(object sender, EventArgs e)
    {
        if (verbose) { print(ClassName + " :: MutableAudioCapturingStopped_Handler"); }

        if (_updatingHasStarted)
        {
            StopUpdating();
        }
        _audioStream = null;
    }

    #endregion


    #region Helper Methods

    void ClearEnergyBufferValues(uint numSamplesToClear)
    {
        for (int i = 0; i < numSamplesToClear; i++)
        {
            int idx = ((int)_energyBufferStartIndex + i) % _energyBuffer.Length;
            _energyBuffer[idx] = 0.5f;
        }
    }


    void PrintAudioBuffer(int readCount)
    {
        print("-------  PrintAudioBuffer  -------");

        StringBuilder sb = new StringBuilder();
        const int inc = 20;
        for (int i = 0; i < readCount; i += inc)
        {
            float sample = _audioBuffer[i];
            sb.Append(sample.ToString("F2") + ", ");
        }
        print(sb.ToString());

        print("-----------------------------------");
    }

    void PrintEnergyBuffer()
    {
        print("-------  PrintEnergyBuffer  -------");

        StringBuilder sb = new StringBuilder();
        const int inc = 20;
        int numSamples = _energyBuffer.Length;
        for (int i = 0; i < numSamples; i += inc)
        {
            float sample = _energyBuffer[(_energyBufferStartIndex + i) % numSamples];
            sb.Append(sample.ToString("F2") + ", ");
        }
        print(sb.ToString());

        print("-----------------------------------");
    }

    #endregion
}
