using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;

public class QueueManager : MonoBehaviour
{
    [Header("Queue Settings")]
    public GameObject nodePrefab;
    public float nodeSpacing = 0.4f;
    public int maxQueueSize = 6;
    
    [Header("Queue Position")]
    public Transform queueStartPosition;
    
    [Header("AR References")]
    public ARPlaneManager planeManager;
    public ARRaycastManager raycastManager;
    
    private List<QueueNode> queueNodes = new List<QueueNode>();
    private Vector3 basePosition;
    private Quaternion baseRotation;
    private bool queuePlaced = false;
    private List<ARRaycastHit> hits = new List<ARRaycastHit>();
    
    void Start()
    {
        // Set initial position
        if (queueStartPosition != null)
        {
            basePosition = queueStartPosition.position;
        }
        else
        {
            basePosition = transform.position;
        }
        
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
        
        // Verify nodePrefab
        if (nodePrefab == null)
        {
            Debug.LogError("❌ NODE PREFAB IS NOT ASSIGNED! Please assign it in the Inspector.");
        }
        else
        {
            Debug.Log("✅ Node Prefab assigned: " + nodePrefab.name);
        }
    }
    
    void Update()
    {
        // Only check for touches if queue hasn't been placed yet
        if (!queuePlaced)
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
                if (raycastManager != null && raycastManager.Raycast(touchPosition, hits, UnityEngine.XR.ARSubsystems.TrackableType.PlaneWithinPolygon))
                {
                    Debug.Log($"✅ Hit {hits.Count} planes!");
                    
                    // Get the hit pose
                    Pose hitPose = hits[0].pose;
                    
                    // Set base position and rotation
                    basePosition = hitPose.position;
                    baseRotation = hitPose.rotation;
                    
                    // Move the entire queue manager to this position
                    transform.position = basePosition;
                    transform.rotation = baseRotation;
                    
                    queuePlaced = true;
                    
                    Debug.Log("🎯 Queue placed at: " + basePosition);
                    
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
    
    // Add a node to the back of the queue (Enqueue)
    public void Enqueue(string value)
    {
        Debug.Log($"🔹 Enqueue called with value: {value}");
        
        // Check if queue is full
        if (queueNodes.Count >= maxQueueSize)
        {
            Debug.LogWarning("⚠️ Queue is full! Cannot enqueue.");
            return;
        }
        
        // Check if nodePrefab is assigned
        if (nodePrefab == null)
        {
            Debug.LogError("❌ Cannot enqueue: nodePrefab is not assigned!");
            return;
        }
        
        // Calculate position for new node (at the back)
        Vector3 newPosition = CalculateNodePosition(queueNodes.Count);
        Debug.Log($"📍 Creating node at position: {newPosition}");
        
        // Create the new node
        GameObject nodeObj = Instantiate(nodePrefab, newPosition, baseRotation);
        nodeObj.transform.SetParent(transform);
        
        Debug.Log($"✅ Node GameObject created: {nodeObj.name}");
        
        // Get the QueueNode component and set its value
        QueueNode node = nodeObj.GetComponent<QueueNode>();
        if (node != null)
        {
            node.SetValue(value);
            node.AnimateAppear();
            Debug.Log($"✅ QueueNode component found and value set to: {value}");
        }
        else
        {
            Debug.LogError("❌ QueueNode component NOT FOUND on prefab! Make sure the prefab has QueueNode script attached.");
        }
        
        // Add to our list
        queueNodes.Add(node);
        
        Debug.Log($"✅ Enqueued: {value}. Queue size: {queueNodes.Count}");
    }
    
    // Remove a node from the front of the queue (Dequeue)
    public void Dequeue()
    {
        // Check if queue is empty
        if (queueNodes.Count == 0)
        {
            Debug.LogWarning("⚠️ Queue is empty! Cannot dequeue.");
            return;
        }
        
        // Get the first node
        QueueNode frontNode = queueNodes[0];
        string value = frontNode.nodeValue;
        
        // Remove from list
        queueNodes.RemoveAt(0);
        
        // Animate and destroy
        frontNode.AnimateDisappear();
        
        // Shift all remaining nodes forward
        ShiftNodesForward();
        
        Debug.Log($"✅ Dequeued: {value}. Queue size: {queueNodes.Count}");
    }
    
    // Peek at the front node without removing it
    public string Peek()
    {
        if (queueNodes.Count == 0)
        {
            return "Empty";
        }
        return queueNodes[0].nodeValue;
    }
    
    // Get current queue size
    public int Size()
    {
        return queueNodes.Count;
    }
    
    // Check if queue is empty
    public bool IsEmpty()
    {
        return queueNodes.Count == 0;
    }
    
    // Check if queue has been placed
    public bool IsQueuePlaced()
    {
        return queuePlaced;
    }
    
    // Get list of nodes (needed for QueueLabels)
    public List<QueueNode> GetNodes()
    {
        return queueNodes;
    }
    
    // Clear all nodes (but keep placement)
    public void Clear()
    {
        foreach (QueueNode node in queueNodes)
        {
            if (node != null)
            {
                Destroy(node.gameObject);
            }
        }
        queueNodes.Clear();
        Debug.Log("🗑️ Queue cleared!");
    }
    
    // NEW: Reset everything - clear nodes AND reset placement
    public void ResetQueue()
    {
        // Clear all nodes
        Clear();
        
        // Reset placement flag
        queuePlaced = false;
        
        // Show planes again
        ShowPlanes();
        
        Debug.Log("🔄 Queue RESET! You can now place the queue again.");
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
    
    // Calculate position for a node at given index
    Vector3 CalculateNodePosition(int index)
    {
        // Arrange nodes horizontally in a line (forward direction relative to queue)
        Vector3 offset = transform.right * (index * nodeSpacing);
        return basePosition + offset;
    }
    
    // Shift all nodes forward after dequeue
    void ShiftNodesForward()
    {
        for (int i = 0; i < queueNodes.Count; i++)
        {
            Vector3 newPos = CalculateNodePosition(i);
            queueNodes[i].MoveTo(newPos);
        }
    }
    
    // Add some test data (for testing)
    public void AddTestData()
    {
        string[] testValues = { "A", "B", "C", "D" };
        foreach (string value in testValues)
        {
            if (queueNodes.Count < maxQueueSize)
            {
                Enqueue(value);
            }
        }
    }
}