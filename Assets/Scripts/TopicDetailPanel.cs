using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Collections;
using UnityEngine.Networking;
using System.Text;

/// <summary>
/// DATABASE-ONLY VERSION WITH 2D INTERACTIVE VISUALIZATIONS
/// Previous/Next navigate through content pages, not lessons
/// Includes interactive visualizations for each topic
/// </summary>
public class TopicDetailPanel : MonoBehaviour
{
    [Header("Panel References")]
    public GameObject topicDetailPanel;
    public GameObject topicsGridPanel;
    public GameObject lessonsScrollView;
    public GameObject lessonContentPanel;
    
    [Header("Header Components")]
    public Button backButton;
    public TextMeshProUGUI titleText;
    
    [Header("Content Page Navigation")]
    public Transform lessonsContainer;
    public GameObject lessonModulePrefab;
    public Button previousPageButton;
    public Button nextPageButton;
    public TextMeshProUGUI pageCounterText;
    
    [Header("Topic Icons")]
    public Sprite queueIcon;
    public Sprite stacksIcon;
    public Sprite linkedListsIcon;
    public Sprite treesIcon;
    public Sprite graphsIcon;
    public Sprite defaultIcon;
    
    [Header("Lesson Content Components")]
    public TextMeshProUGUI lessonContentTitle;
    public TextMeshProUGUI lessonContentDescription;
    public TextMeshProUGUI lessonContentBody;
    public ScrollRect lessonContentScrollRect;
    public Button markCompleteButton;
    public TextMeshProUGUI markCompleteButtonText;
    
    [Header("Interactive Visualization")]
    public Interactive2DVisualizer visualizer;
    public Button toggleVisualizerButton;
    public TextMeshProUGUI toggleVisualizerButtonText;
    private bool isVisualizerActive = false;
    
    [Header("API Settings")]
    public string adminApiUrl = "https://structureality-admin.onrender.com/api";
    
    [Header("Loading Indicator")]
    public GameObject loadingIndicator;
    
    private string currentTopicName;
    private Dictionary<string, List<LessonModule>> cachedLessons = new Dictionary<string, List<LessonModule>>();
    private LessonModule currentLesson;
    private int currentLessonIndex = 0;
    private int currentPageIndex = 0;
    private List<LessonModule> currentTopicLessons;
    private Dictionary<string, int> completedLessonsCount = new Dictionary<string, int>();
    private string currentUsername;
    public bool isLoadingData = false;
    
    [System.Serializable]
    public class LessonModule
    {
        public string title;
        public string description;
        public Sprite icon;
        public bool isCompleted;
        public string lessonId;
        public string content;
        public List<string> contentPages;
    }
    
    void Awake()
    {
        Debug.Log("🔧 TopicDetailPanel Awake() - DATABASE MODE WITH VISUALIZER");
        currentUsername = PlayerPrefs.GetString("CurrentUser", "");
        
        if (string.IsNullOrEmpty(currentUsername))
        {
            Debug.LogError("❌ No user logged in!");
        }
    }
    
    void Start()
    {
        Debug.Log("=== TopicDetailPanel Start() - DATABASE MODE WITH VISUALIZER ===");
        
        if (backButton != null)
        {
            backButton.onClick.RemoveAllListeners();
            backButton.onClick.AddListener(OnBackButtonClicked);
        }
        
        if (markCompleteButton != null)
        {
            markCompleteButton.onClick.RemoveAllListeners();
            markCompleteButton.onClick.AddListener(OnMarkCompleteClicked);
        }
        
        if (previousPageButton != null)
        {
            previousPageButton.onClick.RemoveAllListeners();
            previousPageButton.onClick.AddListener(ShowPreviousPage);
        }
        
        if (nextPageButton != null)
        {
            nextPageButton.onClick.RemoveAllListeners();
            nextPageButton.onClick.AddListener(ShowNextPage);
        }
        
        // NEW: Toggle visualizer button
        if (toggleVisualizerButton != null)
        {
            toggleVisualizerButton.onClick.RemoveAllListeners();
            toggleVisualizerButton.onClick.AddListener(ToggleVisualizer);
        }
        
        if (topicDetailPanel != null)
            topicDetailPanel.SetActive(false);
        
        if (lessonContentPanel != null)
            lessonContentPanel.SetActive(false);
        
        if (loadingIndicator != null)
            loadingIndicator.SetActive(false);
        
        // Initialize visualizer as hidden
        if (visualizer != null && visualizer.visualizationPanel != null)
            visualizer.visualizationPanel.SetActive(false);
        
        Debug.Log("=== TopicDetailPanel Start() Complete ===");
    }
    
    // NEW: Toggle interactive visualizer
    void ToggleVisualizer()
    {
        if (visualizer == null) return;
        
        isVisualizerActive = !isVisualizerActive;
        
        if (isVisualizerActive)
        {
            visualizer.InitializeVisualization(currentTopicName);
            if (toggleVisualizerButtonText != null)
                toggleVisualizerButtonText.text = "Hide Visualizer";
            Debug.Log($"🎨 Showing visualizer for {currentTopicName}");
        }
        else
        {
            visualizer.HideVisualization();
            if (toggleVisualizerButtonText != null)
                toggleVisualizerButtonText.text = "Show Interactive";
            Debug.Log("📖 Hiding visualizer");
        }
    }
    
    void ShowLoadingIndicator(bool show)
    {
        if (loadingIndicator != null)
            loadingIndicator.SetActive(show);
    }
    
IEnumerator FetchUserProgressFromDatabase()
{
    if (string.IsNullOrEmpty(currentUsername) || string.IsNullOrEmpty(adminApiUrl))
    {
        Debug.LogWarning("⚠️ Cannot fetch: No username or API URL");
        yield break;
    }
    
    isLoadingData = true;
    ShowLoadingIndicator(true);
    
    string url = $"{adminApiUrl}/progress/{currentUsername}";
    Debug.Log($"🔄 Fetching from Render (may take 60s+ on cold start): {url}");
    
    // RETRY LOGIC for Render cold starts
    int maxRetries = 2;
    int attempt = 0;
    bool success = false;
    
    while (attempt < maxRetries && !success)
    {
        attempt++;
        if (attempt > 1)
        {
            Debug.Log($"🔄 Retry attempt {attempt}/{maxRetries}...");
        }
        
        UnityWebRequest request = UnityWebRequest.Get(url);
        request.SetRequestHeader("Content-Type", "application/json");
        request.timeout = 90; // 90 second timeout for cold starts
        
        yield return request.SendWebRequest();
        
        if (request.result == UnityWebRequest.Result.Success)
        {
            success = true;
            Debug.Log("✅ User progress fetched successfully");
            
            try
            {
                DatabaseProgressResponse response = JsonUtility.FromJson<DatabaseProgressResponse>(request.downloadHandler.text);
                
                if (response != null && response.success && response.data != null)
                {
                    completedLessonsCount.Clear();
                    
                    foreach (var topic in response.data.topics)
                    {
                        string normalizedTopic = TopicNameConstants.Normalize(topic.topicName);
                        completedLessonsCount[normalizedTopic] = topic.lessonsCompleted;
                        
                        Debug.Log($"📚 {normalizedTopic}: {topic.lessonsCompleted} lessons completed");
                    }
                    
                    SyncLessonCompletionFlags();
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"❌ Parse error: {e.Message}");
            }
        }
        else if (request.result == UnityWebRequest.Result.ConnectionError || 
                 request.result == UnityWebRequest.Result.DataProcessingError)
        {
            Debug.LogWarning($"⚠️ Connection error on attempt {attempt}: {request.error}");
            
            if (attempt < maxRetries)
            {
                Debug.Log("⏳ Waiting 5s before retry...");
                yield return new WaitForSeconds(5f);
            }
        }
        else
        {
            Debug.LogError($"❌ Failed to fetch progress: {request.error}");
            break;
        }
    }
    
    if (!success)
    {
        Debug.LogError("❌ All fetch attempts failed. Render backend may be down or cold starting.");
        Debug.LogError("💡 Solution: Wait 60s and try again, or keep your Render service awake");
    }
    
    ShowLoadingIndicator(false);
    isLoadingData = false;
}

    public bool IsLessonCompleted(string topicName, int lessonIndex)
    {
        string normalizedTopic = TopicNameConstants.Normalize(topicName);
        
        if (completedLessonsCount.ContainsKey(normalizedTopic))
        {
            return lessonIndex < completedLessonsCount[normalizedTopic];
        }
        
        return false;
    }
    
    public bool IsTopicFullyCompleted(string topicName)
    {
        List<LessonModule> lessons = GetLessonsForTopic(topicName);
        if (lessons.Count == 0) return false;
        
        string normalizedTopic = TopicNameConstants.Normalize(topicName);
        
        if (completedLessonsCount.ContainsKey(normalizedTopic))
        {
            bool isComplete = completedLessonsCount[normalizedTopic] >= lessons.Count;
            if (isComplete)
                Debug.Log($"✓ Topic '{topicName}' fully completed!");
            return isComplete;
        }
        
        return false;
    }
    
    
    IEnumerator SyncLessonCompletionToDatabase(string topicName, int lessonsCompleted)
    {
        if (string.IsNullOrEmpty(currentUsername) || string.IsNullOrEmpty(adminApiUrl))
        {
            Debug.LogWarning("Cannot sync: No username or API URL");
            yield break;
        }
        
        string normalizedTopic = TopicNameConstants.Normalize(topicName);
        
        Debug.Log($"🔄 Syncing to database: {normalizedTopic} = {lessonsCompleted} lessons");
        
        string jsonData = $@"{{
            ""username"": ""{currentUsername}"",
            ""topicName"": ""{normalizedTopic}"",
            ""lessonsCompleted"": {lessonsCompleted}
        }}";
        
        string url = $"{adminApiUrl}/lessons/complete";
        
        UnityWebRequest request = new UnityWebRequest(url, "POST");
        request.uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(jsonData));
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        
        yield return request.SendWebRequest();
        
        if (request.result == UnityWebRequest.Result.Success)
        {
            Debug.Log($"✅ Database updated: {normalizedTopic} = {lessonsCompleted} lessons");
        }
        else
        {
            Debug.LogError($"❌ Sync failed: {request.error}");
            Debug.LogError($"Response: {request.downloadHandler.text}");
        }
    }
    
    void UpdateMarkCompleteButton()
    {
        if (markCompleteButton == null || currentLesson == null) return;
        
        bool isCompleted = IsLessonCompleted(currentTopicName, currentLessonIndex);
        
        if (markCompleteButtonText != null)
        {
            markCompleteButtonText.text = isCompleted ? "✓ Completed" : "Mark as Complete";
        }
        
        markCompleteButton.interactable = !isCompleted;
    }
    
void NotifyTopicCompletion(string topicName)
{
    Debug.Log($"📣 NotifyTopicCompletion called for: {topicName}");
    
    string normalizedTopic = TopicNameConstants.Normalize(topicName);
    string currentUser = PlayerPrefs.GetString("CurrentUser", "");
    
    if (string.IsNullOrEmpty(currentUser))
    {
        Debug.LogError("❌ No current user!");
        return;
    }
    
    // Update LearnPanel to refresh puzzle button states
    UpdatedLearnPanelController learnPanel = FindObjectOfType<UpdatedLearnPanelController>();
    if (learnPanel != null)
    {
        learnPanel.UpdatePuzzleButtonsAccess();
        Debug.Log("✅ LearnPanel notified to refresh");
    }
    else
    {
        Debug.LogWarning("⚠️ LearnPanel not found in scene");
    }
    
    Debug.Log("=== TOPIC COMPLETION NOTIFICATION COMPLETE ===");
}

// ✅ ADD THIS NEW METHOD to force-refresh puzzle buttons
public void ForceRefreshPuzzleAccess()
{
    Debug.Log("🔄 Force refreshing puzzle access...");
    
    UpdatedLearnPanelController learnPanel = FindObjectOfType<UpdatedLearnPanelController>();
    if (learnPanel != null)
    {
        learnPanel.UpdatePuzzleButtonsAccess();
        Debug.Log("✅ Puzzle access refreshed");
    }
}
    
    void ShowPreviousPage()
    {
        if (currentLesson == null || currentLesson.contentPages == null || currentLesson.contentPages.Count == 0)
        {
            Debug.LogWarning("No content pages available");
            return;
        }
        
        if (currentPageIndex > 0)
        {
            currentPageIndex--;
            UpdateContentDisplay();
            Debug.Log($"← Previous page: {currentPageIndex + 1}/{currentLesson.contentPages.Count}");
        }
    }
    
    void ShowNextPage()
    {
        if (currentLesson == null || currentLesson.contentPages == null || currentLesson.contentPages.Count == 0)
        {
            Debug.LogWarning("No content pages available");
            return;
        }
        
        if (currentPageIndex < currentLesson.contentPages.Count - 1)
        {
            currentPageIndex++;
            UpdateContentDisplay();
            Debug.Log($"→ Next page: {currentPageIndex + 1}/{currentLesson.contentPages.Count}");
        }
    }
    
    void UpdateContentDisplay()
    {
        if (currentLesson == null) return;
        
        if (lessonContentBody != null && currentLesson.contentPages != null && currentLesson.contentPages.Count > 0)
        {
            lessonContentBody.text = currentLesson.contentPages[currentPageIndex];
        }
        
        if (pageCounterText != null && currentLesson.contentPages != null)
        {
            pageCounterText.text = $"Page {currentPageIndex + 1} of {currentLesson.contentPages.Count}";
        }
        
        if (previousPageButton != null)
            previousPageButton.interactable = (currentPageIndex > 0);
        
        if (nextPageButton != null && currentLesson.contentPages != null)
            nextPageButton.interactable = (currentPageIndex < currentLesson.contentPages.Count - 1);
        
        StartCoroutine(ResetScrollPosition());
    }
    
void ShowLessonAtIndex(int index)
{
    if (currentTopicLessons == null || index < 0 || index >= currentTopicLessons.Count)
    {
        Debug.LogWarning($"Invalid lesson index: {index}");
        return;
    }
    
    // Clear and hide visualizer when switching lessons
    if (visualizer != null)
    {
        visualizer.ClearVisualization();
        visualizer.HideVisualization();
    }
    isVisualizerActive = false;
    if (toggleVisualizerButtonText != null)
        toggleVisualizerButtonText.text = "Show Interactive";
    
    currentLessonIndex = index;
    currentLesson = currentTopicLessons[index];
    currentPageIndex = 0;
    
    currentLesson.isCompleted = IsLessonCompleted(currentTopicName, index);
    
    Debug.Log($"Showing lesson {index + 1}/{currentTopicLessons.Count}: {currentLesson.title}");
    
    if (lessonContentTitle != null)
        lessonContentTitle.text = currentLesson.title;
    
    if (lessonContentDescription != null)
        lessonContentDescription.text = currentLesson.description;
    
    if (currentLesson.contentPages == null || currentLesson.contentPages.Count == 0)
    {
        SplitContentIntoPages();
    }
    
    UpdateContentDisplay();
    
    if (titleText != null)
        titleText.text = $"{currentTopicName} - {currentLesson.title}";
    
    UpdateMarkCompleteButton();
    
    if (toggleVisualizerButton != null)
        toggleVisualizerButton.gameObject.SetActive(true);
    
    StartCoroutine(ResetScrollPosition());
}

    
    void SplitContentIntoPages()
    {
        if (currentLesson == null) return;
        
        string fullContent = currentLesson.content ?? GetLessonContent(currentLesson.title);
        
        currentLesson.contentPages = new List<string>();
        
        string[] sections = fullContent.Split(new string[] { "\n\n" }, System.StringSplitOptions.RemoveEmptyEntries);
        
        if (sections.Length > 1)
        {
            foreach (string section in sections)
            {
                currentLesson.contentPages.Add(section.Trim());
            }
        }
        else
        {
            currentLesson.contentPages.Add(fullContent);
        }
        
        Debug.Log($"Split content into {currentLesson.contentPages.Count} pages");
    }
    
    IEnumerator FetchLessonsFromServer()
    {
        Debug.Log("🚀 Fetching lessons from server...");
        
        string url = adminApiUrl + "/lessons";
        UnityWebRequest request = UnityWebRequest.Get(url);
        
        yield return request.SendWebRequest();
        
        if (request.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("✅ Lessons fetched successfully");
            
            try
            {
                LessonsResponse response = JsonUtility.FromJson<LessonsResponse>(request.downloadHandler.text);
                
                if (response != null && response.success && response.lessons != null)
                {
                    Debug.Log($"📚 Parsed {response.lessons.Length} lessons");
                    
                    cachedLessons.Clear();
                    foreach (var lesson in response.lessons)
                    {
                        string normalizedTopic = TopicNameConstants.Normalize(lesson.topicName);
                        
                        if (!cachedLessons.ContainsKey(normalizedTopic))
                        {
                            cachedLessons[normalizedTopic] = new List<LessonModule>();
                        }
                        
                        cachedLessons[normalizedTopic].Add(new LessonModule
                        {
                            title = lesson.title,
                            description = lesson.description,
                            isCompleted = false,
                            lessonId = lesson._id,
                            content = !string.IsNullOrEmpty(lesson.content) 
                                ? lesson.content 
                                : GetLessonContent(lesson.title),
                            contentPages = null,
                            icon = GetIconForTopic(normalizedTopic)
                        });
                    }
                    
                    Debug.Log($"✅ Cached {cachedLessons.Count} topics");
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
    
 public void ShowTopicDetail(string topicName)
{
    Debug.Log($"=== ShowTopicDetail: {topicName} ===");
    
    currentTopicName = topicName;
    currentLessonIndex = 0;
    currentPageIndex = 0;
    currentTopicLessons = null;
    isVisualizerActive = false;
    
    // Clear visualizer when switching topics
    if (visualizer != null)
    {
        visualizer.ClearVisualization();
        visualizer.HideVisualization();
    }
    
    if (topicsGridPanel != null)
        topicsGridPanel.SetActive(false);
    
    if (topicDetailPanel != null)
        topicDetailPanel.SetActive(true);
    
    if (lessonsScrollView != null)
        lessonsScrollView.SetActive(true);
    
    if (lessonContentPanel != null)
        lessonContentPanel.SetActive(false);
    
    if (titleText != null)
        titleText.text = topicName;
    
    if (toggleVisualizerButton != null)
        toggleVisualizerButton.gameObject.SetActive(false);
    
    StartCoroutine(EnsureLessonsLoadedAndDisplay(topicName));
}
    IEnumerator EnsureLessonsLoadedAndDisplay(string topicName)
    {
        ShowLoadingIndicator(true);
        
        if (cachedLessons.Count == 0 && !string.IsNullOrEmpty(adminApiUrl))
        {
            Debug.Log("⏳ Cache empty, fetching lessons...");
            yield return StartCoroutine(FetchLessonsFromServer());
        }
        
        Debug.Log("⏳ Fetching user progress...");
        yield return StartCoroutine(FetchUserProgressFromDatabase());
        
        ShowLoadingIndicator(false);
        
        LoadLessons(topicName);
    }
    

    
    void LoadLessons(string topicName)
    {
        Debug.Log($"=== Loading lessons for: {topicName} ===");
        
        ClearLessons();
        
        currentTopicLessons = GetLessonsForTopic(topicName);
        Debug.Log($"Found {currentTopicLessons.Count} lessons for {topicName}");
        
        if (lessonsContainer == null)
        {
            Debug.LogError("❌ lessonsContainer is NULL!");
            return;
        }
        
        int createdCount = 0;
        for (int i = 0; i < currentTopicLessons.Count; i++)
        {
            LessonModule lesson = currentTopicLessons[i];
            lesson.isCompleted = IsLessonCompleted(topicName, i);
            
            if (lesson.icon == null)
            {
                lesson.icon = GetIconForTopic(topicName);
            }
            
            if (CreateLessonModule(lesson, i))
                createdCount++;
        }
        
        Debug.Log($"✓ Created {createdCount} lesson UI elements for {topicName}");
    }
    
    bool CreateLessonModule(LessonModule lesson, int lessonIndex)
    {
        if (lessonModulePrefab == null || lessonsContainer == null)
            return false;
        
        GameObject lessonObj = Instantiate(lessonModulePrefab, lessonsContainer);
        lessonObj.SetActive(true);
        
        TextMeshProUGUI titleText = FindChildComponent<TextMeshProUGUI>(lessonObj.transform, 
            new string[] { "TitleText", "Title", "LessonTitle", "Text_Title" });
        
        if (titleText != null)
            titleText.text = lesson.title;
        
        TextMeshProUGUI descText = FindChildComponent<TextMeshProUGUI>(lessonObj.transform,
            new string[] { "DescriptionText", "Description", "LessonDescription", "Text_Description", "Desc" });
        
        if (descText != null)
            descText.text = lesson.description;
        
        Image iconImage = FindChildComponent<Image>(lessonObj.transform,
            new string[] { "Icon", "LessonIcon", "Image_Icon" });
        
        if (iconImage != null && lesson.icon != null)
            iconImage.sprite = lesson.icon;
        
        GameObject checkmark = FindChild(lessonObj.transform,
            new string[] { "Checkmark", "CompletedCheckmark", "CheckIcon", "Icon_Complete" });
        
        if (checkmark != null)
            checkmark.SetActive(lesson.isCompleted);
        
        Button lessonButton = lessonObj.GetComponent<Button>();
        if (lessonButton == null)
            lessonButton = lessonObj.GetComponentInChildren<Button>();
        
        if (lessonButton != null)
        {
            int capturedIndex = lessonIndex;
            lessonButton.onClick.AddListener(() => OnLessonClicked(capturedIndex));
        }
        
        return true;
    }
    
    T FindChildComponent<T>(Transform parent, string[] possibleNames) where T : Component
    {
        foreach (string name in possibleNames)
        {
            Transform child = parent.Find(name);
            if (child != null)
            {
                T component = child.GetComponent<T>();
                if (component != null)
                    return component;
            }
        }
        
        return parent.GetComponentInChildren<T>(true);
    }
    
    GameObject FindChild(Transform parent, string[] possibleNames)
    {
        foreach (string name in possibleNames)
        {
            Transform child = parent.Find(name);
            if (child != null)
                return child.gameObject;
        }
        return null;
    }
    
    void ClearLessons()
    {
        if (lessonsContainer == null) return;
        
        foreach (Transform child in lessonsContainer)
        {
            Destroy(child.gameObject);
        }
    }
    
    void OnLessonClicked(int lessonIndex)
    {
        if (lessonContentPanel == null || currentTopicLessons == null)
            return;
        
        if (lessonsScrollView != null)
            lessonsScrollView.SetActive(false);
        
        lessonContentPanel.SetActive(true);
        
        ShowLessonAtIndex(lessonIndex);
    }
    
    IEnumerator ResetScrollPosition()
    {
        yield return new WaitForEndOfFrame();
        
        if (lessonContentScrollRect != null)
        {
            Canvas.ForceUpdateCanvases();
            lessonContentScrollRect.verticalNormalizedPosition = 1f;
            lessonContentScrollRect.velocity = Vector2.zero;
        }
    }
    
    List<LessonModule> GetLessonsForTopic(string topicName)
    {
        string normalizedTopic = TopicNameConstants.Normalize(topicName);
        
        if (cachedLessons.ContainsKey(normalizedTopic))
        {
            return cachedLessons[normalizedTopic];
        }
        
        List<LessonModule> lessons = new List<LessonModule>();
        
        lessons.Add(new LessonModule { 
            title = "Coming Soon", 
            description = "Lessons for this topic are being prepared", 
            isCompleted = false,
            content = "This topic is currently under development.",
            icon = GetIconForTopic(normalizedTopic)
        });
        
        return lessons;
    }
    
    Sprite GetIconForTopic(string topicName)
    {
        string normalized = TopicNameConstants.Normalize(topicName);
        
        switch (normalized)
        {
            case TopicNameConstants.QUEUE:
                return queueIcon != null ? queueIcon : defaultIcon;
            
            case TopicNameConstants.STACKS:
                return stacksIcon != null ? stacksIcon : defaultIcon;
            
            case TopicNameConstants.LINKED_LISTS:
                return linkedListsIcon != null ? linkedListsIcon : defaultIcon;
            
            case TopicNameConstants.TREES:
                return treesIcon != null ? treesIcon : defaultIcon;
            
            case TopicNameConstants.GRAPHS:
                return graphsIcon != null ? graphsIcon : defaultIcon;
            
            default:
                return defaultIcon;
        }
    }
    
    string GetLessonContent(string lessonTitle)
    {
        return $"<size=24><b>{lessonTitle}</b></size>\n\nThis lesson content is currently being developed.";
    }

    
/// <summary>
/// Ensures PlayerPrefs flag matches database lesson completion
/// Call this after fetching progress from database
/// </summary>
void SyncLessonCompletionFlags()
{
    string currentUser = PlayerPrefs.GetString("CurrentUser", "");
    if (string.IsNullOrEmpty(currentUser)) return;
    
    Debug.Log("=== Syncing Lesson Completion Flags ===");
    
    foreach (var kvp in completedLessonsCount)
    {
        string topicName = kvp.Key;
        int completedCount = kvp.Value;
        
        // Get total lessons for this topic
        List<LessonModule> lessons = GetLessonsForTopic(topicName);
        int totalLessons = lessons.Count;
        
        if (totalLessons == 0) continue;
        
        // Check if all lessons are completed
        bool allComplete = completedCount >= totalLessons;
        
        string normalizedTopic = TopicNameConstants.Normalize(topicName);
        string flagKey = $"TopicReadComplete_{currentUser}_{normalizedTopic}";
        
        int currentFlag = PlayerPrefs.GetInt(flagKey, 0);
        
        if (allComplete && currentFlag != 1)
        {
            // Set the flag
            PlayerPrefs.SetInt(flagKey, 1);
            Debug.Log($"✅ SET completion flag for {topicName} ({completedCount}/{totalLessons} lessons)");
        }
        else if (!allComplete && currentFlag == 1)
        {
            // Clear the flag if somehow it was set incorrectly
            PlayerPrefs.SetInt(flagKey, 0);
            Debug.Log($"🔄 CLEARED completion flag for {topicName} ({completedCount}/{totalLessons} lessons)");
        }
        else
        {
            Debug.Log($"✓ Flag already correct for {topicName}: {(allComplete ? "COMPLETE" : "INCOMPLETE")} ({completedCount}/{totalLessons})");
        }
    }
    
    PlayerPrefs.Save();
    
    // Notify LearnPanel to update puzzle button states
    UpdatedLearnPanelController learnPanel = FindObjectOfType<UpdatedLearnPanelController>();
    if (learnPanel != null)
    {
        learnPanel.UpdatePuzzleButtonsAccess();
        Debug.Log("✓ Notified LearnPanel to refresh puzzle buttons");
    }
}

/// <summary>
/// Returns the completed lesson count for a topic
/// Used by UpdatedLearnPanelController to verify puzzle unlock status
/// </summary>
public int GetCompletedLessonCountForTopic(string topicName)
{
    string normalizedTopic = TopicNameConstants.Normalize(topicName);
    
    if (completedLessonsCount.ContainsKey(normalizedTopic))
    {
        return completedLessonsCount[normalizedTopic];
    }
    
    return 0;
}



// Also update the OnBackButtonClicked method
void OnBackButtonClicked()
{
    Debug.Log("Back button clicked");
    
    if (lessonContentPanel != null && lessonContentPanel.activeSelf)
    {
        // Clear visualizer when going back to lessons list
        if (visualizer != null)
        {
            visualizer.ClearVisualization();
            visualizer.HideVisualization();
        }
        isVisualizerActive = false;
        
        lessonContentPanel.SetActive(false);
        
        if (lessonsScrollView != null)
            lessonsScrollView.SetActive(true);
        
        if (titleText != null && !string.IsNullOrEmpty(currentTopicName))
            titleText.text = currentTopicName;
        
        if (toggleVisualizerButton != null)
            toggleVisualizerButton.gameObject.SetActive(false);
    }
    else
    {
        // Clear visualizer when going back to topic selection
        if (visualizer != null)
        {
            visualizer.ClearVisualization();
            visualizer.HideVisualization();
        }
        isVisualizerActive = false;
        
        if (topicDetailPanel != null)
            topicDetailPanel.SetActive(false);
        
        if (topicsGridPanel != null)
            topicsGridPanel.SetActive(true);
        
        ClearLessons();
        currentTopicLessons = null;
    }
}

void OnMarkCompleteClicked()
{
    if (currentLesson == null || string.IsNullOrEmpty(currentTopicName))
    {
        Debug.LogError("❌ No current lesson!");
        return;
    }
    
    if (isLoadingData)
    {
        Debug.LogWarning("⚠️ Still loading data, please wait...");
        return;
    }
    
    string normalizedTopic = TopicNameConstants.Normalize(currentTopicName);
    
    if (IsLessonCompleted(currentTopicName, currentLessonIndex))
    {
        Debug.Log("Lesson already completed");
        return;
    }
    
    currentLesson.isCompleted = true;
    
    if (!completedLessonsCount.ContainsKey(normalizedTopic))
    {
        completedLessonsCount[normalizedTopic] = 0;
    }
    
    if (currentLessonIndex == completedLessonsCount[normalizedTopic])
    {
        completedLessonsCount[normalizedTopic]++;
    }
    
    int newCount = completedLessonsCount[normalizedTopic];
    
    Debug.Log($"✓ Marked complete: {currentTopicName} - Lesson {currentLessonIndex + 1}");
    Debug.Log($"📊 New count: {newCount}/{currentTopicLessons.Count}");
    
    UpdateMarkCompleteButton();
    StartCoroutine(SyncLessonCompletionToDatabase(currentTopicName, newCount));
    
    // ✅ FIX 1: Check if all lessons are now complete
    if (IsTopicFullyCompleted(currentTopicName))
    {
        Debug.Log($"🎉 All lessons completed for {currentTopicName}!");
        NotifyTopicCompletion(currentTopicName);
        
        // ✅ FIX 2: CRITICAL - Set the flag immediately
        string currentUser = PlayerPrefs.GetString("CurrentUser", "");
        if (!string.IsNullOrEmpty(currentUser))
        {
            string flagKey = $"TopicReadComplete_{currentUser}_{normalizedTopic}";
            PlayerPrefs.SetInt(flagKey, 1);
            PlayerPrefs.Save();
            Debug.Log($"✅ SET completion flag: {flagKey}");
        }
        
        // ✅ FIX 3: Mark tutorial as complete in UserProgressManager
        if (UserProgressManager.Instance != null)
        {
            UserProgressManager.Instance.CompleteTutorial(currentTopicName);
            Debug.Log($"✅ Marked tutorial complete in UserProgressManager");
        }
        
        // ✅ FIX 4: Update UI immediately
        UpdatedLearnPanelController learnPanel = FindObjectOfType<UpdatedLearnPanelController>();
        if (learnPanel != null)
        {
            learnPanel.UpdatePuzzleButtonsAccess();
            Debug.Log("✅ Refreshed puzzle button access");
        }
    }
    
    Debug.Log("✓ Lesson marked complete! Choose your next lesson from the list.");
}


}

