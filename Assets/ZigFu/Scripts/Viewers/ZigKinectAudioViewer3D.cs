using UnityEngine;
using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Text;
using Zigfu.KinectAudio;
using Zigfu.Utility;


public class ZigKinectAudioViewer3D : MonoBehaviour
{
    const string ClassName = "ZigKinectAudioViewer3D";

    const int AudioPollingInterval_MS = 50;
    public int NumRows = 31;

    public int particlesPerRow = 330;
    public uint TotalNumParticles { get { return (uint)(particlesPerRow * (int)NumRows); } }

    public int length = 50;
    public int width = 20;
    public int amplitudeScale = 20;

    public bool verbose = true;


    ZigKinectAudioSource _kinectAudioSource;

    Stream _audioStream;
    byte[] _audioBuffer;
    float[][] _energyBuffers;
    uint _energyBufferStartIndex;

    Particle[] _particles;


    #region Init and Destroy

    void Start()
    {
        if (verbose) { print(ClassName + " :: Start"); }

        _kinectAudioSource = ZigKinectAudioSource.Instance;

        InitAudioAndEnergyBuffers();
        InitParticles();

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

        UInt32 energyBufferSize = (UInt32)particlesPerRow;
        _energyBuffers = new float[NumRows][];
        for (int i = 0; i < _energyBuffers.Length; i++)
        {
            _energyBuffers[i] = new float[energyBufferSize];
        }
        ClearAllEnergyBuffers();
    }

    void InitParticles()
    {
        particleEmitter.emit = false;
        particleEmitter.ClearParticles();
        particleEmitter.Emit((int)TotalNumParticles);

        _particles = particleEmitter.particles;

        InitParticlePositions(_particles);
        InitParticleColors(_particles);

        particleEmitter.particles = _particles;
    }

    void InitParticlePositions(Particle[] particles)
    {
        Vector3 position = Vector3.zero;

        for (int i = 0; i < TotalNumParticles; i++)
        {
            int row = i / particlesPerRow;
            int col = i % particlesPerRow;
            position.x = Mathf.Lerp(0, length, (float)col / particlesPerRow);
            position.y = 0;
            position.z = Mathf.Lerp(0, width, (float)row / NumRows);
            particles[i].position = position;

            particles[i].energy = 1;
        }
    }

    void InitParticleColors(Particle[] particles)
    {
        float c = 1 / 255.0f;
        Color leftColor = new Color(120 * c, 220 * c, 55 * c);
        Color midColor = new Color(225 * c, 40 * c, 119 * c);
        Color rightColor = new Color(15 * c, 190 * c, 240 * c);
        
        Color rowColor = Color.white;
        for (int i = 0; i < TotalNumParticles; i++)
        {
            int row = i / particlesPerRow;
            int col = i % particlesPerRow;
            float rowRatio = (float)row / NumRows;
            
            if (rowRatio < 0.5f)
            {
                rowColor = Color.Lerp(leftColor, midColor, 2 * rowRatio);
            }
            else
            {
                rowColor = Color.Lerp(midColor, rightColor, 2 * (rowRatio - 0.5f));
            }

            // Make particles fade away the further away they are
            rowColor.a = ((float)col / particlesPerRow);

            particles[i].color = rowColor;
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
        catch (Exception e)
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
        _audioStream = null;

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

        UpdateParticles();
    }

    void GetLatestAudioAsEnergy()
    {
        int readCount = _audioStream.Read(_audioBuffer, 0, _audioBuffer.Length);
        if (readCount <= 0)
        {
            return;
        }

        float angle = GetSoundSourceAngleOrBeamAngle();
        uint row = GetRowForSoundSourceAngle(angle);
        float[] energyBuffer = _energyBuffers[row];

        uint numEnergySamplesCreated = AudioToEnergy.Convert(_audioBuffer, readCount, energyBuffer, _energyBufferStartIndex);
        if (numEnergySamplesCreated == 0)
        {
            return;
        }

        float confidence = (float)_kinectAudioSource.SoundSourceAngleConfidence;
        SpreadNewEnergyAcrossRows(row, numEnergySamplesCreated, 1 - confidence);

        _energyBufferStartIndex = (_energyBufferStartIndex + numEnergySamplesCreated) % (uint)particlesPerRow;
    }

    void SpreadNewEnergyAcrossRows(uint originRow, uint numNewEnergySamples, float spreadFactor)
    {
        spreadFactor = Mathf.Clamp(spreadFactor, 0.01f, 1);

        for (uint row = 0; row < NumRows; row++)
        {
            if (row == originRow)
            {
                continue;
            }

            float dist = Mathf.Abs((float)row - originRow) / NumRows;
            float scale = (1 - spreadFactor) * (1 - dist) * (1 - dist) * (1 - dist);
            if (scale <= 0.0001) { scale = 0.0001f; }

            CopyEnergyBufferValues(originRow, row, _energyBufferStartIndex, numNewEnergySamples, scale);
        }
    }

    void UpdateParticles()
    {
        for (uint i = 0; i < NumRows; i++)
        {
            UpdateParticlesInRow(i);
        }
    }

    public void UpdateParticlesInRow(uint row)
    {
        float[] energyBuffer = _energyBuffers[row];

        for (int i = 0; i < particlesPerRow; i++)
        {
            int energyIdx = ((int)_energyBufferStartIndex + i) % particlesPerRow;
            float energy = energyBuffer[energyIdx];
            float newY = amplitudeScale * energy;

            int particleIdx = ((int)row * particlesPerRow) + i;
            Vector3 pos = _particles[particleIdx].position;
            _particles[particleIdx].position = new Vector3(pos.x, newY, pos.z);
        }

        particleEmitter.particles = _particles;
    }

    #endregion


    #region ZigKinectAudioSource Event Handlers

    void MutableAudioCapturingStarted_Handler(object sender, ZigKinectAudioSource.MutableAudioCapturingStarted_EventArgs e)
    {
        if (verbose) { print(ClassName + " :: MutableAudioCapturingStarted_Handler"); }

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
    }

    #endregion


    #region Helper Methods

    float GetSoundSourceAngleOrBeamAngle()
    {
        float angle;

        ZigKinectAudioSource.ZigBeamAngleMode beamAngleMode = _kinectAudioSource.BeamAngleMode;
        if (beamAngleMode == ZigKinectAudioSource.ZigBeamAngleMode.Automatic)
        {
            angle = (float)_kinectAudioSource.SoundSourceAngle;
        }
        else
        {
            angle = (float)_kinectAudioSource.BeamAngle;
        }

        return angle;
    }

    uint GetRowForSoundSourceAngle(float angle)
    {
        float minAngle = (float)ZigKinectAudioSource.MinSoundSourceAngle;
        float maxAngle = (float)ZigKinectAudioSource.MaxSoundSourceAngle;

        float row_float = MathHelper.ConvertFromRangeToRange(minAngle, maxAngle, 0, NumRows, angle);
        uint row = (uint)Mathf.Min(NumRows - 1, Mathf.RoundToInt(row_float));

        return row;
    }

    void ClearAllEnergyBuffers()
    {
        for (uint i = 0; i < _energyBuffers.Length; i++)
        {
            ClearEnergyBufferValues(i, 0, (uint)particlesPerRow);
        }
    }

    void ClearEnergyBufferValues(uint row, uint startIndex, uint numSamplesToClear)
    {
        float[] buffer = _energyBuffers[row];

        for (int i = 0; i < numSamplesToClear; i++)
        {
            int idx = ((int)startIndex + i) % particlesPerRow;
            buffer[idx] = 0.5f;
        }
    }

    void CopyEnergyBufferValues(uint srcRow, uint destRow, uint startIndex, uint numSamplesToCopy, float scale)
    {
        float[] srcBuffer = _energyBuffers[srcRow];
        float[] destBuffer = _energyBuffers[destRow];

        for (int i = 0; i < numSamplesToCopy; i++)
        {
            int idx = ((int)startIndex + i) % particlesPerRow;
            float copiedSample = srcBuffer[idx];
            copiedSample = ((copiedSample - 0.5f) * scale) + 0.5f;
            destBuffer[idx] = copiedSample;
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

    void PrintEnergyBuffer(float[] buffer)
    {
        print("-------  PrintEnergyBuffer  -------");

        StringBuilder sb = new StringBuilder();
        const int inc = 20;
        int numSamples = buffer.Length;
        for (int i = 0; i < numSamples; i += inc)
        {
            float sample = buffer[(_energyBufferStartIndex + i) % numSamples];
            sb.Append(sample.ToString("F2") + ", ");
        }
        print(sb.ToString());

        print("-----------------------------------");
    }

    #endregion
}
