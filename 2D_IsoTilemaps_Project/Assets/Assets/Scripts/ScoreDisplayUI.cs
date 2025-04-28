using UnityEngine;
using UnityEngine.UI; // Required for UI elements like Image
using System.Collections.Generic; // Required for List

/// <summary>
/// Updates UI Image components to display the score using individual digit sprites.
/// </summary>
public class ScoreDisplayUI : MonoBehaviour
{
    #region Inspector Fields
    [Header("UI Setup")]
    [Tooltip("List of UI Image components used to display the score digits, ordered from left (most significant) to right (least significant).")]
    [SerializeField] private List<Image> _digitImages = new List<Image>();

    [Tooltip("Sprites representing digits 0 through 9. Must have exactly 10 sprites.")]
    [SerializeField] private Sprite[] _digitSprites = new Sprite[10]; // 0-9

    [Tooltip("Sprite to show for leading zeros or unused digit slots (optional). If null, these images will be disabled.")]
    [SerializeField] private Sprite _leadingZeroSprite = null;

    [Tooltip("Whether to hide leading zeros (display '5' instead of '005').")]
    [SerializeField] private bool _hideLeadingZeros = true;
    #endregion

    #region Unity Lifecycle
    private void Start()
    {
        // Validate setup
        if (_digitImages.Count == 0)
        {
            Debug.LogError("ScoreDisplayUI: No digit images assigned!", this);
            enabled = false;
            return;
        }
        if (_digitSprites.Length != 10 || System.Array.Exists(_digitSprites, sprite => sprite == null))
        {
            Debug.LogError("ScoreDisplayUI: Digit sprites array must contain exactly 10 non-null sprites (for digits 0-9).", this);
            enabled = false;
            return;
        }

        // Register for score updates
        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.OnScoreChanged.AddListener(UpdateScoreDisplay);
            // Display initial score
            UpdateScoreDisplay(ScoreManager.Instance.GetScore());
        }
        else
        {
            Debug.LogError("ScoreDisplayUI: ScoreManager instance not found! Make sure a ScoreManager exists in the scene.", this);
            // Display initial zero score manually if manager is missing
            UpdateScoreDisplay(0);
        }
    }

    private void OnDestroy()
    {
        // Unregister listener to prevent memory leaks
        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.OnScoreChanged.RemoveListener(UpdateScoreDisplay);
        }
    }
    #endregion

    #region Private Methods
    /// <summary>
    /// Updates the digit images based on the new score.
    /// </summary>
    /// <param name="newScore">The score to display.</param>
    private void UpdateScoreDisplay(int newScore)
    {
        string scoreString = newScore.ToString();
        int numDigits = _digitImages.Count;
        bool leadingZero = true; // Flag to track if we are still in the leading zero part

        // Iterate through the digit images from left to right
        for (int i = 0; i < numDigits; i++)
        {
            int digitIndex = i - (numDigits - scoreString.Length); // Calculate corresponding index in the score string

            if (digitIndex < 0) // This digit is part of the leading zeros
            {
                if (_hideLeadingZeros)
                {
                    // Disable the image entirely if hiding leading zeros
                    _digitImages[i].enabled = false;
                }
                else
                {
                    // Show the leading zero sprite or disable if none provided
                    _digitImages[i].enabled = (_leadingZeroSprite != null);
                    if (_leadingZeroSprite != null)
                    {
                        _digitImages[i].sprite = _leadingZeroSprite;
                    }
                }
            }
            else // This digit is part of the actual score number
            {
                 leadingZero = false; // We've passed the leading zeros
                _digitImages[i].enabled = true; // Ensure image is enabled

                // Get the numeric value of the digit
                int digitValue = int.Parse(scoreString[digitIndex].ToString());

                // Set the corresponding sprite
                if (digitValue >= 0 && digitValue < _digitSprites.Length)
                {
                    _digitImages[i].sprite = _digitSprites[digitValue];
                }
                else
                {
                    Debug.LogWarning($"ScoreDisplayUI: Invalid digit value '{digitValue}' encountered.", this);
                     // Optionally set a default/error sprite here
                     _digitImages[i].enabled = false;
                }
            }
        }
        
         // Special case: If score is 0 and hiding leading zeros, make sure the last digit (0) is shown
        if (_hideLeadingZeros && newScore == 0 && numDigits > 0) {
             _digitImages[numDigits - 1].enabled = true;
             _digitImages[numDigits - 1].sprite = _digitSprites[0];
        }
    }
    #endregion
} 