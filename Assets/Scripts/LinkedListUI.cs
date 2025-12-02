using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LinkedListUI : MonoBehaviour
{
    [Header("References")]
    public LinkedListVisualizer linkedListVisualizer;
    
    [Header("Insert Buttons")]
    public Button insertHeadButton;
    public Button insertTailButton;
    public Button insertMiddleButton;
    
    [Header("Delete Buttons")]
    public Button deleteHeadButton;
    public Button deleteTailButton;
    public Button deleteMiddleButton;
    
    [Header("Other Buttons")]
    public Button clearButton;
    
    [Header("UI Panels")]
    public GameObject headerPanel;
    public GameObject instructionCard;
    public GameObject buttonPanel;
    public GameObject explanationPanel;
    
    [Header("Info Display")]
    public TextMeshProUGUI infoText;
    public TextMeshProUGUI explanationText;
    
    [Header("Position Input (Optional)")]
    public TMP_InputField positionInputField;
    
    private bool buttonsVisible = false;
    
    void Start()
    {
        // Connect button click events
        if (insertHeadButton != null)
            insertHeadButton.onClick.AddListener(OnInsertHeadClicked);
        
        if (insertTailButton != null)
            insertTailButton.onClick.AddListener(OnInsertTailClicked);
        
        if (insertMiddleButton != null)
            insertMiddleButton.onClick.AddListener(OnInsertMiddleClicked);
        
        if (deleteHeadButton != null)
            deleteHeadButton.onClick.AddListener(OnDeleteHeadClicked);
        
        if (deleteTailButton != null)
            deleteTailButton.onClick.AddListener(OnDeleteTailClicked);
        
        if (deleteMiddleButton != null)
            deleteMiddleButton.onClick.AddListener(OnDeleteMiddleClicked);
        
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
        // Check if linked list has been placed
        if (linkedListVisualizer != null && linkedListVisualizer.IsListPlaced() && !buttonsVisible)
        {
            ShowButtons();
            UpdateExplanation("✅ Linked List placed! Choose an operation:");
        }
        
        // Update info text continuously
        if (buttonsVisible)
        {
            UpdateInfoText();
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
            if (insertHeadButton != null) insertHeadButton.gameObject.SetActive(false);
            if (insertTailButton != null) insertTailButton.gameObject.SetActive(false);
            if (insertMiddleButton != null) insertMiddleButton.gameObject.SetActive(false);
            if (deleteHeadButton != null) deleteHeadButton.gameObject.SetActive(false);
            if (deleteTailButton != null) deleteTailButton.gameObject.SetActive(false);
            if (deleteMiddleButton != null) deleteMiddleButton.gameObject.SetActive(false);
            if (clearButton != null) clearButton.gameObject.SetActive(false);
        }
        
        buttonsVisible = false;
    }
    
    void ShowButtons()
    {
        // Hide header and instruction card when list is placed
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
            if (insertHeadButton != null) insertHeadButton.gameObject.SetActive(true);
            if (insertTailButton != null) insertTailButton.gameObject.SetActive(true);
            if (insertMiddleButton != null) insertMiddleButton.gameObject.SetActive(true);
            if (deleteHeadButton != null) deleteHeadButton.gameObject.SetActive(true);
            if (deleteTailButton != null) deleteTailButton.gameObject.SetActive(true);
            if (deleteMiddleButton != null) deleteMiddleButton.gameObject.SetActive(true);
            if (clearButton != null) clearButton.gameObject.SetActive(true);
        }
        
        buttonsVisible = true;
    }
    
    void OnInsertHeadClicked()
    {
        if (linkedListVisualizer == null) return;
        
        int sizeBefore = GetListSize();
        linkedListVisualizer.InsertAtHead();
        
        StartCoroutine(UpdateAfterInsert(sizeBefore, "HEAD (beginning)"));
    }
    
    void OnInsertTailClicked()
    {
        if (linkedListVisualizer == null) return;
        
        int sizeBefore = GetListSize();
        linkedListVisualizer.InsertAtTail();
        
        StartCoroutine(UpdateAfterInsert(sizeBefore, "TAIL (end)"));
    }
    
    void OnInsertMiddleClicked()
    {
        if (linkedListVisualizer == null) return;
        
        int position = GetListSize() / 2; // Insert at middle
        
        // If input field exists, use that position
        if (positionInputField != null && !string.IsNullOrEmpty(positionInputField.text))
        {
            if (int.TryParse(positionInputField.text, out int inputPos))
            {
                position = inputPos;
            }
        }
        
        int sizeBefore = GetListSize();
        linkedListVisualizer.InsertAtPosition(position);
        
        StartCoroutine(UpdateAfterInsert(sizeBefore, $"position {position}"));
    }
    
    System.Collections.IEnumerator UpdateAfterInsert(int sizeBefore, string location)
    {
        yield return new WaitForSeconds(0.1f);
        
        int sizeAfter = GetListSize();
        UpdateInfoText();
        
        if (sizeAfter > sizeBefore)
            UpdateExplanation($"✅ Inserted node at {location}\n💡 All nodes shifted to make space!");
        else
            UpdateExplanation("❌ List is full!");
    }
    
    void OnDeleteHeadClicked()
    {
        if (linkedListVisualizer == null) return;
        
        int sizeBefore = GetListSize();
        
        if (sizeBefore == 0)
        {
            UpdateExplanation("❌ List is empty! Nothing to delete.");
            return;
        }
        
        string nodeValue = linkedListVisualizer.GetNodeValue(0);
        linkedListVisualizer.DeleteFromHead();
        
        StartCoroutine(UpdateAfterDelete(sizeBefore, $"HEAD (Node {nodeValue})"));
    }
    
    void OnDeleteTailClicked()
    {
        if (linkedListVisualizer == null) return;
        
        int sizeBefore = GetListSize();
        
        if (sizeBefore == 0)
        {
            UpdateExplanation("❌ List is empty! Nothing to delete.");
            return;
        }
        
        string nodeValue = linkedListVisualizer.GetNodeValue(sizeBefore - 1);
        linkedListVisualizer.DeleteFromTail();
        
        StartCoroutine(UpdateAfterDelete(sizeBefore, $"TAIL (Node {nodeValue})"));
    }
    
    void OnDeleteMiddleClicked()
    {
        if (linkedListVisualizer == null) return;
        
        int sizeBefore = GetListSize();
        
        if (sizeBefore == 0)
        {
            UpdateExplanation("❌ List is empty! Nothing to delete.");
            return;
        }
        
        int position = sizeBefore / 2; // Delete from middle
        
        // If input field exists, use that position
        if (positionInputField != null && !string.IsNullOrEmpty(positionInputField.text))
        {
            if (int.TryParse(positionInputField.text, out int inputPos))
            {
                position = inputPos;
            }
        }
        
        string nodeValue = linkedListVisualizer.GetNodeValue(position);
        linkedListVisualizer.DeleteAtPosition(position);
        
        StartCoroutine(UpdateAfterDelete(sizeBefore, $"position {position} (Node {nodeValue})"));
    }
    
    System.Collections.IEnumerator UpdateAfterDelete(int sizeBefore, string location)
    {
        yield return new WaitForSeconds(0.1f);
        
        int sizeAfter = GetListSize();
        UpdateInfoText();
        
        if (sizeAfter < sizeBefore)
            UpdateExplanation($"✅ Deleted node from {location}\n💡 Remaining nodes shifted left!");
        else
            UpdateExplanation("❌ Could not delete node!");
    }
    
    void OnClearClicked()
    {
        if (linkedListVisualizer == null) return;
        
        linkedListVisualizer.Clear();
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
        if (infoText == null) return;
        
        int size = GetListSize();
        infoText.text = $"List Size: {size} nodes";
    }
    
    void UpdateExplanation(string message)
    {
        if (explanationText != null)
        {
            explanationText.text = message;
        }
    }
    
    int GetListSize()
    {
        if (linkedListVisualizer == null) return 0;
        return linkedListVisualizer.Size();
    }
}