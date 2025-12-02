using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;
using UnityEngine.Networking;
using System.Text;

public class LoginRegisterManager : MonoBehaviour
{
    [Header("Login Panel")]
    public GameObject loginPanel;
    public TMP_InputField emailField;
    public TMP_InputField passwordField;
    public Button togglePasswordButton;
    public Button loginButton;
    public Button forgotPasswordButton;
    public Button goToRegisterButton;
    public TextMeshProUGUI errorText;

    [Header("Register Panel")]
    public GameObject registerPanel;
    public TMP_InputField registerNameField;
    public TMP_InputField registerUsernameField;
    public TMP_InputField registerEmailField;
    public TMP_InputField registerPasswordField;
    public Button toggleRegisterPasswordButton;
    public TMP_InputField confirmPasswordField;
    public Button toggleConfirmPasswordButton;
    public Button registerButton;
    public Button goToLoginButton;
    public TextMeshProUGUI registerErrorText;

    [Header("Settings")]
    public string mainMenuSceneName = "MainMenu";
    public int minimumPasswordLength = 6;
    
    [Header("Server Settings")]
    [Tooltip("Your server URL - deployed server or local testing")]
    public string serverUrl = "https://structureality-admin.onrender.com";
    public bool enableDebugMode = true;

    private const string CURRENT_USER_KEY = "CurrentUser";

    void Start()
    {
        // CRITICAL: Clear stored user to prevent auto-login
        PlayerPrefs.DeleteKey("CurrentUser");
        PlayerPrefs.Save();
        
        // FIXED: Clear input fields on start to prevent auto-fill
        ClearAllInputFields();
        
        HideAllErrors();

        if (!ValidateRequiredFields())
        {
            Debug.LogError("LoginRegisterManager: Missing required UI references!");
            return;
        }

        ShowLoginPanel();
        SetupButtonListeners();
        SetupEnterKeySupport();
        SetupPasswordFields();

        if (enableDebugMode)
            Debug.Log("LoginRegisterManager initialized - Database Mode");
    }

    void ClearAllInputFields()
    {
        // FIXED: Clear all input fields to prevent auto-population
        if (emailField != null) emailField.text = "";
        if (passwordField != null) passwordField.text = "";
        if (registerNameField != null) registerNameField.text = "";
        if (registerUsernameField != null) registerUsernameField.text = "";
        if (registerEmailField != null) registerEmailField.text = "";
        if (registerPasswordField != null) registerPasswordField.text = "";
        if (confirmPasswordField != null) confirmPasswordField.text = "";
    }

    void HideAllErrors()
    {
        if (errorText != null) errorText.gameObject.SetActive(false);
        if (registerErrorText != null) registerErrorText.gameObject.SetActive(false);
    }

    bool ValidateRequiredFields()
    {
        return loginPanel != null && registerPanel != null && 
               emailField != null && passwordField != null;
    }

    void SetupButtonListeners()
    {
        if (loginButton != null)
            loginButton.onClick.AddListener(AttemptLogin);
        
        if (forgotPasswordButton != null)
            forgotPasswordButton.onClick.AddListener(OnForgotPassword);
        
        if (registerButton != null)
            registerButton.onClick.AddListener(AttemptRegister);

        if (goToRegisterButton != null)
            goToRegisterButton.onClick.AddListener(ShowRegisterPanel);

        if (goToLoginButton != null)
            goToLoginButton.onClick.AddListener(ShowLoginPanel);

        // Password visibility toggles
        if (togglePasswordButton != null)
            togglePasswordButton.onClick.AddListener(() => TogglePasswordVisibility(passwordField, togglePasswordButton));
        
        if (toggleRegisterPasswordButton != null)
            toggleRegisterPasswordButton.onClick.AddListener(() => TogglePasswordVisibility(registerPasswordField, toggleRegisterPasswordButton));
        
        if (toggleConfirmPasswordButton != null)
            toggleConfirmPasswordButton.onClick.AddListener(() => TogglePasswordVisibility(confirmPasswordField, toggleConfirmPasswordButton));
    }

    void SetupEnterKeySupport()
    {
        if (emailField != null)
            emailField.onSubmit.AddListener(delegate { AttemptLogin(); });
        if (passwordField != null)
            passwordField.onSubmit.AddListener(delegate { AttemptLogin(); });
        if (confirmPasswordField != null)
            confirmPasswordField.onSubmit.AddListener(delegate { AttemptRegister(); });
    }

    void SetupPasswordFields()
    {
        // Ensure password fields are set to password mode
        if (passwordField != null)
            passwordField.contentType = TMP_InputField.ContentType.Password;
        
        if (registerPasswordField != null)
            registerPasswordField.contentType = TMP_InputField.ContentType.Password;
        
        if (confirmPasswordField != null)
            confirmPasswordField.contentType = TMP_InputField.ContentType.Password;

        // Update toggle button icons
        UpdatePasswordButtonIcon(togglePasswordButton, false);
        UpdatePasswordButtonIcon(toggleRegisterPasswordButton, false);
        UpdatePasswordButtonIcon(toggleConfirmPasswordButton, false);
    }

    void TogglePasswordVisibility(TMP_InputField inputField, Button toggleButton)
    {
        if (inputField == null) return;

        bool isVisible = inputField.contentType == TMP_InputField.ContentType.Standard;
        
        if (isVisible)
        {
            // Hide password
            inputField.contentType = TMP_InputField.ContentType.Password;
        }
        else
        {
            // Show password
            inputField.contentType = TMP_InputField.ContentType.Standard;
        }

        inputField.ForceLabelUpdate();
        UpdatePasswordButtonIcon(toggleButton, !isVisible);
    }

    void UpdatePasswordButtonIcon(Button button, bool isVisible)
    {
        if (button == null) return;

        var buttonText = button.GetComponentInChildren<TextMeshProUGUI>();
        if (buttonText != null)
        {
            // Use eye icons: 👁 for show, 👁‍🗨 for hide
            buttonText.text = isVisible ? "👁" : "👁‍🗨";
        }
    }

    #region Panel Navigation

    public void ShowLoginPanel()
    {
        if (loginPanel != null) loginPanel.SetActive(true);
        if (registerPanel != null) registerPanel.SetActive(false);
        if (errorText != null) errorText.gameObject.SetActive(false);
    }

    public void ShowRegisterPanel()
    {
        if (loginPanel != null) loginPanel.SetActive(false);
        if (registerPanel != null) registerPanel.SetActive(true);
        if (registerErrorText != null) registerErrorText.gameObject.SetActive(false);
    }

    #endregion

    #region Login System

    void AttemptLogin()
    {
        string emailOrUsername = emailField?.text.Trim() ?? "";
        string password = passwordField?.text ?? "";

        if (string.IsNullOrEmpty(emailOrUsername))
        {
            ShowLoginError("Please enter your email or username");
            return;
        }

        if (string.IsNullOrEmpty(password))
        {
            ShowLoginError("Please enter your password");
            return;
        }

        if (string.IsNullOrEmpty(serverUrl))
        {
            ShowLoginError("Server URL not configured!");
            return;
        }

        // Show loading
        if (loginButton != null)
        {
            var buttonText = loginButton.GetComponentInChildren<TextMeshProUGUI>();
            if (buttonText != null) buttonText.text = "Logging in...";
            loginButton.interactable = false;
        }

        StartCoroutine(CloudLogin(emailOrUsername, password));
    }

    IEnumerator CloudLogin(string emailOrUsername, string password)
    {
        string url = serverUrl + "/api/login";
        
        string jsonData = $@"{{
            ""username"": ""{emailOrUsername}"",
            ""email"": ""{emailOrUsername}"",
            ""password"": ""{password}""
        }}";

        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);

        using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
        {
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.timeout = 10;

            yield return request.SendWebRequest();

            // Reset button
            if (loginButton != null)
            {
                var buttonText = loginButton.GetComponentInChildren<TextMeshProUGUI>();
                if (buttonText != null) buttonText.text = "Log In";
                loginButton.interactable = true;
            }

            if (request.result == UnityWebRequest.Result.Success)
            {
                var response = JsonUtility.FromJson<LoginResponse>(request.downloadHandler.text);
                
                if (response.success)
                {
                    // CRITICAL: Clear ALL local data before setting new user
                    ClearAllUserData();
                    
                    // Save ONLY current session info - server is source of truth
                    PlayerPrefs.SetString(CURRENT_USER_KEY, response.user.username);
                    PlayerPrefs.SetString("User_" + response.user.username + "_Name", response.user.name);
                    PlayerPrefs.SetString("User_" + response.user.username + "_Email", response.user.email);
                    PlayerPrefs.Save();

                    // Initialize UserProgressManager for this user
                    if (UserProgressManager.Instance != null)
                    {
                        UserProgressManager.Instance.InitializeForUser(response.user.username);
                    }

                    if (enableDebugMode)
                        Debug.Log("✅ Login successful: " + response.user.username);

                    SceneManager.LoadScene(mainMenuSceneName);
                }
                else
                {
                    ShowLoginError("Login failed. Please try again.");
                }
            }
            else
            {
                string errorMsg = request.downloadHandler.text;
                
                if (errorMsg.Contains("User not found"))
                    ShowLoginError("Account not found. Please register first.");
                else if (errorMsg.Contains("Incorrect password"))
                    ShowLoginError("Incorrect password. Please try again.");
                else
                    ShowLoginError("Login failed. Check your internet connection.");
                
                if (enableDebugMode)
                    Debug.LogWarning("Login error: " + request.error);
            }
        }
    }

void ClearAllUserData()
{
    Debug.Log("🧹 Clearing ALL local user data to ensure fresh start");
    
    // Get list of all possible usernames that might have data
    string[] possibleUsers = new string[] { 
        PlayerPrefs.GetString(CURRENT_USER_KEY, "")
    };
    
    // Clear all user-specific keys
    foreach (string user in possibleUsers)
    {
        if (string.IsNullOrEmpty(user)) continue;
        
        ClearUserSpecificData(user);
        
        // CRITICAL FIX: Also clear UserProgressManager data
        if (UserProgressManager.Instance != null)
        {
            UserProgressManager.Instance.ClearUserData(user);
        }
    }
    
    // Clear current user key
    PlayerPrefs.DeleteKey(CURRENT_USER_KEY);
    
    PlayerPrefs.Save();
    
    Debug.Log("✓ All local user data cleared");
}

void ClearUserSpecificData(string username)
{
    if (string.IsNullOrEmpty(username)) return;
    
    Debug.Log($"🧹 Clearing data for user: {username}");
    
    // Clear user info
    PlayerPrefs.DeleteKey("User_" + username + "_Name");
    PlayerPrefs.DeleteKey("User_" + username + "_Email");
    PlayerPrefs.DeleteKey("User_" + username + "_Password");
    PlayerPrefs.DeleteKey("User_" + username + "_Streak");
    PlayerPrefs.DeleteKey("User_" + username + "_CompletedTopics");
    PlayerPrefs.DeleteKey("User_" + username + "_LastActivity");
    PlayerPrefs.DeleteKey("User_" + username + "_Username");
    
    // Clear initialization flag
    PlayerPrefs.DeleteKey($"UserProgressInitialized_{username}");
    
    // Clear lesson completion data
    PlayerPrefs.DeleteKey($"CompletedLessons_{username}");
    
    // Clear topic progress for all variations
    string[] topics = { "Queue", "Queues", "Stacks", "Stack", "LinkedLists", "Linked Lists", "Trees", "Tree", "Graphs", "Graph" };
    foreach (string topic in topics)
    {
        // Old format
        PlayerPrefs.DeleteKey($"TopicReadComplete_{username}_{topic}");
        PlayerPrefs.DeleteKey($"User_{username}_{topic}_TutorialCompleted");
        PlayerPrefs.DeleteKey($"User_{username}_{topic}_PuzzleCompleted");
        PlayerPrefs.DeleteKey($"User_{username}_{topic}_Score");
        
        // UserProgressManager format
        PlayerPrefs.DeleteKey($"{username}_{topic}_Tutorial");
        PlayerPrefs.DeleteKey($"{username}_{topic}_Puzzle");
        PlayerPrefs.DeleteKey($"{username}_{topic}_Score");
        PlayerPrefs.DeleteKey($"{username}_{topic}_TimeSpent");
        PlayerPrefs.DeleteKey($"{username}_{topic}_LastAccessed");
        PlayerPrefs.DeleteKey($"{username}_{topic}_LessonsCompleted");
        
        // Lesson completion tracking
        PlayerPrefs.DeleteKey($"CompletedLessons_{username}_{topic}");
    }
    
    // Clear profile picture
    PlayerPrefs.DeleteKey($"ProfilePic_{username}");
    
    Debug.Log($"✓ Cleared all data for: {username}");
}

    void ShowLoginError(string message)
    {
        if (errorText != null)
        {
            errorText.text = message;
            errorText.color = Color.red;
            errorText.gameObject.SetActive(true);
        }
        Debug.LogWarning("Login Error: " + message);
    }

    void OnForgotPassword()
    {
        ShowLoginError("Password reset feature coming soon!");
    }

    #endregion

    #region Registration System

    void AttemptRegister()
    {
        string name = registerNameField?.text.Trim() ?? "";
        string username = registerUsernameField?.text.Trim() ?? "";
        string email = registerEmailField?.text.Trim() ?? "";
        string password = registerPasswordField?.text ?? "";
        string confirmPassword = confirmPasswordField?.text ?? "";

        // Validation
        if (string.IsNullOrEmpty(name))
        {
            ShowRegisterError("Please enter your full name");
            return;
        }

        if (string.IsNullOrEmpty(username) || username.Length < 3)
        {
            ShowRegisterError("Username must be at least 3 characters");
            return;
        }

        if (!System.Text.RegularExpressions.Regex.IsMatch(username, "^[a-zA-Z0-9_]+$"))
        {
            ShowRegisterError("Username: letters, numbers, and underscores only");
            return;
        }

        if (string.IsNullOrEmpty(email) || !IsValidEmail(email))
        {
            ShowRegisterError("Please enter a valid email address");
            return;
        }

        if (string.IsNullOrEmpty(password) || password.Length < minimumPasswordLength)
        {
            ShowRegisterError($"Password must be at least {minimumPasswordLength} characters");
            return;
        }

        if (password != confirmPassword)
        {
            ShowRegisterError("Passwords do not match");
            return;
        }

        if (string.IsNullOrEmpty(serverUrl))
        {
            ShowRegisterError("Server URL not configured!");
            return;
        }

        // Show loading
        if (registerButton != null)
        {
            var buttonText = registerButton.GetComponentInChildren<TextMeshProUGUI>();
            if (buttonText != null) buttonText.text = "Creating...";
            registerButton.interactable = false;
        }

        StartCoroutine(CloudRegister(username, name, email, password));
    }

    IEnumerator CloudRegister(string username, string name, string email, string password)
    {
        string url = serverUrl + "/api/users";
        
        string registrationDate = System.DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss");
        
        string jsonData = $@"{{
            ""username"": ""{username}"",
            ""name"": ""{name}"",
            ""email"": ""{email}"",
            ""password"": ""{password}"",
            ""registerDate"": ""{registrationDate}"",
            ""lastLogin"": ""{registrationDate}"",
            ""streak"": 0,
            ""completedTopics"": 0,
            ""progress"": {{
                ""Queue"": {{""tutorialCompleted"": false, ""puzzleCompleted"": false, ""score"": 0}},
                ""Stacks"": {{""tutorialCompleted"": false, ""puzzleCompleted"": false, ""score"": 0}},
                ""LinkedLists"": {{""tutorialCompleted"": false, ""puzzleCompleted"": false, ""score"": 0}},
                ""Trees"": {{""tutorialCompleted"": false, ""puzzleCompleted"": false, ""score"": 0}},
                ""Graphs"": {{""tutorialCompleted"": false, ""puzzleCompleted"": false, ""score"": 0}}
            }}
        }}";

        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);

        using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
        {
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.timeout = 10;

            yield return request.SendWebRequest();

            // Reset button
            if (registerButton != null)
            {
                var buttonText = registerButton.GetComponentInChildren<TextMeshProUGUI>();
                if (buttonText != null) buttonText.text = "Create Account";
                registerButton.interactable = true;
            }

            if (request.result == UnityWebRequest.Result.Success)
            {
                if (enableDebugMode)
                    Debug.Log("✅ Registration successful: " + username);

                StartCoroutine(ShowRegistrationSuccess(email));
            }
            else
            {
                string errorMsg = request.downloadHandler.text;
                
                if (errorMsg.Contains("already exists"))
                {
                    if (errorMsg.Contains("username"))
                        ShowRegisterError("Username already taken");
                    else
                        ShowRegisterError("Email already registered");
                }
                else
                {
                    ShowRegisterError("Registration failed. Check internet connection.");
                }
                
                if (enableDebugMode)
                    Debug.LogWarning("Registration error: " + request.error);
            }
        }
    }

    IEnumerator ShowRegistrationSuccess(string email)
    {
        if (registerErrorText != null)
        {
            registerErrorText.color = Color.green;
            registerErrorText.text = "✓ Account created successfully!";
            registerErrorText.gameObject.SetActive(true);
        }

        yield return new WaitForSeconds(1.5f);

        // Clear form
        if (registerNameField != null) registerNameField.text = "";
        if (registerUsernameField != null) registerUsernameField.text = "";
        if (registerEmailField != null) registerEmailField.text = "";
        if (registerPasswordField != null) registerPasswordField.text = "";
        if (confirmPasswordField != null) confirmPasswordField.text = "";

        ShowLoginPanel();

        if (registerErrorText != null)
            registerErrorText.color = Color.red;
    }

    void ShowRegisterError(string message)
    {
        if (registerErrorText != null)
        {
            registerErrorText.text = message;
            registerErrorText.color = Color.red;
            registerErrorText.gameObject.SetActive(true);
        }
        Debug.LogWarning("Registration Error: " + message);
    }

    #endregion

    #region Helper Functions

    bool IsValidEmail(string email)
    {
        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email && email.Contains("@") && email.Contains(".");
        }
        catch
        {
            return false;
        }
    }

    #endregion

    #region JSON Response Classes

    [System.Serializable]
    public class LoginResponse
    {
        public bool success;
        public UserData user;
    }

    [System.Serializable]
    public class UserData
    {
        public string username;
        public string name;
        public string email;
        public int streak;
        public int completedTopics;
    }

    #endregion
}