using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;
using UnityEngine.Networking;
using System.Text;

public class ProfilePanelManager : MonoBehaviour
{
    [Header("Profile Panel References")]
    public GameObject profilePanel;
    public GameObject homePanel;
    
    [Header("Profile Header")]
    public Image profilePictureDisplay;
    public Button avatarButton;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI usernameText;
    public TextMeshProUGUI streakText;
    public TextMeshProUGUI completedTopicsText;
    
    [Header("Profile Info Cards")]
    public TextMeshProUGUI usernameValueText;
    public TextMeshProUGUI emailValueText;
    
    [Header("Progress Display")]
    public TextMeshProUGUI queueProgressText;
    public TextMeshProUGUI stacksProgressText;
    public TextMeshProUGUI linkedListsProgressText;
    public TextMeshProUGUI treesProgressText;
    public TextMeshProUGUI graphsProgressText;
    
    [Header("Buttons")]
    public Button logoutButton;
    public Button backButton;
    public Button changePasswordButton;
    
    [Header("Change Password Panel")]
    public GameObject changePasswordPanel;
    public TMP_InputField currentPasswordInput;
    public TMP_InputField newPasswordInput;
    public TMP_InputField confirmPasswordInput;
    public Button confirmChangePasswordButton;
    public Button cancelChangePasswordButton;
    public TextMeshProUGUI passwordErrorText;
    
    [Header("Settings")]
    public string loginSceneName = "LoginRegister";
    public string serverUrl = "https://structureality-admin.onrender.com";
    
    [Header("Avatar Selection (Built-in)")]
    public GameObject avatarSelectionPanel;
    public Transform avatarGridContainer;
    public GameObject avatarButtonPrefab;
    public Button closeAvatarPanelButton;
    
    [Header("Preset Avatars")]
    public Sprite[] presetAvatarSprites;
    public Sprite defaultProfileSprite;
    public Color defaultProfileColor = new Color(0.4f, 0.8f, 1f);
    
    // User data
    private string currentUsername;
    private string studentName;
    private string email;
    private int currentStreak;
    private int completedTopics;
    
    void Start()
    {
        LoadUserData();
        SetupButtons();
        UpdateProfileDisplay();
        
        // Hide panels initially
        if (avatarSelectionPanel != null)
        {
            avatarSelectionPanel.SetActive(false);
        }
        
        if (changePasswordPanel != null)
        {
            changePasswordPanel.SetActive(false);
        }
    }
    
    void LoadUserData()
    {
        // Get current logged-in user
        currentUsername = PlayerPrefs.GetString("CurrentUser", "");
        
        if (string.IsNullOrEmpty(currentUsername))
        {
            Debug.LogWarning("No user logged in!");
            SceneManager.LoadScene(loginSceneName);
            return;
        }
        
        // Always fetch fresh data from server to avoid stale local data
        StartCoroutine(FetchUserDataFromServer());
        
        // Load temporary local data while waiting for server response
        studentName = PlayerPrefs.GetString("User_" + currentUsername + "_Name", currentUsername);
        email = PlayerPrefs.GetString("User_" + currentUsername + "_Email", "");
        currentStreak = PlayerPrefs.GetInt("User_" + currentUsername + "_Streak", 0);
        completedTopics = PlayerPrefs.GetInt("User_" + currentUsername + "_CompletedTopics", 0);
        
        Debug.Log($"Loading profile for: {currentUsername}");
    }
    
    void SetupButtons()
    {
        if (logoutButton != null)
        {
            logoutButton.onClick.RemoveAllListeners();
            logoutButton.onClick.AddListener(OnLogoutClicked);
        }
        
        if (backButton != null)
        {
            backButton.onClick.RemoveAllListeners();
            backButton.onClick.AddListener(OnBackClicked);
        }
        
        if (avatarButton != null)
        {
            avatarButton.onClick.RemoveAllListeners();
            avatarButton.onClick.AddListener(OnAvatarClicked);
        }
        
        if (closeAvatarPanelButton != null)
        {
            closeAvatarPanelButton.onClick.RemoveAllListeners();
            closeAvatarPanelButton.onClick.AddListener(CloseAvatarSelection);
        }
        
        if (changePasswordButton != null)
        {
            changePasswordButton.onClick.RemoveAllListeners();
            changePasswordButton.onClick.AddListener(OnChangePasswordClicked);
        }
        
        if (confirmChangePasswordButton != null)
        {
            confirmChangePasswordButton.onClick.RemoveAllListeners();
            confirmChangePasswordButton.onClick.AddListener(OnConfirmChangePassword);
        }
        
        if (cancelChangePasswordButton != null)
        {
            cancelChangePasswordButton.onClick.RemoveAllListeners();
            cancelChangePasswordButton.onClick.AddListener(OnCancelChangePassword);
        }
    }
    
    void UpdateProfileDisplay()
    {
        // Update name and username in header
        if (nameText != null)
        {
            nameText.text = studentName;
        }
        
        if (usernameText != null)
        {
            usernameText.text = currentUsername;
        }
        
        // Update header stats
        if (streakText != null)
        {
            streakText.text = $"{currentStreak} Days";
        }
        
        if (completedTopicsText != null)
        {
            completedTopicsText.text = $"{completedTopics}/5 Topics";
        }
        
        // Update profile info cards
        if (usernameValueText != null)
        {
            usernameValueText.text = currentUsername;
        }
        
        if (emailValueText != null)
        {
            emailValueText.text = email;
        }
        
        // Load and display profile picture
        LoadProfilePicture();
        
        // Update progress for each topic
        UpdateTopicProgress("Queue", queueProgressText);
        UpdateTopicProgress("Stacks", stacksProgressText);
        UpdateTopicProgress("LinkedLists", linkedListsProgressText);
        UpdateTopicProgress("Trees", treesProgressText);
        UpdateTopicProgress("Graphs", graphsProgressText);
    }
    
    void LoadProfilePicture()
    {
        if (profilePictureDisplay == null)
        {
            Debug.LogWarning("Profile picture display not assigned!");
            return;
        }
        
        // Get saved avatar index for current user
        int avatarIndex = PlayerPrefs.GetInt($"ProfilePic_{currentUsername}", -1);
        
        if (avatarIndex >= 0 && presetAvatarSprites != null && avatarIndex < presetAvatarSprites.Length)
        {
            // Load saved avatar
            profilePictureDisplay.sprite = presetAvatarSprites[avatarIndex];
            profilePictureDisplay.color = Color.white;
            Debug.Log($"✓ Loaded profile picture for {currentUsername}: Avatar {avatarIndex}");
        }
        else
        {
            // Use default
            if (defaultProfileSprite != null)
            {
                profilePictureDisplay.sprite = defaultProfileSprite;
            }
            profilePictureDisplay.color = defaultProfileColor;
            Debug.Log($"Using default profile picture for {currentUsername}");
        }
    }
    
    void OnAvatarClicked()
    {
        Debug.Log("Avatar clicked - opening avatar selection");
        
        if (avatarSelectionPanel != null)
        {
            avatarSelectionPanel.SetActive(true);
            GenerateAvatarButtons();
        }
        else
        {
            Debug.LogError("Avatar Selection Panel not assigned!");
        }
    }
    
    void CloseAvatarSelection()
    {
        if (avatarSelectionPanel != null)
        {
            avatarSelectionPanel.SetActive(false);
        }
    }
    
    void GenerateAvatarButtons()
    {
        if (avatarGridContainer == null || avatarButtonPrefab == null)
        {
            Debug.LogError("Avatar grid container or button prefab not assigned!");
            return;
        }
        
        if (presetAvatarSprites == null || presetAvatarSprites.Length == 0)
        {
            Debug.LogError("No avatar sprites assigned!");
            return;
        }
        
        // Clear existing buttons
        foreach (Transform child in avatarGridContainer)
        {
            Destroy(child.gameObject);
        }
        
        // Get current avatar index
        int currentAvatarIndex = PlayerPrefs.GetInt($"ProfilePic_{currentUsername}", -1);
        
        // Create button for each preset avatar
        for (int i = 0; i < presetAvatarSprites.Length; i++)
        {
            if (presetAvatarSprites[i] == null) continue;
            
            int index = i; // Capture for closure
            GameObject avatarBtn = Instantiate(avatarButtonPrefab, avatarGridContainer);
            avatarBtn.name = $"AvatarButton_{i}";
            
            // Find and set the Image component
            Image avatarImage = avatarBtn.GetComponentInChildren<Image>();
            if (avatarImage != null)
            {
                avatarImage.sprite = presetAvatarSprites[i];
                avatarImage.preserveAspect = true;
                avatarImage.color = Color.white;
            }
            
            // Add button listener
            Button button = avatarBtn.GetComponent<Button>();
            if (button != null)
            {
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => SelectAvatar(index));
            }
            
            // Highlight if currently selected
            if (index == currentAvatarIndex)
            {
                Outline outline = avatarBtn.GetComponent<Outline>();
                if (outline == null)
                {
                    outline = avatarBtn.AddComponent<Outline>();
                }
                outline.enabled = true;
                outline.effectColor = Color.yellow;
                outline.effectDistance = new Vector2(3, 3);
            }
        }
        
        // Force layout update
        Canvas.ForceUpdateCanvases();
    }
    
    void SelectAvatar(int avatarIndex)
    {
        if (avatarIndex < 0 || avatarIndex >= presetAvatarSprites.Length)
        {
            Debug.LogError($"Invalid avatar index: {avatarIndex}");
            return;
        }
        
        // Save avatar for current user
        PlayerPrefs.SetInt($"ProfilePic_{currentUsername}", avatarIndex);
        PlayerPrefs.Save();
        
        Debug.Log($"✓ Avatar {avatarIndex} saved for {currentUsername}");
        
        // Update display
        LoadProfilePicture();
        
        // Close panel
        CloseAvatarSelection();
    }
    
    void OnChangePasswordClicked()
    {
        Debug.Log("Change Password clicked");
        
        if (changePasswordPanel != null)
        {
            changePasswordPanel.SetActive(true);
            
            // Clear input fields
            if (currentPasswordInput != null) currentPasswordInput.text = "";
            if (newPasswordInput != null) newPasswordInput.text = "";
            if (confirmPasswordInput != null) confirmPasswordInput.text = "";
            if (passwordErrorText != null) passwordErrorText.text = "";
        }
        else
        {
            Debug.LogError("Change Password Panel not assigned!");
        }
    }
    
    void OnConfirmChangePassword()
    {
        if (passwordErrorText != null)
        {
            passwordErrorText.text = "";
        }
        
        // Validate inputs
        string currentPassword = currentPasswordInput?.text ?? "";
        string newPassword = newPasswordInput?.text ?? "";
        string confirmPassword = confirmPasswordInput?.text ?? "";
        
        // Check if all fields are filled
        if (string.IsNullOrEmpty(currentPassword) || string.IsNullOrEmpty(newPassword) || string.IsNullOrEmpty(confirmPassword))
        {
            ShowPasswordError("All fields are required!");
            return;
        }
        
        // Check if new passwords match
        if (newPassword != confirmPassword)
        {
            ShowPasswordError("New passwords do not match!");
            return;
        }
        
        // Validate new password length
        if (newPassword.Length < 6)
        {
            ShowPasswordError("Password must be at least 6 characters!");
            return;
        }
        
        // Check if new password is different from current
        if (newPassword == currentPassword)
        {
            ShowPasswordError("New password must be different from current password!");
            return;
        }
        
        // Disable button while processing
        if (confirmChangePasswordButton != null)
        {
            confirmChangePasswordButton.interactable = false;
        }
        
        // Verify password with server
        StartCoroutine(ChangePasswordOnServer(currentPassword, newPassword));
    }
    
    IEnumerator ChangePasswordOnServer(string currentPassword, string newPassword)
    {
        string url = $"{serverUrl}/api/users/{currentUsername}/change-password";
        
        // Create JSON payload
        string jsonData = $@"{{
            ""currentPassword"": ""{currentPassword}"",
            ""newPassword"": ""{newPassword}""
        }}";
        
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);
        
        using (UnityWebRequest request = new UnityWebRequest(url, "PUT"))
        {
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.timeout = 10;
            
            yield return request.SendWebRequest();
            
            // Re-enable button
            if (confirmChangePasswordButton != null)
            {
                confirmChangePasswordButton.interactable = true;
            }
            
            if (request.result == UnityWebRequest.Result.Success)
            {
                Debug.Log($"✓ Password changed successfully for {currentUsername}");
                
                // Show success message
                if (passwordErrorText != null)
                {
                    passwordErrorText.text = "<color=green>Password changed successfully!</color>";
                }
                
                // Close panel after a short delay
                Invoke("OnCancelChangePassword", 1.5f);
            }
            else
            {
                // Get the response text
                string responseText = request.downloadHandler.text;
                Debug.LogError($"❌ Password change failed!");
                Debug.LogError($"Status Code: {request.responseCode}");
                Debug.LogError($"Error: {request.error}");
                Debug.LogError($"Response: {responseText}");
                
                // Parse error message
                string errorMsg = "Failed to change password";
                
                try
                {
                    // Try to parse JSON response
                    ServerResponse response = JsonUtility.FromJson<ServerResponse>(responseText);
                    if (!string.IsNullOrEmpty(response.error))
                    {
                        errorMsg = response.error;
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"Could not parse error response: {e.Message}");
                    // Check for common error keywords in raw response
                    if (responseText.ToLower().Contains("incorrect") || responseText.ToLower().Contains("wrong"))
                    {
                        errorMsg = "Current password is incorrect";
                    }
                    else if (responseText.ToLower().Contains("not found"))
                    {
                        errorMsg = "User not found";
                    }
                }
                
                ShowPasswordError(errorMsg);
            }
        }
    }
    
    void OnCancelChangePassword()
    {
        if (changePasswordPanel != null)
        {
            changePasswordPanel.SetActive(false);
        }
        
        // Clear inputs
        if (currentPasswordInput != null) currentPasswordInput.text = "";
        if (newPasswordInput != null) newPasswordInput.text = "";
        if (confirmPasswordInput != null) confirmPasswordInput.text = "";
        if (passwordErrorText != null) passwordErrorText.text = "";
    }
    
    void ShowPasswordError(string message)
    {
        if (passwordErrorText != null)
        {
            passwordErrorText.text = $"<color=red>{message}</color>";
        }
        Debug.LogWarning($"Password change error: {message}");
    }
    
    void UpdateTopicProgress(string topic, TextMeshProUGUI progressText)
    {
        if (progressText == null) return;
        
        bool tutorialCompleted = PlayerPrefs.GetInt($"User_{currentUsername}_{topic}_TutorialCompleted", 0) == 1;
        bool puzzleCompleted = PlayerPrefs.GetInt($"User_{currentUsername}_{topic}_PuzzleCompleted", 0) == 1;
        int score = PlayerPrefs.GetInt($"User_{currentUsername}_{topic}_Score", 0);
        
        string status = "";
        if (puzzleCompleted)
        {
            status = $"✓ Completed - Score: {score}";
        }
        else if (tutorialCompleted)
        {
            status = $"Tutorial Done - Score: {score}";
        }
        else
        {
            status = "Not Started";
        }
        
        progressText.text = $"{topic}: {status}";
    }
    
    void OnLogoutClicked()
    {
        Debug.Log("Logging out user: " + currentUsername);
        
        // Clear current user session
        PlayerPrefs.DeleteKey("CurrentUser");
        
        // Clear device-specific remember me settings
        string deviceId = GetDeviceId();
        PlayerPrefs.DeleteKey("RememberMeEnabled_" + deviceId);
        PlayerPrefs.DeleteKey("RememberedEmail_" + deviceId);
        
        PlayerPrefs.Save();
        
        Debug.Log("✅ User logged out successfully");
        
        // Go to login scene
        SceneManager.LoadScene(loginSceneName);
    }
    
    void OnBackClicked()
    {
        // Hide profile panel and show home panel
        if (profilePanel != null)
        {
            profilePanel.SetActive(false);
        }
        
        if (homePanel != null)
        {
            homePanel.SetActive(true);
        }
    }
    
    string GetDeviceId()
    {
        string key = "DeviceUniqueID";
        
        if (!PlayerPrefs.HasKey(key))
        {
            string newId = System.Guid.NewGuid().ToString();
            PlayerPrefs.SetString(key, newId);
            PlayerPrefs.Save();
        }
        
        return PlayerPrefs.GetString(key);
    }
    
    // Public method to show profile panel (call from navigation)
    public void ShowProfile()
    {
        if (profilePanel != null)
        {
            profilePanel.SetActive(true);
        }
        
        if (homePanel != null)
        {
            homePanel.SetActive(false);
        }
        
        LoadUserData();
        UpdateProfileDisplay();
    }
    
    // Call this when avatar selection is completed
    public void OnAvatarChanged()
    {
        LoadProfilePicture();
        Debug.Log("✓ Profile avatar updated");
    }
    
    // Helper method to get current user's avatar index
    public int GetCurrentAvatarIndex()
    {
        return PlayerPrefs.GetInt($"ProfilePic_{currentUsername}", -1);
    }
    
    IEnumerator FetchUserDataFromServer()
    {
        string url = $"{serverUrl}/api/users/{currentUsername}";
        
        Debug.Log($"🔄 Fetching fresh user data from server for: {currentUsername}");
        
        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            yield return request.SendWebRequest();
            
            if (request.result == UnityWebRequest.Result.Success)
            {
                var userData = JsonUtility.FromJson<UserDataResponse>(request.downloadHandler.text);
                
                // Update with fresh server data
                studentName = userData.name;
                email = userData.email;
                currentStreak = userData.streak;
                completedTopics = userData.completedTopics;
                
                // Update local cache with server data
                PlayerPrefs.SetString("User_" + currentUsername + "_Name", userData.name);
                PlayerPrefs.SetString("User_" + currentUsername + "_Email", userData.email);
                PlayerPrefs.SetInt("User_" + currentUsername + "_Streak", userData.streak);
                PlayerPrefs.SetInt("User_" + currentUsername + "_CompletedTopics", userData.completedTopics);
                PlayerPrefs.Save();
                
                Debug.Log($"✓ Fetched user data from server:");
                Debug.Log($"  Name: {studentName}");
                Debug.Log($"  Email: {email}");
                Debug.Log($"  Streak: {currentStreak}");
                Debug.Log($"  Completed Topics: {completedTopics}");
                
                // Update display with fresh data
                UpdateProfileDisplay();
                
                // Also sync progress data
                yield return FetchProgressFromServer();
            }
            else
            {
                Debug.LogWarning($"Failed to fetch user data from server: {request.error}");
                Debug.LogWarning($"Response code: {request.responseCode}");
            }
        }
    }
    
    IEnumerator FetchProgressFromServer()
    {
        string url = $"{serverUrl}/api/progress/{currentUsername}";
        
        Debug.Log($"🔄 Fetching progress data from server for: {currentUsername}");
        
        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            yield return request.SendWebRequest();
            
            if (request.result == UnityWebRequest.Result.Success)
            {
                try
                {
                    var progressData = JsonUtility.FromJson<ProgressDataResponse>(request.downloadHandler.text);
                    
                    if (progressData != null && progressData.success && progressData.data != null)
                    {
                        // Clear old local progress data first
                        ClearLocalProgress();
                        
                        // Update with server progress
                        foreach (var topic in progressData.data.topics)
                        {
                            PlayerPrefs.SetInt($"User_{currentUsername}_{topic.topicName}_TutorialCompleted", topic.tutorialCompleted ? 1 : 0);
                            PlayerPrefs.SetInt($"User_{currentUsername}_{topic.topicName}_PuzzleCompleted", topic.puzzleCompleted ? 1 : 0);
                            PlayerPrefs.SetInt($"User_{currentUsername}_{topic.topicName}_Score", topic.puzzleScore);
                        }
                        
                        PlayerPrefs.Save();
                        
                        Debug.Log($"✓ Progress synced from server for {currentUsername}");
                        
                        // Update display
                        UpdateProfileDisplay();
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"Error parsing progress data: {e.Message}");
                }
            }
            else
            {
                Debug.LogWarning($"Failed to fetch progress from server: {request.error}");
            }
        }
    }
    
    void ClearLocalProgress()
    {
        Debug.Log($"🧹 Clearing local progress for: {currentUsername}");
        
        string[] topics = { "Queue", "Stacks", "LinkedLists", "Trees", "Graphs", "Queues" };
        
        foreach (string topic in topics)
        {
            PlayerPrefs.DeleteKey($"User_{currentUsername}_{topic}_TutorialCompleted");
            PlayerPrefs.DeleteKey($"User_{currentUsername}_{topic}_PuzzleCompleted");
            PlayerPrefs.DeleteKey($"User_{currentUsername}_{topic}_Score");
            PlayerPrefs.DeleteKey($"{currentUsername}_{topic}_Tutorial");
            PlayerPrefs.DeleteKey($"{currentUsername}_{topic}_Puzzle");
            PlayerPrefs.DeleteKey($"{currentUsername}_{topic}_Score");
            PlayerPrefs.DeleteKey($"{currentUsername}_{topic}_TimeSpent");
            PlayerPrefs.DeleteKey($"{currentUsername}_{topic}_LastAccessed");
        }
        
        PlayerPrefs.Save();
    }
    
    // JSON Response Classes
    [System.Serializable]
    public class UserDataResponse
    {
        public string username;
        public string name;
        public string email;
        public int streak;
        public int completedTopics;
    }
    
    [System.Serializable]
    public class ProgressDataResponse
    {
        public bool success;
        public ProgressData data;
    }
    
    [System.Serializable]
    public class ProgressData
    {
        public string username;
        public TopicProgress[] topics;
    }
    
    [System.Serializable]
    public class TopicProgress
    {
        public string topicName;
        public bool tutorialCompleted;
        public bool puzzleCompleted;
        public int puzzleScore;
        public float progressPercentage;
    }
    
    [System.Serializable]
    public class ServerResponse
    {
        public bool success;
        public string message;
        public string error;
    }
}