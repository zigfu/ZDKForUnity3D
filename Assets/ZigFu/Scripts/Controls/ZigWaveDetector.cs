using UnityEngine;
using System;
using System.Collections.Generic;

public class ZigWaveDetector : MonoBehaviour {
    public int Waves = 5;
    ZigFader waveFader;
    List<float> timestampBuffer;

    float lastEdge;

    public Vector3 wavePoint { get; private set; }

    public event EventHandler Wave;
    protected virtual void OnWave() {
        SendMessage("WaveDetector_Wave", this, SendMessageOptions.DontRequireReceiver);
        if (null != Wave) {
            Wave.Invoke(this, new EventArgs());
        }
    }

    void Awake() {
        timestampBuffer = new List<float>();
        waveFader = gameObject.AddComponent<ZigFader>();
        // TODO: Init fader with drift, size
    }

    void Fader_Edge(ZigFader f) {
        if (f != waveFader) return;
        
        // prune
        while (timestampBuffer.Count > 0 && (Time.time - timestampBuffer[0] > 2.0f)) {
            timestampBuffer.RemoveAt(0);
        }

        if (timestampBuffer.Count == 0) {
            lastEdge = -1;
        }

        if (!Mathf.Approximately(lastEdge, f.value)) {
            timestampBuffer.Add(Time.time);
        }

        lastEdge = f.value;
        if (timestampBuffer.Count >= Waves) {
            wavePoint = waveFader.GetPosition(0.5f);
            OnWave();
            timestampBuffer.Clear();
        }
    }

    void WaveDetector_Wave(ZigWaveDetector wd) {
        Debug.Log("Wave Detector wave detected");
    }
}
