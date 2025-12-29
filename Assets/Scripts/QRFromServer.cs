using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

// Simple component to download a QR PNG from the backend and display it in a RawImage
public class QRFromServer : MonoBehaviour
{
    [Tooltip("RawImage UI element to display the QR code")]
    public RawImage qrImage;

    [Tooltip("Backend host, e.g. http://192.168.1.42:3000 (no trailing slash)")]
    public string backendHost = "http://localhost:3000";

    // Optional join code to fetch when Start() runs
    [Tooltip("Optional join code to request a QR for on Start")]
    public Text joinCode;

    // Call this to start loading a QR for a room code
    public void GenerateForRoom(string roomCode)
    {
        if (string.IsNullOrEmpty(roomCode))
        {
            Debug.LogWarning("QRFromServer: roomCode is empty");
            return;
        }
        var url = backendHost.TrimEnd('/') + $"/qr?room={UnityWebRequest.EscapeURL(roomCode)}";
        StartCoroutine(DownloadAndSet(url));
    }

    // If joinCode set in inspector, auto-generate on Start
    private void Start()
    {
        if (!string.IsNullOrEmpty(joinCode.ToString())) GenerateForRoom(joinCode.ToString());
    }

    private IEnumerator DownloadAndSet(string url)
    {
        using (var uwr = UnityWebRequestTexture.GetTexture(url))
        {
            uwr.SetRequestHeader("Accept", "image/png");
            yield return uwr.SendWebRequest();

#if UNITY_2020_1_OR_NEWER
            if (uwr.result == UnityWebRequest.Result.ConnectionError || uwr.result == UnityWebRequest.Result.ProtocolError)
#else
            if (uwr.isNetworkError || uwr.isHttpError)
#endif
            {
                Debug.LogError($"[QRFromServer] Failed to download QR: {uwr.error}");
                yield break;
            }

            var tex = DownloadHandlerTexture.GetContent(uwr);
            if (qrImage != null)
            {
                qrImage.texture = tex;
                qrImage.SetNativeSize();
            }
            Debug.Log($"[QRFromServer] Loaded QR from {url}");
        }
    }
}
