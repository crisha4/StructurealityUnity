using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class StackUI : MonoBehaviour
{
    [Header("References")]
    public StackVisualizer stackVisualizer;
    
    [Header("Buttons")]
    public Button pushButton;
    public Button popButton;
    public Button clearButton;
    
    [Header("UI Panels")]
    public GameObject headerPanel; // The blue "← Stacks" header
    public GameObject instructionCard; // The white "Scan a Flat Surface" card
    public GameObject dottedScanBox; // The animated dotted box (optional)
    public GameObject buttonPanel; // Parent panel containing all buttons
    public GameObject explanationPanel; // The explanation text panel at top
    
    [Header("Info Display")]
    public TextMeshProUGUI infoText;
    public TextMeshProUGUI explanationText;
    
    [Header("Auto-Populate Settings")]
    public bool autoPopulateOnPlacement = false; // Set to false to not auto-add items
    public int initialItemCount = 0; // Start with 0 items
    
    private bool buttonsVisible = false;
    private bool hasAutoPopulated = false;
    
    void Start()
    {
        // Connect button click events
        if (pushButton != null)
            pushButton.onClick.AddListener(OnPushClicked);
        
        if (popButton != null)
            popButton.onClick.AddListener(OnPopClicked);
        
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
        // Check if stack has been placed (similar to QueueManager.IsQueuePlaced())
        if (stackVisualizer != null && stackVisualizer.IsStackPlaced() && !buttonsVisible)
        {
            buttonsVisible = true;
            
            // Auto-populate if enabled
            if (autoPopulateOnPlacement && !hasAutoPopulated && initialItemCount > 0)
            {
                hasAutoPopulated = true;
                StartCoroutine(AutoPopulateStack());
            }
            else
            {
                ShowButtons();
                UpdateExplanation("✅ Stack placed! Tap Push to add items.");
            }
        }
    }
    
    System.Collections.IEnumerator AutoPopulateStack()
    {
        yield return new WaitForSeconds(0.3f);
        
        for (int i = 0; i < initialItemCount && i < stackVisualizer.maxStackSize; i++)
        {
            stackVisualizer.Push();
            yield return new WaitForSeconds(0.3f);
        }
        
        ShowButtons();
        UpdateInfoText();
        UpdateExplanation($"✅ Stack initialized!");
    }
    
    void HideButtons()
    {
        if (buttonPanel != null)
        {
            buttonPanel.SetActive(false);
        }
        else
        {
            if (pushButton != null) pushButton.gameObject.SetActive(false);
            if (popButton != null) popButton.gameObject.SetActive(false);
            if (clearButton != null) clearButton.gameObject.SetActive(false);
        }
        
        buttonsVisible = false;
    }
    
    void ShowButtons()
    {
        // Hide header and instruction card when stack is placed
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
            if (pushButton != null) pushButton.gameObject.SetActive(true);
            if (popButton != null) popButton.gameObject.SetActive(true);
            if (clearButton != null) clearButton.gameObject.SetActive(true);
        }
        
        buttonsVisible = true;
    }
    
    void OnPushClicked()
    {
        if (stackVisualizer == null) return;
        
        int sizeBefore = GetStackSize();
        stackVisualizer.Push();
        
        // Wait a frame to get the updated size
        StartCoroutine(UpdateAfterPush(sizeBefore));
    }
    
    System.Collections.IEnumerator UpdateAfterPush(int sizeBefore)
    {
        yield return new WaitForEndOfFrame();
        
        int sizeAfter = GetStackSize();
        UpdateInfoText();
        
        if (sizeAfter > sizeBefore)
            UpdateExplanation($"✅ Pushed item #{sizeAfter} onto the TOP of the stack");
        else
            UpdateExplanation("❌ Stack is full!");
    }
    
    void OnPopClicked()
    {
        if (stackVisualizer == null) return;
        
        int sizeBefore = GetStackSize();
        
        if (sizeBefore == 0)
        {
            UpdateExplanation("❌ Stack is empty!");
            return;
        }
        
        stackVisualizer.Pop();
        
        // Wait a frame to get the updated size
        StartCoroutine(UpdateAfterPop(sizeBefore));
    }
    
    System.Collections.IEnumerator UpdateAfterPop(int sizeBefore)
    {
        yield return new WaitForEndOfFrame();
        
        int sizeAfter = GetStackSize();
        UpdateInfoText();
        
        if (sizeAfter < sizeBefore)
            UpdateExplanation($"✅ Popped item from the TOP of the stack");
        else
            UpdateExplanation("❌ Stack is empty!");
    }
    
    void OnClearClicked()
    {
        if (stackVisualizer == null) return;
        
        stackVisualizer.Clear();
        hasAutoPopulated = false;
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
    
    void UpdateInfoText(string message = "")
    {
        if (infoText == null) return;
        
        if (string.IsNullOrEmpty(message))
        {
            int size = GetStackSize();
            infoText.text = $"Stack Size: {size}";
        }
        else
        {
            infoText.text = message;
        }
    }
    
    void UpdateExplanation(string message)
    {
        if (explanationText != null)
        {
            explanationText.text = message;
        }
    }
    
    int GetStackSize()
    {
        if (stackVisualizer == null) return 0;
        return stackVisualizer.Size();
    }
}