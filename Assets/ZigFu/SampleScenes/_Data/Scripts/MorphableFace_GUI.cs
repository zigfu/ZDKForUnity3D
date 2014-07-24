using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;


[RequireComponent(typeof(MorphableFace))]


// Summary:
//      A Graphical User Interface (GUI) for the MorphableFace.
//       This class maps the value of user-controlled GUI Sliders
//       to the model's facial expressions (controlled via the MorphableFace.MorphableFeatures).
//
public class MorphableFace_GUI : MonoBehaviour
{

    MorphableFace _morphableFace;

    
    void Start()
    {
        _morphableFace = gameObject.GetComponent<MorphableFace>() as MorphableFace;
    }


    void OnGUI()
    {
        // Get the GUI's latest Slider values, and use them to update the associated MorphableFace's MorphCoefficients

        Rect area = new Rect(50, 300, 200, 400);

        GUILayout.BeginArea(area);
        GUILayout.BeginVertical();
        {
            GUILayout.Label("Morphable Feature Coefficients");

            OverrideNUI_GUI();

            foreach (MorphableFace.MorphableFeature mf in Enum.GetValues(typeof(MorphableFace.MorphableFeature)))
            {
                MorphableFeature_GUI(mf);
            }
        }
        GUILayout.EndVertical();
        GUILayout.EndArea();
    }


    bool _overrideNUI = false;
    void OverrideNUI_GUI()
    {
        GUILayout.BeginHorizontal();
        {
            MorphableFace_NUI nui = gameObject.GetComponent<MorphableFace_NUI>();
            _overrideNUI = GUILayout.Toggle(_overrideNUI, "Override NUI");
            nui.enabled = !_overrideNUI;

            if (_overrideNUI)
            {
                if (GUILayout.Button("Reset All"))
                {
                    _morphableFace.Reset();
                }
            }
        }
        GUILayout.EndHorizontal();
    }

    void MorphableFeature_GUI(MorphableFace.MorphableFeature mf)
    {
        float min = MorphableFace.MinMorphCoeff;
        float max = MorphableFace.MaxMorphCoeff;

        float oldVal = _morphableFace.GetMorphCoefficient(mf);
        bool oldEnabled = _morphableFace.GetMorphEnabled(mf);

        GUILayout.BeginHorizontal();
        {
            // Enabled?
            string name = mf.ToString();
            bool newEnabled = GUILayout.Toggle(oldEnabled, " " + name);
            if (newEnabled != oldEnabled) { _morphableFace.SetMorphEnabled(mf, newEnabled); }

            // Value
            GUILayout.Label(oldVal.ToString("0.00"));            
        }
        GUILayout.EndHorizontal();

        // Slider
        float newVal = GUILayout.HorizontalSlider(oldVal, min, max);
        if (newVal != oldVal) { _morphableFace.SetMorphCoefficient(mf, newVal); }
    }

}
