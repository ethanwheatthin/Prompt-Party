using NativeWebSocket;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using UnityEngine;

public class NetworkManager : MonoBehaviour
{
    public static NetworkManager Instance { get; private set; }

    [Header("WebSocket")]
    public string serverUrl = "ws://localhost:3000"; // change to wss://... in production
    private NativeWebSocket.WebSocket websocket;

    public bool IsConnected => websocket != null && websocket.State == NativeWebSocket.WebSocketState.Open;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(this.gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(this.gameObject);
    }

    private async void Start()
    {
        await Connect();
    }

    public async Task Connect()
    {
        if (websocket != null)
        {
            await Close();
        }

        websocket = new WebSocket(serverUrl);

        websocket.OnOpen += () =>
        {
            Debug.Log("[Network] WebSocket opened.");
            // Example: send auth after opening if you have a token
            // SendAuth("dev-jwt-token");
        };

        websocket.OnError += (e) =>
        {
            Debug.LogError("[Network] WebSocket error: " + e);
        };

        websocket.OnClose += (e) =>
        {
            Debug.Log("[Network] WebSocket closed: " + e);
        };

        websocket.OnMessage += (bytes) =>
        {
            try
            {
                var msg = Encoding.UTF8.GetString(bytes);
                HandleMessage(msg);
            }
            catch (Exception ex)
            {
                Debug.LogError("[Network] Error parsing message: " + ex);
            }
        };

        await websocket.Connect();
    }

    private void Update()
    {
#if !UNITY_WEBGL || UNITY_EDITOR
        websocket?.DispatchMessageQueue();
#endif
    }

    public async Task Close()
    {
        if (websocket != null)
        {
            await websocket.Close();
            websocket = null;
        }
    }

    private void OnApplicationQuit()
    {
        _ = Close();
    }

    // Envelope structure: { type: "...", payload: { ... } }
    private void HandleMessage(string json)
    {
        JObject env;
        try
        {
            env = JObject.Parse(json);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[Network] Invalid JSON: {e.Message} — raw: {json}");
            return;
        }

        string type = env.Value<string>("type");
        var payload = env["payload"];

        if (string.IsNullOrEmpty(type))
        {
            Debug.LogWarning("[Network] Missing type field in envelope.");
            return;
        }

        Debug.Log($"[Network] Received message type={type}, payload={payload?.ToString(Newtonsoft.Json.Formatting.None)}");

        // dispatch by type
        switch (type)
        {
            case "auth_ok":
                // payload: { playerId, roomState }
                var roomState = payload?["roomState"];
                if (roomState != null)
                {
                    var joinCode = roomState.Value<string>("joinCode") ?? roomState.Value<string>("roomId");
                    if (!string.IsNullOrEmpty(joinCode)) RoomStateController.Instance?.SetJoinCode(joinCode);
                }
                break;
            case "room_state":
                // payload is the roomState
                var join = payload?.Value<string>("joinCode") ?? payload?.Value<string>("roomId");
                if (!string.IsNullOrEmpty(join)) RoomStateController.Instance?.SetJoinCode(join);
                break;
            default:
                // other message types (round_started, cut_vote_update, etc.) are ignored by UI controller
                Debug.Log($"[Network] Unhandled or UI-ignored message type {type}");
                break;
        }
    }

    public async Task SendEnvelopeAsync(string type, object payload)
    {
        if (!IsConnected)
        {
            Debug.LogWarning("[Network] Not connected, cannot send message.");
            return;
        }

        var env = new { type, payload };
        var json = JsonConvert.SerializeObject(env);
        await websocket.SendText(json);
        Debug.Log($"[Network] Sent {type}: {json}");
    }

    public Task SendAuth(string token)
    {
        return SendEnvelopeAsync("auth", new { token });
    }

    public Task SendHostAction(string action, object payload = null)
    {
        return SendEnvelopeAsync("host_action", new { action, payload });
    }
}