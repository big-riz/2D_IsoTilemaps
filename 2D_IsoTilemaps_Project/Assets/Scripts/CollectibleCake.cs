using UnityEngine;

/// <summary>
/// Makes the cake destroyable when triggered by the player and adds points.
/// </summary>
public class CollectibleCake : MonoBehaviour
{
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private float destroyDelay = 0.2f;
    [SerializeField] private bool playEffectOnCollection = true;
    [SerializeField] private GameObject collectEffect;
    [SerializeField] private int scoreValue = 1; // Points awarded for collecting this cake
    
    private bool isCollected = false;
    
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isCollected || !other.CompareTag(playerTag))
            return;
            
        Collect();
    }
    
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (isCollected || !collision.gameObject.CompareTag(playerTag))
            return;
            
        Collect();
    }
    
    /// <summary>
    /// Collects the cake, adds score, and triggers destruction
    /// </summary>
    public void Collect()
    {
        if (isCollected)
            return;
            
        isCollected = true;
        
        // Add score
        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.AddScore(scoreValue);
        }
        else
        {
            Debug.LogWarning("CollectibleCake: ScoreManager instance not found. Cannot add score.", this);
        }
        
        // Play collection effect if enabled
        if (playEffectOnCollection && collectEffect != null)
        {
            Instantiate(collectEffect, transform.position, Quaternion.identity);
        }
        
        // Disable colliders
        Collider2D[] colliders = GetComponentsInChildren<Collider2D>();
        foreach (var collider in colliders)
        {
            collider.enabled = false;
        }
        
        // Disable renderer
        SpriteRenderer renderer = GetComponent<SpriteRenderer>();
        if (renderer != null)
        {
            renderer.enabled = false;
        }
        
        // Destroy after delay
        Destroy(gameObject, destroyDelay);
    }
} 