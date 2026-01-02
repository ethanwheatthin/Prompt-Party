using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Newtonsoft.Json.Linq;
using System;

public class RoundController : MonoBehaviour
{
    public static RoundController Instance { get; private set; }

    [Header("UI References")]
    public TextMeshProUGUI topicText;
    public TextMeshProUGUI actorNameText;
    public TextMeshProUGUI timerText;
    public TextMeshProUGUI statusText;
    public GameObject actorBanner; // "You are the actor!" banner

    [Header("Round Data")]
    private string roundId;
    private string actorId;
    private string topic;
    private long startedAt;
    private long minCutoffAt;
    private long maxEndAt;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        // Hide actor banner by default
        if (actorBanner != null)
        {
            actorBanner.SetActive(false);
        }

        if (statusText != null)
        {
            statusText.text = "Waiting for round data...";
        }

        // Load round data from NetworkManager if available
        if (NetworkManager.Instance != null)
        {
            var roundData = NetworkManager.Instance.GetCurrentRoundData();
            if (roundData != null)
            {
                SetRoundData(roundData);
            }
        }
    }

    private void Update()
    {
        if (maxEndAt > 0)
        {
            UpdateTimer();
        }
    }

    public void SetRoundData(JObject roundData)
    {
        if (roundData == null) return;

        roundId = roundData.Value<string>("roundId");
        actorId = roundData.Value<string>("actorId");
        topic = roundData.Value<string>("topic");
        startedAt = roundData.Value<long>("startedAt");
        minCutoffAt = roundData.Value<long>("minCutoffAt");
        maxEndAt = roundData.Value<long>("maxEndAt");

        Debug.Log($"[RoundController] Round set - Topic: {topic}, Actor: {actorId}");

        // Update UI
        if (topicText != null)
        {
            topicText.text = topic;
        }

        // TODO: Look up actor name from room state
        if (actorNameText != null)
        {
            actorNameText.text = $"Actor: {actorId.Substring(0, 8)}...";
        }

        if (statusText != null)
        {
            statusText.text = "Waiting for prompts...";
        }

        // Show actor banner if this client is the actor
        // (We'll need to track our playerId in NetworkManager)
        CheckIfActor();
    }

    private void CheckIfActor()
    {
        // TODO: Compare actorId with our playerId from NetworkManager
        // For now, just hide the banner
        if (actorBanner != null)
        {
            actorBanner.SetActive(false);
        }
    }

    private void UpdateTimer()
    {
        if (timerText == null) return;

        long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        long timeRemaining = maxEndAt - now;

        if (timeRemaining <= 0)
        {
            timerText.text = "0:00";
            return;
        }

        int seconds = (int)(timeRemaining / 1000);
        int minutes = seconds / 60;
        seconds = seconds % 60;

        timerText.text = $"{minutes}:{seconds:D2}";
    }
}
