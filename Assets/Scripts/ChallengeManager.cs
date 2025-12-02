using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using System.Linq;

public class ChallengeManager : MonoBehaviour
{
    [System.Serializable]
    public class ChallengeQuestion
    {
        public string questionText;
        public string[] answerOptions;
        public int correctAnswerIndex;
        public string explanation;
    }
    
    [System.Serializable]
    public class PuzzleData
    {
        public string description;
        public string hint;
        public string[] items;
        public string[] correctOrder;
    }
    
    [System.Serializable]
    public class TopicChallenge
    {
        public string topicName;
        public List<ChallengeQuestion> questions;
        public PuzzleData puzzle;
    }
    
    [Header("UI References")]
    public GameObject challengePanel;
    public TextMeshProUGUI headerText;
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI timerText;
    public TextMeshProUGUI questionNumberText;
    
    [Header("Question Display")]
    public GameObject questionPanel;
    public TextMeshProUGUI questionText;
    public Button[] answerButtons;
    public TextMeshProUGUI[] answerTexts;
    
    [Header("Puzzle UI")]
    public GameObject puzzlePanel;
    public TextMeshProUGUI puzzleDescriptionText;
    public TextMeshProUGUI puzzleHintText;
    public Transform puzzleItemsContainer;
    public GameObject puzzleItemPrefab;
    public Button checkPuzzleButton;
    public Button skipPuzzleButton;
    public Button nextPuzzleButton; // NEW: Add this button in Unity Inspector
    public TextMeshProUGUI puzzleFeedbackText;
    
    [Header("Feedback")]
    public GameObject feedbackPanel;
    public TextMeshProUGUI feedbackText;
    public TextMeshProUGUI explanationText;
    public Button nextQuestionButton;
    
    [Header("Results Screen")]
    public GameObject resultsPanel;
    public TextMeshProUGUI finalScoreText;
    public TextMeshProUGUI accuracyText;
    public TextMeshProUGUI timeUsedText;
    public Image resultStarImage;
    public Sprite[] starSprites;
    public Button retryButton;
    public Button backToMenuButton;
    
    [Header("Timer Settings")]
    public float timePerQuestion = 30f;
    public float timePerPuzzle = 60f;
    public Color normalTimeColor = Color.white;
    public Color warningTimeColor = Color.red;
    public float warningTimeThreshold = 10f;
    
    [Header("Challenge Data")]
    public List<TopicChallenge> allTopicChallenges;
    
    // Runtime variables
    private TopicChallenge currentChallenge;
    private int currentQuestionIndex = 0;
    private int correctAnswers = 0;
    private int totalQuestions = 0;
    private float currentTimeRemaining;
    private bool questionAnswered = false;
    private float totalTimeUsed = 0f;
    private bool isTimerRunning = false;
    private bool puzzleCompleted = false;
    private bool showingPuzzle = false;
    private string currentTopic;
    private List<GameObject> currentPuzzleItems = new List<GameObject>();
    
    void Start()
    {
        InitializeChallengeData();
        
        for (int i = 0; i < answerButtons.Length; i++)
        {
            int index = i;
            if (answerButtons[i] != null)
            {
                answerButtons[i].onClick.AddListener(() => SelectAnswer(index));
            }
        }
        
        if (nextQuestionButton != null)
            nextQuestionButton.onClick.AddListener(ShowNextQuestion);
        
        if (checkPuzzleButton != null)
            checkPuzzleButton.onClick.AddListener(CheckPuzzleAnswer);
        
        if (skipPuzzleButton != null)
            skipPuzzleButton.onClick.AddListener(SkipPuzzle);
        
        // NEW: Add listener for puzzle next button
        if (nextPuzzleButton != null)
            nextPuzzleButton.onClick.AddListener(ShowNextQuestion);
        
        if (retryButton != null)
            retryButton.onClick.AddListener(RetryChallenge);
        
        if (backToMenuButton != null)
            backToMenuButton.onClick.AddListener(BackToMenu);
        
        HideAllPanels();
    }
    
    void InitializeChallengeData()
    {
        allTopicChallenges = new List<TopicChallenge>
        {
            CreateQueueChallenge(),
            CreateStackChallenge(),
            CreateLinkedListChallenge(),
            CreateTreeChallenge(),
            CreateGraphChallenge()
        };
    }
    
    TopicChallenge CreateQueueChallenge()
    {
        TopicChallenge challenge = new TopicChallenge { topicName = "Queue" };
        
        challenge.puzzle = new PuzzleData
        {
            description = "🧩 Queue Puzzle: Arrange customers in FIFO (First In, First Out) order",
            hint = "💡 Hint: The first person to arrive should be at the TOP of the list",
            items = new string[] { "Dave (4th)", "Carol (3rd)", "Alice (1st)", "Bob (2nd)" },
            correctOrder = new string[] { "Alice (1st)", "Bob (2nd)", "Carol (3rd)", "Dave (4th)" }
        };
        
        challenge.questions = new List<ChallengeQuestion>
        {
            new ChallengeQuestion
            {
                questionText = "What is the time complexity of enqueue and dequeue operations in a queue?",
                answerOptions = new string[] { "O(1)", "O(n)", "O(log n)", "O(n²)" },
                correctAnswerIndex = 0,
                explanation = "Both enqueue and dequeue are O(1) operations as they only modify the front or rear of the queue."
            },
            new ChallengeQuestion
            {
                questionText = "Which principle does a queue follow?",
                answerOptions = new string[] { "FIFO", "LIFO", "Priority-based", "Random access" },
                correctAnswerIndex = 0,
                explanation = "Queue follows First-In-First-Out (FIFO) principle where the first element added is the first to be removed."
            },
            new ChallengeQuestion
            {
                questionText = "If we enqueue elements 1,2,3,4,5 and then dequeue 3 times, what is at the front?",
                answerOptions = new string[] { "4", "3", "5", "2" },
                correctAnswerIndex = 0,
                explanation = "After enqueuing [1,2,3,4,5] and dequeuing 3 times (removing 1,2,3), we're left with [4,5]. The front is 4."
            },
            new ChallengeQuestion
            {
                questionText = "What happens when you try to dequeue from an empty queue?",
                answerOptions = new string[] { "Underflow error", "Returns null", "Returns 0", "Creates new element" },
                correctAnswerIndex = 0,
                explanation = "Attempting to dequeue from an empty queue results in an underflow error since there are no elements to remove."
            },
            new ChallengeQuestion
            {
                questionText = "Which of these is a real-world example of a queue?",
                answerOptions = new string[] { "Line at a store", "Stack of plates", "Family tree", "Road map" },
                correctAnswerIndex = 0,
                explanation = "A line at a store is a perfect real-world example of a queue - first come, first served!"
            }
        };
        
        return challenge;
    }
    
    TopicChallenge CreateStackChallenge()
    {
        TopicChallenge challenge = new TopicChallenge { topicName = "Stacks" };
        
        challenge.puzzle = new PuzzleData
        {
            description = "🧩 Stack Puzzle: Arrange books in LIFO (Last In, First Out) order",
            hint = "💡 Hint: The last book placed should be at the TOP of the list",
            items = new string[] { "Book1 (1st)", "Book4 (4th)", "Book2 (2nd)", "Book3 (3rd)" },
            correctOrder = new string[] { "Book4 (4th)", "Book3 (3rd)", "Book2 (2nd)", "Book1 (1st)" }
        };
        
        challenge.questions = new List<ChallengeQuestion>
        {
            new ChallengeQuestion
            {
                questionText = "What principle does a stack follow?",
                answerOptions = new string[] { "LIFO", "FIFO", "Priority-based", "Random access" },
                correctAnswerIndex = 0,
                explanation = "Stack follows Last-In-First-Out (LIFO) principle where the most recently added element is removed first."
            },
            new ChallengeQuestion
            {
                questionText = "Which of these is NOT a valid stack operation?",
                answerOptions = new string[] { "Insert at middle", "Push", "Pop", "Peek" },
                correctAnswerIndex = 0,
                explanation = "Stacks only allow operations at the top. Inserting at the middle violates the stack principle."
            },
            new ChallengeQuestion
            {
                questionText = "What is the time complexity of push and pop operations?",
                answerOptions = new string[] { "O(1)", "O(n)", "O(log n)", "O(n²)" },
                correctAnswerIndex = 0,
                explanation = "Both push and pop are O(1) constant time operations as they only affect the top element."
            },
            new ChallengeQuestion
            {
                questionText = "Stacks are commonly used for:",
                answerOptions = new string[] { "Function call management", "Breadth-first search", "Sorting arrays", "Database indexing" },
                correctAnswerIndex = 0,
                explanation = "Stacks are used for function call management (call stack), where the most recent function called is executed first."
            },
            new ChallengeQuestion
            {
                questionText = "What happens when you try to pop from an empty stack?",
                answerOptions = new string[] { "Underflow error", "Returns 0", "Creates element", "Does nothing" },
                correctAnswerIndex = 0,
                explanation = "Attempting to pop from an empty stack results in an underflow error."
            }
        };
        
        return challenge;
    }
    
    TopicChallenge CreateLinkedListChallenge()
    {
        TopicChallenge challenge = new TopicChallenge { topicName = "LinkedLists" };
        
        challenge.puzzle = new PuzzleData
        {
            description = "🧩 Linked List Puzzle: Build the correct node traversal order",
            hint = "💡 Hint: Follow the 'next' pointers from Head to Tail",
            items = new string[] { "Node D", "Node B", "Head → A", "Node C" },
            correctOrder = new string[] { "Head → A", "Node B", "Node C", "Node D" }
        };
        
        challenge.questions = new List<ChallengeQuestion>
        {
            new ChallengeQuestion
            {
                questionText = "What is the main advantage of a linked list over an array?",
                answerOptions = new string[] { "Dynamic size", "Faster access", "Less memory", "Better cache locality" },
                correctAnswerIndex = 0,
                explanation = "Linked lists have dynamic size and can grow/shrink efficiently without reallocating memory."
            },
            new ChallengeQuestion
            {
                questionText = "What is the time complexity of inserting at the head of a linked list?",
                answerOptions = new string[] { "O(1)", "O(n)", "O(log n)", "O(n²)" },
                correctAnswerIndex = 0,
                explanation = "Inserting at the head is O(1) since we only need to update the head pointer and the new node's next pointer."
            },
            new ChallengeQuestion
            {
                questionText = "In a singly linked list, each node contains:",
                answerOptions = new string[] { "Data and next pointer", "Only data", "Data and two pointers", "Index and data" },
                correctAnswerIndex = 0,
                explanation = "Each node in a singly linked list contains data and a pointer to the next node."
            },
            new ChallengeQuestion
            {
                questionText = "What is the disadvantage of linked lists compared to arrays?",
                answerOptions = new string[] { "No random access", "Fixed size", "Can't insert", "Can't delete" },
                correctAnswerIndex = 0,
                explanation = "Linked lists don't support random access. To reach the nth element, you must traverse from the head."
            },
            new ChallengeQuestion
            {
                questionText = "A doubly linked list has:",
                answerOptions = new string[] { "Next and previous pointers", "Only next pointer", "Only data", "Two data fields" },
                correctAnswerIndex = 0,
                explanation = "Doubly linked lists have both next and previous pointers, allowing bidirectional traversal."
            }
        };
        
        return challenge;
    }
    
    TopicChallenge CreateTreeChallenge()
    {
        TopicChallenge challenge = new TopicChallenge { topicName = "Trees" };
        
        challenge.puzzle = new PuzzleData
        {
            description = "🧩 Tree Puzzle: Arrange BST Inorder Traversal (Left-Root-Right)",
            hint = "💡 Hint: Inorder traversal visits: Left subtree → Root → Right subtree",
            items = new string[] { "70", "20", "30", "50" },
            correctOrder = new string[] { "20", "30", "50", "70" }
        };
        
        challenge.questions = new List<ChallengeQuestion>
        {
            new ChallengeQuestion
            {
                questionText = "In a binary tree, each node can have at most how many children?",
                answerOptions = new string[] { "2", "1", "3", "Unlimited" },
                correctAnswerIndex = 0,
                explanation = "A binary tree is defined as a tree where each node has at most 2 children (left and right)."
            },
            new ChallengeQuestion
            {
                questionText = "What traversal visits nodes in the order: Left, Root, Right?",
                answerOptions = new string[] { "Inorder", "Preorder", "Postorder", "Level-order" },
                correctAnswerIndex = 0,
                explanation = "Inorder traversal visits left subtree, then root, then right subtree."
            },
            new ChallengeQuestion
            {
                questionText = "The height of a tree with only a root node is:",
                answerOptions = new string[] { "0", "1", "-1", "Undefined" },
                correctAnswerIndex = 0,
                explanation = "Height is defined as the number of edges from root to deepest leaf. A single node has height 0."
            },
            new ChallengeQuestion
            {
                questionText = "In a Binary Search Tree (BST), for each node:",
                answerOptions = new string[] { "Left < Node < Right", "Left > Node > Right", "No ordering", "Left = Right" },
                correctAnswerIndex = 0,
                explanation = "In a BST, all values in the left subtree are less than the node, and all in the right are greater."
            },
            new ChallengeQuestion
            {
                questionText = "What is the maximum number of nodes in a binary tree of height h?",
                answerOptions = new string[] { "2^(h+1) - 1", "2^h", "h + 1", "h * 2" },
                correctAnswerIndex = 0,
                explanation = "A complete binary tree of height h can have at most 2^(h+1) - 1 nodes."
            }
        };
        
        return challenge;
    }
    
    TopicChallenge CreateGraphChallenge()
    {
        TopicChallenge challenge = new TopicChallenge { topicName = "Graphs" };
        
        challenge.puzzle = new PuzzleData
        {
            description = "🧩 Graph Puzzle: Arrange BFS (Breadth-First Search) traversal order",
            hint = "💡 Hint: BFS visits all neighbors of a node before moving to the next level",
            items = new string[] { "Node D", "Node A (Start)", "Node C", "Node B" },
            correctOrder = new string[] { "Node A (Start)", "Node B", "Node C", "Node D" }
        };
        
        challenge.questions = new List<ChallengeQuestion>
        {
            new ChallengeQuestion
            {
                questionText = "A graph consists of:",
                answerOptions = new string[] { "Vertices and edges", "Only vertices", "Only edges", "Nodes and pointers" },
                correctAnswerIndex = 0,
                explanation = "A graph is made up of vertices (nodes) connected by edges (relationships)."
            },
            new ChallengeQuestion
            {
                questionText = "In an undirected graph, if vertex A connects to vertex B:",
                answerOptions = new string[] { "B also connects to A", "B may not connect to A", "A is the parent", "B is deleted" },
                correctAnswerIndex = 0,
                explanation = "In undirected graphs, edges are bidirectional. If A connects to B, then B connects to A."
            },
            new ChallengeQuestion
            {
                questionText = "Which algorithm is used for finding shortest path in an unweighted graph?",
                answerOptions = new string[] { "BFS", "DFS", "Merge Sort", "Binary Search" },
                correctAnswerIndex = 0,
                explanation = "Breadth-First Search (BFS) finds the shortest path in unweighted graphs by exploring level by level."
            },
            new ChallengeQuestion
            {
                questionText = "A graph with no cycles is called:",
                answerOptions = new string[] { "Tree or Forest", "Complete graph", "Cyclic graph", "Dense graph" },
                correctAnswerIndex = 0,
                explanation = "An acyclic graph is called a tree (if connected) or a forest (if disconnected)."
            },
            new ChallengeQuestion
            {
                questionText = "The degree of a vertex in a graph is:",
                answerOptions = new string[] { "Number of edges connected to it", "Its position", "Its value", "Number of vertices" },
                correctAnswerIndex = 0,
                explanation = "The degree of a vertex is the count of edges connected to that vertex."
            }
        };
        
        return challenge;
    }
    
    public void StartChallenge(string topicName)
    {
        currentTopic = topicName;
        currentChallenge = allTopicChallenges.Find(c => c.topicName == topicName);
        
        if (currentChallenge == null || currentChallenge.questions.Count == 0)
        {
            Debug.LogError("No challenge found for topic: " + topicName);
            return;
        }
        
        currentQuestionIndex = 0;
        correctAnswers = 0;
        totalQuestions = currentChallenge.questions.Count + 1;
        totalTimeUsed = 0f;
        puzzleCompleted = false;
        
        HideAllPanels();
        ShowPuzzle();
    }
    
    void ShowPuzzle()
    {
        showingPuzzle = true;
        questionAnswered = false;
        currentTimeRemaining = timePerPuzzle;
        isTimerRunning = true;
        
        if (challengePanel != null)
            challengePanel.SetActive(true);
        
        if (puzzlePanel != null)
            puzzlePanel.SetActive(true);
        
        if (questionPanel != null)
            questionPanel.SetActive(false);
        
        if (resultsPanel != null)
            resultsPanel.SetActive(false);
        
        if (headerText != null)
            headerText.text = $"Challenge: {currentChallenge.topicName}";
        
        if (questionNumberText != null)
            questionNumberText.text = $"1/{totalQuestions}";
        
        if (scoreText != null)
            scoreText.text = $"⭐ 0/0";
        
        if (puzzleDescriptionText != null)
            puzzleDescriptionText.text = currentChallenge.puzzle.description;
        
        if (puzzleHintText != null)
            puzzleHintText.text = currentChallenge.puzzle.hint;
        
        if (puzzleFeedbackText != null)
            puzzleFeedbackText.text = "";
        
        ClearPuzzleItems();
        CreatePuzzleItems(currentChallenge.puzzle.items);
        
        if (checkPuzzleButton != null)
        {
            checkPuzzleButton.gameObject.SetActive(true);
            checkPuzzleButton.interactable = true;
        }
        
        if (skipPuzzleButton != null)
        {
            skipPuzzleButton.gameObject.SetActive(true);
            skipPuzzleButton.interactable = true;
        }
        
        // NEW: Hide next puzzle button initially
        if (nextPuzzleButton != null)
            nextPuzzleButton.gameObject.SetActive(false);
        
        if (feedbackPanel != null)
            feedbackPanel.SetActive(false);
    }
    
    void ClearPuzzleItems()
    {
        foreach (GameObject item in currentPuzzleItems)
        {
            if (item != null)
                Destroy(item);
        }
        currentPuzzleItems.Clear();
    }
    
    void CreatePuzzleItems(string[] items)
    {
        if (puzzleItemPrefab == null || puzzleItemsContainer == null)
        {
            Debug.LogError("Puzzle item prefab or container is not assigned!");
            return;
        }
        
        List<string> shuffledItems = new List<string>(items);
        for (int i = 0; i < shuffledItems.Count; i++)
        {
            string temp = shuffledItems[i];
            int randomIndex = Random.Range(i, shuffledItems.Count);
            shuffledItems[i] = shuffledItems[randomIndex];
            shuffledItems[randomIndex] = temp;
        }
        
        foreach (string itemText in shuffledItems)
        {
            GameObject item = Instantiate(puzzleItemPrefab, puzzleItemsContainer);
            item.transform.SetParent(puzzleItemsContainer, false);
            
            TextMeshProUGUI text = item.GetComponentInChildren<TextMeshProUGUI>();
            if (text != null)
                text.text = itemText;
            
            if (item.GetComponent<Image>() == null)
            {
                item.AddComponent<Image>().color = Color.white;
            }
            
            LayoutElement layoutElement = item.GetComponent<LayoutElement>();
            if (layoutElement == null)
            {
                layoutElement = item.AddComponent<LayoutElement>();
            }
            layoutElement.preferredHeight = 70;
            layoutElement.minHeight = 60;
            
            PuzzleItemDragHandler dragHandler = item.GetComponent<PuzzleItemDragHandler>();
            if (dragHandler == null)
            {
                dragHandler = item.AddComponent<PuzzleItemDragHandler>();
            }
            dragHandler.SetContainer(puzzleItemsContainer);
            
            currentPuzzleItems.Add(item);
        }
        
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(puzzleItemsContainer as RectTransform);
    }
    
    void CheckPuzzleAnswer()
    {
        if (questionAnswered) return;
        
        questionAnswered = true;
        isTimerRunning = false;
        
        List<string> currentOrder = new List<string>();
        foreach (Transform child in puzzleItemsContainer)
        {
            TextMeshProUGUI text = child.GetComponentInChildren<TextMeshProUGUI>();
            if (text != null)
                currentOrder.Add(text.text);
        }
        
        bool isCorrect = true;
        if (currentOrder.Count == currentChallenge.puzzle.correctOrder.Length)
        {
            for (int i = 0; i < currentOrder.Count; i++)
            {
                if (currentOrder[i] != currentChallenge.puzzle.correctOrder[i])
                {
                    isCorrect = false;
                    break;
                }
            }
        }
        else
        {
            isCorrect = false;
        }
        
        if (isCorrect)
        {
            correctAnswers++;
            puzzleCompleted = true;
        }
        
        ShowPuzzleFeedback(isCorrect);
    }
    
    void ShowPuzzleFeedback(bool correct)
    {
        if (checkPuzzleButton != null)
        {
            checkPuzzleButton.interactable = false;
            checkPuzzleButton.gameObject.SetActive(false);
        }
        
        if (skipPuzzleButton != null)
        {
            skipPuzzleButton.interactable = false;
            skipPuzzleButton.gameObject.SetActive(false);
        }
        
        foreach (GameObject item in currentPuzzleItems)
        {
            PuzzleItemDragHandler dragHandler = item.GetComponent<PuzzleItemDragHandler>();
            if (dragHandler != null)
                dragHandler.enabled = false;
        }
        
        if (puzzleFeedbackText != null)
        {
            string feedbackMessage = correct ? "✓ Puzzle Solved! Great job!\n\n" : "✗ Incorrect order.\n\n";
            feedbackMessage += "Correct order:\n";
            for (int i = 0; i < currentChallenge.puzzle.correctOrder.Length; i++)
            {
                feedbackMessage += $"{i + 1}. {currentChallenge.puzzle.correctOrder[i]}\n";
            }
            
            puzzleFeedbackText.text = feedbackMessage;
            puzzleFeedbackText.color = correct ? new Color(0.3f, 0.8f, 0.3f) : new Color(0.8f, 0.3f, 0.3f);
        }
        
        if (scoreText != null)
            scoreText.text = $"⭐ {correctAnswers}/1";
        
        // NEW: Show the next puzzle button
        if (nextPuzzleButton != null)
        {
            nextPuzzleButton.gameObject.SetActive(true);
            nextPuzzleButton.interactable = true;
        }
    }
        
    void SkipPuzzle()
    {
        if (questionAnswered) return;
        
        questionAnswered = true;
        isTimerRunning = false;
        
        ShowPuzzleFeedback(false);
    }
    
void ShowQuestion()
{
    if (currentQuestionIndex >= currentChallenge.questions.Count)
    {
        ShowResults();
        return;
    }
    
    showingPuzzle = false;
    questionAnswered = false;
    currentTimeRemaining = timePerQuestion;
    isTimerRunning = true;
    
    if (challengePanel != null)
        challengePanel.SetActive(true);
    
    if (questionPanel != null)
        questionPanel.SetActive(true);
    
    if (puzzlePanel != null)
        puzzlePanel.SetActive(false);
    
    ChallengeQuestion question = currentChallenge.questions[currentQuestionIndex];
    
    if (headerText != null)
        headerText.text = $"Challenge: {currentChallenge.topicName}";
    
    if (questionNumberText != null)
        questionNumberText.text = $"{currentQuestionIndex + 2}/{totalQuestions}";
    
    int totalAnswered = currentQuestionIndex + (puzzleCompleted ? 1 : 0);
    if (scoreText != null)
        scoreText.text = $"⭐ {correctAnswers}/{totalAnswered + 1}";
    
    if (questionText != null)
        questionText.text = question.questionText;
    
    // Set up answer buttons
    for (int i = 0; i < answerButtons.Length; i++)
    {
        if (i < question.answerOptions.Length)
        {
            if (answerButtons[i] != null)
            {
                answerButtons[i].gameObject.SetActive(true);
                answerButtons[i].interactable = true;
                
                ColorBlock colors = answerButtons[i].colors;
                colors.normalColor = Color.white;
                colors.selectedColor = new Color(0.9f, 0.9f, 0.9f);
                answerButtons[i].colors = colors;
            }
            
            if (answerTexts[i] != null)
                answerTexts[i].text = question.answerOptions[i];
        }
        else
        {
            if (answerButtons[i] != null)
                answerButtons[i].gameObject.SetActive(false);
        }
    }
    
    if (feedbackPanel != null)
        feedbackPanel.SetActive(false);
}
    void TimeUp()
    {
        if (questionAnswered) return;
        
        questionAnswered = true;
        isTimerRunning = false;
        
        if (showingPuzzle)
        {
            ShowPuzzleFeedback(false);
        }
        else
        {
            ChallengeQuestion question = currentChallenge.questions[currentQuestionIndex];
            
            for (int i = 0; i < answerButtons.Length; i++)
            {
                if (answerButtons[i] != null && answerButtons[i].gameObject.activeSelf)
                {
                    answerButtons[i].interactable = false;
                    
                    ColorBlock colors = answerButtons[i].colors;
                    
                    if (i == question.correctAnswerIndex)
                    {
                        colors.disabledColor = new Color(0.3f, 0.8f, 0.3f);
                    }
                    else
                    {
                        colors.disabledColor = new Color(0.7f, 0.7f, 0.7f);
                    }
                    
                    answerButtons[i].colors = colors;
                }
            }
            
            if (feedbackPanel != null)
                feedbackPanel.SetActive(true);
            
            if (feedbackText != null)
            {
                feedbackText.text = "⏰ Time's Up!";
                feedbackText.color = new Color(0.8f, 0.3f, 0.3f);
            }
            
            if (explanationText != null)
                explanationText.text = question.explanation;
        }
    }
    
    void ShowNextQuestion()
    {
        if (showingPuzzle)
        {
            ShowQuestion();
        }
        else
        {
            currentQuestionIndex++;
            ShowQuestion();
        }
    }
    
    void ShowResults()
    {
        HideAllPanels();
        
        if (resultsPanel != null)
            resultsPanel.SetActive(true);
        
        float accuracy = (float)correctAnswers / totalQuestions * 100f;
        int stars = GetStarRating(accuracy);
        
        if (finalScoreText != null)
            finalScoreText.text = $"{correctAnswers}/{totalQuestions}";
        
        if (accuracyText != null)
            accuracyText.text = $"Accuracy: {accuracy:F0}%";
        
        if (timeUsedText != null)
        {
            int minutes = Mathf.FloorToInt(totalTimeUsed / 60f);
            int seconds = Mathf.FloorToInt(totalTimeUsed % 60f);
            timeUsedText.text = $"Time: {minutes:D2}:{seconds:D2}";
        }
        
        if (resultStarImage != null && starSprites != null && stars < starSprites.Length)
        {
            resultStarImage.sprite = starSprites[stars];
        }
        
        // Save progress
        SaveChallengeProgress(accuracy);
    }
    
    int GetStarRating(float accuracy)
    {
        if (accuracy >= 90f) return 3;
        if (accuracy >= 70f) return 2;
        if (accuracy >= 50f) return 1;
        return 0;
    }
    
    void SaveChallengeProgress(float accuracy)
    {
        string currentUserEmail = PlayerPrefs.GetString("CurrentUser", "");
        if (string.IsNullOrEmpty(currentUserEmail)) return;
        
        // Save to PlayerPrefs (existing code)
        string key = $"User_{currentUserEmail}_{currentTopic}_ChallengeScore";
        float bestScore = PlayerPrefs.GetFloat(key, 0f);
        
        if (accuracy > bestScore)
        {
            PlayerPrefs.SetFloat(key, accuracy);
            PlayerPrefs.Save();
        }
        
        // NEW: Track puzzle completion in progress system
        if (UserProgressManager.Instance != null)
        {
            UserProgressManager.Instance.CompletePuzzle(currentTopic, (int)accuracy);
            Debug.Log($"✓ Progress system updated: {currentTopic} - {accuracy}%");
        }
        else
        {
            Debug.LogWarning("UserProgressManager not found!");
        }
        
        // Mark as completed if score is good enough
        if (accuracy >= 70f)
        {
            string completedKey = $"User_{currentUserEmail}_{currentTopic}_Completed";
            if (!PlayerPrefs.HasKey(completedKey))
            {
                PlayerPrefs.SetInt(completedKey, 1);
                
                int completedCount = PlayerPrefs.GetInt($"User_{currentUserEmail}_CompletedTopics", 0);
                PlayerPrefs.SetInt($"User_{currentUserEmail}_CompletedTopics", completedCount + 1);
                
                PlayerPrefs.Save();
            }
        }
    }
    
    void RetryChallenge()
    {
        StartChallenge(currentTopic);
    }
    
    void BackToMenu()
    {
        SceneManager.LoadScene("MainMenu");
    }
    
    void HideAllPanels()
    {
        if (challengePanel != null) 
            challengePanel.SetActive(false);
        
        if (resultsPanel != null) 
            resultsPanel.SetActive(false);
    }
    
    void Update()
    {
        if (isTimerRunning && !questionAnswered)
        {
            currentTimeRemaining -= Time.deltaTime;
            totalTimeUsed += Time.deltaTime;
            
            if (currentTimeRemaining <= 0)
            {
                currentTimeRemaining = 0;
                TimeUp();
            }
            
            UpdateTimerDisplay();
        }
    }
    
    void UpdateTimerDisplay()
    {
        if (timerText != null)
        {
            int seconds = Mathf.CeilToInt(currentTimeRemaining);
            timerText.text = $"⏱️ {seconds}s";
            
            if (currentTimeRemaining <= warningTimeThreshold)
                timerText.color = warningTimeColor;
            else
                timerText.color = normalTimeColor;
        }
    }
    
    void SelectAnswer(int selectedIndex)
    {
        if (questionAnswered) return;
        
        questionAnswered = true;
        isTimerRunning = false;
        
        ChallengeQuestion question = currentChallenge.questions[currentQuestionIndex];
        bool isCorrect = selectedIndex == question.correctAnswerIndex;
        
        if (isCorrect)
            correctAnswers++;
        
        ShowQuestionFeedback(isCorrect, selectedIndex, question);
    }
    
    void ShowQuestionFeedback(bool correct, int selectedIndex, ChallengeQuestion question)
    {
        for (int i = 0; i < answerButtons.Length; i++)
        {
            if (answerButtons[i] != null && answerButtons[i].gameObject.activeSelf)
            {
                answerButtons[i].interactable = false;
                
                ColorBlock colors = answerButtons[i].colors;
                
                if (i == question.correctAnswerIndex)
                {
                    colors.disabledColor = new Color(0.3f, 0.8f, 0.3f);
                }
                else if (i == selectedIndex)
                {
                    colors.disabledColor = new Color(0.8f, 0.3f, 0.3f);
                }
                else
                {
                    colors.disabledColor = new Color(0.7f, 0.7f, 0.7f);
                }
                
                answerButtons[i].colors = colors;
            }
        }
        
        if (feedbackPanel != null)
            feedbackPanel.SetActive(true);
        
        if (feedbackText != null)
        {
            feedbackText.text = correct ? "✓ Correct!" : "✗ Incorrect";
            feedbackText.color = correct ? new Color(0.3f, 0.8f, 0.3f) : new Color(0.8f, 0.3f, 0.3f);
        }
        
        if (explanationText != null)
            explanationText.text = question.explanation;
        
        int totalAnswered = currentQuestionIndex + 1 + (puzzleCompleted ? 1 : 0);
        if (scoreText != null)
            scoreText.text = $"⭐ {correctAnswers}/{totalAnswered}";
    }
}