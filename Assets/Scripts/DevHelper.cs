using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR
/// <summary>
/// Development helper to quickly test with multiple players
/// Only works in Unity Editor
/// </summary>
public class DevHelper : MonoBehaviour
{
    [Header("Settings")]
    public string backendUrl = "http://localhost:3000";
    public int numberOfPlayers = 3;
    
    [Header("Player Names")]
    public string[] playerNames = new string[] { "Alice", "Bob", "Charlie", "Diana", "Eve", "Frank" };

    [Header("UI (Optional - for button in scene)")]
    public Button seedPlayersButton;

    private string currentJoinCode;

    private void Start()
    {
        // Wire up button if provided
        if (seedPlayersButton != null)
        {
            seedPlayersButton.onClick.AddListener(SeedTestPlayers);
        }

        // Monitor for room code changes
        InvokeRepeating(nameof(CheckForJoinCode), 1f, 1f);
    }

    private void CheckForJoinCode()
    {
        if (RoomStateController.Instance != null && RoomStateController.Instance.roomCodeText != null)
        {
            string displayText = RoomStateController.Instance.roomCodeText.text;
            // Extract just the code (format is "Room: PPXXXXXX")
            if (displayText.Contains("Room: "))
            {
                string code = displayText.Replace("Room: ", "").Trim();
                if (!string.IsNullOrEmpty(code) && code.StartsWith("PP") && code.Length == 8 && code != currentJoinCode)
                {
                    currentJoinCode = code;
                    Debug.Log($"[DevHelper] Detected room code: {currentJoinCode}");
                    
                    // Enable button if it exists
                    if (seedPlayersButton != null)
                    {
                        seedPlayersButton.interactable = true;
                    }
                }
            }
        }
    }

    /// <summary>
    /// Open browser tabs with test players
    /// Can be called from context menu or button
    /// </summary>
    [ContextMenu("Seed Test Players")]
    public void SeedTestPlayers()
    {
        if (string.IsNullOrEmpty(currentJoinCode))
        {
            Debug.LogWarning("[DevHelper] No join code available yet. Create a room first!");
            return;
        }

        int playersToCreate = Mathf.Min(numberOfPlayers, playerNames.Length);
        Debug.Log($"[DevHelper] Opening {playersToCreate} browser tabs with test players for room {currentJoinCode}...");

        for (int i = 0; i < playersToCreate; i++)
        {
            string playerName = playerNames[i];
            string url = $"{backendUrl}/player.html?code={currentJoinCode}&name={UnityEngine.Networking.UnityWebRequest.EscapeURL(playerName)}&autoJoin=true";
            
            OpenBrowser(url);
            Debug.Log($"[DevHelper] Opening browser for player: {playerName}");
            
            // Small delay between opening tabs to avoid browser blocking
            System.Threading.Thread.Sleep(100);
        }

        Debug.Log($"[DevHelper] Opened {playersToCreate} browser tabs. Check your browser!");
    }

    private void OpenBrowser(string url)
    {
        try
        {
#if UNITY_EDITOR_WIN
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            });
#elif UNITY_EDITOR_OSX
            System.Diagnostics.Process.Start("open", url);
#elif UNITY_EDITOR_LINUX
            System.Diagnostics.Process.Start("xdg-open", url);
#endif
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[DevHelper] Failed to open browser: {e.Message}");
        }
    }

    private void OnGUI()
    {
        // Show debug info and button in game view
        GUILayout.BeginArea(new Rect(10, 10, 250, 150));
        
        GUILayout.Label("Dev Helper (Editor Only)", GUI.skin.box);
        
        if (!string.IsNullOrEmpty(currentJoinCode))
        {
            GUILayout.Label($"Room Code: {currentJoinCode}");
            
            if (GUILayout.Button("Seed Test Players", GUILayout.Height(40)))
            {
                SeedTestPlayers();
            }
        }
        else
        {
            GUILayout.Label("Waiting for room code...");
        }
        
        GUILayout.EndArea();
    }
}
#endif
