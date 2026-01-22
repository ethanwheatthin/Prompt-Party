using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Creates a RawImage UI element dynamically and uses QRFromServer to display QR codes
/// Attach this to any GameObject in the scene
/// </summary>
public class DynamicQRDisplay : MonoBehaviour
{
    [Header("QR Display Settings")]
    [Tooltip("Size of the QR code display")]
    public Vector2 qrSize = new Vector2(400, 400);
    
    [Tooltip("Position on screen (Anchored Position)")]
    public Vector2 position = new Vector2(0, 0);
    
    [Header("Backend")]
    public string backendHost = "http://localhost:3000";
    
    private QRFromServer qrFetcher;
    private Canvas canvas;
    private GameObject qrImageObject;

    private void Awake()
    {
        CreateQRDisplay();
    }

    private void CreateQRDisplay()
    {
        // Find or create canvas
        canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            var canvasObj = new GameObject("DynamicCanvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            
            var scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            
            canvasObj.AddComponent<GraphicRaycaster>();
            
            Debug.Log("[DynamicQRDisplay] Created new Canvas");
        }

        // Create QR Image GameObject
        qrImageObject = new GameObject("QRCodeDisplay");
        qrImageObject.transform.SetParent(canvas.transform, false);
        
        // Add RawImage component
        var rawImage = qrImageObject.AddComponent<RawImage>();
        rawImage.color = Color.white;
        
        // Configure RectTransform
        var rectTransform = qrImageObject.GetComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.sizeDelta = qrSize;
        rectTransform.anchoredPosition = position;
        
        // Add QRFromServer component to this GameObject
        qrFetcher = gameObject.AddComponent<QRFromServer>();
        qrFetcher.qrImage = rawImage;
        qrFetcher.backendHost = backendHost;
        
        Debug.Log("[DynamicQRDisplay] Created RawImage for QR display");
    }

    /// <summary>
    /// Generate QR code for a room
    /// </summary>
    public void GenerateQRForRoom(string roomCode)
    {
        if (qrFetcher != null)
        {
            qrFetcher.GenerateForRoom(roomCode);
        }
    }

    /// <summary>
    /// Generate QR code for a URL
    /// </summary>
    public void GenerateQRForUrl(string url)
    {
        if (qrFetcher != null)
        {
            qrFetcher.GenerateForUrl(url);
        }
    }

    /// <summary>
    /// Hide the QR display
    /// </summary>
    public void Hide()
    {
        if (qrImageObject != null)
        {
            qrImageObject.SetActive(false);
        }
    }

    /// <summary>
    /// Show the QR display
    /// </summary>
    public void Show()
    {
        if (qrImageObject != null)
        {
            qrImageObject.SetActive(true);
        }
    }
}
