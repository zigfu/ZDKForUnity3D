using UnityEngine;
using UnityEditor;
using System;
using System.Collections;
using System.Collections.Generic;
using Zigfu.Speech;


[CustomEditor(typeof(ZigKinectSpeechRecognizer))]

// Summary:
//      This custom editor behaves in 2 different ways depending on whether or not EditorApplication.isPlaying.
//
//      If (EditorApplication.isPlaying or EditorApplication.isPaused):
//          OnInspectorGUI gets/sets the targets's properties directly.
//      Else:
//          OnInspectorGUI() gets/sets the target's "initial" data members.
//          The target will automatically set it's properties to their corresponding
//          "initial" values when the EditorApplication has begun playing.
//
public class ZigKinectSpeechRecognizer_Editor : Editor
{
    
    [MenuItem("GameObject/Create Zig/Zig Kinect Speech Recognizer", false, 0)]
    static void CreateZigKinectSpeechRecognizer()
    {
        if (ZigKinectSpeechRecognizer.InstanceExists)
        {
            Debug.LogWarning("ZigKinectSpeechRecognizer was not created because only one instance may exist at a time.");
            return;
        }
        ZigKinectSpeechRecognizer sr = ZigKinectSpeechRecognizer.Instance;
        sr.name = "ZigKinectSpeechRecognizer";
    }


    const string ClassName = "ZigKinectSpeechRecognizer_Editor";

    static public bool verbose = false;

    ZigKinectSpeechRecognizer _target;

    // GUI Settings
    float _guiConfidenceThreshold;
    bool _guiAdaptationEnabled;

    // Stored copies of GUI Settings from previous OnInspectorGUI() call
    float _oldConfidenceThreshold;
    bool _oldAdaptationEnabled;


    #region Init

    SerializedObject _srObj;


    void OnEnable()
    {
        if (verbose) { Debug.Log(ClassName + " :: OnEnable"); }

        _srObj = new SerializedObject(target);


        _target = (ZigKinectSpeechRecognizer)target;


        _target.StartedListening += _SR_StoppedListening;
        _target.StoppedListening += _SR_StartedListening;

        InitGuiSettings();
    }

    void _SR_StoppedListening(object sender, EventArgs e) { Repaint(); } 
    void _SR_StartedListening(object sender, EventArgs e) { Repaint(); } 

    void InitGuiSettings()
    {
        _guiConfidenceThreshold     = _target.initialConfidenceThreshold;
        _guiAdaptationEnabled       = _target.initialAdaptationEnabled;
    }

    #endregion


    #region GUI

    public override void OnInspectorGUI()
    {
        if (UnityEditorIsPlayingOrPaused)
        {
            SyncGuiWithTargetProperties();
        }

        StoreCopiesOfCurrentGuiSettings();

        UpdateGUI();

        if (GUI.changed)
        {
            OnGuiChanged();
        }
    }

    void SyncGuiWithTargetProperties()
    {
        _guiConfidenceThreshold     = _target.ConfidenceThreshold;
        _guiAdaptationEnabled       = _target.AdaptationEnabled;
    }

    void StoreCopiesOfCurrentGuiSettings()
    {
        _oldConfidenceThreshold     = _guiConfidenceThreshold;
        _oldAdaptationEnabled       = _guiAdaptationEnabled;
    }


    #region UpdateGui

    void UpdateGUI()
    {
        _srObj.Update();

        EditorGUIUtility.LookLikeInspector();

        if (!UnityEditorIsPlayingOrPaused) { _errorMessage_StartSpeechRecognition = null; }

        GUI_SmallSeparator();

        GUI_DialectEnumPopup();
        GUI_ConfidenceSlider();
        GUI_AdaptationCheckBox();
        GUI_VerboseCheckBox();


        GUI_SmallSeparator();


        GUILayout.BeginHorizontal();
        {
            GUILayout.Space(18);

            GUI_StartStopButton();
            GUI_NewGrammarButton();
        }
        GUILayout.EndHorizontal();


        if (_errorMessage_StartSpeechRecognition != null) 
        {
            EditorGUILayout.HelpBox(_errorMessage_StartSpeechRecognition, MessageType.Warning, true);
        }
        else 
        {
            GUI_SmallSeparator();
        }

        _srObj.ApplyModifiedProperties();
    }


    void GUI_DialectEnumPopup()
    {
        bool oldGuiEnabled = GUI.enabled;
        GUI.enabled = oldGuiEnabled && !UnityEditorIsPlayingOrPaused;

        string labelText = "Dialect";
        string toolTipText = "The language and regional dialect that the SpeechEngine will use when interpreting speech."
                            + "  This cannot be changed during runtime.  Note that you must have the corresponding"
                            + " Language Pack installed to use a given Dialect.  These can be downloaded here:"
                            + " http://www.microsoft.com/en-us/kinectforwindows/develop/developer-downloads.aspx.";
        GUIContent toolTip = new GUIContent(labelText, toolTipText);
        _target.initialDialect = (LanguagePack.DialectEnum)EditorGUILayout.EnumPopup(toolTip, _target.initialDialect);

        GUI.enabled = oldGuiEnabled;
    }

    void GUI_ConfidenceSlider()
    {
        GUIContent toolTip = new GUIContent("Confidence Threshold", 
            "The SpeechRecognizer will only send a SpeechRecognized event when its Confidence is above this value.");
        _guiConfidenceThreshold = EditorGUILayout.Slider(toolTip, _guiConfidenceThreshold, 0, 1);

    }

    void GUI_AdaptationCheckBox()
    {
        GUIContent toolTip = new GUIContent("Adaptation", 
            "For long recognition sessions (a few hours or more), it may be beneficial to turn off adaptation of the acoustic model."
            + "This will prevent recognition accuracy from degrading over time.");

        _guiAdaptationEnabled = EditorGUILayout.Toggle(toolTip, _guiAdaptationEnabled);
    }

    void GUI_VerboseCheckBox()
    {
        GUIContent toolTip = new GUIContent("Verbose", 
            "Whether or not to log frequent status updates");

        ZigKinectSpeechRecognizer.verbose = EditorGUILayout.Toggle(toolTip, ZigKinectSpeechRecognizer.verbose);
    }


    void GUI_StartStopButton()
    {
        bool inTransitionState = _target.SpeechRecognitionIsStarting || _target.SpeechRecognitionIsStopping;

        string btnText = "Start Listening";

        if (UnityEditorIsPlayingOrPaused)
        { 
            if      (_target.SpeechRecognitionIsStarting) { btnText = "Starting..."; }
            else if (_target.SpeechRecognitionIsStopping) { btnText = "Stopping..."; }
            else if (_target.SpeechRecognitionHasStarted) { btnText = "Stop Listening"; }
        }
        
        string toolTipText = "Starts Speech Recognition.  Only enabled if EditorApplication is Playing or Paused.";

        GUIStyle style = GUI.skin.GetStyle("Button");
        style.fontStyle = FontStyle.Bold;
        Vector2 size = style.CalcSize(new GUIContent(btnText));
        float width = size.x + 10;


        bool oldGuiEnabled = GUI.enabled;
        GUI.enabled = oldGuiEnabled && UnityEditorIsPlayingOrPaused && !inTransitionState;

        GUIContent toolTip = new GUIContent(btnText, toolTipText);
        if (GUILayout.Button(toolTip, style, GUILayout.Width(width)))
        {
            if (_target.SpeechRecognitionHasStarted) { _target.StopSpeechRecognition_Async(); }
            else                                     { TryStartSpeechRecognition(); }
        }

        GUI.enabled = oldGuiEnabled;
    }

    string _errorMessage_StartSpeechRecognition = null;
    void TryStartSpeechRecognition()
    {
        _errorMessage_StartSpeechRecognition = null;
        try
        {
            _target.StartSpeechRecognition_Async();
        }
        catch (Exception e)
        {
            Debug.LogException(e);
            _errorMessage_StartSpeechRecognition = e.Message;
        }
    }

    void GUI_NewGrammarButton()
    {
        string btnText = "New Grammar";
        string toolTipText = "Creates a new Speech Grammar which is then added as a child GameObject. Only enabled when editor is not playing.";

        GUIStyle style = GUI.skin.GetStyle("Button");
        style.fontStyle = FontStyle.Bold;
        Vector2 size = style.CalcSize(new GUIContent(btnText));
        float width = size.x + 10;


        bool oldGuiEnabled = GUI.enabled;
        GUI.enabled = oldGuiEnabled && !UnityEditorIsPlayingOrPaused;

        GUIContent toolTip = new GUIContent(btnText, toolTipText);
        if (GUILayout.Button(toolTip, style, GUILayout.Width(width)))
        {
            CreateNewGrammar();
        }

        GUI.enabled = oldGuiEnabled;
    }

    void CreateNewGrammar()
    {
        ZigSpeechGrammar gr = ZigSpeechGrammar.CreateGrammar();
        gr.transform.parent = _target.transform;
    }


    void GUI_SmallSeparator()
    {
        GUILayout.Space(5);
    }

    #endregion


    void OnGuiChanged()
    {
        EditorUtility.SetDirty(_target);

        if (UnityEditorIsPlayingOrPaused)
        {
            ApplySettingsToTargetsProperties();
        }
        else
        {
            ApplySettingsToTargetsInitialState();
        }
    }


    void ApplySettingsToTargetsProperties()
    {
        if (verbose) { Debug.Log(ClassName + " :: ApplySettingsToTargetsProperties"); }

        if (_guiConfidenceThreshold != _oldConfidenceThreshold) { _target.ConfidenceThreshold = _guiConfidenceThreshold; }
        if (_guiAdaptationEnabled != _oldAdaptationEnabled) { _target.AdaptationEnabled = _guiAdaptationEnabled; }
    }

    void ApplySettingsToTargetsInitialState()
    {
        if (verbose) { Debug.Log(ClassName + " :: ApplySettingsToTargetsInitialState"); }

        _target.initialConfidenceThreshold = _guiConfidenceThreshold;
        _target.initialAdaptationEnabled = _guiAdaptationEnabled;
    }


    bool UnityEditorIsPlayingOrPaused
    {
        get { return EditorApplication.isPlaying || EditorApplication.isPaused; }
    }

    #endregion

}
