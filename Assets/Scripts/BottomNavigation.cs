using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class BottomNavigation : MonoBehaviour
{
    [System.Serializable]
    public class NavButton
    {
        public Button button;
        public Image iconImage;
        public TextMeshProUGUI label;
        public Sprite inactiveIcon;
        public Sprite activeIcon;
        public GameObject panel;
        
        [HideInInspector] public RectTransform iconTransform;
    }
    
    [Header("Navigation Buttons")]
    public NavButton homeButton;
    public NavButton learnButton;
    public NavButton progressButton;
    public NavButton profileButton;
    
    [Header("Label Colors")]
    public Color activeColor = new Color(0.3f, 0.6f, 1f);
    public Color inactiveColor = new Color(0.6f, 0.6f, 0.6f);
    
    [Header("Animation Settings")]
    public float scaleAmount = 1.15f;
    public float animationDuration = 0.2f;
    
    private NavButton currentActiveButton;
    
    void Start()
    {
        // Cache icon transforms
        CacheIconTransform(homeButton);
        CacheIconTransform(learnButton);
        CacheIconTransform(progressButton);
        CacheIconTransform(profileButton);
        
        // Set up button listeners
        if (homeButton.button != null) 
            homeButton.button.onClick.AddListener(() => SwitchToTab(homeButton));
        
        if (learnButton.button != null) 
            learnButton.button.onClick.AddListener(() => SwitchToTab(learnButton));
        
        if (progressButton.button != null) 
            progressButton.button.onClick.AddListener(() => SwitchToTab(progressButton));
        
        if (profileButton.button != null) 
            profileButton.button.onClick.AddListener(() => SwitchToTab(profileButton));
        
        // Start with Home active
        SwitchToTab(homeButton);
    }
    
    void CacheIconTransform(NavButton navButton)
    {
        if (navButton != null && navButton.iconImage != null)
        {
            navButton.iconTransform = navButton.iconImage.GetComponent<RectTransform>();
        }
    }
    
    void SwitchToTab(NavButton navButton)
    {
        if (navButton == null || navButton.button == null) return;
        
        // Don't do anything if already active
        if (currentActiveButton == navButton) return;
        
        // NEW: Close any open detail panels before switching
        CloseAnyOpenPanels();
        
        // Deactivate all buttons first
        DeactivateButton(homeButton);
        DeactivateButton(learnButton);
        DeactivateButton(progressButton);
        DeactivateButton(profileButton);
        
        // Activate selected button with animation
        ActivateButton(navButton);
        currentActiveButton = navButton;
        
        // Hide all panels
        HideAllPanels();
        
        // Show selected panel
        if (navButton.panel != null)
            navButton.panel.SetActive(true);
    }
    
    // NEW: Close any open detail panels or visualizers
    void CloseAnyOpenPanels()
    {
        // Method 1: Try to find TopicDetailPanel component and use its topicDetailPanel
        TopicDetailPanel detailPanel = FindObjectOfType<TopicDetailPanel>();
        if (detailPanel != null)
        {
            // Close the detail panel
            if (detailPanel.topicDetailPanel != null && detailPanel.topicDetailPanel.activeSelf)
            {
                detailPanel.topicDetailPanel.SetActive(false);
                Debug.Log("🧹 Closed topicDetailPanel");
            }
            
            // Also make sure to show the topics grid
            if (detailPanel.topicsGridPanel != null)
            {
                detailPanel.topicsGridPanel.SetActive(true);
                Debug.Log("✅ Restored topicsGridPanel");
            }
            
            // Hide lesson content if showing
            if (detailPanel.lessonContentPanel != null && detailPanel.lessonContentPanel.activeSelf)
            {
                detailPanel.lessonContentPanel.SetActive(false);
                Debug.Log("🧹 Closed lessonContentPanel");
            }
        }
        
        // Close visualizer
        Interactive2DVisualizer visualizer = FindObjectOfType<Interactive2DVisualizer>();
        if (visualizer != null)
        {
            visualizer.HideVisualization();
            Debug.Log("🧹 Closed Visualizer");
        }
        
        Debug.Log("🧹 Finished closing any open panels");
    }
    
    void ActivateButton(NavButton navButton)
    {
        if (navButton == null) return;
        
        // Change icon to active version
        if (navButton.iconImage != null && navButton.activeIcon != null)
        {
            navButton.iconImage.sprite = navButton.activeIcon;
            navButton.iconImage.color = Color.white;
        }
        
        // Change label color
        if (navButton.label != null)
            navButton.label.color = activeColor;
        
        // Animate scale up
        if (navButton.iconTransform != null)
        {
            StopAllCoroutines();
            StartCoroutine(AnimateIconScale(navButton.iconTransform, scaleAmount));
        }
    }
    
    void DeactivateButton(NavButton navButton)
    {
        if (navButton == null) return;
        
        // Change icon to inactive version
        if (navButton.iconImage != null && navButton.inactiveIcon != null)
        {
            navButton.iconImage.sprite = navButton.inactiveIcon;
            navButton.iconImage.color = Color.white;
        }
        
        // Change label color
        if (navButton.label != null)
            navButton.label.color = inactiveColor;
        
        // Animate scale down
        if (navButton.iconTransform != null)
        {
            StartCoroutine(AnimateIconScale(navButton.iconTransform, 1f));
        }
    }
    
    IEnumerator AnimateIconScale(RectTransform iconTransform, float targetScale)
    {
        Vector3 startScale = iconTransform.localScale;
        Vector3 endScale = Vector3.one * targetScale;
        
        float elapsed = 0f;
        
        while (elapsed < animationDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / animationDuration;
            // Ease out
            t = 1f - Mathf.Pow(1f - t, 3f);
            
            iconTransform.localScale = Vector3.Lerp(startScale, endScale, t);
            yield return null;
        }
        
        iconTransform.localScale = endScale;
    }
    
    void HideAllPanels()
    {
        if (homeButton.panel != null) homeButton.panel.SetActive(false);
        if (learnButton.panel != null) learnButton.panel.SetActive(false);
        if (progressButton.panel != null) progressButton.panel.SetActive(false);
        if (profileButton.panel != null) profileButton.panel.SetActive(false);
    }
}