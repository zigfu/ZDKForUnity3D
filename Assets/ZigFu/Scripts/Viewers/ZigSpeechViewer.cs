using UnityEngine;
using System.Collections;
using Zigfu.Speech;


// Summary:
//
//      ZigSpeechViewer serves as a readonly GUI, displaying the status of the 
//       ZigKinectSpeechRecognizer (Listening or Not Listening),
//       as well as the last spoken Phrase and corresponding Confidence.
//
public class ZigSpeechViewer : MonoBehaviour
{
    const string ClassName = "ZigSpeechViewer";


    #region GUI Properties

    // GUI Text
    const string SpeechRecognition_LabelText    = "Speech Recognition:";
    const string LastSpoken_LabelText           = "Last Spoken:";

    const string Listening_Text                 = "Listening";
    const string NotListening_Text              = "Not Listening";
    const string Starting_Text                  = "Starting...";
    const string Stopping_Text                  = "Stopping...";

    const string PhraseAndConfidence_Text       = "\t{0}  ({1})";


    // GUI Layout
    public float width      = 260;
    public float height     = 88;
    public int fontSize     = 16;


    // GUI Colors
    public Color listening_Color        = Color.green;
    public Color notListening_Color     = new Color(1, 0, 0, 0.5f);
    public Color transitioning_Color    = new Color(0.8f, 0.75f, 0);
    public Color phrase_Color           = Color.cyan;

    #endregion


    public bool verbose = true;
    

    ZigKinectSpeechRecognizer _speechRecognizer;
    public Rect GuiArea {
        get { return new Rect((Screen.width  - width)  * 0.5f, 
                              (Screen.height - height), 
                              width, height); }
    }

    string _semanticTag = string.Empty;     // The recognized Phrase to display
    float _confidence = 0.0f;               // The Confidence of the recognized phrase


	void Start () 
    {
        if (verbose) { print(ClassName + " :: Start"); }

        _speechRecognizer = ZigKinectSpeechRecognizer.Instance;
        _speechRecognizer.SpeechRecognized += SpeechRecognized_Handler;
	}

    void OnDestroy()
    {
        if (verbose) { print(ClassName + " :: OnDestroy"); }

        if (_speechRecognizer) { _speechRecognizer.SpeechRecognized -= SpeechRecognized_Handler; }
    }


    void SpeechRecognized_Handler(object sender, ZigKinectSpeechRecognizer.SpeechRecognized_EventArgs e)
    {
        _semanticTag = e.SemanticTag;
        _confidence = e.Confidence;

        _phraseAlpha = 1.0f;
    }

    
    void Update()
    {
        FadePhrase();
    }

    float _phraseAlpha = 0.0f;
    const float MinPhraseAlpha = 0.1f;
    const float PhraseAlphaFadeDuration = 5.0f;
    void FadePhrase()
    {
        _phraseAlpha -= (Time.deltaTime / PhraseAlphaFadeDuration);
        _phraseAlpha = Mathf.Max(MinPhraseAlpha, _phraseAlpha);
    }


    void OnGUI()
    {
        int oldFontSize = GUI.skin.GetStyle("Label").fontSize;
        GUI.skin.GetStyle("Label").fontSize = fontSize;
        {
            Rect r = GuiArea;
            GUI_ListeningLabel(r);

            r.y += 2 * fontSize;
            GUI_LastSpokenLabel(r);
        }
        GUI.skin.GetStyle("Label").fontSize = oldFontSize;
    }

    void GUI_ListeningLabel(Rect position)
    {
        Rect rect = position;

        // "Speech Recognition:"

        string text = SpeechRecognition_LabelText;

        GUIStyle style = new GUIStyle(GUI.skin.GetStyle("Label"));
        Vector2 size = style.CalcSize(new GUIContent(text));
        rect.width = size.x;

        GUI.Label(rect, text);

        rect.x += size.x;


        // "Listening/NotListening"

        bool inTransitionState = _speechRecognizer.SpeechRecognitionIsStarting || _speechRecognizer.SpeechRecognitionIsStopping;

        string isListeningText;
        if (inTransitionState) { isListeningText = _speechRecognizer.SpeechRecognitionIsStarting ? Starting_Text  : Stopping_Text; }
        else                   { isListeningText = _speechRecognizer.SpeechRecognitionHasStarted ? Listening_Text : NotListening_Text; } 

        text = "\t" + isListeningText;

        style = new GUIStyle(GUI.skin.GetStyle("Label"));
        size = style.CalcSize(new GUIContent(text));
        rect.width = size.x;

        Color color;
        if (inTransitionState) { color = transitioning_Color; }
        else                   { color = _speechRecognizer.SpeechRecognitionHasStarted ? listening_Color : notListening_Color; }
        SetAllTextColorPropertiesOnStyle(style, color);

        GUI.Label(rect, text, style);
    }

    void GUI_LastSpokenLabel(Rect position)
    {
        Rect rect = position;

        // "Last Spoken:"

        string text = LastSpoken_LabelText;

        GUIStyle style = new GUIStyle(GUI.skin.GetStyle("Label"));
        Vector2 size = style.CalcSize(new GUIContent(text));
        rect.width = size.x;

        GUI.Label(rect, text);

        rect.x += size.x;


        // Phrase and Confidence

        text = string.Format(PhraseAndConfidence_Text, _semanticTag, _confidence.ToString("##0%"));

        style = new GUIStyle(GUI.skin.GetStyle("Label"));
        size = style.CalcSize(new GUIContent(text));
        rect.width = size.x;

        Color color = phrase_Color;
        color.a = _phraseAlpha;
        SetAllTextColorPropertiesOnStyle(style, color);

        GUI.Label(rect, text, style);
    }


    static void SetAllTextColorPropertiesOnStyle(GUIStyle style, Color color)
    {
        style.normal.textColor = color;
        style.onNormal.textColor = color;
        style.hover.textColor = color;
        style.onHover.textColor = color;
        style.focused.textColor = color;
        style.onFocused.textColor = color;
        style.active.textColor = color;
        style.onActive.textColor = color;
    }

}
