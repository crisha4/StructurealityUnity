using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class QueueUI : MonoBehaviour
{
    [Header("References")]
    public QueueManager queueManager;
    
    [Header("Buttons")]
    public Button enqueueButton;
    public Button dequeueButton;
    public Button peekButton;
    public Button clearButton;
    
    [Header("UI Panels")]
    public GameObject headerPanel; // The blue "← Queues" header
    public GameObject instructionCard; // The white "Scan a Flat Surface" card
    public GameObject dottedScanBox; // The animated dotted box (optional)
    public GameObject buttonPanel; // Parent panel containing all buttons
    public GameObject explanationPanel; // The explanation text panel at top
    
    [Header("Info Display")]
    public TextMeshProUGUI infoText;
    public TextMeshProUGUI explanationText;
    
    [Header("Auto-Populate Settings")]
    public bool autoPopulateOnPlacement = false; // Set to false to not auto-add nodes
    public int initialNodeCount = 0; // Start with 0 nodes
    
    private int valueCounter = 0;
    private string[] testValues = { "A", "B", "C", "D", "E", "F", "G", "H", "I", "J" };
    private bool buttonsVisible = false;
    private bool hasAutoPopulated = false;
    
    void Start()
    {
        // Connect button click events
        if (enqueueButton != null)
            enqueueButton.onClick.AddListener(OnEnqueueClicked);
        
        if (dequeueButton != null)
            dequeueButton.onClick.AddListener(OnDequeueClicked);
        
        if (peekButton != null)
            peekButton.onClick.AddListener(OnPeekClicked);
        
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
        // Check if queue has been placed and show buttons
        if (queueManager != null && queueManager.IsQueuePlaced() && !buttonsVisible)
        {
            buttonsVisible = true;
            
            // Auto-populate if enabled
            if (autoPopulateOnPlacement && !hasAutoPopulated && initialNodeCount > 0)
            {
                hasAutoPopulated = true;
                StartCoroutine(AutoPopulateQueue());
            }
            else
            {
                ShowButtons();
                UpdateExplanation("✅ Queue placed! Tap Enqueue to add nodes.");
            }
        }
    }
    
    System.Collections.IEnumerator AutoPopulateQueue()
    {
        yield return new WaitForSeconds(0.3f);
        
        for (int i = 0; i < initialNodeCount && i < queueManager.maxQueueSize; i++)
        {
            string value = testValues[valueCounter % testValues.Length];
            valueCounter++;
            queueManager.Enqueue(value);
            yield return new WaitForSeconds(0.2f);
        }
        
        ShowButtons();
        UpdateInfoText();
        UpdateExplanation($"✅ Queue initialized!");
    }
    
    void HideButtons()
    {
        if (buttonPanel != null)
        {
            buttonPanel.SetActive(false);
        }
        else
        {
            if (enqueueButton != null) enqueueButton.gameObject.SetActive(false);
            if (dequeueButton != null) dequeueButton.gameObject.SetActive(false);
            if (peekButton != null) peekButton.gameObject.SetActive(false);
            if (clearButton != null) clearButton.gameObject.SetActive(false);
        }
        
        buttonsVisible = false;
    }
    
    void ShowButtons()
    {
        // Hide header and instruction card when queue is placed
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
            if (enqueueButton != null) enqueueButton.gameObject.SetActive(true);
            if (dequeueButton != null) dequeueButton.gameObject.SetActive(true);
            if (peekButton != null) peekButton.gameObject.SetActive(true);
            if (clearButton != null) clearButton.gameObject.SetActive(true);
        }
        
        buttonsVisible = true;
    }
    
    void OnEnqueueClicked()
    {
        if (queueManager == null) return;
        
        string value = testValues[valueCounter % testValues.Length];
        valueCounter++;
        
        queueManager.Enqueue(value);
        UpdateInfoText();
        UpdateExplanation($"✅ Added '{value}' to the BACK of the queue");
    }
    
    void OnDequeueClicked()
    {
        if (queueManager == null) return;
        
        string value = queueManager.Peek();
        queueManager.Dequeue();
        UpdateInfoText();
        
        if (value != "Empty")
            UpdateExplanation($"✅ Dequeued '{value}' from the FRONT");
        else
            UpdateExplanation("❌ Queue is empty!");
    }
    
    void OnPeekClicked()
    {
        if (queueManager == null) return;
        
        string frontValue = queueManager.Peek();
        UpdateInfoText();
        UpdateExplanation($"👁️ Front element: '{frontValue}'");
    }
    
    void OnClearClicked()
    {
        if (queueManager == null) return;
        
        queueManager.ResetQueue();
        valueCounter = 0;
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
            int size = queueManager != null ? queueManager.Size() : 0;
            infoText.text = $"Queue Size: {size}";
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
}