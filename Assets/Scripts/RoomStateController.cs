using System;
using UnityEngine;
using UnityEngine.UI;
using Newtonsoft.Json.Linq;
using TMPro;

public class RoomStateController : MonoBehaviour
{
    public static RoomStateController Instance { get; private set; }

    [Header("UI References")]
    public Text roomCodeText;

    [Header("Player List UI")]
    public GameObject playerListItemPrefab; // prefab with a Text component
    public Transform playersListContent; // parent transform for instantiated rows

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(this.gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        // Generate a simple room code at startup and set it on the UI
        var joinCode = GenerateJoinCode(6);
        SetJoinCode(joinCode);
    }

    private string GenerateJoinCode(int length)
    {
        const string chars = "ABCDEFGHJKMNPQRSTUVWXYZ23456789"; // exclude confusing chars
        var rng = new System.Random();
        var buffer = new char[length];
        for (int i = 0; i < length; i++) buffer[i] = chars[rng.Next(chars.Length)];
        return new string(buffer);
    }

    public void SetJoinCode(string joinCode)
    {
        if (string.IsNullOrEmpty(joinCode)) return;
        if (roomCodeText != null)
        {
            roomCodeText.text = $"Room: {joinCode}";
        }
    }

    // Accept a room state JSON object (payload from server) and update UI
    public void UpdateRoomState(JObject roomState)
    {
        if (roomState == null) return;

        // update join code UI
        var joinCode = roomState.Value<string>("joinCode") ?? roomState.Value<string>("roomId");
        if (!string.IsNullOrEmpty(joinCode)) SetJoinCode(joinCode);

        // update players list
        var players = roomState["players"] as JArray;
        if (players == null) return;

        // clear existing list items
        if (playersListContent != null)
        {
            for (int i = playersListContent.childCount - 1; i >= 0; i--)
            {
                var child = playersListContent.GetChild(i);
                Destroy(child.gameObject);
            }
        }

        // instantiate a new item for each player (skip host)
        foreach (var p in players)
        {
            var name = p.Value<string>("name") ?? "Player";
            var isHost = p.Value<bool?>("isHost") ?? false;

            // skip showing the Unity host in the players list
            if (isHost) continue;

            if (playerListItemPrefab == null || playersListContent == null) continue;

            var go = Instantiate(playerListItemPrefab, playersListContent);
            // prefer TextMeshPro if present
            var tmp = go.GetComponentInChildren<TextMeshProUGUI>();
            if (tmp != null)
            {
                tmp.text = name;
                continue;
            }

            // fallback to legacy UI Text
            var txt = go.GetComponentInChildren<Text>();
            if (txt != null)
            {
                txt.text = name;
            }
            else
            {
                // fallback: set GameObject name
                go.name = name;
            }
        }
    }
}