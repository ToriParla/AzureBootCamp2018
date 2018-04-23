using UnityEngine;
using System;
using Assets.BotDirectLine;
using SimpleJSON;
using System.Text;

public class BotDirectLineManager
{
    #region Private Fields

    private const string DirectLineV3ApiUriPrefix = "https://directline.botframework.com/v3/directline/";

    private const string ConversationsApiUri = "conversations";

    private const string ActivitiesApiUriPostfix = "activities";

    private const string DirectLineChannelId = "directline";

    private static BotDirectLineManager instance;

    #endregion

    #region Public Propertices

    public event EventHandler<BotResponseEventArgs> BotResponse;

    public static BotDirectLineManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = new BotDirectLineManager();
            }

            return instance;
        }
    }

    public bool IsInitialized
    {
        get;
        private set;
    }

    public string SecretKey
    {
        get;
        set;
    }

    #endregion

    #region Constructor

    /// <summary>
    /// Constructor.
    /// </summary>
    public BotDirectLineManager()
    {
        IsInitialized = false;
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Initializes this instance by setting the bot secret.
    /// </summary>
    /// <param name="secretKey">The secret key of the bot.</param>
    public static void Initialize(string secretKey)
    {
        if (string.IsNullOrEmpty(secretKey))
        {
            throw new ArgumentException("Secret key cannot be null or empty");
        }

        BotDirectLineManager instance = Instance;
        instance.SecretKey = secretKey;
        instance.IsInitialized = true;
    }

    /// <summary>
    /// Starts a new conversation with the bot.
    /// </summary>
    /// <returns></returns>
    public async System.Threading.Tasks.Task StartConversationAsync()
    {
        if (IsInitialized)
        {
            string responseAsString = await PostAsync(ConversationsApiUri);

            Invoke(responseAsString);
        }
        else
        {
            Debug.Log("Bot Direct Line manager is not initialized");
        }
    }

    #endregion

    /// <summary>
    /// Sends the given message to the given conversation.
    /// </summary>
    /// <param name="conversationId">The conversation ID.</param>
    /// <param name="fromId">The ID of the sender.</param>
    /// <param name="message">The message to sent.</param>
    /// <param name="fromName">The name of the sender (optional).</param>
    /// <returns></returns>
    public async System.Threading.Tasks.Task SendMessageAsync(string conversationId, string fromId, string message, string fromName = null)
    {
#if UNITY_UWP

        if (string.IsNullOrEmpty(conversationId))
        {
            throw new ArgumentException("Conversation ID cannot be null or empty");
        }

        if (IsInitialized)
        {
            Debug.Log("SendMessage: " + conversationId + "; " + message);

            var content = new System.Net.Http.StringContent(new MessageActivity(fromId, message, DirectLineChannelId, null, fromName).ToJsonString(), Encoding.UTF8, "application/json");

            var responseAsString = await PostAsync(string.Format("{0}/{1}/{2}", ConversationsApiUri, conversationId, ActivitiesApiUriPostfix), content);

            Invoke(responseAsString);
        }
        else
        {
            Debug.Log("Bot Direct Line manager is not initialized");
        }
#endif
    }

    /// <summary>
    /// Retrieves the activities of the given conversation.
    /// </summary>
    /// <param name="conversationId">The conversation ID.</param>
    /// <param name="watermark">Indicates the most recent message seen (optional).</param>
    /// <returns></returns>
    public async System.Threading.Tasks.Task GetMessagesAsync(string conversationId, string watermark = null)
    {
        if (string.IsNullOrEmpty(conversationId))
        {
            throw new ArgumentException("Conversation ID cannot be null or empty");
        }

        if (IsInitialized)
        {
            var responseAsString = await GetAsync(string.Format("{0}/{1}/{2}", ConversationsApiUri, conversationId, ActivitiesApiUriPostfix));

            Invoke(responseAsString);
        }
        else
        {
            Debug.Log("Bot Direct Line manager is not initialized");
        }
    }

    #region Private Methods

    private async System.Threading.Tasks.Task<string> PostAsync(string url, object content = null)
    {
        var responseAsString = string.Empty;

#if UNITY_UWP

        using (var client = new System.Net.Http.HttpClient
        {
            BaseAddress = new Uri(DirectLineV3ApiUriPrefix)
        })
        {
            client.DefaultRequestHeaders.Add("Authorization", "Bearer " + SecretKey);
            client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

            var result = client.PostAsync(url, (System.Net.Http.StringContent)content).Result;

            if (result.IsSuccessStatusCode)
            {
                using (var resultContent = result.Content)
                {
                    responseAsString = await resultContent.ReadAsStringAsync();
                }
            }
        }

#endif

        return responseAsString;
    }

    private async System.Threading.Tasks.Task<string> GetAsync(string url)
    {
        var responseAsString = string.Empty;

#if UNITY_UWP

        using (var client = new System.Net.Http.HttpClient
        {
            BaseAddress = new Uri(DirectLineV3ApiUriPrefix)
        })
        {
            client.DefaultRequestHeaders.Add("Authorization", "Bearer " + SecretKey);
            client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

            var result = client.GetAsync(url).Result;

            if (result.IsSuccessStatusCode)
            {
                using (var resultContent = result.Content)
                {
                    responseAsString = await resultContent.ReadAsStringAsync();
                }
            }
        }

#endif

        return responseAsString;
    }

    private void Invoke(string responseAsString)
    {
        if (!string.IsNullOrEmpty(responseAsString))
        {
            Debug.Log("Received response:\n" + responseAsString);
            BotResponseEventArgs eventArgs = CreateBotResponseEventArgs(responseAsString);

            if (BotResponse != null)
            {
                BotResponse.Invoke(this, eventArgs);
            }
        }
        else
        {
            Debug.Log("Received an empty response");
        }
    }

    /// <summary>
    /// Creates a new BotResponseEventArgs instance based on the given response.
    /// </summary>
    /// <param name="responseAsString"></param>
    /// <returns></returns>
    private BotResponseEventArgs CreateBotResponseEventArgs(string responseAsString)
    {
        if (string.IsNullOrEmpty(responseAsString))
        {
            throw new ArgumentException("Response cannot be null or empty");
        }

        JSONNode responseJsonRootNode = JSONNode.Parse(responseAsString);
        JSONNode jsonNode = null;
        BotResponseEventArgs eventArgs = new BotResponseEventArgs();

        if ((jsonNode = responseJsonRootNode[BotJsonProtocol.KeyError]) != null)
        {
            eventArgs.EventType = EventTypes.Error;
            eventArgs.Code = jsonNode[BotJsonProtocol.KeyCode];
            string message = jsonNode[BotJsonProtocol.KeyMessage];

            if (!string.IsNullOrEmpty(message))
            {
                eventArgs.Message = message;
            }
        }
        else if (responseJsonRootNode[BotJsonProtocol.KeyConversationId] != null)
        {
            eventArgs.EventType = EventTypes.ConversationStarted;
            eventArgs.ConversationId = responseJsonRootNode[BotJsonProtocol.KeyConversationId];
        }
        else if (responseJsonRootNode[BotJsonProtocol.KeyId] != null)
        {
            eventArgs.EventType = EventTypes.MessageSent;
            eventArgs.SentMessageId = responseJsonRootNode[BotJsonProtocol.KeyId];
        }
        else if ((jsonNode = responseJsonRootNode[BotJsonProtocol.KeyActivities]) != null)
        {
            eventArgs.EventType = EventTypes.MessageReceived;
            eventArgs.Watermark = responseJsonRootNode[BotJsonProtocol.KeyWatermark];
            JSONArray jsonArray = jsonNode.AsArray;

            foreach (JSONNode activityNode in jsonArray)
            {
                MessageActivity messageActivity = MessageActivity.FromJson(activityNode);
                eventArgs.Messages.Add(messageActivity);
            }
        }

        return eventArgs;
    }

    private byte[] Utf8StringToByteArray(string stringToBeConverted)
    {
        return Encoding.UTF8.GetBytes(stringToBeConverted);
    }

    #endregion
}