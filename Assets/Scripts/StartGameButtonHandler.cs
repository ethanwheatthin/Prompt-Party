using UnityEngine;

public class StartGameButtonHandler : MonoBehaviour
{
    // Called by the UI Button OnClick
    public void OnStartGameButtonPressed()
    {
        if (NetworkManager.Instance == null)
        {
            Debug.LogWarning("[StartGameButton] NetworkManager not found.");
            return;
        }

        // send host_action { action: "start_round" }
        _ = NetworkManager.Instance.SendHostAction("start_round", null);
        Debug.Log("[StartGameButton] Sent start_round host_action.");
    }
}