using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using TMPro;

public class LinkedListVisualizer : MonoBehaviour
{
    [Header("Linked List Settings")]
    public GameObject nodePrefab;
    public GameObject arrowPrefab;
    public float nodeSpacing = 0.3f;
    public int maxNodes = 10;
    public float animationDuration = 0.5f;
    
    [Header("AR References")]
    public ARRaycastManager raycastManager;
    public ARPlaneManager planeManager;
    
    private List<GameObject> nodes = new List<GameObject>();
    private List<GameObject> arrows = new List<GameObject>();
    private Vector3 startPosition;
    private Quaternion baseRotation;
    private bool isPlaced = false;
    private List<ARRaycastHit> hits = new List<ARRaycastHit>();
    private int nodeIdCounter = 1;
    
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
        
        // Verify prefabs
        if (nodePrefab == null)
        {
            Debug.LogError("❌ NODE PREFAB IS NOT ASSIGNED! Please assign it in the Inspector.");
        }
        else
        {
            Debug.Log("✅ Node Prefab assigned: " + nodePrefab.name);
        }
        
        if (arrowPrefab == null)
        {
            Debug.LogWarning("⚠️ Arrow Prefab is not assigned. Arrows will not be shown.");
        }
    }
    
    void Update()
    {
        // Only check for touches if list hasn't been placed yet
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
                    startPosition = hitPose.position;
                    startPosition.y += 0.01f; // Slightly above the plane
                    baseRotation = hitPose.rotation;
                    
                    // Move the entire list manager to this position
                    transform.position = startPosition;
                    transform.rotation = baseRotation;
                    
                    isPlaced = true;
                    
                    Debug.Log("🎯 Linked List placed at: " + startPosition);
                    
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
    
    public bool IsListPlaced()
    {
        return isPlaced;
    }
    
    // Insert at the beginning (HEAD)
    public void InsertAtHead()
    {
        if (!isPlaced)
        {
            Debug.Log("Linked List not placed yet! Tap on a plane first.");
            return;
        }
        
        if (nodes.Count >= maxNodes)
        {
            Debug.Log("List is full!");
            return;
        }
        
        if (nodePrefab == null)
        {
            Debug.LogError("❌ Cannot add node: nodePrefab is not assigned!");
            return;
        }
        
        // Create new node at head position
        Vector3 newPosition = startPosition;
        GameObject newNode = CreateNode(newPosition, nodeIdCounter++);
        
        // Insert at beginning
        nodes.Insert(0, newNode);
        
        // Shift all other nodes to the right
        StartCoroutine(ShiftNodesRight(0));
        
        Debug.Log($"✅ Inserted at HEAD. List size: {nodes.Count}");
    }
    
    // Insert at the end (TAIL)
    public void InsertAtTail()
    {
        if (!isPlaced)
        {
            Debug.Log("Linked List not placed yet! Tap on a plane first.");
            return;
        }
        
        if (nodes.Count >= maxNodes)
        {
            Debug.Log("List is full!");
            return;
        }
        
        if (nodePrefab == null)
        {
            Debug.LogError("❌ Cannot add node: nodePrefab is not assigned!");
            return;
        }
        
        Vector3 newPosition = startPosition + transform.right * (nodes.Count * nodeSpacing);
        GameObject newNode = CreateNode(newPosition, nodeIdCounter++);
        
        nodes.Add(newNode);
        
        // Add arrow from previous node
        if (nodes.Count > 1)
        {
            CreateArrow(nodes.Count - 2);
        }
        
        StartCoroutine(AnimateAddNode(newNode, newPosition));
        Debug.Log($"✅ Inserted at TAIL. List size: {nodes.Count}");
    }
    
    // Insert at specific position
    public void InsertAtPosition(int position)
    {
        if (!isPlaced) return;
        
        if (nodes.Count >= maxNodes)
        {
            Debug.Log("List is full!");
            return;
        }
        
        // Clamp position
        position = Mathf.Clamp(position, 0, nodes.Count);
        
        if (position == 0)
        {
            InsertAtHead();
            return;
        }
        
        if (position >= nodes.Count)
        {
            InsertAtTail();
            return;
        }
        
        // Insert in middle
        Vector3 newPosition = startPosition + transform.right * (position * nodeSpacing);
        GameObject newNode = CreateNode(newPosition, nodeIdCounter++);
        
        nodes.Insert(position, newNode);
        
        // Shift nodes after insertion point
        StartCoroutine(ShiftNodesRight(position));
        
        Debug.Log($"✅ Inserted at position {position}. List size: {nodes.Count}");
    }
    
    // Delete from head
    public void DeleteFromHead()
    {
        if (nodes.Count == 0)
        {
            Debug.Log("List is empty!");
            return;
        }
        
        GameObject nodeToRemove = nodes[0];
        nodes.RemoveAt(0);
        
        // Remove first arrow if exists
        if (arrows.Count > 0)
        {
            Destroy(arrows[0]);
            arrows.RemoveAt(0);
        }
        
        StartCoroutine(AnimateRemoveNode(nodeToRemove));
        StartCoroutine(ShiftNodesLeft(0));
        
        Debug.Log($"✅ Deleted from HEAD. List size: {nodes.Count}");
    }
    
    // Delete from tail
    public void DeleteFromTail()
    {
        if (nodes.Count == 0)
        {
            Debug.Log("List is empty!");
            return;
        }
        
        GameObject nodeToRemove = nodes[nodes.Count - 1];
        nodes.RemoveAt(nodes.Count - 1);
        
        // Remove last arrow
        if (arrows.Count > 0)
        {
            Destroy(arrows[arrows.Count - 1]);
            arrows.RemoveAt(arrows.Count - 1);
        }
        
        StartCoroutine(AnimateRemoveNode(nodeToRemove));
        Debug.Log($"✅ Deleted from TAIL. List size: {nodes.Count}");
    }
    
    // Delete from position
    public void DeleteAtPosition(int position)
    {
        if (nodes.Count == 0)
        {
            Debug.Log("List is empty!");
            return;
        }
        
        position = Mathf.Clamp(position, 0, nodes.Count - 1);
        
        if (position == 0)
        {
            DeleteFromHead();
            return;
        }
        
        if (position >= nodes.Count - 1)
        {
            DeleteFromTail();
            return;
        }
        
        GameObject nodeToRemove = nodes[position];
        nodes.RemoveAt(position);
        
        // Remove arrow
        if (position < arrows.Count)
        {
            Destroy(arrows[position]);
            arrows.RemoveAt(position);
        }
        
        StartCoroutine(AnimateRemoveNode(nodeToRemove));
        StartCoroutine(ShiftNodesLeft(position));
        
        Debug.Log($"✅ Deleted from position {position}. List size: {nodes.Count}");
    }
    
    public void Clear()
    {
        foreach (GameObject node in nodes) Destroy(node);
        foreach (GameObject arrow in arrows) Destroy(arrow);
        
        nodes.Clear();
        arrows.Clear();
        nodeIdCounter = 1;
        
        isPlaced = false;
        
        // Show planes again
        ShowPlanes();
        
        Debug.Log("🗑️ Linked List cleared and reset!");
    }
    
    public int Size()
    {
        return nodes.Count;
    }
    
    // Get node value at position
    public string GetNodeValue(int position)
    {
        if (position < 0 || position >= nodes.Count)
            return "Invalid";
        
        TextMeshPro textMesh = nodes[position].GetComponentInChildren<TextMeshPro>();
        if (textMesh != null)
            return textMesh.text;
        
        return $"Node {position}";
    }
    
    GameObject CreateNode(Vector3 position, int id)
    {
        GameObject newNode = Instantiate(nodePrefab, position, baseRotation);
        newNode.transform.SetParent(transform);
        newNode.name = $"Node_{id}";
        
        // Set color based on position
        Renderer renderer = newNode.GetComponent<Renderer>();
        if (renderer != null)
        {
            float hue = (nodes.Count * 0.15f) % 1f;
            renderer.material.color = Color.HSVToRGB(hue, 0.7f, 0.9f);
        }
        
        // Add text label
        TextMeshPro textMesh = newNode.GetComponentInChildren<TextMeshPro>();
        if (textMesh != null)
        {
            textMesh.text = id.ToString();
        }
        
        return newNode;
    }
    
    System.Collections.IEnumerator ShiftNodesRight(int startIndex)
    {
        yield return new WaitForSeconds(0.1f);
        
        // Update all node positions
        for (int i = startIndex; i < nodes.Count; i++)
        {
            Vector3 targetPos = startPosition + transform.right * (i * nodeSpacing);
            StartCoroutine(MoveNode(nodes[i], targetPos));
        }
        
        // Recreate all arrows
        yield return new WaitForSeconds(animationDuration);
        RecreateAllArrows();
    }
    
    System.Collections.IEnumerator ShiftNodesLeft(int startIndex)
    {
        yield return new WaitForSeconds(0.1f);
        
        // Update all node positions
        for (int i = startIndex; i < nodes.Count; i++)
        {
            Vector3 targetPos = startPosition + transform.right * (i * nodeSpacing);
            StartCoroutine(MoveNode(nodes[i], targetPos));
        }
        
        // Recreate all arrows
        yield return new WaitForSeconds(animationDuration);
        RecreateAllArrows();
    }
    
    System.Collections.IEnumerator MoveNode(GameObject node, Vector3 targetPos)
    {
        Vector3 startPos = node.transform.position;
        float elapsed = 0f;
        
        while (elapsed < animationDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / animationDuration;
            node.transform.position = Vector3.Lerp(startPos, targetPos, t);
            yield return null;
        }
        
        node.transform.position = targetPos;
    }
    
    void RecreateAllArrows()
    {
        // Clear existing arrows
        foreach (GameObject arrow in arrows)
        {
            if (arrow != null) Destroy(arrow);
        }
        arrows.Clear();
        
        // Create new arrows
        for (int i = 0; i < nodes.Count - 1; i++)
        {
            CreateArrow(i);
        }
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
    
    void CreateArrow(int fromIndex)
    {
        if (arrowPrefab == null || fromIndex >= nodes.Count - 1) return;
        
        Vector3 start = nodes[fromIndex].transform.position;
        Vector3 end = nodes[fromIndex + 1].transform.position;
        Vector3 midPoint = (start + end) / 2f;
        
        GameObject arrow = Instantiate(arrowPrefab, midPoint, Quaternion.identity);
        arrow.transform.SetParent(transform);
        arrow.transform.LookAt(end);
        arrow.transform.localScale = new Vector3(0.05f, 0.05f, Vector3.Distance(start, end));
        
        arrows.Add(arrow);
    }
    
    System.Collections.IEnumerator AnimateAddNode(GameObject node, Vector3 targetPos)
    {
        Vector3 startPos = targetPos + Vector3.up * 0.3f;
        node.transform.position = startPos;
        node.transform.localScale = Vector3.zero;
        
        float elapsed = 0f;
        while (elapsed < animationDuration)
        {
            elapsed += Time.deltaTime;
            float t = 1f - Mathf.Pow(1f - elapsed / animationDuration, 3f);
            
            node.transform.position = Vector3.Lerp(startPos, targetPos, t);
            node.transform.localScale = Vector3.one * 0.15f * t;
            yield return null;
        }
        
        node.transform.position = targetPos;
        node.transform.localScale = Vector3.one * 0.15f;
    }
    
    System.Collections.IEnumerator AnimateRemoveNode(GameObject node)
    {
        float elapsed = 0f;
        Vector3 startPos = node.transform.position;
        
        while (elapsed < animationDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / animationDuration;
            
            node.transform.position = startPos + Vector3.up * (0.3f * t);
            node.transform.localScale = Vector3.one * 0.15f * (1f - t);
            yield return null;
        }
        
        Destroy(node);
    }
}