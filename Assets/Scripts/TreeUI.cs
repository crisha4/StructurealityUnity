using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TreeUI : MonoBehaviour
{
    [Header("References")]
    public TreeVisualizer treeVisualizer;
    
    [Header("Buttons")]
    public Button addRootButton;
    public Button addLeftChildButton;
    public Button addRightChildButton;
    public Button removeNodeButton;
    public Button showTraversalButton;
    public Button clearButton;
    
    [Header("UI Panels")]
    public GameObject headerPanel;
    public GameObject instructionCard;
    public GameObject buttonPanel;
    public GameObject explanationPanel;
    public GameObject traversalPanel;
    
    [Header("Info Display")]
    public TextMeshProUGUI infoText;
    public TextMeshProUGUI explanationText;
    public TextMeshProUGUI traversalText;
    
    private bool buttonsVisible = false;
    private bool traversalVisible = false;
    
    void Start()
    {
        // Connect button click events
        if (addRootButton != null)
            addRootButton.onClick.AddListener(OnAddRootClicked);
        
        if (addLeftChildButton != null)
            addLeftChildButton.onClick.AddListener(OnAddLeftChildClicked);
        
        if (addRightChildButton != null)
            addRightChildButton.onClick.AddListener(OnAddRightChildClicked);
        
        if (removeNodeButton != null)
            removeNodeButton.onClick.AddListener(OnRemoveNodeClicked);
        
        if (showTraversalButton != null)
            showTraversalButton.onClick.AddListener(OnShowTraversalClicked);
        
        if (clearButton != null)
            clearButton.onClick.AddListener(OnClearClicked);
        
        // Show header and instruction card initially
        if (headerPanel != null)
            headerPanel.SetActive(true);
        
        if (instructionCard != null)
            instructionCard.SetActive(true);
        
        // Hide explanation panel, info text, and traversal panel initially
        if (explanationPanel != null)
            explanationPanel.SetActive(false);
        
        if (infoText != null)
            infoText.gameObject.SetActive(false);
        
        if (traversalPanel != null)
            traversalPanel.SetActive(false);
        
        // Hide buttons initially
        HideButtons();
    }
    
    void Update()
    {
        // Check if tree has been placed
        if (treeVisualizer != null && treeVisualizer.IsTreePlaced() && !buttonsVisible)
        {
            ShowButtons();
            UpdateExplanation("✅ Tree placed! Start by adding the ROOT node:");
        }
        
        // Update info text continuously
        if (buttonsVisible)
        {
            UpdateInfoText();
        }
        
        // Update traversal in real-time if visible
        if (traversalVisible && GetTreeSize() > 0)
        {
            UpdateTraversalDisplay();
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
            if (addRootButton != null) addRootButton.gameObject.SetActive(false);
            if (addLeftChildButton != null) addLeftChildButton.gameObject.SetActive(false);
            if (addRightChildButton != null) addRightChildButton.gameObject.SetActive(false);
            if (removeNodeButton != null) removeNodeButton.gameObject.SetActive(false);
            if (showTraversalButton != null) showTraversalButton.gameObject.SetActive(false);
            if (clearButton != null) clearButton.gameObject.SetActive(false);
        }
        
        buttonsVisible = false;
    }
    
    void ShowButtons()
    {
        // Hide header and instruction card when tree is placed
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
            if (addRootButton != null) addRootButton.gameObject.SetActive(true);
            if (addLeftChildButton != null) addLeftChildButton.gameObject.SetActive(true);
            if (addRightChildButton != null) addRightChildButton.gameObject.SetActive(true);
            if (removeNodeButton != null) removeNodeButton.gameObject.SetActive(true);
            if (showTraversalButton != null) showTraversalButton.gameObject.SetActive(true);
            if (clearButton != null) clearButton.gameObject.SetActive(true);
        }
        
        buttonsVisible = true;
    }
    
    void OnAddRootClicked()
    {
        if (treeVisualizer == null) return;
        
        int sizeBefore = GetTreeSize();
        
        if (sizeBefore > 0)
        {
            UpdateExplanation("❌ Root already exists! Use Left/Right buttons to add children.");
            return;
        }
        
        treeVisualizer.AddRoot();
        
        StartCoroutine(UpdateAfterAdd("ROOT (red)", "The root is the starting point of the tree!"));
    }
    
    void OnAddLeftChildClicked()
    {
        if (treeVisualizer == null) return;
        
        int sizeBefore = GetTreeSize();
        
        if (sizeBefore == 0)
        {
            UpdateExplanation("❌ Add the ROOT node first!");
            return;
        }
        
        treeVisualizer.AddLeftChild();
        
        StartCoroutine(UpdateAfterAdd("LEFT child (cyan)", "Left children go to the left of their parent!"));
    }
    
    void OnAddRightChildClicked()
    {
        if (treeVisualizer == null) return;
        
        int sizeBefore = GetTreeSize();
        
        if (sizeBefore == 0)
        {
            UpdateExplanation("❌ Add the ROOT node first!");
            return;
        }
        
        treeVisualizer.AddRightChild();
        
        StartCoroutine(UpdateAfterAdd("RIGHT child (magenta)", "Right children go to the right of their parent!"));
    }
    
    System.Collections.IEnumerator UpdateAfterAdd(string nodeType, string explanation)
    {
        yield return new WaitForSeconds(0.1f);
        
        UpdateInfoText();
        UpdateExplanation($"✅ Added {nodeType}\n💡 {explanation}");
    }
    
    void OnRemoveNodeClicked()
    {
        if (treeVisualizer == null) return;
        
        int sizeBefore = GetTreeSize();
        
        if (sizeBefore == 0)
        {
            UpdateExplanation("❌ Tree is empty! Nothing to remove.");
            return;
        }
        
        treeVisualizer.RemoveNode();
        
        StartCoroutine(UpdateAfterRemove());
    }
    
    System.Collections.IEnumerator UpdateAfterRemove()
    {
        yield return new WaitForSeconds(0.1f);
        
        UpdateInfoText();
        
        int sizeAfter = GetTreeSize();
        if (sizeAfter == 0)
        {
            UpdateExplanation("✅ Removed last node! Tree is now empty.");
        }
        else
        {
            UpdateExplanation("✅ Removed last added node\n💡 Nodes are removed in reverse order!");
        }
    }
    
    void OnShowTraversalClicked()
    {
        if (treeVisualizer == null) return;
        
        if (GetTreeSize() == 0)
        {
            UpdateExplanation("❌ Tree is empty! Add nodes first to see traversals.");
            return;
        }
        
        // Toggle traversal panel
        traversalVisible = !traversalVisible;
        
        if (traversalPanel != null)
            traversalPanel.SetActive(traversalVisible);
        
        if (traversalVisible)
        {
            UpdateTraversalDisplay();
            UpdateExplanation("📚 Showing tree traversal orders!\n💡 These show different ways to visit nodes.");
        }
        else
        {
            UpdateExplanation("📚 Traversal panel hidden.");
        }
    }
    
    void UpdateTraversalDisplay()
    {
        if (treeVisualizer == null || traversalText == null) return;
        
        string traversalInfo = treeVisualizer.GetTraversalInfo();
        traversalText.text = traversalInfo;
    }
    
    void OnClearClicked()
    {
        if (treeVisualizer == null) return;
        
        treeVisualizer.Clear();
        buttonsVisible = false;
        traversalVisible = false;
        
        // Hide buttons, explanation, info, and traversal panel
        HideButtons();
        
        if (explanationPanel != null)
            explanationPanel.SetActive(false);
        
        if (infoText != null)
            infoText.gameObject.SetActive(false);
        
        if (traversalPanel != null)
            traversalPanel.SetActive(false);
        
        // Show header and instruction card again
        if (headerPanel != null)
            headerPanel.SetActive(true);
        
        if (instructionCard != null)
            instructionCard.SetActive(true);
    }
    
    void UpdateInfoText()
    {
        if (infoText == null) return;
        
        int size = GetTreeSize();
        int height = GetTreeHeight();
        infoText.text = $"Nodes: {size} | Height: {height}";
    }
    
    void UpdateExplanation(string message)
    {
        if (explanationText != null)
        {
            explanationText.text = message;
        }
    }
    
    int GetTreeSize()
    {
        if (treeVisualizer == null) return 0;
        return treeVisualizer.GetNodeCount();
    }
    
    int GetTreeHeight()
    {
        if (treeVisualizer == null) return 0;
        return treeVisualizer.GetHeight();
    }
}