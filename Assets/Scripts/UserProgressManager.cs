using UnityEngine;
using System;
using System.Collections.Generic;
using UnityEngine.Networking;
using System.Collections;
using System.Text;

/// <summary>
/// CRITICAL: Standardize all topic names across the application
/// This fixes the Queue vs Queues inconsistency
/// </summary>
public static class TopicNameConstants
{
    public const string QUEUE = "Queue";
    public const string STACKS = "Stacks";
    public const string LINKED_LISTS = "LinkedLists";
    public const string TREES = "Trees";
    public const string GRAPHS = "Graphs";
    
    public static readonly List<string> ALL_TOPICS = new List<string>
    {
        QUEUE,
        STACKS,
        LINKED_LISTS,
        TREES,
        GRAPHS
    };
    
public static string Normalize(string topicName)
{
    if (string.IsNullOrEmpty(topicName))
        return "";
    
    string cleaned = topicName.Trim();
    
    switch (cleaned.ToLower())
    {
        case "queues":
        case "queue":
            return QUEUE;
            
        case "stacks":
        case "stack":
            return STACKS;
            
        case "linkedlists":
        case "linked lists":
        case "linkedlist":
        case "linked list":  // ADDED: Handle single space variant
            return LINKED_LISTS;
            
        case "trees":
        case "tree":
            return TREES;
            
        case "graphs":
        case "graph":
            return GRAPHS;
            
        default:
            Debug.LogWarning($"⚠️ Unknown topic name: {topicName}");
            return cleaned;
    }
}
}

/// <summary>
/// Complete User Progress Manager - SINGLE FILE ONLY
/// Tracks all student progress locally and syncs to database
/// </summary>
public class UserProgressManager : MonoBehaviour
{
    public static UserProgressManager Instance { get; private set; }
    
    [System.Serializable]
    public class TopicProgress
    {
        public string topicName;
        public bool tutorialCompleted;
        public bool puzzleCompleted;
        public int puzzleScore;
        public DateTime lastAccessed;
        public float timeSpent;
        public int lessonsCompleted; // ADDED: Track lesson completion
        
        public bool IsCompleted()
        {
            return tutorialCompleted && puzzleCompleted;
        }
        
        public float GetCompletionPercentage()
        {
            int completed = 0;
            if (tutorialCompleted) completed += 50;
            if (puzzleCompleted) completed += 50;
            return completed;
        }
    }
    
    [Header("Database Settings (Optional)")]
    [Tooltip("Leave empty to use local-only mode")]
    public string adminApiUrl = "https://structureality-admin.onrender.com/api";
    [Tooltip("Enable to sync progress to database automatically")]
    public bool autoSync = true;
    
    private string currentUsername;
    private string currentUserEmail;
    private Dictionary<string, TopicProgress> userProgress;
    private float sessionStartTime;
    private string currentTopic;
    private bool isInitialized = false;
    
    public List<string> topicOrder = TopicNameConstants.ALL_TOPICS;
    
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            sessionStartTime = Time.time;
            
            string storedUser = PlayerPrefs.GetString("CurrentUser", "");
            if (!string.IsNullOrEmpty(storedUser))
            {
                Debug.Log($"✓ Found stored user: {storedUser}, loading progress...");
                LoadUserProgress();
            }
            else
            {
                Debug.Log("✓ UserProgressManager initialized - waiting for login");
                userProgress = new Dictionary<string, TopicProgress>();
            }
            
            Debug.Log($"✓ Auto Sync: {autoSync}, API URL: {adminApiUrl}");
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    public void LoadUserProgress()
    {
        currentUserEmail = PlayerPrefs.GetString("CurrentUser", "");
        userProgress = new Dictionary<string, TopicProgress>();
        
        if (string.IsNullOrEmpty(currentUserEmail))
        {
            Debug.LogWarning("No user logged in");
            isInitialized = false;
            return;
        }
        
        currentUsername = PlayerPrefs.GetString("User_" + currentUserEmail + "_Username", "");
        
        if (string.IsNullOrEmpty(currentUsername))
        {
            Debug.LogWarning($"⚠️ Username not found for email: {currentUserEmail}");
            currentUsername = currentUserEmail.Split('@')[0];
        }
        
        Debug.Log($"✓ Current User - Email: {currentUserEmail}, Username: {currentUsername}");
        
        // CRITICAL FIX: On fresh login, fetch from database FIRST to avoid using stale local data
        bool shouldFetchFromDB = !PlayerPrefs.HasKey($"UserProgressInitialized_{currentUserEmail}");
        
        if (shouldFetchFromDB)
        {
            Debug.Log("🔄 Fresh login detected - will fetch from database");
            // Initialize with empty progress, database will populate it
            foreach (string topic in TopicNameConstants.ALL_TOPICS)
            {
                userProgress[topic] = new TopicProgress { topicName = topic };
            }
            
            PlayerPrefs.SetInt($"UserProgressInitialized_{currentUserEmail}", 1);
            PlayerPrefs.Save();
            
            isInitialized = true;
            
            // Fetch from database immediately
            if (autoSync && !string.IsNullOrEmpty(adminApiUrl))
            {
                StartCoroutine(FetchAndMergeFromDatabase());
            }
            return;
        }
        
        // Load local progress for all topics (only for existing sessions)
        foreach (string topic in TopicNameConstants.ALL_TOPICS)
        {
            string legacyTopic = topic == TopicNameConstants.QUEUE ? "Queues" : topic;
            
            TopicProgress progress = new TopicProgress
            {
                topicName = topic,
                tutorialCompleted = PlayerPrefs.GetInt($"{currentUserEmail}_{topic}_Tutorial", 0) == 1 ||
                                   PlayerPrefs.GetInt($"{currentUserEmail}_{legacyTopic}_Tutorial", 0) == 1,
                puzzleCompleted = PlayerPrefs.GetInt($"{currentUserEmail}_{topic}_Puzzle", 0) == 1 ||
                                 PlayerPrefs.GetInt($"{currentUserEmail}_{legacyTopic}_Puzzle", 0) == 1,
                puzzleScore = Mathf.Max(
                    PlayerPrefs.GetInt($"{currentUserEmail}_{topic}_Score", 0),
                    PlayerPrefs.GetInt($"{currentUserEmail}_{legacyTopic}_Score", 0)
                ),
                timeSpent = PlayerPrefs.GetFloat($"{currentUserEmail}_{topic}_TimeSpent", 0f) +
                           PlayerPrefs.GetFloat($"{currentUserEmail}_{legacyTopic}_TimeSpent", 0f),
                lessonsCompleted = PlayerPrefs.GetInt($"{currentUserEmail}_{topic}_LessonsCompleted", 0) // ADDED
            };
            
            string lastAccessedStr = PlayerPrefs.GetString($"{currentUserEmail}_{topic}_LastAccessed", "");
            if (string.IsNullOrEmpty(lastAccessedStr))
            {
                lastAccessedStr = PlayerPrefs.GetString($"{currentUserEmail}_{legacyTopic}_LastAccessed", "");
            }
            
            if (!string.IsNullOrEmpty(lastAccessedStr))
            {
                try
                {
                    progress.lastAccessed = DateTime.Parse(lastAccessedStr);
                }
                catch
                {
                    progress.lastAccessed = DateTime.Now;
                }
            }
            else
            {
                progress.lastAccessed = DateTime.Now;
            }
            
            userProgress[topic] = progress;
            
            // Migrate old "Queues" data
            if (topic == TopicNameConstants.QUEUE && PlayerPrefs.HasKey($"{currentUserEmail}_Queues_Score"))
            {
                Debug.Log("🔄 Migrating old 'Queues' data to 'Queue'");
                SaveProgress(topic);
                
                PlayerPrefs.DeleteKey($"{currentUserEmail}_Queues_Tutorial");
                PlayerPrefs.DeleteKey($"{currentUserEmail}_Queues_Puzzle");
                PlayerPrefs.DeleteKey($"{currentUserEmail}_Queues_Score");
                PlayerPrefs.DeleteKey($"{currentUserEmail}_Queues_TimeSpent");
                PlayerPrefs.DeleteKey($"{currentUserEmail}_Queues_LastAccessed");
                
                Debug.Log("✓ Migration complete - old 'Queues' keys removed");
            }
        }
        
        PlayerPrefs.Save();
        isInitialized = true;
        Debug.Log($"✓ Progress loaded and migrated for: {currentUsername}");
        
        // ADDED: Fetch latest data from database to sync lesson counts
        if (autoSync && !string.IsNullOrEmpty(adminApiUrl))
        {
            StartCoroutine(FetchAndMergeFromDatabase());
        }
    }
    
    // ADDED: Fetch database data and merge with local data
    IEnumerator FetchAndMergeFromDatabase()
    {
        string url = $"{adminApiUrl}/progress/{currentUsername}";
        Debug.Log($"🔄 Fetching progress from database: {url}");
        
        UnityWebRequest request = UnityWebRequest.Get(url);
        request.SetRequestHeader("Content-Type", "application/json");
        
        yield return request.SendWebRequest();
        
        if (request.result == UnityWebRequest.Result.Success)
        {
            try
            {
               DatabaseProgressResponse response = JsonUtility.FromJson<DatabaseProgressResponse>(request.downloadHandler.text);

                if (response != null && response.success && response.data != null)
                {
                    Debug.Log("✅ Database progress fetched successfully");
                    
                    // CRITICAL FIX: Clear ALL local data first, then populate from database
                    ClearAllLocalProgress();
                    
                    foreach (var topicData in response.data.topics)
                    {
                        string normalizedTopic = TopicNameConstants.Normalize(topicData.topicName);
                        
                        if (userProgress.ContainsKey(normalizedTopic))
                        {
                            // Populate from database (source of truth)
                            userProgress[normalizedTopic].tutorialCompleted = topicData.tutorialCompleted;
                            userProgress[normalizedTopic].puzzleCompleted = topicData.puzzleCompleted;
                            userProgress[normalizedTopic].puzzleScore = topicData.puzzleScore;
                            userProgress[normalizedTopic].lessonsCompleted = topicData.lessonsCompleted;
                            userProgress[normalizedTopic].timeSpent = topicData.timeSpent;
                            
                            try
                            {
                                userProgress[normalizedTopic].lastAccessed = DateTime.Parse(topicData.lastAccessed);
                            }
                            catch
                            {
                                userProgress[normalizedTopic].lastAccessed = DateTime.Now;
                            }
                            
                            // Save to local cache
                            SaveProgress(normalizedTopic);
                            
                            Debug.Log($"✓ Loaded {normalizedTopic}: {topicData.lessonsCompleted} lessons, tutorial: {topicData.tutorialCompleted}, puzzle: {topicData.puzzleCompleted}");
                        }
                    }
                    
                    PlayerPrefs.Save();
                    Debug.Log("✅ Database merge complete - local data synchronized");
                }
                else
                {
                    Debug.Log("⚠️ No progress found in database - starting fresh");
                    ClearAllLocalProgress();
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"⚠️ Failed to parse database data: {e.Message}");
                Debug.LogWarning("Starting with empty progress");
                ClearAllLocalProgress();
            }
        }
        else
        {
            Debug.LogWarning($"⚠️ Database fetch failed: {request.error}");
            Debug.LogWarning("Starting with empty progress");
            ClearAllLocalProgress();
        }
    }
    
    // ADDED: Clear all local progress data
    void ClearAllLocalProgress()
    {
        Debug.Log("🧹 Clearing all local progress data");
        
        foreach (string topic in TopicNameConstants.ALL_TOPICS)
        {
            if (userProgress.ContainsKey(topic))
            {
                userProgress[topic] = new TopicProgress { topicName = topic };
            }
            
            // Clear PlayerPrefs for this topic
            PlayerPrefs.DeleteKey($"{currentUserEmail}_{topic}_Tutorial");
            PlayerPrefs.DeleteKey($"{currentUserEmail}_{topic}_Puzzle");
            PlayerPrefs.DeleteKey($"{currentUserEmail}_{topic}_Score");
            PlayerPrefs.DeleteKey($"{currentUserEmail}_{topic}_TimeSpent");
            PlayerPrefs.DeleteKey($"{currentUserEmail}_{topic}_LastAccessed");
            PlayerPrefs.DeleteKey($"{currentUserEmail}_{topic}_LessonsCompleted");
        }
        
        PlayerPrefs.Save();
        Debug.Log("✓ Local progress cleared");
    }
    
    public void InitializeForUser(string username)
    {
        currentUsername = username;
        currentUserEmail = PlayerPrefs.GetString("CurrentUser", "");
        
        // CRITICAL FIX: Clear initialization flag to force database fetch on fresh login
        PlayerPrefs.DeleteKey($"UserProgressInitialized_{currentUserEmail}");
        PlayerPrefs.Save();
        
        LoadUserProgress();
        Debug.Log($"✓ UserProgressManager initialized for: {username}");
    }
    
    public void StartTopicSession(string topicName)
    {
        if (!isInitialized)
        {
            Debug.LogWarning("⚠️ UserProgressManager not initialized - no user logged in");
            return;
        }
        
        currentTopic = TopicNameConstants.Normalize(topicName);
        sessionStartTime = Time.time;
        PlayerPrefs.SetString("CurrentTopic", currentTopic);
        Debug.Log($"Started session for: {currentTopic} (normalized from: {topicName})");
    }
    
    public void EndTopicSession()
    {
        if (string.IsNullOrEmpty(currentTopic)) return;
        
        float sessionTime = Time.time - sessionStartTime;
        AddTimeSpent(currentTopic, sessionTime);
        
        Debug.Log($"Ended session for {currentTopic}. Time: {sessionTime:F1}s");
        currentTopic = "";
    }
    
    public void CompleteTutorial(string topicName)
    {
        topicName = TopicNameConstants.Normalize(topicName);
        
        if (string.IsNullOrEmpty(currentUserEmail)) 
        {
            Debug.LogError("❌ No user logged in!");
            return;
        }
        
        if (!userProgress.ContainsKey(topicName))
        {
            userProgress[topicName] = new TopicProgress { topicName = topicName };
        }
        
        userProgress[topicName].tutorialCompleted = true;
        userProgress[topicName].lastAccessed = DateTime.Now;
        
        SaveProgress(topicName);
        UpdateOverallProgress();
        
        Debug.Log($"✓ Tutorial completed for {topicName}");
        
        if (autoSync && !string.IsNullOrEmpty(adminApiUrl))
        {
            Debug.Log($"🔄 Syncing tutorial completion to database...");
            StartCoroutine(SyncToDatabase());
        }
    }
    
    public void CompletePuzzle(string topicName, int score)
    {
        topicName = TopicNameConstants.Normalize(topicName);
        
        if (string.IsNullOrEmpty(currentUserEmail)) 
        {
            Debug.LogError("❌ No user logged in!");
            return;
        }
        
        if (!userProgress.ContainsKey(topicName))
        {
            userProgress[topicName] = new TopicProgress { topicName = topicName };
        }
        
        if (score > userProgress[topicName].puzzleScore)
        {
            userProgress[topicName].puzzleScore = score;
        }
        
        userProgress[topicName].puzzleCompleted = true;
        userProgress[topicName].lastAccessed = DateTime.Now;
        
        SaveProgress(topicName);
        UpdateOverallProgress();
        
        Debug.Log($"✓ Puzzle completed for {topicName} with score: {score}%");
        
        if (autoSync && !string.IsNullOrEmpty(adminApiUrl))
        {
            Debug.Log($"🔄 Syncing puzzle completion to database...");
            StartCoroutine(SyncToDatabase());
        }
    }
    
    // ADDED: Method to update lesson completion count
    public void UpdateLessonCount(string topicName, int lessonCount)
    {
        topicName = TopicNameConstants.Normalize(topicName);
        
        if (string.IsNullOrEmpty(currentUserEmail)) 
        {
            Debug.LogError("❌ No user logged in!");
            return;
        }
        
        if (!userProgress.ContainsKey(topicName))
        {
            userProgress[topicName] = new TopicProgress { topicName = topicName };
        }
        
        userProgress[topicName].lessonsCompleted = lessonCount;
        userProgress[topicName].lastAccessed = DateTime.Now;
        
        SaveProgress(topicName);
        
        Debug.Log($"✓ Lesson count updated for {topicName}: {lessonCount}");
        
        if (autoSync && !string.IsNullOrEmpty(adminApiUrl))
        {
            StartCoroutine(SyncToDatabase());
        }
    }
    
    public void AddTimeSpent(string topicName, float seconds)
    {
        topicName = TopicNameConstants.Normalize(topicName);
        
        if (string.IsNullOrEmpty(currentUserEmail)) return;
        
        if (!userProgress.ContainsKey(topicName))
        {
            userProgress[topicName] = new TopicProgress { topicName = topicName };
        }
        
        userProgress[topicName].timeSpent += seconds;
        userProgress[topicName].lastAccessed = DateTime.Now;
        SaveProgress(topicName);
    }
    
    void SaveProgress(string topicName)
    {
        if (!userProgress.ContainsKey(topicName)) return;
        
        TopicProgress progress = userProgress[topicName];
        
        PlayerPrefs.SetInt($"{currentUserEmail}_{topicName}_Tutorial", progress.tutorialCompleted ? 1 : 0);
        PlayerPrefs.SetInt($"{currentUserEmail}_{topicName}_Puzzle", progress.puzzleCompleted ? 1 : 0);
        PlayerPrefs.SetInt($"{currentUserEmail}_{topicName}_Score", progress.puzzleScore);
        PlayerPrefs.SetFloat($"{currentUserEmail}_{topicName}_TimeSpent", progress.timeSpent);
        PlayerPrefs.SetString($"{currentUserEmail}_{topicName}_LastAccessed", progress.lastAccessed.ToString());
        PlayerPrefs.SetInt($"{currentUserEmail}_{topicName}_LessonsCompleted", progress.lessonsCompleted); // ADDED
        PlayerPrefs.Save();
        
        Debug.Log($"✓ Saved progress for {topicName} ({progress.lessonsCompleted} lessons)");
    }
    
    void UpdateOverallProgress()
    {
        int totalCompleted = 0;
        foreach (var progress in userProgress.Values)
        {
            if (progress.IsCompleted()) totalCompleted++;
        }
        
        PlayerPrefs.SetInt($"User_{currentUserEmail}_CompletedTopics", totalCompleted);
        UpdateStreak();
        PlayerPrefs.Save();
    }
    
    void UpdateStreak()
    {
        string lastActivityDate = PlayerPrefs.GetString($"User_{currentUserEmail}_LastActivity", "");
        DateTime today = DateTime.Today;
        
        if (string.IsNullOrEmpty(lastActivityDate))
        {
            PlayerPrefs.SetInt($"User_{currentUserEmail}_Streak", 1);
        }
        else
        {
            DateTime lastDate = DateTime.Parse(lastActivityDate);
            int daysDifference = (today - lastDate).Days;
            
            if (daysDifference == 0)
            {
                // Same day, no change
            }
            else if (daysDifference == 1)
            {
                int currentStreak = PlayerPrefs.GetInt($"User_{currentUserEmail}_Streak", 0);
                PlayerPrefs.SetInt($"User_{currentUserEmail}_Streak", currentStreak + 1);
            }
            else
            {
                PlayerPrefs.SetInt($"User_{currentUserEmail}_Streak", 1);
            }
        }
        
        PlayerPrefs.SetString($"User_{currentUserEmail}_LastActivity", today.ToString());
    }
    
    IEnumerator SyncToDatabase()
    {
        if (string.IsNullOrEmpty(currentUsername) || string.IsNullOrEmpty(adminApiUrl))
        {
            Debug.LogWarning("⚠️ Cannot sync: username or API URL is empty");
            yield break;
        }
        
        Debug.Log($"🔄 Syncing progress for username: {currentUsername}");
        
        UserProgressData progressData = new UserProgressData
        {
            username = currentUsername,
            name = PlayerPrefs.GetString("User_" + currentUserEmail + "_Name", "Student"),
            email = currentUserEmail,
            streak = GetStreak(),
            completedTopics = PlayerPrefs.GetInt("User_" + currentUserEmail + "_CompletedTopics", 0),
            lastUpdated = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
            topics = new List<UserTopicData>()
        };
        
        foreach (var kvp in userProgress)
        {
            progressData.topics.Add(new UserTopicData
            {
                topicName = kvp.Value.topicName,
                tutorialCompleted = kvp.Value.tutorialCompleted,
                puzzleCompleted = kvp.Value.puzzleCompleted,
                puzzleScore = kvp.Value.puzzleScore,
                progressPercentage = kvp.Value.GetCompletionPercentage(),
                lastAccessed = kvp.Value.lastAccessed.ToString("yyyy-MM-dd HH:mm:ss"),
                timeSpent = kvp.Value.timeSpent,
                lessonsCompleted = kvp.Value.lessonsCompleted // CRITICAL: Include lesson count
            });
        }
        
        string jsonData = JsonUtility.ToJson(progressData, true);
        Debug.Log($"📤 Sending data to: {adminApiUrl}/progress/{currentUsername}");
        Debug.Log($"📦 Data: {jsonData}");
        
        UnityWebRequest request = new UnityWebRequest(adminApiUrl + "/progress/" + currentUsername, "PUT");
        request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(jsonData));
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        
        yield return request.SendWebRequest();
        
        if (request.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("✅ Progress synced to database successfully!");
            Debug.Log($"📥 Response: {request.downloadHandler.text}");
        }
        else
        {
            Debug.LogError($"❌ Database sync failed: {request.error}");
            Debug.LogError($"❌ Response Code: {request.responseCode}");
            Debug.LogError($"❌ Response: {request.downloadHandler.text}");
        }
    }
    
    public float GetOverallProgress()
    {
        if (userProgress.Count == 0) return 0f;
        float total = 0f;
        foreach (var p in userProgress.Values) total += p.GetCompletionPercentage();
        return total / userProgress.Count;
    }
    
    public float GetTopicProgress(string topicName)
    {
        topicName = TopicNameConstants.Normalize(topicName);
        return userProgress.ContainsKey(topicName) ? userProgress[topicName].GetCompletionPercentage() : 0f;
    }
    
    public bool IsTutorialCompleted(string topicName)
    {
        topicName = TopicNameConstants.Normalize(topicName);
        return userProgress.ContainsKey(topicName) && userProgress[topicName].tutorialCompleted;
    }
    
    public bool IsPuzzleCompleted(string topicName)
    {
        topicName = TopicNameConstants.Normalize(topicName);
        return userProgress.ContainsKey(topicName) && userProgress[topicName].puzzleCompleted;
    }
    
    public int GetPuzzleScore(string topicName)
    {
        topicName = TopicNameConstants.Normalize(topicName);
        return userProgress.ContainsKey(topicName) ? userProgress[topicName].puzzleScore : 0;
    }
    
    public float GetTimeSpent(string topicName)
    {
        topicName = TopicNameConstants.Normalize(topicName);
        return userProgress.ContainsKey(topicName) ? userProgress[topicName].timeSpent : 0f;
    }
    
    // ADDED: Get lesson count
    public int GetLessonCount(string topicName)
    {
        topicName = TopicNameConstants.Normalize(topicName);
        return userProgress.ContainsKey(topicName) ? userProgress[topicName].lessonsCompleted : 0;
    }
    
    public bool IsTopicUnlocked(string topicName)
    {
        topicName = TopicNameConstants.Normalize(topicName);
        
        if (topicOrder.Count == 0 || topicName == topicOrder[0]) return true;
        int index = topicOrder.IndexOf(topicName);
        if (index <= 0) return true;
        return GetTopicProgress(topicOrder[index - 1]) >= 50f;
    }
    
    public string GetCurrentUsername() => currentUsername;
    
    public string GetDisplayName()
    {
        return string.IsNullOrEmpty(currentUserEmail) ? "Student" : 
            PlayerPrefs.GetString($"User_{currentUserEmail}_Name", currentUsername);
    }
    
    public int GetStreak()
    {
        return PlayerPrefs.GetInt($"User_{currentUserEmail}_Streak", 0);
    }
    
    public void ManualSync()
    {
        if (!string.IsNullOrEmpty(adminApiUrl))
        {
            Debug.Log("🔄 Manual sync triggered");
            StartCoroutine(SyncToDatabase());
        }
        else
        {
            Debug.LogWarning("⚠️ Cannot sync: API URL is empty");
        }
    }
    
    /// <summary>
    /// CRITICAL: Call this on logout to clear ALL user data
    /// Ensures fresh start on next login
    /// </summary>
    public void ClearUserData(string userEmail)
    {
        Debug.Log($"🧹 UserProgressManager: Clearing data for {userEmail}");
        
        // Clear the initialization flag
        PlayerPrefs.DeleteKey($"UserProgressInitialized_{userEmail}");
        
        // Clear all topic progress
        foreach (string topic in TopicNameConstants.ALL_TOPICS)
        {
            PlayerPrefs.DeleteKey($"{userEmail}_{topic}_Tutorial");
            PlayerPrefs.DeleteKey($"{userEmail}_{topic}_Puzzle");
            PlayerPrefs.DeleteKey($"{userEmail}_{topic}_Score");
            PlayerPrefs.DeleteKey($"{userEmail}_{topic}_TimeSpent");
            PlayerPrefs.DeleteKey($"{userEmail}_{topic}_LastAccessed");
            PlayerPrefs.DeleteKey($"{userEmail}_{topic}_LessonsCompleted");
            
            // Also clear legacy "Queues" variant
            if (topic == TopicNameConstants.QUEUE)
            {
                PlayerPrefs.DeleteKey($"{userEmail}_Queues_Tutorial");
                PlayerPrefs.DeleteKey($"{userEmail}_Queues_Puzzle");
                PlayerPrefs.DeleteKey($"{userEmail}_Queues_Score");
                PlayerPrefs.DeleteKey($"{userEmail}_Queues_TimeSpent");
                PlayerPrefs.DeleteKey($"{userEmail}_Queues_LastAccessed");
                PlayerPrefs.DeleteKey($"{userEmail}_Queues_LessonsCompleted");
            }
        }
        
        // Clear overall progress
        PlayerPrefs.DeleteKey($"User_{userEmail}_CompletedTopics");
        PlayerPrefs.DeleteKey($"User_{userEmail}_Streak");
        PlayerPrefs.DeleteKey($"User_{userEmail}_LastActivity");
        
        PlayerPrefs.Save();
        
        Debug.Log($"✓ UserProgressManager data cleared for {userEmail}");
        
        // If this is current user, reset internal state
        if (userEmail == currentUserEmail)
        {
            userProgress.Clear();
            currentUsername = "";
            currentUserEmail = "";
            isInitialized = false;
            Debug.Log("✓ Internal state reset");
        }
    }

    public void CompleteARExploration(string topicName)
    {
        topicName = TopicNameConstants.Normalize(topicName);
        
        if (string.IsNullOrEmpty(currentUserEmail)) 
        {
            Debug.LogError("❌ No user logged in!");
            return;
        }
        
        if (!userProgress.ContainsKey(topicName))
        {
            userProgress[topicName] = new TopicProgress { topicName = topicName };
        }
        
        TopicProgress progress = userProgress[topicName];
        progress.lastAccessed = DateTime.Now;
        
        SaveProgress(topicName);
        UpdateOverallProgress();
        
        Debug.Log($"✓ AR Exploration completed for {topicName}");
        
        if (autoSync && !string.IsNullOrEmpty(adminApiUrl))
        {
            Debug.Log($"🔄 Syncing AR exploration to database...");
            StartCoroutine(SyncToDatabase());
        }
    }
    
    void OnApplicationQuit()
    {
        EndTopicSession();
        if (autoSync && !string.IsNullOrEmpty(adminApiUrl))
        {
            Debug.Log("🔄 Final sync on app quit...");
            StartCoroutine(SyncToDatabase());
        }
    }
}

// NOTE: Database classes should be in DatabaseClasses.cs - NOT HERE to avoid duplicates!