using System;
using System.Linq;
using Assets.BotDirectLine;
using HoloToolkit.Unity;
using UnityEngine;
using UnityEngine.Windows.Speech;

public class MainSceneManager : MonoBehaviour
{
    #region Public Properties

    public AudioSource RoboHeadAudioManager;

    public Animator RoboHeadAnimController;

    #endregion

    #region Private Fields

    // PUT THE DIRECT LINE KEY HERE
    private const string DirectLineKey = "He15a4ifywY.cwA.jMo.z50dwq7FQazD1vp4KcmC60tuzeQOWSgjJFcNt9fpQrc";

    private DictationRecognizer dictationRecognizer;

    private TextToSpeech textToSpeech;

    private string conversationId;

    #endregion

    #region Unity3D Default Methods

    void Start()
    {
        textToSpeech = gameObject.AddComponent<TextToSpeech>();
        textToSpeech.Voice = TextToSpeechVoice.Mark;

        dictationRecognizer = new DictationRecognizer();
        dictationRecognizer.DictationResult += DictationRecognizer_DictationResult;
        dictationRecognizer.DictationHypothesis += DictationRecognizer_DictationHypothesis;
        dictationRecognizer.DictationComplete += DictationRecognizer_DictationComplete;
        dictationRecognizer.DictationError += DictationRecognizer_DictationError;

        dictationRecognizer.Start();

        BotDirectLineManager.Initialize(DirectLineKey);
        BotDirectLineManager.Instance.BotResponse += OnBotResponse;

        BotDirectLineManager.Instance.StartConversationAsync().Wait();
    }

    void Update()
    {
        if (textToSpeech.IsSpeaking())
        {
            RoboHeadAnimController.SetInteger("ChangeStateAnimation", 1);
        }
        else
        {
            RoboHeadAnimController.SetInteger("ChangeStateAnimation", 0);
        }
    }

    void OnApplicationQuit()
    {
        Debug.Log($"Application ending after {Time.time} seconds");

        dictationRecognizer.DictationResult -= DictationRecognizer_DictationResult;
        dictationRecognizer.DictationComplete -= DictationRecognizer_DictationComplete;
        dictationRecognizer.DictationHypothesis -= DictationRecognizer_DictationHypothesis;
        dictationRecognizer.DictationError -= DictationRecognizer_DictationError;
        dictationRecognizer.Dispose();
    }

    #endregion

    #region Private Methods

    private void DictationRecognizer_DictationError(string error, int hresult)
    {
        Debug.Log($"DictationRecognizer_DictationError: {error}");
    }

    private void DictationRecognizer_DictationComplete(DictationCompletionCause cause)
    {
        Debug.Log($"DictationRecognizer_DictationComplete: {cause.ToString()}");

        if (cause != DictationCompletionCause.Complete)
        {
            this.dictationRecognizer.Start();
            Debug.Log("dictationRecognizer.Start");
        }
    }

    private void DictationRecognizer_DictationHypothesis(string text)
    {
        Debug.Log($"DictationRecognizer_DictationHypothesis: {text}");
    }

    private void DictationRecognizer_DictationResult(string text, ConfidenceLevel confidence)
    {
        Debug.Log($"DictationRecognizer_DictationResult: {text}confidence: {confidence.ToString()}");

        if (confidence == ConfidenceLevel.Rejected || confidence == ConfidenceLevel.Low)
        {
            textToSpeech.SpeakSsml("<?xml version=\"1.0\"?><speak speed=\"80%\" version=\"1.0\" xmlns=\"http://www.w3.org/2001/10/synthesis\" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xsi:schemaLocation=\"http://www.w3.org/2001/10/synthesis http://www.w3.org/TR/speech-synthesis/synthesis.xsd\" xml:lang=\"en-US\">Sorry, but I don't understand you.</speak>");
        }

        if (!string.IsNullOrWhiteSpace(conversationId))
        {
            BotDirectLineManager.Instance.SendMessageAsync(conversationId, "AzureBootCamp2018", text).Wait();
        }
    }

    private void OnBotResponse(object sender, Assets.BotDirectLine.BotResponseEventArgs e)
    {
        Debug.Log($"OnBotResponse: {e.ToString()}");

        switch (e.EventType)
        {
            case EventTypes.ConversationStarted:
                if (!string.IsNullOrWhiteSpace(e.ConversationId))
                {
                    // Store the ID
                    textToSpeech.SpeakSsml("<?xml version=\"1.0\"?><speak speed=\"80%\" version=\"1.0\" xmlns=\"http://www.w3.org/2001/10/synthesis\" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xsi:schemaLocation=\"http://www.w3.org/2001/10/synthesis http://www.w3.org/TR/speech-synthesis/synthesis.xsd\" xml:lang=\"en-US\">Bot connection established!</speak>");
                    conversationId = e.ConversationId;
                }
                else
                {
                    textToSpeech.SpeakSsml("<?xml version=\"1.0\"?><speak speed=\"80%\" version=\"1.0\" xmlns=\"http://www.w3.org/2001/10/synthesis\" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xsi:schemaLocation=\"http://www.w3.org/2001/10/synthesis http://www.w3.org/TR/speech-synthesis/synthesis.xsd\" xml:lang=\"en-US\">Error while connecting to Bot!</speak>");
                }
                break;
            case EventTypes.MessageSent:
                if (!string.IsNullOrEmpty(conversationId))
                {
                    // Get the bot's response(s)
                    BotDirectLineManager.Instance.GetMessagesAsync(conversationId).Wait();
                }

                break;
            case EventTypes.MessageReceived:
                // Handle the received message(s)
                if (!string.IsNullOrWhiteSpace(conversationId))
                {
                    var messageActivity = e.Messages.LastOrDefault();
                    Debug.Log(messageActivity.Text);
                    textToSpeech.SpeakSsml("<?xml version=\"1.0\"?><speak speed=\"80%\" version=\"1.0\" xmlns=\"http://www.w3.org/2001/10/synthesis\" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xsi:schemaLocation=\"http://www.w3.org/2001/10/synthesis http://www.w3.org/TR/speech-synthesis/synthesis.xsd\" xml:lang=\"en-US\"> " + messageActivity.Text + "</speak>");
                }
                break;
            case EventTypes.Error:
                // Handle the error
                break;
        }
    }

    #endregion

    #region Public Mehtods

    #endregion
}