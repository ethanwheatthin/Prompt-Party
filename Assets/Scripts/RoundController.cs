using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;

public class RoundController : MonoBehaviour
{
    public static RoundController Instance { get; private set; }

    [Header("UI References")]
    public TextMeshProUGUI topicText;
    public TextMeshProUGUI actorNameText;
    public TextMeshProUGUI timerText;
    public TextMeshProUGUI statusText;
    public GameObject actorBanner; // "You are the actor!" banner

    [Header("Prompts Display")]
    public Transform promptsListContent; // Parent for prompt items
    public GameObject promptItemPrefab; // Prefab with text to show each prompt
    public TextMeshProUGUI promptCountText; // Shows "X/Y prompts submitted"

    [Header("Voting Display")]
    public GameObject votingPanel; // Panel shown during voting
    public TextMeshProUGUI votingStatusText;
    public TextMeshProUGUI selectedPromptText; // Shows the winning prompt

    [Header("Round Data")]
    private string roundId;
    private string actorId;
    private string topic;
    private long startedAt;
    private long minCutoffAt;
    private long maxEndAt;
    private List<PromptData> prompts = new List<PromptData>();
    private bool allPromptsSubmitted = false;
    private bool votingStarted = false;
    private bool promptSelected = false;

    [System.Serializable]
    private class PromptData
    {
        public string playerId;
        public string playerName;
        public string text;
        public bool revealed;
    }

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

        // Check for prompts in the data
        UpdatePrompts(roundData);

        // Show actor banner if this client is the actor
        // (We'll need to track our playerId in NetworkManager)
        CheckIfActor();
    }

    public void UpdatePrompts(JObject roundData)
    {
        if (roundData == null) return;

        var promptsArray = roundData["prompts"] as JArray;
        allPromptsSubmitted = roundData.Value<bool>("allPromptsSubmitted");

        if (promptsArray == null || promptsArray.Count == 0)
        {
            if (statusText != null)
            {
                statusText.text = $"Waiting for prompts... (0 received)";
            }
            if (promptCountText != null)
            {
                promptCountText.text = "0 prompts";
            }
            return;
        }

        prompts.Clear();
        foreach (var p in promptsArray)
        {
            prompts.Add(new PromptData
            {
                playerId = p.Value<string>("playerId"),
                playerName = p.Value<string>("playerName"),
                text = p.Value<string>("text"),
                revealed = p.Value<bool>("revealed")
            });
        }

        Debug.Log($"[RoundController] Received {prompts.Count} prompts, allSubmitted: {allPromptsSubmitted}");

        if (promptCountText != null)
        {
            promptCountText.text = $"{prompts.Count} prompt{(prompts.Count != 1 ? "s" : "")}";
        }

        if (statusText != null)
        {
            if (allPromptsSubmitted)
            {
                statusText.text = "All prompts received! Voting...";
            }
            else
            {
                statusText.text = $"{prompts.Count} prompt(s) received, waiting for more...";
            }
        }

        // Update prompts display
        DisplayPrompts();
    }

    private void DisplayPrompts()
    {
        if (promptsListContent == null) return;

        // Clear existing prompt items
        for (int i = promptsListContent.childCount - 1; i >= 0; i--)
        {
            Destroy(promptsListContent.GetChild(i).gameObject);
        }

        // Create new prompt items (flashcards)
        foreach (var prompt in prompts)
        {
            if (promptItemPrefab == null) continue;

            var item = Instantiate(promptItemPrefab, promptsListContent);

            // Display as flashcard - face down until revealed
            string displayText = prompt.revealed
                ? $"{prompt.playerName}: \"{prompt.text}\""
                : $"[Face Down Card] {prompt.playerName}";

            var tmp = item.GetComponentInChildren<TextMeshProUGUI>();
            if (tmp != null)
            {
                tmp.text = displayText;
                // Could add styling to make face-down cards look different
                if (!prompt.revealed)
                {
                    tmp.fontStyle = FontStyles.Italic;
                    tmp.color = new Color(0.5f, 0.5f, 0.5f);
                }
            }
            else
            {
                var legacyText = item.GetComponentInChildren<Text>();
                if (legacyText != null)
                {
                    legacyText.text = displayText;
                    if (!prompt.revealed)
                    {
                        legacyText.fontStyle = FontStyle.Italic;
                        legacyText.color = new Color(0.5f, 0.5f, 0.5f);
                    }
                }
            }
        }
    }

    public void HandleVotingStarted()
    {
        votingStarted = true;
        Debug.Log("[RoundController] Voting phase started!");

        if (votingPanel != null)
        {
            votingPanel.SetActive(true);
        }

        if (votingStatusText != null)
        {
            votingStatusText.text = "Players are voting...";
        }

        // Refresh display to show revealed prompts
        DisplayPrompts();
    }

    public void HandlePromptSelected(string promptText, string playerName, int votes)
    {
        promptSelected = true;
        Debug.Log($"[RoundController] Prompt selected: {promptText} by {playerName}");

        if (selectedPromptText != null)
        {
            selectedPromptText.text = $"Selected: \"{promptText}\"\n by {playerName} ({votes} vote{(votes != 1 ? "s" : "")})";
        }

        if (statusText != null)
        {
            statusText.text = "Ready to perform!";
        }
    }

    private void CheckIfActor()
    {
        // TODO: Compare actorId with our playerId from NetworkManager
        // For now, just hide the banner since Unity is always the host
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
