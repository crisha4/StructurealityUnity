using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Collections;
using UnityEngine.Networking;

/// <summary>
/// DATABASE-ONLY Progress Panel - Fixed streak dot colors
/// </summary>
public class ProgressPanelController : MonoBehaviour
{
    [Header("Header Stats")]
    public TextMeshProUGUI lessonsCountText;
    public TextMeshProUGUI challengesCountText;
    public TextMeshProUGUI pointsCountText;
    
    [Header("Achievement Cards")]
    public List<TopicAchievementCard> achievementCards;
    
    [Header("Learning Streak")]
    public TextMeshProUGUI streakDaysText;
    public TextMeshProUGUI streakMessageText;
    public Transform streakDotsContainer;
    public GameObject streakDotPrefab;
    
    [Header("Colors")]
    public Color completedColor = new Color(1f, 0.95f, 0.6f);
    public Color inProgressColor = new Color(1f, 1f, 0.9f);
    public Color lockedColor = Color.white;
    public Color streakActiveColor = new Color(0.4f, 0.8f, 0.4f); // Green
    public Color streakInactiveColor = new Color(0.8f, 0.8f, 0.8f); // Gray
    
    [Header("API Settings")]
    public string adminApiUrl = "https://structureality-admin.onrender.com/api";
    
    [Header("Loading Indicator")]
    public GameObject loadingIndicator;
    
    private UserProgressManager progressManager;
    private DatabaseProgressData cachedProgressData;
    private bool isDataLoaded = false;
    
    [System.Serializable]
    public class TopicAchievementCard
    {
        public string topicName;
        public string displayTitle;
        public Image backgroundImage;
        public TextMeshProUGUI titleText;
        public GameObject completedBadge;
        public GameObject lockIcon;
        public Image progressBar;
    }
    
    void Start()
    {
        progressManager = UserProgressManager.Instance;
        
        if (progressManager == null)
        {
            Debug.LogError("UserProgressManager not found!");
            return;
        }
        
        InitializeCards();
        StartCoroutine(FetchAndDisplayProgress());
    }
    
    void OnEnable()
    {
        if (!isDataLoaded)
        {
            StartCoroutine(FetchAndDisplayProgress());
        }
        else
        {
            UpdateProgressDisplay();
        }
    }
    
    void ShowLoadingIndicator(bool show)
    {
        if (loadingIndicator != null)
            loadingIndicator.SetActive(show);
    }
    
    IEnumerator FetchAndDisplayProgress()
    {
        string username = progressManager.GetCurrentUsername();
        
        if (string.IsNullOrEmpty(username))
        {
            Debug.LogError("❌ No username!");
            yield break;
        }
        
        ShowLoadingIndicator(true);
        
        string url = $"{adminApiUrl}/progress/{username}";
        Debug.Log($"🔄 Fetching progress from: {url}");
        
        UnityWebRequest request = UnityWebRequest.Get(url);
        request.SetRequestHeader("Content-Type", "application/json");
        
        yield return request.SendWebRequest();
        
        ShowLoadingIndicator(false);
        
        if (request.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("✅ Progress fetched");
            
            try
            {
                DatabaseProgressResponse response = JsonUtility.FromJson<DatabaseProgressResponse>(request.downloadHandler.text);
                
                if (response != null && response.success && response.data != null)
                {
                    cachedProgressData = response.data;
                    isDataLoaded = true;
                    
                    Debug.Log($"✅ Loaded data for: {cachedProgressData.username}");
                    Debug.Log($"📊 Topics: {cachedProgressData.topics.Count}");
                    
                    UpdateProgressDisplay();
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"❌ Parse error: {e.Message}");
            }
        }
        else
        {
            Debug.LogError($"❌ Fetch failed: {request.error}");
        }
    }
    
    void InitializeCards()
    {
        foreach (var card in achievementCards)
        {
            if (card.completedBadge != null)
                card.completedBadge.SetActive(false);
            
            if (card.lockIcon != null)
                card.lockIcon.SetActive(false);
        }
        
        Debug.Log("✓ Achievement cards initialized");
    }
    
    public void UpdateProgressDisplay()
    {
        if (!isDataLoaded || cachedProgressData == null)
        {
            Debug.LogWarning("⚠️ No data to display");
            return;
        }
        
        UpdateHeaderStats();
        UpdateAchievementCards();
        UpdateStreakDisplay();
    }
    
    void UpdateHeaderStats()
    {
        if (cachedProgressData == null) return;
        
        int totalLessons = 0;
        int challengesCompleted = 0;
        int points = 0;
        
        foreach (var topic in cachedProgressData.topics)
        {
            totalLessons += topic.lessonsCompleted;
            
            if (topic.puzzleCompleted)
            {
                challengesCompleted++;
                points += topic.puzzleScore;
            }
        }
        
        points += totalLessons * 50;
        points += challengesCompleted * 100;
        
        if (lessonsCountText != null)
        {
            lessonsCountText.text = totalLessons.ToString();
            Debug.Log($"✅ Lessons Count: {totalLessons}");
        }
        else
        {
            Debug.LogError("❌ lessonsCountText is NULL!");
        }
        
        if (challengesCountText != null)
            challengesCountText.text = challengesCompleted.ToString();
        
        if (pointsCountText != null)
            pointsCountText.text = points.ToString();
    }
    
void UpdateAchievementCards()
    {
        if (cachedProgressData == null || achievementCards == null) return;
        
        Debug.Log("=== Updating Achievement Cards ===");
        
        foreach (var card in achievementCards)
        {
            if (card.backgroundImage == null)
            {
                Debug.LogWarning("Card background image is null!");
                continue;
            }
            
            string normalizedTopic = TopicNameConstants.Normalize(card.topicName);
            
            var topicData = cachedProgressData.topics.Find(t => 
                TopicNameConstants.Normalize(t.topicName) == normalizedTopic);
            
            float progress = 0f;
            bool isCompleted = false;
            bool tutorialComplete = false;
            bool puzzleComplete = false;
            
            if (topicData != null)
            {
                progress = topicData.progressPercentage;
                tutorialComplete = topicData.tutorialCompleted;
                puzzleComplete = topicData.puzzleCompleted;
                
                // Topic is 100% complete when BOTH tutorial and puzzle are done
                isCompleted = tutorialComplete && puzzleComplete && progress >= 100f;
            }
            
            Debug.Log($"Card: {card.displayTitle} | Progress: {progress}% | Tutorial: {tutorialComplete} | Puzzle: {puzzleComplete} | Completed: {isCompleted}");
            
            if (card.titleText != null)
                card.titleText.text = card.displayTitle;
            
            // Update background color based on completion status
            if (isCompleted)
                card.backgroundImage.color = completedColor;
            else if (progress > 0)
                card.backgroundImage.color = inProgressColor;
            else
                card.backgroundImage.color = lockedColor;
            
            // Show completed badge ONLY when topic is 100% complete
            if (card.completedBadge != null)
            {
                card.completedBadge.SetActive(isCompleted);
                Debug.Log($"  → Badge: {(isCompleted ? "✓ SHOWN" : "✗ HIDDEN")}");
            }
            
            // Show lock icon when topic is NOT 100% complete
            if (card.lockIcon != null)
            {
                bool shouldShowLock = !isCompleted;
                card.lockIcon.SetActive(shouldShowLock);
                Debug.Log($"  → Lock: {(shouldShowLock ? "🔒 SHOWN" : "✗ HIDDEN")}");
            }
            
            if (card.progressBar != null)
                card.progressBar.fillAmount = progress / 100f;
        }
        
        Debug.Log("=== Achievement Cards Update Complete ===");
    }
    
    void UpdateStreakDisplay()
    {
        if (cachedProgressData == null)
        {
            Debug.LogWarning("⚠️ No cached data");
            return;
        }
        
        if (streakDotsContainer == null)
        {
            Debug.LogError("❌ StreakDotsContainer is NULL!");
            return;
        }
        
        if (streakDotPrefab == null)
        {
            Debug.LogError("❌ StreakDotPrefab is NULL!");
            return;
        }
        
        int streak = cachedProgressData.streak;
        Debug.Log($"=== Updating Streak Display: {streak} days ===");
        
        if (streakDaysText != null)
        {
            streakDaysText.text = $"{streak} Days";
            Debug.Log($"✓ Streak text: {streak} Days");
        }
        
        if (streakMessageText != null)
        {
            streakMessageText.text = streak > 0 ? "Keep it up!" : "Start your streak!";
        }
        
        // Clear existing dots
        Debug.Log($"Clearing {streakDotsContainer.childCount} existing dots...");
        
        List<GameObject> dotsToDestroy = new List<GameObject>();
        foreach (Transform child in streakDotsContainer)
        {
            dotsToDestroy.Add(child.gameObject);
        }
        
        foreach (GameObject dot in dotsToDestroy)
        {
            Destroy(dot);
        }
        
        StartCoroutine(CreateStreakDotsNextFrame(streak));
    }
    
    IEnumerator CreateStreakDotsNextFrame(int streak)
    {
        yield return null;
        
        Debug.Log("=== Creating Streak Dots ===");
        Debug.Log($"Active Color: R={streakActiveColor.r}, G={streakActiveColor.g}, B={streakActiveColor.b}");
        Debug.Log($"Inactive Color: R={streakInactiveColor.r}, G={streakInactiveColor.g}, B={streakInactiveColor.b}");
        
        string[] dayNames = { "M", "T", "W", "T", "F", "S", "S" };
        
        for (int i = 0; i < 7; i++)
        {
            GameObject dot = Instantiate(streakDotPrefab, streakDotsContainer);
            dot.name = $"StreakDot_{dayNames[i]}_{i}";
            
            bool isActive = i < streak;
            Color targetColor = isActive ? streakActiveColor : streakInactiveColor;
            
            Debug.Log($"Dot {i}: Active={isActive}, TargetColor=({targetColor.r},{targetColor.g},{targetColor.b})");
            
            // Find Image component
            Image dotImage = dot.GetComponent<Image>();
            
            if (dotImage == null)
            {
                dotImage = dot.GetComponentInChildren<Image>(true);
                Debug.Log($"  → Found Image in children: {dotImage != null}");
            }
            
            if (dotImage != null)
            {
                dotImage.color = targetColor;
                Debug.Log($"  ✅ Set color to: R={dotImage.color.r}, G={dotImage.color.g}, B={dotImage.color.b}, A={dotImage.color.a}");
                
                dotImage.enabled = true;
            }
            else
            {
                Debug.LogError($"  ❌ No Image component found on dot {i}!");
                
                Component[] components = dot.GetComponents<Component>();
                Debug.Log($"  Components on dot: {string.Join(", ", System.Array.ConvertAll(components, c => c.GetType().Name))}");
            }
            
            // Update text label
            TextMeshProUGUI label = dot.GetComponentInChildren<TextMeshProUGUI>(true);
            if (label != null)
            {
                label.text = dayNames[i];
                Debug.Log($"  ✓ Label set to: {dayNames[i]}");
            }
            
            // Ensure RectTransform is properly set for layout
            RectTransform dotRect = dot.GetComponent<RectTransform>();
            if (dotRect != null)
            {
                // Reset any prefab overrides that might mess with layout
                dotRect.anchorMin = new Vector2(0.5f, 0.5f);
                dotRect.anchorMax = new Vector2(0.5f, 0.5f);
                dotRect.pivot = new Vector2(0.5f, 0.5f);
            }
            
            dot.SetActive(true);
        }
        
        Debug.Log($"✅ Created 7 streak dots");
        
        // Force layout rebuild multiple times to ensure proper positioning
        yield return null;
        Canvas.ForceUpdateCanvases();
        
        if (streakDotsContainer is RectTransform containerRect)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(containerRect);
            yield return null;
            LayoutRebuilder.ForceRebuildLayoutImmediate(containerRect);
        }
        
        // Verify colors after layout update
        Debug.Log("=== VERIFYING Colors After Layout ===");
        int verifyIndex = 0;
        foreach (Transform child in streakDotsContainer)
        {
            Image img = child.GetComponent<Image>();
            if (img == null) img = child.GetComponentInChildren<Image>(true);
            
            if (img != null)
            {
                Debug.Log($"Dot {verifyIndex}: Color=({img.color.r},{img.color.g},{img.color.b},{img.color.a}), Enabled={img.enabled}");
            }
            verifyIndex++;
        }
        
        Debug.Log("=== Streak Display Complete ===");
    }
    
    public void RefreshProgress()
    {
        StartCoroutine(FetchAndDisplayProgress());
    }
    
    public static string GetAchievementTitle(string topicName, float progress)
    {
        string normalized = TopicNameConstants.Normalize(topicName);
        
        if (progress >= 100f)
        {
            switch (normalized)
            {
                case TopicNameConstants.QUEUE:
                    return "Queue Master";
                case TopicNameConstants.STACKS:
                    return "First Stack";
                case TopicNameConstants.LINKED_LISTS:
                    return "Link Expert";
                case TopicNameConstants.TREES:
                    return "Tree Walker";
                case TopicNameConstants.GRAPHS:
                    return "Graph Pro";
                default:
                    return normalized;
            }
        }
        else if (progress > 0)
        {
            return normalized + " (In Progress)";
        }
        else
        {
            return normalized;
        }
    }
}

public class AchievementCardButton : MonoBehaviour
{
    public string topicName;
    
    public void OnCardClicked()
    {
        Debug.Log($"Achievement card clicked: {topicName}");
        
        if (UserProgressManager.Instance != null)
        {
            bool isUnlocked = UserProgressManager.Instance.IsTopicUnlocked(topicName);
            
            if (isUnlocked)
            {
                PlayerPrefs.SetString("SelectedTopic", topicName);
            }
            else
            {
                Debug.Log("Topic is locked!");
            }
        }
    }
}