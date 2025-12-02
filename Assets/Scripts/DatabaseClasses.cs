using System.Collections.Generic;

/// <summary>
/// SHARED DATABASE CLASSES - Use these across all scripts
/// This file contains all database response/data classes to avoid duplicates
/// </summary>

// ==================== PROGRESS DATA ====================

[System.Serializable]
public class DatabaseProgressResponse
{
    public bool success;
    public DatabaseProgressData data;
}

[System.Serializable]
public class DatabaseProgressData
{
    public string username;
    public string name;
    public string email;
    public int streak;
    public int completedTopics;
    public string lastUpdated;
    public List<DatabaseTopicData> topics;
}

[System.Serializable]
public class DatabaseTopicData
{
    public string topicName;
    public bool tutorialCompleted;
    public bool puzzleCompleted;
    public int puzzleScore;
    public float progressPercentage;
    public string lastAccessed;
    public float timeSpent;
    public int lessonsCompleted;
}

// ==================== LESSON DATA ====================

[System.Serializable]
public class LessonsResponse
{
    public bool success;
    public int count;
    public LessonData[] lessons;
}

[System.Serializable]
public class LessonData
{
    public string _id;
    public string topicName;
    public string title;
    public string description;
    public string content;
    public int order;
}

// ==================== USER DATA ====================

[System.Serializable]
public class UserProgressData
{
    public string username;
    public string name;
    public string email;
    public int streak;
    public int completedTopics;
    public string lastUpdated;
    public List<UserTopicData> topics;
}

[System.Serializable]
public class UserTopicData
{
    public string topicName;
    public bool tutorialCompleted;
    public bool puzzleCompleted;
    public int puzzleScore;
    public float progressPercentage;
    public string lastAccessed;
    public float timeSpent;
    public int lessonsCompleted;
}