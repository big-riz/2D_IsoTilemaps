using UnityEngine;
using UnityEngine.Events; // Required for UnityEvent

/// <summary>
/// Manages the player's score (hypercrumbs).
/// </summary>
public class ScoreManager : MonoBehaviour
{
    #region Singleton
    public static ScoreManager Instance { get; private set; }

    private void Awake()
    {
        // Singleton pattern
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        // Optional: Keep the ScoreManager across scene loads
        // DontDestroyOnLoad(gameObject); 
    }
    #endregion

    #region Fields
    [Header("Score Settings")]
    [SerializeField] private int _currentScore = 0;

    // Event triggered when the score changes
    public UnityEvent<int> OnScoreChanged; 
    #endregion

    #region Public Methods
    /// <summary>
    /// Adds the specified amount to the current score.
    /// </summary>
    /// <param name="amount">The amount to add (can be negative).</param>
    public void AddScore(int amount)
    {
        _currentScore += amount;
        // Ensure score doesn't go below zero (optional)
        _currentScore = Mathf.Max(0, _currentScore); 
        
        Debug.Log($"Score updated: {_currentScore} (+{amount})");
        
        // Trigger the event to notify listeners (like the UI)
        OnScoreChanged?.Invoke(_currentScore);
    }

    /// <summary>
    /// Gets the current score.
    /// </summary>
    /// <returns>The current score.</returns>
    public int GetScore()
    {
        return _currentScore;
    }

    /// <summary>
    /// Resets the score to zero.
    /// </summary>
    public void ResetScore()
    {
        _currentScore = 0;
        OnScoreChanged?.Invoke(_currentScore);
        Debug.Log("Score reset to 0.");
    }
    #endregion
} 