using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.Collections;
using UnityEngine.Networking;

public class UpdatedLearnPanelController : MonoBehaviour
{
    [Header("Header")]
    public GameObject loadingIndicator;
    public TextMeshProUGUI topicNameHeader;
    public TextMeshProUGUI lessonCounterText; // NEW: Shows "X Lessons Available"
    
    [Header("Panels")]
    public GameObject topicSelectionPanel;
    public GameObject learningModesPanel;
    
    [Header("Mode Buttons")]
    public Button guidedBuildingButton;
    public Button puzzleChallengeButton;
    public Button backToTopicsButton;
    
    [Header("Puzzle Lock UI")]
    public GameObject puzzleLockOverlay;
    public TextMeshProUGUI puzzleLockMessage;
    
    [Header("Topic Cards with Progress")]
    public TopicCard queuesCard;
    public TopicCard stacksCard;
    public TopicCard linkedListsCard;
    public TopicCard treesCard;
    public TopicCard graphsCard;
    
    [Header("Challenge Manager")]
    public ChallengeManager challengeManager;
    
    [Header("API Settings")]
    public string adminApiUrl = "https://structureality-admin.onrender.com/api";
    
    [System.Serializable]
    public class TopicCard
    {
        public Button button;
        public string topicName;
        public TextMeshProUGUI progressText;
        public Image progressBar;
        public GameObject completedBadge;
        public Image lockIcon;
        public TextMeshProUGUI lessonCountText; // NEW: Shows lesson count on card
    }
    
    private string currentSelectedTopic = "";
    private Dictionary<string, TopicCard> topicCards;
    private UserProgressManager progressManager;
    private Dictionary<string, int> topicLessonCounts = new Dictionary<string, int>();
    
    void Start()
    {
        Debug.Log("=== UpdatedLearnPanelController Start() ===");
        
        progressManager = UserProgressManager.Instance;
        if (progressManager == null)
        {
            Debug.LogError("UserProgressManager not found in scene!");
        }
        
        VerifyButtonReferences();
        InitializeTopicCards();
        SetupButtons();
        
        // Fetch lesson counts from database
        StartCoroutine(FetchLessonCounts());
        
        UpdateAllTopicProgress();
        
        Debug.Log("=== LearnPanelController initialization complete ===");
    }

    void VerifyButtonReferences()
    {
        Debug.Log("=== VERIFYING LEARN PANEL BUTTON REFERENCES ===");
        
        if (queuesCard.button != null)
            Debug.Log($"✓ Queues button: {queuesCard.button.gameObject.name}");
        else
            Debug.LogError("✗ Queues button is NULL!");
        
        if (stacksCard.button != null)
            Debug.Log($"✓ Stacks button: {stacksCard.button.gameObject.name}");
        else
            Debug.LogError("✗ Stacks button is NULL!");
        
        if (linkedListsCard.button != null)
            Debug.Log($"✓ LinkedLists button: {linkedListsCard.button.gameObject.name}");
        else
            Debug.LogError("✗ LinkedLists button is NULL!");
        
        if (treesCard.button != null)
            Debug.Log($"✓ Trees button: {treesCard.button.gameObject.name}");
        else
            Debug.LogError("✗ Trees button is NULL!");
        
        if (graphsCard.button != null)
            Debug.Log($"✓ Graphs button: {graphsCard.button.gameObject.name}");
        else
            Debug.LogError("✗ Graphs button is NULL!");
        
        Debug.Log("=== END BUTTON VERIFICATION ===");
    }

 void InitializeTopicCards()
{
    Debug.Log("=== Initializing LearnPanel Topic Cards ===");
    
    topicCards = new Dictionary<string, TopicCard>
    {
        { "Queue", queuesCard },
        { "Stacks", stacksCard },
        { "LinkedLists", linkedListsCard },  // ✅ CHANGED: Match TopicNameConstants
        { "Trees", treesCard },
        { "Graphs", graphsCard }
    };

    foreach (var kvp in topicCards)
    {
        string topicName = kvp.Key;
        TopicCard card = kvp.Value;
        
        if (card.button != null)
        {
            Debug.Log($"[LEARN] Setting up {card.button.gameObject.name} for topic: {topicName}");
            
            card.button.onClick.RemoveAllListeners();
            card.button.onClick.AddListener(() => SelectTopic(topicName));
            
            Debug.Log($"[LEARN] ✓ Listener added to {card.button.gameObject.name}");
        }
        else
        {
            Debug.LogError($"[LEARN] ✗ Button is NULL for topic: {topicName}!");
        }
    }
    
    Debug.Log($"=== {topicCards.Count} LearnPanel topic cards initialized ===");
}
    
    void SetupButtons()
    {
        if (guidedBuildingButton != null)
        {
            guidedBuildingButton.onClick.AddListener(StartTutorialMode);
            Debug.Log("✓ Guided Building button listener added");
        }
        
        if (puzzleChallengeButton != null)
        {
            puzzleChallengeButton.onClick.AddListener(StartChallengeMode);
            Debug.Log("✓ Puzzle Challenge button listener added");
        }
        
        if (backToTopicsButton != null)
        {
            backToTopicsButton.onClick.AddListener(ShowTopicSelection);
        }
        
        if (challengeManager != null)
        {
            Debug.Log("✓ ChallengeManager is assigned");
        }
    }
    
    // NEW: Fetch lesson counts from database
 IEnumerator FetchLessonCounts()
{
    if (string.IsNullOrEmpty(adminApiUrl))
    {
        Debug.LogWarning("⚠️ API URL not set, using default counts");
        yield break;
    }
    
    string url = $"{adminApiUrl}/lessons";
    Debug.Log($"🔄 Fetching lesson counts from: {url}");
    
    UnityWebRequest request = UnityWebRequest.Get(url);
    request.SetRequestHeader("Content-Type", "application/json");
    
    yield return request.SendWebRequest();
    
    if (request.result == UnityWebRequest.Result.Success)
    {
        Debug.Log("✅ Lessons fetched successfully");
        
        try
        {
            // Use the shared LessonsResponse class
            LessonsResponse response = JsonUtility.FromJson<LessonsResponse>(request.downloadHandler.text);
            
            if (response != null && response.success && response.lessons != null)
            {
                topicLessonCounts.Clear();
                
                foreach (var lesson in response.lessons)
                {
                    string normalizedTopic = TopicNameConstants.Normalize(lesson.topicName);
                    
                    if (!topicLessonCounts.ContainsKey(normalizedTopic))
                    {
                        topicLessonCounts[normalizedTopic] = 0;
                    }
                    topicLessonCounts[normalizedTopic]++;
                }
                
                Debug.Log($"✅ Lesson counts loaded:");
                foreach (var kvp in topicLessonCounts)
                {
                    Debug.Log($"  📚 {kvp.Key}: {kvp.Value} lessons");
                }
                
                // Update UI with lesson counts
                UpdateLessonCountsOnCards();
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ Parse error: {e.Message}");
        }
    }
    else
    {
        Debug.LogError($"❌ Failed to fetch lessons: {request.error}");
    }
}
    
    // NEW: Update lesson count display on topic cards
    void UpdateLessonCountsOnCards()
    {
        foreach (var kvp in topicCards)
        {
            string topicName = kvp.Key;
            TopicCard card = kvp.Value;
            
            if (card.lessonCountText != null)
            {
                int lessonCount = GetLessonCount(topicName);
                card.lessonCountText.text = lessonCount > 0 
                    ? $"📚 {lessonCount} Lesson{(lessonCount != 1 ? "s" : "")}"
                    : "Coming Soon";
                
                Debug.Log($"✓ Updated lesson count for {topicName}: {lessonCount}");
            }
        }
    }
    
    // NEW: Get lesson count for a topic
   int GetLessonCount(string topicName)
{
    string normalizedTopic = TopicNameConstants.Normalize(topicName);
    
    if (topicLessonCounts.ContainsKey(normalizedTopic))
    {
        int count = topicLessonCounts[normalizedTopic];
        Debug.Log($"📚 GetLessonCount({topicName}): {count}");
        return count;
    }
    
    Debug.LogWarning($"⚠️ GetLessonCount({topicName}): NO DATA - topicLessonCounts is empty or missing key '{normalizedTopic}'");
    Debug.LogWarning($"   Available keys: {string.Join(", ", new List<string>(topicLessonCounts.Keys))}");
    return 0;
}
    
    void OnEnable()
    {
        if (topicCards != null)
        {
            InitializeTopicCards();
        }
        
        ShowTopicSelection();
        UpdateAllTopicProgress();
        UpdateLessonCountsOnCards();
    }
    
    void UpdateAllTopicProgress()
    {
        if (progressManager == null) 
        {
            progressManager = UserProgressManager.Instance;
            if (progressManager == null) return;
        }
        
        foreach (var kvp in topicCards)
        {
            string topicName = kvp.Key;
            TopicCard card = kvp.Value;
            
            float progress = progressManager.GetTopicProgress(topicName);
            bool isCompleted = progress >= 100f;
            
            if (card.progressBar != null)
            {
                card.progressBar.fillAmount = progress / 100f;
            }
            
            if (card.progressText != null)
            {
                card.progressText.text = $"{progress:F0}%";
            }
            
            if (card.completedBadge != null)
            {
                card.completedBadge.SetActive(isCompleted);
            }
        }
    }
    
bool IsTopicUnlocked(string topicName)
{
    if (progressManager == null) return true;
    
    if (topicName == "Queue") return true;
    
    if (topicName == "Stacks")
    {
        return progressManager.GetTopicProgress("Queue") >= 50f;
    }
    
    if (topicName == "LinkedLists")  // ✅ CHANGED: Use consistent naming
    {
        return progressManager.GetTopicProgress("Stacks") >= 50f;
    }
    
    if (topicName == "Trees")
    {
        return progressManager.GetTopicProgress("LinkedLists") >= 50f;  // ✅ CHANGED
    }
    
    if (topicName == "Graphs")
    {
        return progressManager.GetTopicProgress("Trees") >= 50f;
    }
    
    return false;
}
    
    public void ShowTopicSelection()
    {
        Debug.Log("ShowTopicSelection called");
        
        if (topicNameHeader != null)
            topicNameHeader.text = "Choose Topic";
        
        // Hide lesson counter in topic selection view
        if (lessonCounterText != null)
            lessonCounterText.gameObject.SetActive(false);
        
        if (topicSelectionPanel != null)
            topicSelectionPanel.SetActive(true);
            
        if (learningModesPanel != null)
            learningModesPanel.SetActive(false);
        
        currentSelectedTopic = "";
        
        UpdateAllTopicProgress();
        UpdateLessonCountsOnCards();
    }
    
public void SelectTopic(string topicName)
{
    currentSelectedTopic = topicName;
    
    Debug.Log("=== Topic Selected: " + topicName + " ===");
    
    PlayerPrefs.SetString("SelectedTopic", topicName);
    PlayerPrefs.Save();
    
    if (progressManager != null)
    {
        progressManager.StartTopicSession(topicName);
    }
    
    if (topicNameHeader != null)
        topicNameHeader.text = topicName;
    
    // Show lesson counter below header
    if (lessonCounterText != null)
    {
        int lessonCount = GetLessonCount(topicName);
        lessonCounterText.text = lessonCount > 0 
            ? $"📚 {lessonCount} Lesson{(lessonCount != 1 ? "s" : "")} Available"
            : "Coming Soon";
        lessonCounterText.gameObject.SetActive(true);
        
        Debug.Log($"✓ Lesson counter updated: {lessonCount} lessons");
    }
    
    if (topicSelectionPanel != null)
        topicSelectionPanel.SetActive(false);
        
    if (learningModesPanel != null)
        learningModesPanel.SetActive(true);
    
    // SIMPLE: Just update buttons directly like the working version
    UpdateModeButtons();
    
    Debug.Log("Learning modes panel shown");
}

IEnumerator WaitForLessonCountsAndUpdateButtons()
{
    Debug.Log("⏳ Waiting for lesson counts to load...");
    
    float timeout = 90f; // Long timeout for Render cold starts
    float elapsed = 0f;
    
    // Wait until lesson counts are loaded
    while (elapsed < timeout)
    {
        int lessonCount = GetLessonCount(currentSelectedTopic);
        
        if (lessonCount > 0)
        {
            Debug.Log($"✅ Lesson count loaded: {lessonCount} lessons for {currentSelectedTopic}");
            break;
        }
        
        // Log progress every 5 seconds
        if (elapsed % 5f < 0.3f && elapsed > 0)
        {
            Debug.Log($"⏳ Still waiting for lesson data... ({elapsed:F0}s elapsed)");
        }
        
        yield return new WaitForSeconds(0.3f);
        elapsed += 0.3f;
    }
    
    if (GetLessonCount(currentSelectedTopic) == 0)
    {
        Debug.LogError($"❌ TIMEOUT: No lessons loaded for {currentSelectedTopic} after {timeout}s");
        Debug.LogError("💡 Render backend may be cold starting. Please wait and try again.");
    }
    
    // Update lesson counter in UI
    if (lessonCounterText != null)
    {
        int lessonCount = GetLessonCount(currentSelectedTopic);
        lessonCounterText.text = lessonCount > 0 
            ? $"📚 {lessonCount} Lesson{(lessonCount != 1 ? "s" : "")} Available"
            : "Loading...";
        lessonCounterText.gameObject.SetActive(true);
    }
    
    // Now wait for progress data and update buttons
    yield return StartCoroutine(WaitForDataAndUpdateButtons());
}

IEnumerator WaitForDataAndUpdateButtons()
{
    Debug.Log("⏳ Waiting for progress data...");
    
    float timeout = 90f;
    float elapsed = 0f;
    bool dataReady = false;
    
    while (elapsed < timeout && !dataReady)
    {
        bool progressManagerReady = progressManager != null && 
                                    (progressManager.GetTopicProgress(currentSelectedTopic) > 0 || 
                                     progressManager.IsTutorialCompleted(currentSelectedTopic));
        
        TopicDetailPanel detailPanel = FindObjectOfType<TopicDetailPanel>();
        bool lessonDataReady = detailPanel != null && !detailPanel.isLoadingData;
        
        if (lessonDataReady)
        {
            int totalLessons = GetLessonCount(currentSelectedTopic);
            int completedLessons = GetCompletedLessonCount(currentSelectedTopic);
            
            if (totalLessons > 0)
            {
                dataReady = true;
                Debug.Log($"✅ Progress data ready for {currentSelectedTopic}:");
                Debug.Log($"  - Total lessons: {totalLessons}");
                Debug.Log($"  - Completed: {completedLessons}");
                Debug.Log($"  - Progress: {progressManager?.GetTopicProgress(currentSelectedTopic)}%");
                break;
            }
        }
        
        yield return new WaitForSeconds(0.3f);
        elapsed += 0.3f;
    }
    
    if (!dataReady)
    {
        Debug.LogWarning($"⚠️ Timeout after {elapsed:F0}s waiting for progress data");
    }
    
    UpdateModeButtons();
}

    
void UpdateModeButtons()
{
    if (progressManager == null) return;
    
    bool tutorialCompleted = progressManager.IsTutorialCompleted(currentSelectedTopic);
    bool puzzleCompleted = progressManager.IsPuzzleCompleted(currentSelectedTopic);
    bool allLessonsRead = IsTopicReadingComplete(currentSelectedTopic);
    
    if (guidedBuildingButton != null)
    {
        guidedBuildingButton.interactable = true;
        
        TextMeshProUGUI buttonText = guidedBuildingButton.GetComponentInChildren<TextMeshProUGUI>();
        if (buttonText != null)
        {
            buttonText.text = "Guided Building";
        }
    }
    
    if (puzzleChallengeButton != null)
    {
        puzzleChallengeButton.interactable = allLessonsRead;
        
        TextMeshProUGUI buttonText = puzzleChallengeButton.GetComponentInChildren<TextMeshProUGUI>();
        if (buttonText != null)
        {
            if (!allLessonsRead)
            {
                buttonText.text = "🔒 Complete Lessons First";
            }
            else if (puzzleCompleted)
            {
                buttonText.text = "✓ Puzzle Challenge";
            }
            else
            {
                buttonText.text = "Puzzle Challenge";
            }
        }
        
        if (puzzleLockOverlay != null)
        {
            puzzleLockOverlay.SetActive(!allLessonsRead);
        }
        
        if (puzzleLockMessage != null && !allLessonsRead)
        {
            puzzleLockMessage.text = "Complete all lessons to unlock puzzle challenges!";
        }
    }
    
    Debug.Log($"Puzzle button access - Topic: {currentSelectedTopic}, All lessons read: {allLessonsRead}");
}
    
bool IsTopicReadingComplete(string topicName)
{
    string currentUser = PlayerPrefs.GetString("CurrentUser", "");
    if (string.IsNullOrEmpty(currentUser))
    {
        Debug.LogWarning("No current user found");
        return false;
    }
    
    string normalizedTopic = TopicNameConstants.Normalize(topicName);
    
    // Check PlayerPrefs flag
    int completionStatus = PlayerPrefs.GetInt($"TopicReadComplete_{currentUser}_{normalizedTopic}", 0);
    bool flagComplete = completionStatus == 1;
    
    // ALSO check if lesson count matches total lessons available
    int totalLessons = GetLessonCount(topicName);
    int completedLessons = GetCompletedLessonCount(topicName);
    
    bool lessonCountComplete = (totalLessons > 0 && completedLessons >= totalLessons);
    
    Debug.Log($"🔍 Unlock check for {topicName}:");
    Debug.Log($"  - PlayerPrefs flag: {flagComplete}");
    Debug.Log($"  - Lessons: {completedLessons}/{totalLessons} = {lessonCountComplete}");
    
    // If lessons are complete but flag isn't set, fix it
    if (lessonCountComplete && !flagComplete)
    {
        Debug.Log($"⚠️ Fixing: Lessons complete but flag not set!");
        PlayerPrefs.SetInt($"TopicReadComplete_{currentUser}_{normalizedTopic}", 1);
        PlayerPrefs.Save();
        flagComplete = true;
    }
    
    // Return true if EITHER check passes (for backwards compatibility)
    bool isComplete = flagComplete || lessonCountComplete;
    
    Debug.Log($"✓ Topic '{topicName}' reading complete: {isComplete}");
    return isComplete;
}
int GetCompletedLessonCount(string topicName)
{
    TopicDetailPanel detailPanel = FindObjectOfType<TopicDetailPanel>();
    if (detailPanel != null)
    {
        int count = detailPanel.GetCompletedLessonCountForTopic(topicName);
        Debug.Log($"  📚 TopicDetailPanel reports: {count} completed");
        return count;
    }
    
    Debug.LogWarning($"  ⚠️ TopicDetailPanel not found - cannot verify lesson completion");
    return 0;
}
    public void UpdatePuzzleButtonsAccess()
    {
        Debug.Log("UpdatePuzzleButtonsAccess called - refreshing button states");
        UpdateModeButtons();
    }
    
    public void StartTutorialMode()
    {
        Debug.Log("=== StartTutorialMode() CALLED ===");
        Debug.Log("Current topic: " + currentSelectedTopic);
        
        if (string.IsNullOrEmpty(currentSelectedTopic))
        {
            Debug.LogError("No topic selected!");
            return;
        }
        
        PlayerPrefs.SetString("QueueMode", "Tutorial");
        PlayerPrefs.Save();
        
        string sceneName = GetSceneName(currentSelectedTopic);
        if (!string.IsNullOrEmpty(sceneName))
        {
            SceneManager.LoadScene(sceneName);
        }
        else
        {
            Debug.LogWarning("No scene found for topic: " + currentSelectedTopic);
        }
    }
    
    public void StartChallengeMode()
    {
        Debug.Log("=== START CHALLENGE MODE CLICKED ===");
        Debug.Log("Current Topic: " + currentSelectedTopic);
        
        if (string.IsNullOrEmpty(currentSelectedTopic))
        {
            Debug.LogError("❌ No topic selected!");
            return;
        }
        
        if (!IsTopicReadingComplete(currentSelectedTopic))
        {
            Debug.LogWarning("⚠️ Cannot start challenge - lessons not completed!");
            
            if (puzzleLockMessage != null)
            {
                puzzleLockMessage.text = "Please complete all lessons before attempting puzzles!";
                if (puzzleLockOverlay != null)
                {
                    puzzleLockOverlay.SetActive(true);
                }
            }
            return;
        }
        
        if (challengeManager == null)
        {
            Debug.LogError("❌ ChallengeManager is NOT assigned!");
            return;
        }
        
        Debug.Log("✓ Starting challenge for: " + currentSelectedTopic);
        
        PlayerPrefs.SetString("QueueMode", "Challenge");
        PlayerPrefs.Save();
        
        if (learningModesPanel != null)
        {
            learningModesPanel.SetActive(false);
        }
            
        challengeManager.StartChallenge(currentSelectedTopic);
        Debug.Log("✓ Challenge started");
    }
    
string GetSceneName(string topicName)
{
    topicName = TopicNameConstants.Normalize(topicName);

    switch (topicName)
    {
        case "Queue":
            return "QueueScene";

        case "Stacks":
            return "StackScene";

        case "LinkedLists":  // ✅ CHANGED: Match normalized name
            return "LinkedListScene";

        case "Trees":
            return "TreeScene";

        case "Graphs":
            return "GraphScene";

        default:
            return "";
    }
}

[System.Serializable]
public class LessonsResponse
{
    public bool success;
    public int count;
    public Lesson[] lessons;
}

[System.Serializable]
public class Lesson
{
    public string _id;
    public string topicName;
    public string title;
    public string description;
    public string content;
    public int order;
    public string createdAt;
}

}
