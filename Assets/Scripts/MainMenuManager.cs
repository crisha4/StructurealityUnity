using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;
using UnityEngine.Networking;
using System.Text;

public class MainMenuManager : MonoBehaviour
{
    [Header("Screens")]
    public GameObject homeScreen;
    public GameObject topicSelectionScreen;
    public GameObject queueModesScreen;
    
    [Header("Home Screen Elements")]
    public TextMeshProUGUI welcomeText;
    public TextMeshProUGUI streakText;
    public TextMeshProUGUI completedTopicsText;
    public TextMeshProUGUI overallProgressText;
    public Image overallProgressBar;
    public Image profilePictureImage;
    public Button profilePictureButton;
    
    [Header("Navigation Bar")]
    public Button homeButton;
    public Button learnButton;
    public Button progressButton;
    public Button profileButton;
    
    [Header("Quick Access Cards")]
    public Button continueQueueButton;
    public Button startNewTopicButton;
    public Button viewProgressButton;
    
    [Header("Topic Cards")]
    public Button queueTopicButton;
    public Button stackTopicButton;
    public Button linkedListTopicButton;
    public Button treeTopicButton;
    public Button graphTopicButton;
    
    [Header("Queue Mode Buttons")]
    public Button tutorialModeButton;
    public Button guidedModeButton;
    public Button challengeModeButton;
    public Button backToTopicsButton;
    
    [Header("Profile Picture Assets")]
    public Sprite[] profilePictureSprites;
    
    [Header("Profile Screen")]
    public GameObject profileEditScreen;
    public Image profileEditImage;
    public Button[] profileEditOptions;
    public Button saveProfileButton;
    public Button cancelProfileButton;
    public Button logoutButton;
    public TextMeshProUGUI profileNameText;
    public TextMeshProUGUI profileUsernameText;
    
    [Header("Settings")]
    public string queueSceneName = "QueueScene";
    public string loginSceneName = "LoginRegister";
    [Tooltip("Leave empty to disable cloud sync")]
    public string adminApiUrl = "https://structureality-admin.onrender.com/api";
    
    [Header("Loading")]
    public GameObject loadingIndicator; // NEW: Add a loading spinner/text
    
    // User data
    private string currentUsername;
    private string studentName;
    private int currentProfilePictureIndex;
    private int currentStreak;
    private int completedTopics;
    private int tempSelectedProfilePic;
    private bool isDataLoaded = false;
    
    void Start()
    {
        LoadUserData();
        InitializeUI();
        ShowHomeScreen();
        
        // NEW: Wait for data sync before displaying progress
        StartCoroutine(WaitForDataSyncAndUpdate());
        
        // Send activity update to admin (only if URL is set)
        if (!string.IsNullOrEmpty(adminApiUrl))
        {
            StartCoroutine(UpdateUserActivity());
        }
    }
    
    // NEW: Wait for UserProgressManager to finish syncing
    IEnumerator WaitForDataSyncAndUpdate()
    {
        Debug.Log("⏳ MainMenu: Waiting for UserProgressManager to sync data...");
        
        if (loadingIndicator != null)
            loadingIndicator.SetActive(true);
        
        // Wait for UserProgressManager to be ready
        while (UserProgressManager.Instance == null)
        {
            yield return new WaitForSeconds(0.1f);
        }
        
        Debug.Log("✓ UserProgressManager found");
        
        // Wait for data to be loaded (check if at least one topic has progress)
        float timeout = 5f;
        float elapsed = 0f;
        bool dataFound = false;
        
        while (elapsed < timeout && !dataFound)
        {
            // Check if any topic has non-zero progress
            foreach (string topic in TopicNameConstants.ALL_TOPICS)
            {
                float progress = UserProgressManager.Instance.GetTopicProgress(topic);
                if (progress > 0)
                {
                    dataFound = true;
                    Debug.Log($"✓ Found progress data: {topic} = {progress}%");
                    break;
                }
            }
            
            if (!dataFound)
            {
                yield return new WaitForSeconds(0.2f);
                elapsed += 0.2f;
            }
        }
        
        if (elapsed >= timeout && !dataFound)
        {
            Debug.LogWarning("⚠️ Timeout waiting for progress data - user may be new");
        }
        else
        {
            Debug.Log($"✅ Data loaded after {elapsed:F1}s");
        }
        
        if (loadingIndicator != null)
            loadingIndicator.SetActive(false);
        
        // Now update the display with the loaded data
        isDataLoaded = true;
        UpdateHomeScreenData();
    }
    
    void LoadUserData()
    {
        // Get current logged-in user (username)
        currentUsername = PlayerPrefs.GetString("CurrentUser", "");
        
        // Check if user is logged in
        if (string.IsNullOrEmpty(currentUsername))
        {
            Debug.Log("No user logged in - redirecting to login");
            SceneManager.LoadScene(loginSceneName);
            return;
        }
        
        // Load user data using username
        studentName = PlayerPrefs.GetString("User_" + currentUsername + "_Name", "Student");
        currentProfilePictureIndex = PlayerPrefs.GetInt("User_" + currentUsername + "_ProfilePic", 0);
        
        // Load basic data (will be updated after sync)
        if (UserProgressManager.Instance != null)
        {
            currentStreak = UserProgressManager.Instance.GetStreak();
        }
        else
        {
            currentStreak = PlayerPrefs.GetInt("User_" + currentUsername + "_Streak", 0);
        }
        
        completedTopics = PlayerPrefs.GetInt("User_" + currentUsername + "_CompletedTopics", 0);
        
        Debug.Log($"Loaded user: {studentName} (Username: {currentUsername})");
    }
    
    void InitializeUI()
    {
        // Navigation bar
        if (homeButton != null) homeButton.onClick.AddListener(ShowHomeScreen);
        if (learnButton != null) learnButton.onClick.AddListener(ShowTopicSelection);
        if (progressButton != null) progressButton.onClick.AddListener(ShowProgress);
        if (profileButton != null) profileButton.onClick.AddListener(ShowProfile);
        
        // Home screen quick access
        if (continueQueueButton != null) continueQueueButton.onClick.AddListener(ContinueQueue);
        if (startNewTopicButton != null) startNewTopicButton.onClick.AddListener(ShowTopicSelection);
        if (viewProgressButton != null) viewProgressButton.onClick.AddListener(ShowProgress);
        
        // Profile picture button
        if (profilePictureButton != null) 
            profilePictureButton.onClick.AddListener(ShowProfileEdit);
        
        // Topic selection cards
        if (queueTopicButton != null) queueTopicButton.onClick.AddListener(ShowQueueModes);
        if (stackTopicButton != null) stackTopicButton.onClick.AddListener(() => ShowComingSoon("Stacks"));
        if (linkedListTopicButton != null) linkedListTopicButton.onClick.AddListener(() => ShowComingSoon("Linked Lists"));
        if (treeTopicButton != null) treeTopicButton.onClick.AddListener(() => ShowComingSoon("Trees"));
        if (graphTopicButton != null) graphTopicButton.onClick.AddListener(() => ShowComingSoon("Graphs"));
        
        // Queue modes
        if (tutorialModeButton != null) tutorialModeButton.onClick.AddListener(StartTutorialMode);
        if (guidedModeButton != null) guidedModeButton.onClick.AddListener(StartGuidedMode);
        if (challengeModeButton != null) challengeModeButton.onClick.AddListener(StartChallengeMode);
        if (backToTopicsButton != null) backToTopicsButton.onClick.AddListener(ShowTopicSelection);
        
        // Profile edit buttons
        if (saveProfileButton != null) saveProfileButton.onClick.AddListener(SaveProfileChanges);
        if (cancelProfileButton != null) cancelProfileButton.onClick.AddListener(CancelProfileEdit);
        if (logoutButton != null) logoutButton.onClick.AddListener(Logout);
        
        // Profile picture selection in edit screen
        for (int i = 0; i < profileEditOptions.Length; i++)
        {
            int index = i;
            if (profileEditOptions[i] != null)
            {
                profileEditOptions[i].onClick.AddListener(() => SelectTempProfilePicture(index));
            }
        }
        
        // Initial display update (will show 0% until data loads)
        UpdateHomeScreenData();
        UpdateProfilePicture();
    }
    
    void UpdateHomeScreenData()
    {
        // Show the registered NAME in the welcome message
        if (welcomeText != null)
            welcomeText.text = $"Welcome, {studentName}!";
        
        if (streakText != null)
            streakText.text = $"{currentStreak} Days";
        
        if (completedTopicsText != null)
            completedTopicsText.text = $"{completedTopics}/5 Topics";
        
        // Show overall progress percentage
        if (UserProgressManager.Instance != null)
        {
            float overallProgress = UserProgressManager.Instance.GetOverallProgress();
            
            // Count completed topics (100% progress)
            completedTopics = 0;
            foreach (string topic in TopicNameConstants.ALL_TOPICS)
            {
                float topicProgress = UserProgressManager.Instance.GetTopicProgress(topic);
                if (topicProgress >= 100f)
                {
                    completedTopics++;
                }
                
                Debug.Log($"  {topic}: {topicProgress}%");
            }
            
            if (completedTopicsText != null)
                completedTopicsText.text = $"{completedTopics}/5 Topics";
            
            if (overallProgressText != null)
            {
                overallProgressText.text = $"{overallProgress:F0}%";
                Debug.Log($"📊 Overall Progress Display: {overallProgress:F0}%");
            }
            
            if (overallProgressBar != null)
            {
                overallProgressBar.fillAmount = overallProgress / 100f;
            }
        }
        else
        {
            Debug.LogWarning("⚠️ UserProgressManager not available yet");
            
            // Show placeholder until data loads
            if (overallProgressText != null)
                overallProgressText.text = "...";
        }
    }
    
    void UpdateProfilePicture()
    {
        if (profilePictureImage != null && profilePictureSprites != null && 
            currentProfilePictureIndex < profilePictureSprites.Length)
        {
            profilePictureImage.sprite = profilePictureSprites[currentProfilePictureIndex];
        }
    }
    
    IEnumerator UpdateUserActivity()
    {
        if (string.IsNullOrEmpty(adminApiUrl))
        {
            Debug.Log("Cloud sync disabled - no admin API URL set");
            yield break;
        }

        string jsonData = JsonUtility.ToJson(new UserActivityData
        {
            username = currentUsername,
            name = studentName,
            lastLogin = System.DateTime.Now.ToString(),
            streak = currentStreak,
            completedTopics = completedTopics
        });
        
        UnityWebRequest request = new UnityWebRequest(adminApiUrl + "/users/" + currentUsername, "PUT");
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        
        yield return request.SendWebRequest();
        
        if (request.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("✅ User activity updated to cloud");
        }
        else
        {
            Debug.LogWarning($"Failed to update user activity: {request.error}");
        }
    }
    
    // Screen Navigation
    void ShowHomeScreen()
    {
        HideAllScreens();
        if (homeScreen != null) homeScreen.SetActive(true);
        
        // Refresh progress when returning to home screen
        if (isDataLoaded)
        {
            UpdateHomeScreenData();
        }
    }
    
    void ShowTopicSelection()
    {
        HideAllScreens();
        if (topicSelectionScreen != null) topicSelectionScreen.SetActive(true);
    }
    
    void ShowQueueModes()
    {
        HideAllScreens();
        if (queueModesScreen != null) queueModesScreen.SetActive(true);
    }
    
    void ShowProgress()
    {
        Debug.Log("Progress screen - Coming soon!");
    }
    
    void ShowProfile()
    {
        ShowProfileEdit();
    }
    
    void ShowProfileEdit()
    {
        HideAllScreens();
        if (profileEditScreen != null) 
        {
            profileEditScreen.SetActive(true);
            
            if (profileNameText != null) profileNameText.text = studentName;
            if (profileUsernameText != null) profileUsernameText.text = currentUsername;
            
            tempSelectedProfilePic = currentProfilePictureIndex;
            UpdateProfileEditDisplay();
        }
    }
    
    void HideAllScreens()
    {
        if (homeScreen != null) homeScreen.SetActive(false);
        if (topicSelectionScreen != null) topicSelectionScreen.SetActive(false);
        if (queueModesScreen != null) queueModesScreen.SetActive(false);
        if (profileEditScreen != null) profileEditScreen.SetActive(false);
    }
    
    void SelectTempProfilePicture(int index)
    {
        tempSelectedProfilePic = index;
        UpdateProfileEditDisplay();
    }
    
    void UpdateProfileEditDisplay()
    {
        if (profileEditImage != null && profilePictureSprites != null && 
            tempSelectedProfilePic < profilePictureSprites.Length)
        {
            profileEditImage.sprite = profilePictureSprites[tempSelectedProfilePic];
        }
        
        for (int i = 0; i < profileEditOptions.Length; i++)
        {
            if (profileEditOptions[i] != null)
            {
                Transform border = profileEditOptions[i].transform.Find("SelectedBorder");
                if (border != null)
                {
                    border.gameObject.SetActive(i == tempSelectedProfilePic);
                }
            }
        }
    }
    
    void SaveProfileChanges()
    {
        currentProfilePictureIndex = tempSelectedProfilePic;
        PlayerPrefs.SetInt("User_" + currentUsername + "_ProfilePic", currentProfilePictureIndex);
        PlayerPrefs.Save();
        
        UpdateProfilePicture();
        ShowHomeScreen();
    }
    
    void CancelProfileEdit()
    {
        ShowHomeScreen();
    }
    
    void Logout()
    {
        // Clear current user
        PlayerPrefs.DeleteKey("CurrentUser");
        PlayerPrefs.DeleteKey("RememberMe");
        PlayerPrefs.DeleteKey("SavedUsername");
        
        PlayerPrefs.Save();
        
        Debug.Log("User logged out successfully");
        
        // Go back to login scene
        SceneManager.LoadScene(loginSceneName);
    }
    
    void ContinueQueue()
    {
        LoadQueueScene();
    }
    
    void StartTutorialMode()
    {
        PlayerPrefs.SetString("QueueMode", "Tutorial");
        LoadQueueScene();
    }
    
    void StartGuidedMode()
    {
        PlayerPrefs.SetString("QueueMode", "Guided");
        LoadQueueScene();
    }
    
    void StartChallengeMode()
    {
        PlayerPrefs.SetString("QueueMode", "Challenge");
        LoadQueueScene();
    }
    
    void LoadQueueScene()
    {
        if (Application.CanStreamedLevelBeLoaded(queueSceneName))
        {
            SceneManager.LoadScene(queueSceneName);
        }
        else
        {
            Debug.LogWarning($"Scene '{queueSceneName}' not found in build settings!");
        }
    }
    
    void ShowComingSoon(string topicName)
    {
        Debug.Log($"{topicName} - Coming Soon!");
    }
    
    // NEW: Manual refresh for testing
    public void RefreshProgress()
    {
        Debug.Log("🔄 Manual refresh triggered");
        UpdateHomeScreenData();
    }
}

[System.Serializable]
public class UserActivityData
{
    public string username;
    public string name;
    public string lastLogin;
    public int streak;
    public int completedTopics;
}