using UnityEngine;
using UnityEditor;
using System;
using System.Collections;
using System.Collections.Generic;
using Zigfu.Speech;


[CustomEditor(typeof(ZigSpeechGrammar))]

public class ZigSpeechGrammar_Editor : Editor
{
    
    [MenuItem("GameObject/Create Zig/Zig Speech Grammar", false, 0)]
    static void CreateZigKinectSpeechGrammar()
    {
        ZigSpeechGrammar.CreateGrammar();
    }


    const string GrammarXmlStorageDirectory = "Assets/Zig Audio and Speech/Grammar XML";


    #region GUI Properties

    // SerializedProperty Names
    const string PhrasesArray_PropertyName  = "_phrases";
    const string GrammarName_PropertyName   = "_grammarName";

    // GUI Text
    const string TableHeader_Text           = "Phrases";
    const string LoadFromXmlButton_Text     = "Load";
    const string SaveAsXmlButton_Text       = "Save";
    const string EditButtonHidden_Text      = "Edit";
    const string EditButtonShowing_Text     = "Hide Edit";

    const string CollapseAllButton_Text     = "-";
    const string ExpandAllButton_Text       = "+";
    const string DeleteEntry_Text           = "x";

    const string NewPhraseButton_Text       = "New Phrase";
    const string HelpButtonHidden_Text      = "Help";
    const string HelpButtonShowing_Text     = "Hide Help";
    const string HelpMessage_Text = "  A ZigSpeechGrammar represents a collection of words or phrases that you are interested in recognizing and responding to."
        + "\n  Each Phrase has a SemanticTag (in BOLD) and a list of Synonyms."
        + "\n  The Synonyms are the spoken words or phrases to be recognized."
        + "\n  The SemanticTag is the string that will be passed to your SpeechRecognized handler whenever any of its associated Synonyms are recognized.";


    // GUI Layout
    const uint LeftIndent                   = 18;
    const uint SynonymCell_Indent           = 80;
    const uint PhraseDeleteBtn_HorzPadding  = 8;
    const uint BtnText_HorzPadding          = 20;

    const uint PhraseRowCellHeight          = 20;
    const uint SynonymRowCellHeight         = 18;


    // GUI Colors
    static Color SemanticTagColor           = new Color(0.5f, 0.7f, 0.9f);
    static Color CreationColor              = new Color(0.5f, 0.9f, 0.7f);
    static Color SaveBtnColor               = new Color(0.95f, 0.4f, 0.55f);
    static Color LoadBtnColor               = new Color(0.5f, 0.7f, 0.9f);
    static Color HelpBtnColor               = new Color(0.9f, 0.8f, 0.2f);

    #endregion


    SerializedObject _grammarObject;
    SerializedProperty _grammarPhrases;
    SerializedProperty _grammarName;


    void OnEnable()
    {
        _grammarObject  = new SerializedObject(target);
        _grammarPhrases = _grammarObject.FindProperty(PhrasesArray_PropertyName);
        _grammarName    = _grammarObject.FindProperty(GrammarName_PropertyName);
    }


    #region Data Methods


    #region Phrases Array Methods

    int GetPhraseCount()
    {
        return _grammarPhrases.arraySize;
    }
    bool ValidatePhraseIndex(int phraseIndex)
    {
        return (phraseIndex >= 0) && (phraseIndex < GetPhraseCount());
    }

    void SetPhraseAtIndex(int phraseIndex, Phrase newPhrase)
    {
        if (!ValidatePhraseIndex(phraseIndex)) { return; }
        _grammarPhrases.GetArrayElementAtIndex(phraseIndex).objectReferenceValue = newPhrase;
    }
    Phrase GetPhraseAtIndex(int phraseIndex)
    {
        if (!ValidatePhraseIndex(phraseIndex)) { return null; }
        return _grammarPhrases.GetArrayElementAtIndex(phraseIndex).objectReferenceValue as Phrase;
    }

    void AddPhrase(String semanticTag)
    {
        Phrase newPhrase = Phrase.CreatePhrase(semanticTag, Phrase.NewSynonymPlaceholderText);
        if (!newPhrase) { return; }

        int newPhraseIndex = GetPhraseCount();
        _grammarPhrases.InsertArrayElementAtIndex(newPhraseIndex);
        SetPhraseAtIndex(newPhraseIndex, newPhrase);
    }

    void RemovePhrase(int phraseIndex)
    {
        if (!ValidatePhraseIndex(phraseIndex)) { return; }
        SetPhraseAtIndex(phraseIndex, null);
        _grammarPhrases.DeleteArrayElementAtIndex(phraseIndex);
    }

    #endregion


    #region SemanticTag Methods

    void SetSemanticTag(int phraseIndex, String newTag)
    {
        Phrase phrase = GetPhraseAtIndex(phraseIndex);
        if (!phrase) { return; }
        phrase.SemanticTag = newTag;
    }

    String GetSemanticTag(int phraseIndex)
    {
        Phrase phrase = GetPhraseAtIndex(phraseIndex);
        if (!phrase) { return null; }
        return phrase.SemanticTag;
    }

    #endregion


    #region Synonym Array Methods

    int GetSynonymCount(int phraseIndex)
    {
        Phrase phrase = GetPhraseAtIndex(phraseIndex);
        if (!phrase) { return 0; }
        return phrase.Synonyms.Count;
    }
    bool ValidateSynonymIndex(int phraseIndex, int synonymIndex)
    {
        if (!ValidatePhraseIndex(phraseIndex)) { return false; }
        return (synonymIndex >= 0) && (synonymIndex < GetSynonymCount(phraseIndex));
    }

    void SetSynonym(int phraseIndex, int synonymIndex, String newSynonym)
    {
        if (!ValidateSynonymIndex(phraseIndex, synonymIndex)) { return; }
        Phrase phrase = GetPhraseAtIndex(phraseIndex);
        if (!phrase) { return; }
        phrase.Synonyms[synonymIndex] = newSynonym;
    }
    String GetSynonym(int phraseIndex, int synonymIndex)
    {
        if (!ValidateSynonymIndex(phraseIndex, synonymIndex)) { return null; }
        Phrase phrase = GetPhraseAtIndex(phraseIndex);
        if (!phrase) { return null; }
        return phrase.Synonyms[synonymIndex];
    }

    void AddSynonym(int phraseIndex, String newSynonym)
    {
        Phrase phrase = GetPhraseAtIndex(phraseIndex);
        if (!phrase) { return; }
        phrase.AddSynonym(newSynonym);
    }
    void RemoveSynonym(int phraseIndex, int synonymIndex)
    {
        String synonym = GetSynonym(phraseIndex, synonymIndex);
        RemoveSynonym(phraseIndex, synonym);
    }
    void RemoveSynonym(int phraseIndex, String synonym)
    {
        Phrase phrase = GetPhraseAtIndex(phraseIndex);
        if (!phrase) { return; }
        phrase.RemoveSynonym(synonym);
    }

    #endregion

    #endregion


    #region GUI

    public override void OnInspectorGUI()
    {
        _grammarObject.Update();

        EditorGUIUtility.LookLikeInspector();

        if (UnityEditorIsPlayingOrPaused) 
            { _editGrammarIsEnabled = false; }
        else 
            { _errorMessage_Edit = null; }


        GUI_SmallSeparator();

        GUI_TableHeader();

        GUI_SmallSeparator();


        for (int p = 0; p < GetPhraseCount(); p++)
        {
            bool doListSynonymsForPhrase = GUI_PhraseCellRow(p);
            if (!doListSynonymsForPhrase) { continue; }

            GUI_SmallSeparator();

            int numSynonymsToShow = GetSynonymCount(p);
            if (!_editGrammarIsEnabled) { numSynonymsToShow--; }
            for (int s = 0; s < numSynonymsToShow; s++)
            {
                GUI_SynonymCellRow(p, s);
            }
            EnsurePlaceholderSynonymExists(p);

            GUI_SmallSeparator();
        }

        int indexOfBottommostPhrase = GetPhraseCount() - 1;
        if (IsPhraseFoldoutCollapsed(indexOfBottommostPhrase))
        {
            EditorGUILayout.Separator();
        }


        GUI_TableFooter();

        _grammarObject.ApplyModifiedProperties();
    }


    #region GUI_TableHeader

    void GUI_TableHeader()
    {
        GUI_EnabledLabel();

        GUI_SmallSeparator();

        GUI_GrammarNameTextField();


        EditorGUILayout.Separator();


        EditorGUILayout.BeginHorizontal();
        {
            GUILayout.Space(LeftIndent);

            GUI_CollapseAllButton();

            GUILayout.Label("");    // This forces the next Control to align to the right side (not left)

            GUI_EditButton();
        }
        EditorGUILayout.EndHorizontal();


        if (_errorMessage_Edit != null)
        {
            EditorGUILayout.HelpBox(_errorMessage_Edit, MessageType.Warning, true);
        }
    }


    void GUI_EnabledLabel()
    {
        EditorGUIUtility.LookLikeInspector();

        bool isEnabled = (target as ZigSpeechGrammar).WantsActive;
        string text = isEnabled ? "Enabled" : "DISABLED";
        //String text = "Enabled: " + isEnabledStr;

        GUIStyle style = new GUIStyle(GUI.skin.GetStyle("Label"));
        style.fontSize = 15;
        style.fontStyle = FontStyle.Bold;
        Color color = isEnabled ? Color.green : new Color(0.9f, 0.2f, 0.2f);
        SetAllTextColorPropertiesOnStyle(style, color);

        EditorGUILayout.LabelField(text, style);

        EditorGUIUtility.LookLikeControls();
    }

    void GUI_GrammarNameTextField()
    {
        String text = _grammarName.stringValue;
        GUIStyle style = new GUIStyle(GUI.skin.GetStyle("Label"));
        style.fontSize = 15;
        style.fontStyle = FontStyle.Bold;
        SetAllTextColorPropertiesOnStyle(style, new Color(0.2f, 0.7f, 1.0f));


        bool oldGuiEnabled = GUI.enabled;
        GUI.enabled = oldGuiEnabled && !UnityEditorIsPlayingOrPaused;

        _grammarName.stringValue = EditorGUILayout.TextField(text, style);

        GUI.enabled = oldGuiEnabled;
    }

    void GUI_CollapseAllButton()
    {
        bool allAreCollapsed = AllPhraseFoldoutsAreCollapsed();

        GUIStyle style = GetButtonStyle();
        string btnText = allAreCollapsed ? ExpandAllButton_Text : CollapseAllButton_Text;
        Vector2 size = style.CalcSize(new GUIContent(btnText));
        float btnWidth = size.x + BtnText_HorzPadding;

        GUIContent toolTip = new GUIContent(btnText, "Collapses/Expands all Phrase entries listed in table below.");
        if (GUILayout.Button(toolTip, style, GUILayout.Width(btnWidth)))
        {
            SetAllPhraseFoldoutCollapseStates(!allAreCollapsed);
        }
    }

    bool _editGrammarIsEnabled = true;
    string _errorMessage_Edit = null;
    void GUI_EditButton()
    {
        string btnText = _editGrammarIsEnabled ? EditButtonShowing_Text : EditButtonHidden_Text;
        GUIStyle style = GetButtonStyle();
        SetAllTextColorPropertiesOnStyle(style, CreationColor);
        Vector2 size = style.CalcSize(new GUIContent(btnText));
        float btnWidth = size.x + BtnText_HorzPadding;

        
        string toolTipText = "Enables/Disables editing of this SpeechGrammar.  Editing is only allowed while Editor is not playing/paused.";
        GUIContent toolTip = new GUIContent(btnText, toolTipText);
        if (GUILayout.Button(toolTip, style, GUILayout.Width(btnWidth)))
        {
            ToggleEditMode();
        }
    }

    void ToggleEditMode()
    {
        if (UnityEditorIsPlayingOrPaused)
        {
            _editGrammarIsEnabled = false;
            _errorMessage_Edit = "Grammars may only be edited when Editor is not playing/paused.";
            return;
        }

        _editGrammarIsEnabled = !_editGrammarIsEnabled;
    }

    #endregion


    #region GUI_PhraseCellRow

    bool GUI_PhraseCellRow(int phraseIndex)
    {
        int p = phraseIndex;
        bool phraseFoldOutIsExpanded;
        EditorGUILayout.BeginHorizontal();
        {
            phraseFoldOutIsExpanded = GUI_PhraseFoldout(p);
            GUI_PhraseTextField(p);

            GUILayout.Label("");    // This forces the next Control to align to the right side (not left)

            if (_editGrammarIsEnabled)
            {
                if (GUI_PhraseDeleteButton(p))
                {
                    RemovePhraseFoldoutCollapsedState(p);
                    phraseFoldOutIsExpanded = false;
                }
            }
        }
        EditorGUILayout.EndHorizontal();
        return phraseFoldOutIsExpanded;
    }


    bool GUI_PhraseFoldout(int phraseIndex)
    {
        bool oldFoldoutState = IsPhraseFoldoutExpanded(phraseIndex);
        bool newFoldoutState = EditorGUILayout.Foldout(oldFoldoutState, String.Empty);
        if (newFoldoutState != oldFoldoutState)
        {
            TogglePhraseFoldoutCollapsedState(phraseIndex);
        }
        return newFoldoutState;
    }

    bool GUI_PhraseTextField(int phraseIndex)
    {
        String oldSemanticTag = GetSemanticTag(phraseIndex);
        String newSemanticTag;

        GUIStyle style = new GUIStyle(GUI.skin.GetStyle("BoldLabel"));
        SetAllTextColorPropertiesOnStyle(style, SemanticTagColor);
        float height = PhraseRowCellHeight;

        if (_editGrammarIsEnabled)
        {
            newSemanticTag = EditorGUILayout.TextField(oldSemanticTag, style, GUILayout.Height(height));
        }
        else
        {
            EditorGUILayout.SelectableLabel(oldSemanticTag, style, GUILayout.Height(height));
            newSemanticTag = oldSemanticTag;
        }

        bool textChanged = (newSemanticTag != oldSemanticTag);
        if (textChanged)
        {
            SetSemanticTag(phraseIndex, newSemanticTag);
        }
        return textChanged;
    }

    bool GUI_PhraseDeleteButton(int phraseIndex)
    {
        GUIStyle style = GetButtonStyle();
        style.fontSize = 14;
        Vector2 size = style.CalcSize(new GUIContent(DeleteEntry_Text));
        float buttonWidth = size.x + PhraseDeleteBtn_HorzPadding;
        float buttonHeight = PhraseRowCellHeight;

        GUIContent toolTip = new GUIContent(DeleteEntry_Text, "Removes the Phrase.");
        bool wasPressed = GUILayout.Button(toolTip, style, GUILayout.Width(buttonWidth), GUILayout.Height(buttonHeight));
        if (wasPressed)
        {
            RemovePhrase(phraseIndex);
        }
        return wasPressed;
    }

    #endregion


    #region GUI_SynonymCellRow

    void GUI_SynonymCellRow(int phraseIndex, int synonymIndex)
    {
        int p = phraseIndex;
        int s = synonymIndex;

        EditorGUILayout.BeginHorizontal();
        {
            GUILayout.Space(SynonymCell_Indent);

            GUI_SynonymTextField(p, s);

            if (_editGrammarIsEnabled)
            {
                bool thisIsBottomSynonym = (s == GetSynonymCount(p) - 1);
                if (!thisIsBottomSynonym)
                {
                    GUI_SynonymDeleteButton(p, s);
                }
            }
        }
        EditorGUILayout.EndHorizontal();
    }


    bool GUI_SynonymTextField(int phraseIndex, int synonymIndex)
    {
        int p = phraseIndex;
        int s = synonymIndex;

        String oldSynonym = GetSynonym(p, s);
        String newSynonym;

        GUIStyle style = new GUIStyle(GUI.skin.GetStyle("Label"));

        bool thisIsBottomSynonym = (s == GetSynonymCount(p) - 1);
        style.fontStyle = thisIsBottomSynonym ? FontStyle.Italic : FontStyle.Normal;

        Color color = thisIsBottomSynonym ? CreationColor : Color.white;
        SetAllTextColorPropertiesOnStyle(style, color);

        float height = SynonymRowCellHeight;

        if (_editGrammarIsEnabled)
        {
            newSynonym = EditorGUILayout.TextField(oldSynonym, style, GUILayout.Height(height));
        }
        else
        {
            EditorGUILayout.SelectableLabel(oldSynonym, style, GUILayout.Height(height));
            newSynonym = oldSynonym;
        }

        bool textChanged = (newSynonym != oldSynonym);
        if (textChanged)
        {
            SetSynonym(p, s, newSynonym);
        }
        return textChanged;
    }

    bool GUI_SynonymDeleteButton(int phraseIndex, int synonymIndex)
    {
        GUIStyle style = GetButtonStyle();
        Vector2 size = style.CalcSize(new GUIContent(DeleteEntry_Text));

        GUIContent toolTip = new GUIContent(DeleteEntry_Text, "Removes the Synonym.");
        bool wasPressed = GUILayout.Button(toolTip, style, GUILayout.Width(size.x), GUILayout.Height(SynonymRowCellHeight));
        if(wasPressed)
        {
            RemoveSynonym(phraseIndex, synonymIndex);
        }
        return wasPressed;
    }

    // The bottom-most synonym listed for each phrase will always be Phrase.NewSynonymPlaceholderText.
    //  When the user types into that synonym's TextField, a new Synonym is automatically added beneath it.
    void EnsurePlaceholderSynonymExists(int phraseIndex)
    {
        int p = phraseIndex;
        int synonymCount = GetSynonymCount(p);

        String bottomSynonym = GetSynonym(p, synonymCount - 1);
        if (bottomSynonym != Phrase.NewSynonymPlaceholderText)
        {
            AddSynonym(p, Phrase.NewSynonymPlaceholderText);
        }
    }

    #endregion


    #region GUI_TableFooter

    void GUI_TableFooter()
    {
        bool doDisplayHelpBox;

        if (_editGrammarIsEnabled) { GUI_NewPhraseButton(); }

        GUI_SmallSeparator();


        EditorGUILayout.BeginHorizontal();
        {
            GUILayout.Space(LeftIndent);

            GUI_LoadFromXmlButton();
            GUI_SaveToXmlButton();

            GUILayout.Label("");    // This forces the next Control to align to the right side (not left)
            doDisplayHelpBox = GUI_HelpButton();
        }
        EditorGUILayout.EndHorizontal();


        if (doDisplayHelpBox)
        {
            EditorGUILayout.HelpBox(HelpMessage_Text, MessageType.Info, true);
        }
        else 
        {
            GUI_SmallSeparator();
        }
    }


    bool GUI_NewPhraseButton()
    {
        bool wasPressed;

        EditorGUILayout.BeginHorizontal();
        {
            GUILayout.Space(LeftIndent);

            GUIStyle style = GetButtonStyle();
            SetAllTextColorPropertiesOnStyle(style, CreationColor);
            Vector2 size = style.CalcSize(new GUIContent(NewPhraseButton_Text));

            GUIContent toolTip = new GUIContent(NewPhraseButton_Text, "Adds a new Phrase.");
            wasPressed = GUILayout.Button(toolTip, style, GUILayout.Width(size.x));
            if (wasPressed)
            {
                AddPhrase(Phrase.NewPhrasePlaceholderText);

                // Ensure the new Phrase Foldout will appear in Expanded state
                int indexOfNewPhrase = GetPhraseCount() - 1;
                ExpandPhraseFoldout(indexOfNewPhrase);
            }
        }
        EditorGUILayout.EndHorizontal();

        return wasPressed;
    }

    void GUI_LoadFromXmlButton()
    {
        string btnText = LoadFromXmlButton_Text;
        GUIStyle style = GetButtonStyle();
        SetAllTextColorPropertiesOnStyle(style, LoadBtnColor);
        Vector2 size = style.CalcSize(new GUIContent(btnText));
        float btnWidth = size.x + BtnText_HorzPadding;


        bool oldGuiEnabled = GUI.enabled;
        GUI.enabled = oldGuiEnabled && !UnityEditorIsPlayingOrPaused;

        GUIContent toolTip = new GUIContent(btnText, "Loads a SpeechGrammar from a specified Grammar XML (grxml) file.");
        if (GUILayout.Button(toolTip, style, GUILayout.Width(btnWidth)))
        {
            string extension = "grxml";
            string path = EditorUtility.OpenFilePanel("Load Grammar XML", GrammarXmlStorageDirectory, extension);
            if (path.Length != 0 && path.EndsWith(extension))
            {
                (target as ZigSpeechGrammar).InitializeFromXmlFile(path);
            }
        }

        GUI.enabled = oldGuiEnabled;
    }

    void GUI_SaveToXmlButton()
    {
        string btnText = SaveAsXmlButton_Text;
        GUIStyle style = GetButtonStyle();
        SetAllTextColorPropertiesOnStyle(style, SaveBtnColor);
        Vector2 size = style.CalcSize(new GUIContent(btnText));
        float btnWidth = size.x + BtnText_HorzPadding;


        bool oldGuiEnabled = GUI.enabled;
        GUI.enabled = oldGuiEnabled && !UnityEditorIsPlayingOrPaused;

        GUIContent toolTip = new GUIContent(btnText, "Saves this SpeechGrammar to a specified Grammar XML (grxml) file.");
        if (GUILayout.Button(toolTip, style, GUILayout.Width(btnWidth)))
        {
            string extension = "grxml";
            string fileName = _grammarName.stringValue + "." + extension;
            string path = EditorUtility.SaveFilePanel("Save as Grammar XML", GrammarXmlStorageDirectory, fileName, extension);
            if (path.Length != 0)
            {
                (target as ZigSpeechGrammar).SaveAsXml(path);
            }
        }

        GUI.enabled = oldGuiEnabled;
    }

    bool _helpBoxIsShowing = false;
    bool GUI_HelpButton()
    {
        string btnText = _helpBoxIsShowing ? HelpButtonShowing_Text : HelpButtonHidden_Text;
        GUIStyle style = GetButtonStyle();
        SetAllTextColorPropertiesOnStyle(style, HelpBtnColor);
        Vector2 size = style.CalcSize(new GUIContent(btnText));
        float btnWidth = size.x + BtnText_HorzPadding;

        GUIContent toolTip = new GUIContent(btnText, "Explains the parts of SpeechGrammar.");
        if (GUILayout.Button(toolTip, style, GUILayout.Width(btnWidth)))
        {
            _helpBoxIsShowing = !_helpBoxIsShowing;
        }

        return _helpBoxIsShowing;
    }

    #endregion


    #region GUI Helpers

    void GUI_SmallSeparator()
    {
        GUILayout.Space(5);
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

    static GUIStyle GetButtonStyle()
    {
        GUIStyle style = new GUIStyle(GUI.skin.GetStyle("Button"));
        style.fontStyle = FontStyle.Bold;
        return style;
    }

    #endregion


    #endregion


    #region PhraseFoldoutCollapsedStates

    List<bool> _phraseFoldoutCollapsedStates = new List<bool>();

    bool IsPhraseFoldoutCollapsed(int phraseIndex)
    {
        if (!ValidatePhraseIndex(phraseIndex)) { return true; }
        EnsureFoldoutCollapsedStatesExistForEachPhrase();
        return _phraseFoldoutCollapsedStates[phraseIndex];
    }
    bool IsPhraseFoldoutExpanded(int phraseIndex)
    {
        return !IsPhraseFoldoutCollapsed(phraseIndex);
    }

    void CollapsePhraseFoldout(int phraseIndex)
    {
        if (!ValidatePhraseIndex(phraseIndex)) { return; }
        EnsureFoldoutCollapsedStatesExistForEachPhrase();
        _phraseFoldoutCollapsedStates[phraseIndex] = true;
    }
    void ExpandPhraseFoldout(int phraseIndex)
    {
        if (!ValidatePhraseIndex(phraseIndex)) { return; }
        EnsureFoldoutCollapsedStatesExistForEachPhrase();
        _phraseFoldoutCollapsedStates[phraseIndex] = false;
    }
    void TogglePhraseFoldoutCollapsedState(int phraseIndex)
    {
        bool isCollapsed = IsPhraseFoldoutCollapsed(phraseIndex);
        if (isCollapsed) { ExpandPhraseFoldout(phraseIndex); }
        else             { CollapsePhraseFoldout(phraseIndex); }
    }

    void SetAllPhraseFoldoutCollapseStates(bool newCollapsedState)
    {
        EnsureFoldoutCollapsedStatesExistForEachPhrase();
        for (int i = 0; i < _phraseFoldoutCollapsedStates.Count; i++)
        {
            _phraseFoldoutCollapsedStates[i] = newCollapsedState;
        }
    }
    bool AllPhraseFoldoutsAreCollapsed()
    {
        foreach (bool collapsedState in _phraseFoldoutCollapsedStates)
	    {
            if (!collapsedState) { return false; }
	    }
        return true;
    }

    void RemovePhraseFoldoutCollapsedState(int phraseIndex)
    {
        EnsureFoldoutCollapsedStatesExistForEachPhrase();
        if (phraseIndex < 0 || phraseIndex >= _phraseFoldoutCollapsedStates.Count) { return; }
        _phraseFoldoutCollapsedStates.RemoveAt(phraseIndex);
    }
    void EnsureFoldoutCollapsedStatesExistForEachPhrase()
    {
        while (_phraseFoldoutCollapsedStates.Count < GetPhraseCount())
        {
            _phraseFoldoutCollapsedStates.Add(false);
        }
    }

    #endregion


    bool UnityEditorIsPlayingOrPaused
    {
        get { return (EditorApplication.isPlaying || EditorApplication.isPaused); }
    }

}
