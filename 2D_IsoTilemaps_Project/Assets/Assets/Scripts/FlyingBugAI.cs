using UnityEngine;
using System.Collections;

/// <summary>
/// Controls a simple flying bug AI that moves randomly within bounds and gives points on collection.
/// </summary>
[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class FlyingBugAI : MonoBehaviour
{
    #region Inspector Fields
    [Header("Movement Settings")]
    [SerializeField] private float _moveSpeed = 2f;
    [SerializeField] private Vector2 _moveAreaCenter = Vector2.zero;
    [SerializeField] private Vector2 _moveAreaSize = new Vector2(20f, 20f); // Defines the rectangular bounds
    [SerializeField] private float _minTimeBetweenMoves = 1.0f;
    [SerializeField] private float _maxTimeBetweenMoves = 3.0f;
    [SerializeField] private float _arrivalThreshold = 0.2f; // How close to get to the target point

    [Header("Collection Settings")]
    [SerializeField] private string _playerTag = "Player";
    [SerializeField] private int _scoreValue = 3;
    [SerializeField] private GameObject _collectEffectPrefab;
    [SerializeField] private float _destroyDelay = 0.1f;

    [Header("Components (Optional)")]
    [Tooltip("Assign if the bug should visually face its movement direction.")]
    [SerializeField] private IsometricCharacterRenderer _isoRenderer; // Optional renderer
    #endregion

    #region Private Fields
    private Rigidbody2D _rigidbody;
    private Collider2D _collider;
    private Vector2 _currentTargetPosition;
    private bool _isMoving = false;
    private bool _isCollected = false;
    #endregion

    #region Unity Lifecycle
    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody2D>();
        _collider = GetComponent<Collider2D>();
        // Ensure Rigidbody2D is set up correctly for trigger detection and movement
        _rigidbody.isKinematic = true; // Often best for AI controlled movement like this
        _rigidbody.gravityScale = 0;
        _collider.isTrigger = true; // Necessary for OnTriggerEnter2D

        if (_isoRenderer == null) // Attempt to find it if not assigned
        {
            _isoRenderer = GetComponentInChildren<IsometricCharacterRenderer>();
        }
    }

    private void Start()
    {
        // Set initial random target
        SetNewRandomTarget();
        StartCoroutine(MovementRoutine());
    }

    private void FixedUpdate()
    {
        if (_isMoving && !_isCollected)
        {
            MoveTowardsTarget();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!_isCollected && other.CompareTag(_playerTag))
        {
            Collect();
        }
    }
    #endregion

    #region Private Methods
    /// <summary>
    /// Sets a new random target position within the defined movement area.
    /// </summary>
    private void SetNewRandomTarget()
    {
        float randomX = Random.Range(_moveAreaCenter.x - _moveAreaSize.x / 2f, _moveAreaCenter.x + _moveAreaSize.x / 2f);
        float randomY = Random.Range(_moveAreaCenter.y - _moveAreaSize.y / 2f, _moveAreaCenter.y + _moveAreaSize.y / 2f);
        _currentTargetPosition = new Vector2(randomX, randomY);
        _isMoving = true;

        // Update renderer direction if available
        if (_isoRenderer != null)
        {
             Vector2 moveDirection = (_currentTargetPosition - (Vector2)transform.position).normalized;
             // We might need a GetCardinalDirection equivalent here if the renderer uses enums
             // For now, just pass the vector
             // TODO: Adapt this if IsometricCharacterRenderer needs a specific direction enum
             _isoRenderer.SetDirection(moveDirection);
        }
    }

    /// <summary>
    /// Coroutine to manage the timing between movements.
    /// </summary>
    private IEnumerator MovementRoutine()
    {
        while (!_isCollected) // Loop while the bug hasn't been collected
        {
            // Wait until the bug reaches its current target
            yield return new WaitUntil(() => !_isMoving || _isCollected);

            if (_isCollected) break; // Exit loop if collected during wait

            // Wait for a random duration before picking a new target
            float waitTime = Random.Range(_minTimeBetweenMoves, _maxTimeBetweenMoves);
            yield return new WaitForSeconds(waitTime);

            if (_isCollected) break; // Exit loop if collected during wait

            SetNewRandomTarget();
        }
    }

    /// <summary>
    /// Moves the Rigidbody towards the current target position.
    /// </summary>
    private void MoveTowardsTarget()
    {
        Vector2 currentPosition = _rigidbody.position;
        Vector2 direction = (_currentTargetPosition - currentPosition).normalized;
        Vector2 movement = direction * _moveSpeed * Time.fixedDeltaTime;

        // Check if we are close enough to the target
        if (Vector2.Distance(currentPosition, _currentTargetPosition) <= _arrivalThreshold)
        {
            _rigidbody.MovePosition(_currentTargetPosition); // Snap to target
            _isMoving = false;
            // Stop moving animation if applicable (renderer might handle this)
            if (_isoRenderer != null)
            {
                // TODO: Implement stop logic if renderer needs it
                // _isoRenderer.SetDirection(Vector2.zero); // Example
            }
        }
        else
        {
            _rigidbody.MovePosition(currentPosition + movement);
        }
    }

    /// <summary>
    /// Handles the collection of the bug by the player.
    /// </summary>
    private void Collect()
    {
        if (_isCollected) return;
        _isCollected = true;
        _isMoving = false; // Stop movement

        // Add score
        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.AddScore(_scoreValue);
        }
        else
        {
            Debug.LogWarning("FlyingBugAI: ScoreManager instance not found. Cannot add score.", this);
        }

        // Play collection effect
        if (_collectEffectPrefab != null)
        {
            Instantiate(_collectEffectPrefab, transform.position, Quaternion.identity);
        }

        // Disable visuals and collider immediately
        _collider.enabled = false;
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        foreach(Renderer rend in renderers) {
            rend.enabled = false;
        }

        // Destroy the GameObject after a short delay
        Destroy(gameObject, _destroyDelay);
    }
    #endregion

    #region Gizmos
    // Draw the movement boundaries in the editor
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0f, 1f, 1f, 0.3f); // Cyan
        Gizmos.DrawWireCube(_moveAreaCenter, _moveAreaSize);

        if (Application.isPlaying && _isMoving)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, _currentTargetPosition);
            Gizmos.DrawWireSphere(_currentTargetPosition, _arrivalThreshold);
        }
    }
    #endregion
} 