using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class StackVisualizer : MonoBehaviour
{
    [Header("Stack Settings")]
    public GameObject stackItemPrefab;
    public float itemHeight = 0.2f;
    public float spacing = 0.02f;
    public int maxStackSize = 10;
    public float animationDuration = 0.5f;
    
    [Header("AR References")]
    public ARRaycastManager raycastManager;
    public ARPlaneManager planeManager;
    
    private Stack<GameObject> stackItems = new Stack<GameObject>();
    private Vector3 spawnPosition;
    private Quaternion baseRotation;
    private bool isPlaced = false;
    private List<ARRaycastHit> hits = new List<ARRaycastHit>();
    
    void Start()
    {
        // Auto-find ARPlaneManager if not assigned
        if (planeManager == null)
        {
            planeManager = FindObjectOfType<ARPlaneManager>();
            Debug.Log("ARPlaneManager: " + (planeManager != null ? "Found" : "NOT FOUND"));
        }
        
        // Auto-find ARRaycastManager if not assigned
        if (raycastManager == null)
        {
            raycastManager = FindObjectOfType<ARRaycastManager>();
            Debug.Log("ARRaycastManager: " + (raycastManager != null ? "Found" : "NOT FOUND"));
        }
        
        // Verify stackItemPrefab
        if (stackItemPrefab == null)
        {
            Debug.LogError("❌ STACK ITEM PREFAB IS NOT ASSIGNED! Please assign it in the Inspector.");
        }
        else
        {
            Debug.Log("✅ Stack Item Prefab assigned: " + stackItemPrefab.name);
        }
    }
    
    void Update()
    {
        // Only check for touches if stack hasn't been placed yet
        if (!isPlaced)
        {
            // Check for touch input (also support mouse for editor testing)
            if (Input.touchCount > 0 || Input.GetMouseButtonDown(0))
            {
                Vector2 touchPosition;
                
                if (Input.touchCount > 0)
                {
                    Touch touch = Input.GetTouch(0);
                    if (touch.phase != TouchPhase.Began) return;
                    touchPosition = touch.position;
                }
                else
                {
                    touchPosition = Input.mousePosition;
                }
                
                Debug.Log("Touch detected at: " + touchPosition);
                
                // Raycast against AR planes
                if (raycastManager != null && raycastManager.Raycast(touchPosition, hits, TrackableType.PlaneWithinPolygon))
                {
                    Debug.Log($"✅ Hit {hits.Count} planes!");
                    
                    // Get the hit pose
                    Pose hitPose = hits[0].pose;
                    
                    // Set base position and rotation
                    spawnPosition = hitPose.position;
                    spawnPosition.y += 0.01f; // Slightly above the plane
                    baseRotation = hitPose.rotation;
                    
                    // Move the entire stack manager to this position
                    transform.position = spawnPosition;
                    transform.rotation = baseRotation;
                    
                    isPlaced = true;
                    
                    Debug.Log("🎯 Stack placed at: " + spawnPosition);
                    
                    // Disable plane visualization after placement
                    HidePlanes();
                }
                else
                {
                    Debug.LogWarning("⚠️ Raycast did NOT hit any planes. Make sure planes are detected.");
                }
            }
        }
    }
    
    public bool IsStackPlaced()
    {
        return isPlaced;
    }
    
    public void Push()
    {
        if (!isPlaced)
        {
            Debug.Log("Stack not placed yet! Tap on a plane first.");
            return;
        }
        
        if (stackItems.Count >= maxStackSize)
        {
            Debug.Log("Stack is full!");
            return;
        }
        
        if (stackItemPrefab == null)
        {
            Debug.LogError("❌ Cannot push: stackItemPrefab is not assigned!");
            return;
        }
        
        Vector3 newPosition = spawnPosition;
        newPosition.y += stackItems.Count * (itemHeight + spacing);
        
        Debug.Log($"📍 Creating stack item at position: {newPosition}");
        
        GameObject newItem = Instantiate(stackItemPrefab, newPosition, baseRotation);
        newItem.transform.SetParent(transform);
        
        Debug.Log($"✅ Stack item GameObject created: {newItem.name}");
        
        Renderer renderer = newItem.GetComponent<Renderer>();
        if (renderer != null)
        {
            float hue = (stackItems.Count * 0.1f) % 1f;
            renderer.material.color = Color.HSVToRGB(hue, 0.8f, 0.9f);
        }
        
        stackItems.Push(newItem);
        StartCoroutine(AnimatePush(newItem, newPosition));
        
        Debug.Log($"✅ Pushed. Stack size: {stackItems.Count}");
    }
    
    public void Pop()
    {
        if (stackItems.Count == 0)
        {
            Debug.Log("Stack is empty!");
            return;
        }
        
        GameObject topItem = stackItems.Pop();
        StartCoroutine(AnimatePop(topItem));
        
        Debug.Log($"✅ Popped. Stack size: {stackItems.Count}");
    }
    
    public void Clear()
    {
        while (stackItems.Count > 0)
        {
            Destroy(stackItems.Pop());
        }
        
        isPlaced = false;
        
        // Show planes again
        ShowPlanes();
        
        Debug.Log("🗑️ Stack cleared and reset!");
    }
    
    public int Size()
    {
        return stackItems.Count;
    }
    
    // Hide AR planes
    void HidePlanes()
    {
        if (planeManager != null)
        {
            foreach (var plane in planeManager.trackables)
            {
                plane.gameObject.SetActive(false);
            }
            Debug.Log("👻 AR Planes hidden");
        }
    }
    
    // Show AR planes
    void ShowPlanes()
    {
        if (planeManager != null)
        {
            foreach (var plane in planeManager.trackables)
            {
                plane.gameObject.SetActive(true);
            }
            Debug.Log("👁️ AR Planes visible again");
        }
    }
    
    System.Collections.IEnumerator AnimatePush(GameObject item, Vector3 targetPos)
    {
        Vector3 startPos = targetPos + Vector3.up * 0.5f;
        item.transform.position = startPos;
        item.transform.localScale = Vector3.zero;
        
        float elapsed = 0f;
        while (elapsed < animationDuration)
        {
            elapsed += Time.deltaTime;
            float t = 1f - Mathf.Pow(1f - elapsed / animationDuration, 3f);
            
            item.transform.position = Vector3.Lerp(startPos, targetPos, t);
            item.transform.localScale = Vector3.one * 0.15f * t;
            yield return null;
        }
        
        item.transform.position = targetPos;
        item.transform.localScale = Vector3.one * 0.15f;
    }
    
    System.Collections.IEnumerator AnimatePop(GameObject item)
    {
        Vector3 startPos = item.transform.position;
        Vector3 endPos = startPos + Vector3.up * 0.5f;
        
        float elapsed = 0f;
        while (elapsed < animationDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / animationDuration;
            
            item.transform.position = Vector3.Lerp(startPos, endPos, t);
            item.transform.localScale = Vector3.Lerp(Vector3.one * 0.15f, Vector3.zero, t);
            yield return null;
        }
        
        Destroy(item);
    }
}