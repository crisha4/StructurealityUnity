using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GraphUI : MonoBehaviour
{
    [Header("References")]
    public GraphVisualizer graphVisualizer;
    
    [Header("Vertex Buttons")]
    public Button addVertexButton;
    public Button removeVertexButton;
    
    [Header("Edge Buttons")]
    public Button addEdgeButton;
    public Button addRandomEdgeButton;
    public Button removeEdgeButton;
    
    [Header("Other Buttons")]
    public Button toggleDirectedButton;
    public Button clearButton;
    
    [Header("UI Panels")]
    public GameObject headerPanel;
    public GameObject instructionCard;
    public GameObject buttonPanel;
    public GameObject explanationPanel;
    
    [Header("Info Display")]
    public TextMeshProUGUI infoText;
    public TextMeshProUGUI explanationText;
    public TextMeshProUGUI directedStatusText; // Shows "Directed" or "Undirected"
    
    [Header("Edge Input (Optional)")]
    public TMP_InputField fromVertexInput;
    public TMP_InputField toVertexInput;
    
    private bool buttonsVisible = false;
    
    void Start()
    {
        // Connect button click events
        if (addVertexButton != null)
            addVertexButton.onClick.AddListener(OnAddVertexClicked);
        
        if (removeVertexButton != null)
            removeVertexButton.onClick.AddListener(OnRemoveVertexClicked);
        
        if (addEdgeButton != null)
            addEdgeButton.onClick.AddListener(OnAddEdgeClicked);
        
        if (addRandomEdgeButton != null)
            addRandomEdgeButton.onClick.AddListener(OnAddRandomEdgeClicked);
        
        if (removeEdgeButton != null)
            removeEdgeButton.onClick.AddListener(OnRemoveEdgeClicked);
        
        if (toggleDirectedButton != null)
            toggleDirectedButton.onClick.AddListener(OnToggleDirectedClicked);
        
        if (clearButton != null)
            clearButton.onClick.AddListener(OnClearClicked);
        
        // Show header and instruction card initially
        if (headerPanel != null)
            headerPanel.SetActive(true);
        
        if (instructionCard != null)
            instructionCard.SetActive(true);
        
        // Hide explanation panel and info text initially
        if (explanationPanel != null)
            explanationPanel.SetActive(false);
        
        if (infoText != null)
            infoText.gameObject.SetActive(false);
        
        // Hide buttons initially
        HideButtons();
    }
    
    void Update()
    {
        // Check if graph has been placed
        if (graphVisualizer != null && graphVisualizer.IsGraphPlaced() && !buttonsVisible)
        {
            ShowButtons();
            UpdateExplanation("✅ Graph placed! Add vertices to start building.");
        }
        
        // Update info text continuously
        if (buttonsVisible)
        {
            UpdateInfoText();
            UpdateDirectedStatus();
        }
    }
    
    void HideButtons()
    {
        if (buttonPanel != null)
        {
            buttonPanel.SetActive(false);
        }
        else
        {
            if (addVertexButton != null) addVertexButton.gameObject.SetActive(false);
            if (removeVertexButton != null) removeVertexButton.gameObject.SetActive(false);
            if (addEdgeButton != null) addEdgeButton.gameObject.SetActive(false);
            if (addRandomEdgeButton != null) addRandomEdgeButton.gameObject.SetActive(false);
            if (removeEdgeButton != null) removeEdgeButton.gameObject.SetActive(false);
            if (toggleDirectedButton != null) toggleDirectedButton.gameObject.SetActive(false);
            if (clearButton != null) clearButton.gameObject.SetActive(false);
        }
        
        buttonsVisible = false;
    }
    
    void ShowButtons()
    {
        // Hide header and instruction card when graph is placed
        if (headerPanel != null)
            headerPanel.SetActive(false);
        
        if (instructionCard != null)
            instructionCard.SetActive(false);
        
        // Show explanation panel and info text
        if (explanationPanel != null)
            explanationPanel.SetActive(true);
        
        if (infoText != null)
            infoText.gameObject.SetActive(true);
        
        // Show buttons
        if (buttonPanel != null)
        {
            buttonPanel.SetActive(true);
        }
        else
        {
            if (addVertexButton != null) addVertexButton.gameObject.SetActive(true);
            if (removeVertexButton != null) removeVertexButton.gameObject.SetActive(true);
            if (addEdgeButton != null) addEdgeButton.gameObject.SetActive(true);
            if (addRandomEdgeButton != null) addRandomEdgeButton.gameObject.SetActive(true);
            if (removeEdgeButton != null) removeEdgeButton.gameObject.SetActive(true);
            if (toggleDirectedButton != null) toggleDirectedButton.gameObject.SetActive(true);
            if (clearButton != null) clearButton.gameObject.SetActive(true);
        }
        
        buttonsVisible = true;
    }
    
    void OnAddVertexClicked()
    {
        if (graphVisualizer == null) return;
        
        int countBefore = GetVertexCount();
        graphVisualizer.AddVertex();
        
        StartCoroutine(UpdateAfterAddVertex(countBefore));
    }
    
    System.Collections.IEnumerator UpdateAfterAddVertex(int countBefore)
    {
        yield return new WaitForEndOfFrame();
        
        int countAfter = GetVertexCount();
        UpdateInfoText();
        
        if (countAfter > countBefore)
            UpdateExplanation($"✅ Vertex {countAfter} added\n💡 Vertices are the nodes in a graph!");
        else
            UpdateExplanation("❌ Graph is full! Maximum vertices reached.");
    }
    
    void OnRemoveVertexClicked()
    {
        if (graphVisualizer == null) return;
        
        int countBefore = GetVertexCount();
        
        if (countBefore == 0)
        {
            UpdateExplanation("❌ Graph is empty! Add vertices first.");
            return;
        }
        
        graphVisualizer.RemoveVertex();
        
        StartCoroutine(UpdateAfterRemoveVertex(countBefore));
    }
    
    System.Collections.IEnumerator UpdateAfterRemoveVertex(int countBefore)
    {
        yield return new WaitForEndOfFrame();
        
        int countAfter = GetVertexCount();
        UpdateInfoText();
        
        if (countAfter < countBefore)
            UpdateExplanation($"✅ Removed vertex and its connections\n💡 Connected edges were also deleted!");
        else
            UpdateExplanation("❌ Could not remove vertex!");
    }
    
    void OnAddEdgeClicked()
    {
        if (graphVisualizer == null) return;
        
        if (GetVertexCount() < 2)
        {
            UpdateExplanation("❌ Need at least 2 vertices to create an edge!");
            return;
        }
        
        // Check if we have input fields
        if (fromVertexInput != null && toVertexInput != null &&
            !string.IsNullOrEmpty(fromVertexInput.text) && !string.IsNullOrEmpty(toVertexInput.text))
        {
            if (int.TryParse(fromVertexInput.text, out int from) && 
                int.TryParse(toVertexInput.text, out int to))
            {
                // Convert to 0-indexed
                from -= 1;
                to -= 1;
                
                int edgesBefore = GetEdgeCount();
                graphVisualizer.AddEdgeBetweenVertices(from, to);
                
                StartCoroutine(UpdateAfterAddEdge(edgesBefore, from + 1, to + 1));
                return;
            }
        }
        
        // If no input or invalid, add random edge
        OnAddRandomEdgeClicked();
    }
    
    void OnAddRandomEdgeClicked()
    {
        if (graphVisualizer == null) return;
        
        if (GetVertexCount() < 2)
        {
            UpdateExplanation("❌ Need at least 2 vertices to create an edge!");
            return;
        }
        
        int edgesBefore = GetEdgeCount();
        graphVisualizer.AddRandomEdge();
        
        StartCoroutine(UpdateAfterAddRandomEdge(edgesBefore));
    }
    
    System.Collections.IEnumerator UpdateAfterAddEdge(int edgesBefore, int from, int to)
    {
        yield return new WaitForEndOfFrame();
        
        int edgesAfter = GetEdgeCount();
        UpdateInfoText();
        
        string graphType = graphVisualizer.GetGraphType();
        
        if (edgesAfter > edgesBefore)
            UpdateExplanation($"✅ Edge created: {from} → {to}\n💡 Edges connect vertices in a {graphType} graph!");
        else
            UpdateExplanation("⚠️ Edge already exists or could not be created!");
    }
    
    System.Collections.IEnumerator UpdateAfterAddRandomEdge(int edgesBefore)
    {
        yield return new WaitForEndOfFrame();
        
        int edgesAfter = GetEdgeCount();
        UpdateInfoText();
        
        string graphType = graphVisualizer.GetGraphType();
        
        if (edgesAfter > edgesBefore)
            UpdateExplanation($"✅ Random edge created!\n💡 This is a {graphType} graph.");
        else
            UpdateExplanation("⚠️ Could not create edge! All possible edges may exist.");
    }
    
    void OnRemoveEdgeClicked()
    {
        if (graphVisualizer == null) return;
        
        int edgesBefore = GetEdgeCount();
        
        if (edgesBefore == 0)
        {
            UpdateExplanation("❌ No edges to remove!");
            return;
        }
        
        graphVisualizer.RemoveLastEdge();
        
        StartCoroutine(UpdateAfterRemoveEdge(edgesBefore));
    }
    
    System.Collections.IEnumerator UpdateAfterRemoveEdge(int edgesBefore)
    {
        yield return new WaitForEndOfFrame();
        
        int edgesAfter = GetEdgeCount();
        UpdateInfoText();
        
        if (edgesAfter < edgesBefore)
            UpdateExplanation("✅ Last edge removed\n💡 Vertices remain, only connection is gone!");
        else
            UpdateExplanation("❌ Could not remove edge!");
    }
    
    void OnToggleDirectedClicked()
    {
        if (graphVisualizer == null) return;
        
        graphVisualizer.ToggleDirected();
        
        string graphType = graphVisualizer.GetGraphType();
        UpdateDirectedStatus();
        
        if (graphType == "Directed")
            UpdateExplanation("🔄 Changed to DIRECTED graph\n💡 Edges now have direction (one-way)!");
        else
            UpdateExplanation("🔄 Changed to UNDIRECTED graph\n💡 Edges are now bidirectional (two-way)!");
    }
    
    void OnClearClicked()
    {
        if (graphVisualizer == null) return;
        
        graphVisualizer.Clear();
        buttonsVisible = false;
        
        // Hide buttons, explanation, and info
        HideButtons();
        
        if (explanationPanel != null)
            explanationPanel.SetActive(false);
        
        if (infoText != null)
            infoText.gameObject.SetActive(false);
        
        // Show header and instruction card again
        if (headerPanel != null)
            headerPanel.SetActive(true);
        
        if (instructionCard != null)
            instructionCard.SetActive(true);
    }
    
    void UpdateInfoText()
    {
        if (infoText == null || graphVisualizer == null) return;
        
        int vertices = GetVertexCount();
        int edges = GetEdgeCount();
        infoText.text = $"Vertices: {vertices} | Edges: {edges}";
    }
    
    void UpdateDirectedStatus()
    {
        if (directedStatusText != null && graphVisualizer != null)
        {
            string type = graphVisualizer.GetGraphType();
            directedStatusText.text = $"Type: {type}";
            
            // Color code
            if (type == "Directed")
                directedStatusText.color = Color.yellow;
            else
                directedStatusText.color = Color.cyan;
        }
    }
    
    void UpdateExplanation(string message)
    {
        if (explanationText != null)
        {
            explanationText.text = message;
        }
    }
    
    int GetVertexCount()
    {
        if (graphVisualizer == null) return 0;
        return graphVisualizer.GetVertexCount();
    }
    
    int GetEdgeCount()
    {
        if (graphVisualizer == null) return 0;
        return graphVisualizer.GetEdgeCount();
    }
}