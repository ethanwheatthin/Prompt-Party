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

    private Coroutine currentDownload;

    private void Awake()
    {
        // Try to find RawImage if not assigned
        if (qrImage == null)
        {
            qrImage = GetComponent<RawImage>();
        }
        
        if (qrImage == null)
        {
            qrImage = GetComponentInChildren<RawImage>();
        }
        
        if (qrImage == null)
        {
            qrImage = FindObjectOfType<RawImage>();
        }
        
        if (qrImage == null)
        {
            Debug.LogWarning("[QRFromServer] No RawImage found. QR code will not be displayed.");
        }
    }

    // Call this to start loading a QR for a room code
    public void GenerateForRoom(string roomCode)
    {
        if (string.IsNullOrEmpty(roomCode))
        {
            Debug.LogWarning("[QRFromServer] roomCode is empty");
            return;
        }

        var url = backendHost.TrimEnd('/') + $"/qr?room={UnityWebRequest.EscapeURL(roomCode)}";
        Debug.Log($"[QRFromServer] Requesting QR code from: {url}");
        
        // Cancel any existing download
        if (currentDownload != null)
        {
            StopCoroutine(currentDownload);
        }
        
        currentDownload = StartCoroutine(DownloadAndSet(url));
    }

    // Call this to request a QR for an arbitrary full URL via backend (/qr?url=...)
    public void GenerateForUrl(string targetUrl)
    {
        if (string.IsNullOrEmpty(targetUrl))
        {
            Debug.LogWarning("[QRFromServer] targetUrl is empty");
            return;
        }
        
        if (qrImage == null)
        {
            Debug.LogError("[QRFromServer] qrImage is not assigned! Please assign a RawImage component in the inspector.");
            return;
        }

        var url = backendHost.TrimEnd('/') + $"/qr?url={UnityWebRequest.EscapeURL(targetUrl)}";
        Debug.Log($"[QRFromServer] Requesting QR code from: {url}");
        
        // Cancel any existing download
        if (currentDownload != null)
        {
            StopCoroutine(currentDownload);
        }
        
        currentDownload = StartCoroutine(DownloadAndSet(url));
    }

    private void Start()
    {
        // Optionally generate QR on start if needed
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
                Debug.LogError($"[QRFromServer] URL was: {url}");
                Debug.LogError($"[QRFromServer] Response code: {uwr.responseCode}");
                yield break;
            }

            var tex = DownloadHandlerTexture.GetContent(uwr);
            if (qrImage != null)
            {
                qrImage.texture = tex;
                qrImage.SetNativeSize();
                Debug.Log($"[QRFromServer] Successfully loaded and displayed QR code from {url}");
            }
            else
            {
                Debug.LogWarning("[QRFromServer] qrImage became null before texture could be set");
            }
        }
        
        currentDownload = null;
    }
}
