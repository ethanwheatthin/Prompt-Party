using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class RoundStarter : MonoBehaviour
{
    public static RoundStarter Instance { get; private set; }

    [Header("UI")]
    public Button startRoundButton;

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
        if (startRoundButton != null)
        {
            startRoundButton.onClick.AddListener(OnStartRoundClicked);
        }
    }

    public async void OnStartRoundClicked()
    {
        Debug.Log("[RoundStarter] Start Round button clicked");

        if (!NetworkManager.Instance.IsConnected)
        {
            Debug.LogWarning("[RoundStarter] Not connected to server");
            return;
        }

        // Disable button to prevent multiple clicks
        if (startRoundButton != null)
        {
            startRoundButton.interactable = false;
        }

        // Send host_action to backend
        await NetworkManager.Instance.SendHostAction("start_round");

        Debug.Log("[RoundStarter] Sent start_round action to backend");
    }

    public void LoadRoundScene()
    {
        Debug.Log("[RoundStarter] Loading Round scene");
        SceneManager.LoadScene("RoundScene");
    }
}
