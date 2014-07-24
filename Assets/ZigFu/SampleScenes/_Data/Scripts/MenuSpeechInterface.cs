using UnityEngine;
using System;
using System.Collections.Generic;
using Zigfu.Speech;


// Summary:
//
//      A Speech Interface to a MenuController.
//       - This class serves as a "middle man" between the 
//          ZigKinectSpeechRecognizer and a GUI Menu.
//       - The ZigSpeechGrammars you assign to this interface 
//          should correspond to the choices the menu presents to the user.
//
public class MenuSpeechInterface : MonoBehaviour
{
    public const string ClassName = "MenuSpeechInterface";


    // A reference to a simple GUI menu.  
    //  When speech is recognized we give the MenuController corresponding commands such as
    //  SelectChoice, ConfirmPendingChoice, DenyPendingChoice, and Restart;
    public MenuController1 menuController;

    // The following Grammars define the spoken phrases or commands we want to listen for
    //  when navigating the menu referrered to by menuController.
    public ZigSpeechGrammar choicesGrammar;
    public ZigSpeechGrammar confirmationGrammar;
    public ZigSpeechGrammar restartGrammar;

    // Whether or not to Log status updates to console.
    public bool verbose = true;


    // Our reference to the ZigKinectSpeechRecognizer instance.
    ZigKinectSpeechRecognizer _speechRecognizer = null;
    public ZigKinectSpeechRecognizer SpeechRecognizer {
        get {
            if (!_speechRecognizer) { _speechRecognizer = ZigKinectSpeechRecognizer.Instance; }
            return _speechRecognizer;
        }
    }


    #region enum ListeningMode

    public enum ListeningModeEnum {
        ChoiceSelection,
        ChoiceConfirmation,
        Restart
    }

    // ListeningModeForMenuPhase
    //  Depending on where the user is at in the menu (the MenuPhase), 
    //  we will set the SpeechRecognizer to listen for different sets of Phrases
    static Dictionary<MenuController1.MenuPhaseEnum, ListeningModeEnum> _ListeningModeForMenuPhase = new Dictionary<MenuController1.MenuPhaseEnum, ListeningModeEnum> 
    {
		{ MenuController1.MenuPhaseEnum.PresentChoices,      ListeningModeEnum.ChoiceSelection },
		{ MenuController1.MenuPhaseEnum.AskForConfirmation,  ListeningModeEnum.ChoiceConfirmation },
        { MenuController1.MenuPhaseEnum.ShowResults,         ListeningModeEnum.Restart }
	};
    static public ListeningModeEnum GetListeningModeForMenuPhase(MenuController1.MenuPhaseEnum menuPhase)
    {
        ListeningModeEnum newListeningMode;
        _ListeningModeForMenuPhase.TryGetValue(menuPhase, out newListeningMode);
        return newListeningMode;
    }

    ListeningModeEnum _listeningMode = ListeningModeEnum.ChoiceSelection;
    public ListeningModeEnum ListeningMode
    {
        get 
        { 
            return _listeningMode;
        }
        private set
        {
            _listeningMode = value;

            if (verbose) { print(ClassName + " :: Set ListeningMode: " + _listeningMode.ToString()); }

            // Activate/deactivate the relevant/irrelevant grammars respectively.
            switch (_listeningMode)
            {
                case ListeningModeEnum.ChoiceSelection:
                    choicesGrammar.Activate();
                    confirmationGrammar.Deactivate();
                    restartGrammar.Deactivate();
                    break;
                case ListeningModeEnum.ChoiceConfirmation:
                    choicesGrammar.Deactivate();
                    confirmationGrammar.Activate();
                    restartGrammar.Deactivate();
                    break;
                case ListeningModeEnum.Restart:
                    choicesGrammar.Deactivate();
                    confirmationGrammar.Deactivate();
                    restartGrammar.Activate();
                    break;
            }
        }
    }

    #endregion


    #region Init/Destroy

    void Start () 
    {
        if (verbose) { print(ClassName + " :: Start"); }

        // Register for the SpeechRecognizer Events
        SpeechRecognizer.SpeechRecognized += SpeechRecognizer_SpeechRecognized;
        SpeechRecognizer.StartedListening += SpeechRecognizer_StartedListening;
        SpeechRecognizer.StoppedListening += SpeechRecognizer_StoppedListening;

        /***  If you uncomment the call to CreateGrammarsProgrammatically(), 
               you could then delete all the ZigSpeechGrammars from the Editor, 
               and everything would still work exactly the same.            ***/

        //CreateGrammarsProgrammatically();

        /**********************************************************************/


        // Tell the SpeechRecognizer to Start Listening
        TryStartSpeechRecognition();

        // Register for the MenuPhaseChanged Event
        //  This way we can ensure the phrases we are listening for
        //  always correctly correspond to the menu options being displayed.
        menuController.MenuPhaseChanged += MenuPhaseChanged_Handler;

        // Listen for the appropriate Phrases for the current MenuPhase
        ListeningMode = GetListeningModeForMenuPhase(menuController.MenuPhase);
	}

    // Summary:
    //      Demonstrates how to create ZigSpeechGrammars in code,
    //       as opposed to using the Editor GUI Components
    //
    void CreateGrammarsProgrammatically()
    {
        // --- Create the Choices Grammar ---

        string choicesGrammarName = "Numbers";
        List<Phrase> choicesGrammarPhrases = new List<Phrase>()
        {
            Phrase.CreatePhrase("ONE",      "one"),
            Phrase.CreatePhrase("TWO",      "two", "to", "too"),
            Phrase.CreatePhrase("THREE",    "three")
        };
        choicesGrammar = ZigSpeechGrammar.CreateGrammar(choicesGrammarName, choicesGrammarPhrases);

        // Grammars must be registered with the ZigKinectSpeechRecognizer to take effect.
        choicesGrammar.Register(SpeechRecognizer);


        // --- Create the Confirmation Grammar ---

        string confirmationGrammarName = "Confirmation";
        List<Phrase> confirmationGrammarPhrases = new List<Phrase>()
        {
            Phrase.CreatePhrase("YES",      "yes", "you got it", "affirmative"),
            Phrase.CreatePhrase("NO",       "no", "no way", "negative")
        };
        confirmationGrammar = ZigSpeechGrammar.CreateGrammar(confirmationGrammarName, confirmationGrammarPhrases);

        confirmationGrammar.Register(SpeechRecognizer);


        // --- Create the Restart Grammar ---

        string restartGrammarName = "Restart";
        List<Phrase> restartGrammarPhrases = new List<Phrase>()
        {
            Phrase.CreatePhrase("RESTART",  "restart", "again"),
        };
        restartGrammar = ZigSpeechGrammar.CreateGrammar(restartGrammarName, restartGrammarPhrases);

        restartGrammar.Register(SpeechRecognizer);
    }

    // Try to Start Speech Recognition
    //
    //  If ZigKinectSpeechRecognizer::StartSpeechRecognition_Async() throws an Exception,
    //   stop the Editor from playing and ensure the following:
    //
    //      1) If there is a ZigKinectAudioSource instance in the hierarchy, 
    //          its AudioProcessingIntent is set to SpeechRecognition.
    //      2) There are no calls to ZigKinectAudioSource::StartCapturingAudio(ZigAudioProcessingIntent audioProcessingIntent)
    //          in which the audioProcessingIntent argument is ZigKinectAudioSource.ZigAudioProcessingIntent.CaptureMutableAudio;
    //
    bool TryStartSpeechRecognition()
    {
        try
        {
            SpeechRecognizer.StartSpeechRecognition_Async();
        }
        catch (Exception e)
        {
            Debug.LogException(e);
            return false;
        }
        return true;
    }


    void OnDestroy()
    {
        if (verbose) { print(ClassName + " :: OnDestroy"); }

        if (menuController) { menuController.MenuPhaseChanged -= MenuPhaseChanged_Handler; }

        // Note:  Try to avoid calling SpeechRecognizer.Instance within an OnDestroy method
        //          without first verifying that ZigKinectSpeechRecognizer.InstanceExists 
        //          returns true.  This is because, if the app is exiting, the 
        //          ZigKinectSpeechRecognizer instance may have already been Destroyed,
        //          and calling ZigKinectSpeechRecognizer.Instance would reinstantiate it
        //          potentially creating a memory leak.
        //
        if (ZigKinectSpeechRecognizer.InstanceExists) 
        {
            SpeechRecognizer.SpeechRecognized -= SpeechRecognizer_SpeechRecognized;
            SpeechRecognizer.StartedListening -= SpeechRecognizer_StartedListening;
            SpeechRecognizer.StoppedListening -= SpeechRecognizer_StoppedListening;
        }
    }

    #endregion


    #region MenuPhaseChanged_Handler

    // Summary:
    //      Whenever the MenuController changes to a new phase, we are notified here
    //       and change what phrases to listen for accordingly.
    void MenuPhaseChanged_Handler(object sender, MenuController1.MenuPhaseChanged_EventArgs e)
    {
        if (verbose) { print(ClassName + " :: MenuPhaseChanged_Handler: " + e.NewMenuPhase); }

        ListeningMode = GetListeningModeForMenuPhase(e.NewMenuPhase);
    }

    #endregion


    #region SpeechRecognizer Event Handlers

    void SpeechRecognizer_StartedListening(object sender, EventArgs e)
    {
        menuController.SpeechRecognizerIsLoading = false;
    }
    void SpeechRecognizer_StoppedListening(object sender, EventArgs e)
    {
        menuController.SpeechRecognizerIsLoading = true;
    }


    // The ZigKinectSpeechRecognizer will send a SpeechRecognized event whenever it 
    //  recognizes a spoken phrase as being a phrase included in one of its assigned Grammars.
    void SpeechRecognizer_SpeechRecognized(object sender, ZigKinectSpeechRecognizer.SpeechRecognized_EventArgs e)
    {
        string semanticTag = e.SemanticTag;

        switch (ListeningMode)
        {
            case ListeningModeEnum.ChoiceSelection:      OnChoiceSpoken(semanticTag);        break;
            case ListeningModeEnum.ChoiceConfirmation:   OnConfirmationSpoken(semanticTag);  break;
            case ListeningModeEnum.Restart:              OnRestartSpoken(semanticTag);       break;
        }
    }

    void OnChoiceSpoken(string choiceStr)
    {
        MenuController1.Choice choice;
        if (!MenuController1.TryGetChoiceForString(choiceStr, out choice))
        {
            if (verbose) { Debug.LogWarning(ClassName + " :: OnChoiceSpoken - Unrecognized Choice: " + choiceStr); }
            return; 
        }

        // Give the MenuController the SelectChoice command
        menuController.SelectChoice(choice);
    }

    void OnConfirmationSpoken(string confirmationStr)
    {
        bool choiceConfirmed;
        if (!TryGetBoolForYesNoString(confirmationStr, out choiceConfirmed))
        {
            if (verbose) { Debug.LogWarning(ClassName + " :: OnConfirmationSpoken - Unrecognized Confirmation: " + confirmationStr); }
            return;
        }

        if (choiceConfirmed) 
        {
            // Give the MenuController the ConfirmPendingChoice command
            menuController.ConfirmPendingChoice(); 
        }
        else                
        {
            // Give the MenuController the DenyPendingChoice command
            menuController.DenyPendingChoice(); 
        }
    }

    static public bool TryGetBoolForYesNoString(string str, out bool outBool)
    {
        outBool = false;

        string strUpper = str.ToUpper();
        if      (strUpper == "YES") { outBool = true;  return true; }
        else if (strUpper == "NO")  { outBool = false; return true; }

        return false;
    }

    void OnRestartSpoken(string restartStr)
    {
        // Give the MenuController the Restart command
        menuController.Restart();
    }

    #endregion

}
