using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using TMPro;

public class TreeVisualizer : MonoBehaviour
{
    [Header("Tree Settings")]
    public GameObject nodePrefab;
    public GameObject linePrefab;
    public float levelHeight = 0.3f;
    public float horizontalSpacing = 0.4f;
    public float animationDuration = 0.3f;
    
    [Header("AR Components")]
    public ARRaycastManager raycastManager;
    public ARPlaneManager planeManager;
    
    private List<TreeNode> treeNodes = new List<TreeNode>();
    private List<GameObject> lines = new List<GameObject>();
    private bool isTreePlaced = false;
    private int nodeIdCounter = 0;
    
    // Tree node class
    private class TreeNode
    {
        public GameObject nodeObject;
        public int id;
        public int level;
        public int positionInLevel;
        public TreeNode leftChild;
        public TreeNode rightChild;
        public TreeNode parent;
        public string nodeType;
    }
    
    void Start()
    {
        if (raycastManager == null)
            raycastManager = FindObjectOfType<ARRaycastManager>();
        
        if (planeManager == null)
            planeManager = FindObjectOfType<ARPlaneManager>();
    }
    
    void Update()
    {
        if (!isTreePlaced && Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            
            if (touch.phase == TouchPhase.Began)
            {
                PlaceTreeOnPlane(touch.position);
            }
        }
    }
    
    void PlaceTreeOnPlane(Vector2 touchPosition)
    {
        if (raycastManager == null) return;
        
        List<ARRaycastHit> hits = new List<ARRaycastHit>();
        
        if (raycastManager.Raycast(touchPosition, hits, TrackableType.PlaneWithinPolygon))
        {
            Pose hitPose = hits[0].pose;
            transform.position = hitPose.position;
            transform.rotation = hitPose.rotation;
            
            isTreePlaced = true;
            HidePlanes();
        }
    }
    
    public bool IsTreePlaced()
    {
        return isTreePlaced;
    }
    
    public void AddRoot()
    {
        if (treeNodes.Count > 0)
        {
            Debug.LogWarning("Root already exists!");
            return;
        }
        
        Vector3 rootPosition = transform.position;
        GameObject nodeObj = Instantiate(nodePrefab, rootPosition, Quaternion.identity, transform);
        
        Renderer renderer = nodeObj.GetComponent<Renderer>();
        if (renderer != null)
            renderer.material.color = Color.red;
        
        AddNodeLabel(nodeObj, "R");
        
        TreeNode root = new TreeNode
        {
            nodeObject = nodeObj,
            id = nodeIdCounter++,
            level = 0,
            positionInLevel = 0,
            nodeType = "root"
        };
        
        treeNodes.Add(root);
        AnimateNodeAppearance(nodeObj);
    }
    
    public void AddLeftChild()
    {
        if (treeNodes.Count == 0)
        {
            Debug.LogWarning("Add root first!");
            return;
        }
        
        TreeNode parent = FindNodeWithoutLeftChild();
        
        if (parent == null)
        {
            Debug.LogWarning("All nodes have left children!");
            return;
        }
        
        int childLevel = parent.level + 1;
        int childPositionInLevel = parent.positionInLevel * 2;
        
        // Calculate position - simpler approach
        Vector3 parentPos = parent.nodeObject.transform.position;
        float offset = horizontalSpacing / Mathf.Pow(2, childLevel);
        
        Vector3 childPosition = new Vector3(
            parentPos.x - offset,
            parentPos.y - levelHeight,
            parentPos.z
        );
        
        GameObject nodeObj = Instantiate(nodePrefab, childPosition, Quaternion.identity, transform);
        
        Renderer renderer = nodeObj.GetComponent<Renderer>();
        if (renderer != null)
            renderer.material.color = Color.cyan;
        
        AddNodeLabel(nodeObj, $"L{parent.id}");
        
        TreeNode leftChild = new TreeNode
        {
            nodeObject = nodeObj,
            id = nodeIdCounter++,
            level = childLevel,
            positionInLevel = childPositionInLevel,
            parent = parent,
            nodeType = "left"
        };
        
        parent.leftChild = leftChild;
        treeNodes.Add(leftChild);
        
        DrawLine(parentPos, childPosition);
        AnimateNodeAppearance(nodeObj);
    }
    
    public void AddRightChild()
    {
        if (treeNodes.Count == 0)
        {
            Debug.LogWarning("Add root first!");
            return;
        }
        
        TreeNode parent = FindNodeWithoutRightChild();
        
        if (parent == null)
        {
            Debug.LogWarning("All nodes have right children!");
            return;
        }
        
        int childLevel = parent.level + 1;
        int childPositionInLevel = parent.positionInLevel * 2 + 1;
        
        // Calculate position - simpler approach
        Vector3 parentPos = parent.nodeObject.transform.position;
        float offset = horizontalSpacing / Mathf.Pow(2, childLevel);
        
        Vector3 childPosition = new Vector3(
            parentPos.x + offset,
            parentPos.y - levelHeight,
            parentPos.z
        );
        
        GameObject nodeObj = Instantiate(nodePrefab, childPosition, Quaternion.identity, transform);
        
        Renderer renderer = nodeObj.GetComponent<Renderer>();
        if (renderer != null)
            renderer.material.color = Color.magenta;
        
        AddNodeLabel(nodeObj, $"R{parent.id}");
        
        TreeNode rightChild = new TreeNode
        {
            nodeObject = nodeObj,
            id = nodeIdCounter++,
            level = childLevel,
            positionInLevel = childPositionInLevel,
            parent = parent,
            nodeType = "right"
        };
        
        parent.rightChild = rightChild;
        treeNodes.Add(rightChild);
        
        DrawLine(parentPos, childPosition);
        AnimateNodeAppearance(nodeObj);
    }
    
    public void RemoveNode()
    {
        if (treeNodes.Count == 0) return;
        
        TreeNode lastNode = treeNodes[treeNodes.Count - 1];
        
        if (lastNode.parent != null)
        {
            if (lastNode.parent.leftChild == lastNode)
                lastNode.parent.leftChild = null;
            else if (lastNode.parent.rightChild == lastNode)
                lastNode.parent.rightChild = null;
        }
        
        if (lastNode.nodeObject != null)
            Destroy(lastNode.nodeObject);
        
        treeNodes.RemoveAt(treeNodes.Count - 1);
        
        RedrawLines();
    }
    
    public void Clear()
    {
        foreach (TreeNode node in treeNodes)
        {
            if (node.nodeObject != null)
                Destroy(node.nodeObject);
        }
        treeNodes.Clear();
        
        foreach (GameObject line in lines)
        {
            if (line != null)
                Destroy(line);
        }
        lines.Clear();
        
        isTreePlaced = false;
        nodeIdCounter = 0;
        ShowPlanes();
    }
    
    public int GetNodeCount()
    {
        return treeNodes.Count;
    }
    
    public int GetHeight()
    {
        if (treeNodes.Count == 0) return 0;
        
        int maxLevel = 0;
        foreach (TreeNode node in treeNodes)
        {
            if (node.level > maxLevel)
                maxLevel = node.level;
        }
        
        return maxLevel + 1;
    }
    
    public string GetTraversalInfo()
    {
        if (treeNodes.Count == 0) return "Tree is empty";
        
        TreeNode root = treeNodes[0];
        
        List<string> inOrder = new List<string>();
        List<string> preOrder = new List<string>();
        List<string> postOrder = new List<string>();
        
        InOrderTraversal(root, inOrder);
        PreOrderTraversal(root, preOrder);
        PostOrderTraversal(root, postOrder);
        
        string result = "📚 Tree Traversals:\n\n";
        result += "In-Order (Left-Root-Right):\n" + string.Join(" → ", inOrder) + "\n\n";
        result += "Pre-Order (Root-Left-Right):\n" + string.Join(" → ", preOrder) + "\n\n";
        result += "Post-Order (Left-Right-Root):\n" + string.Join(" → ", postOrder);
        
        return result;
    }
    
    private void InOrderTraversal(TreeNode node, List<string> result)
    {
        if (node == null) return;
        
        InOrderTraversal(node.leftChild, result);
        result.Add($"Node{node.id}");
        InOrderTraversal(node.rightChild, result);
    }
    
    private void PreOrderTraversal(TreeNode node, List<string> result)
    {
        if (node == null) return;
        
        result.Add($"Node{node.id}");
        PreOrderTraversal(node.leftChild, result);
        PreOrderTraversal(node.rightChild, result);
    }
    
    private void PostOrderTraversal(TreeNode node, List<string> result)
    {
        if (node == null) return;
        
        PostOrderTraversal(node.leftChild, result);
        PostOrderTraversal(node.rightChild, result);
        result.Add($"Node{node.id}");
    }
    
    private TreeNode FindNodeWithoutLeftChild()
    {
        foreach (TreeNode node in treeNodes)
        {
            if (node.leftChild == null)
                return node;
        }
        return null;
    }
    
    private TreeNode FindNodeWithoutRightChild()
    {
        foreach (TreeNode node in treeNodes)
        {
            if (node.rightChild == null)
                return node;
        }
        return null;
    }
    
    private void DrawLine(Vector3 start, Vector3 end)
    {
        GameObject line = new GameObject("TreeLine");
        line.transform.parent = transform;
        
        LineRenderer lr = line.AddComponent<LineRenderer>();
        lr.startWidth = 0.01f;
        lr.endWidth = 0.01f;
        
        // Create a simple material for the line
        lr.material = new Material(Shader.Find("Unlit/Color"));
        lr.material.color = Color.white;
        
        lr.startColor = Color.white;
        lr.endColor = Color.white;
        lr.positionCount = 2;
        lr.SetPosition(0, start);
        lr.SetPosition(1, end);
        
        // Disable shadows
        lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        lr.receiveShadows = false;
        
        lines.Add(line);
        
        Debug.Log($"Drew line from {start} to {end}");
    }
    
    private void RedrawLines()
    {
        foreach (GameObject line in lines)
        {
            if (line != null)
                Destroy(line);
        }
        lines.Clear();
        
        foreach (TreeNode node in treeNodes)
        {
            if (node.leftChild != null)
            {
                DrawLine(node.nodeObject.transform.position, 
                        node.leftChild.nodeObject.transform.position);
            }
            
            if (node.rightChild != null)
            {
                DrawLine(node.nodeObject.transform.position, 
                        node.rightChild.nodeObject.transform.position);
            }
        }
    }
    
    private void AddNodeLabel(GameObject nodeObj, string label)
    {
        // Create text object as child of the node
        GameObject textObj = new GameObject("Label");
        textObj.transform.SetParent(nodeObj.transform);
        
        // Position text ABOVE the sphere in WORLD space first
        // Sphere scale is 0.1 (radius = 0.05), so offset by 0.15 units above
        textObj.transform.position = nodeObj.transform.position + new Vector3(0, 0.15f, 0);
        
        // Add TextMeshPro component
        TextMeshPro tmp = textObj.AddComponent<TextMeshPro>();
        tmp.text = label;
        tmp.fontSize = 0.5f;  // Smaller font size for 3D world space
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        tmp.fontStyle = FontStyles.Bold;
        
        // Set the renderer sorting
        MeshRenderer meshRenderer = textObj.GetComponent<MeshRenderer>();
        if (meshRenderer != null)
        {
            meshRenderer.sortingOrder = 100;
        }
        
        // Set rect transform size
        RectTransform rectTransform = tmp.GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            rectTransform.sizeDelta = new Vector2(2, 1);
        }
        
        // Make text always face camera
        StartCoroutine(FaceCamera(textObj.transform));
        
        Debug.Log($"Added label '{label}' at world position: {textObj.transform.position}, parent: {nodeObj.name}");
    }
    
    private System.Collections.IEnumerator FaceCamera(Transform textTransform)
    {
        while (textTransform != null)
        {
            if (Camera.main != null)
            {
                textTransform.rotation = Quaternion.LookRotation(textTransform.position - Camera.main.transform.position);
            }
            yield return null;
        }
    }
    
    private void AnimateNodeAppearance(GameObject nodeObj)
    {
        nodeObj.transform.localScale = Vector3.zero;
        StartCoroutine(ScaleUp(nodeObj));
    }
    
    private System.Collections.IEnumerator ScaleUp(GameObject obj)
    {
        float elapsed = 0f;
        Vector3 targetScale = Vector3.one * 0.1f;
        
        while (elapsed < animationDuration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / animationDuration;
            obj.transform.localScale = Vector3.Lerp(Vector3.zero, targetScale, progress);
            yield return null;
        }
        
        obj.transform.localScale = targetScale;
    }
    
    private void HidePlanes()
    {
        if (planeManager != null)
        {
            foreach (var plane in planeManager.trackables)
            {
                plane.gameObject.SetActive(false);
            }
            planeManager.enabled = false;
        }
    }
    
    private void ShowPlanes()
    {
        if (planeManager != null)
        {
            planeManager.enabled = true;
            foreach (var plane in planeManager.trackables)
            {
                plane.gameObject.SetActive(true);
            }
        }
    }
}