using System;
using UnityEngine;
using UnityEngine.UI;

public class RoomStateController : MonoBehaviour
{
    public static RoomStateController Instance { get; private set; }

    [Header("UI References")]
    public Text roomCodeText;

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
}