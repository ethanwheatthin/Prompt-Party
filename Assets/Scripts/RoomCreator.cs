using System;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json.Linq;

public class RoomCreator : MonoBehaviour
{
    [Header("API")]
    public string createRoomUrl = "http://localhost:3000/rooms";

    [Header("UI")]
    public UnityEngine.UI.Button createRoomButton;
    public UnityEngine.UI.Text feedbackText;

    [Header("Behavior")]
    public bool createRoomOnStart = true;

    [Header("QR")]
    public QRFromServer qrFetcher; // optional: assign to display backend-generated QR
    public string backendHost = "http://localhost:3000";

    private void Start()
    {
        if (createRoomButton != null)
        {
            createRoomButton.onClick.AddListener(OnCreateRoomClicked);
        }

        if (createRoomOnStart)
        {
            // fire-and-forget async call
            _ = CreateRoomAsync();
        }
    }

    public void OnCreateRoomClicked()
    {
        _ = CreateRoomAsync();
    }

    private async Task CreateRoomAsync()
    {
        try
        {
            if (feedbackText != null) feedbackText.text = "Creating room...";

            if (string.IsNullOrEmpty(createRoomUrl))
            {
                Debug.LogError("[RoomCreator] createRoomUrl is empty");
                if (feedbackText != null) feedbackText.text = "Invalid createRoomUrl";
                return;
            }

            var payload = new { hostName = "Unity Host" };
            string json = Newtonsoft.Json.JsonConvert.SerializeObject(payload);

            using (var uwr = new UnityWebRequest(createRoomUrl, "POST"))
            {
                byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
                uwr.uploadHandler = new UploadHandlerRaw(bodyRaw);
                uwr.downloadHandler = new DownloadHandlerBuffer();
                uwr.SetRequestHeader("Content-Type", "application/json");

                var operation = uwr.SendWebRequest();
                while (!operation.isDone) await Task.Yield();

#if UNITY_2020_1_OR_NEWER
                if (uwr.result == UnityWebRequest.Result.ConnectionError || uwr.result == UnityWebRequest.Result.ProtocolError)
#else
                if (uwr.isNetworkError || uwr.isHttpError)
#endif
                {
                    Debug.LogError($"[RoomCreator] Error creating room: {uwr.error}");
                    if (feedbackText != null) feedbackText.text = $"Error: {uwr.error}";
                    return;
                }

                var text = uwr.downloadHandler.text;
                Debug.Log($"[RoomCreator] create room response: {text}");
                var obj = JObject.Parse(text);

                var joinCode = obj.Value<string>("joinCode") ?? obj.Value<string>("roomId");
                var token = obj.Value<string>("token");

                // set join code in RoomStateController
                if (!string.IsNullOrEmpty(joinCode) && RoomStateController.Instance != null)
                {
                    RoomStateController.Instance.SetJoinCode(joinCode);
                }

                // set token in NetworkManager (so it will send auth when socket opens)
                if (NetworkManager.Instance != null)
                {
                    NetworkManager.Instance.authToken = token;
                    // ensure WebSocket connects (it may already be connected)
                    _ = NetworkManager.Instance.Connect();
                }

                if (feedbackText != null)
                {
                    feedbackText.text = string.IsNullOrEmpty(joinCode) ? "Room created" : $"Room: {joinCode}";
                }

                // generate QR via backend and show in UI if qrFetcher assigned
                if (qrFetcher != null && !string.IsNullOrEmpty(joinCode))
                {
                    qrFetcher.backendHost = backendHost;
                    qrFetcher.GenerateForRoom(joinCode);
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogError("[RoomCreator] Exception: " + ex);
            if (feedbackText != null) feedbackText.text = "Exception creating room";
        }
    }
}