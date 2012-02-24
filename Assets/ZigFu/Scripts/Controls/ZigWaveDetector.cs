using UnityEngine;
using System;
using System.Collections.Generic;

public class ZigWaveDetector : MonoBehaviour {
    public int Waves = 5;
    ZigFader waveFader;
    List<float> timestampBuffer;

    float lastEdge;

    void Awake() {
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
            SendMessage("WaveDetector_Wave", this, SendMessageOptions.DontRequireReceiver);
            timestampBuffer.Clear();
        }
    }
}
