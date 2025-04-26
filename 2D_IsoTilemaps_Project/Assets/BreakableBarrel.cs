using UnityEngine;

public class BreakableBarrel : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField] private string breakAnimationTrigger = "Break";
    [SerializeField] private bool useCollisionDetection = true;
    [SerializeField] private string playerTag = "Player";
    
    private bool broken = false;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // If animator component isn't assigned, try to get it from this GameObject
        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }
    }

    // Update is called once per frame
    void Update()
    {
        // Keep the update method for potential future functionality
    }
    
    // Called when another collider enters this object's collider (if both have rigidbodies)
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!useCollisionDetection) return;
        
        // Check if we collided with the player
        if (!broken && collision.gameObject.CompareTag(playerTag))
        {
            BreakBarrel();
        }
    }
    
    // Alternative trigger-based collision (for trigger colliders)
    private void OnTriggerEnter2D(Collider2D collider)
    {
        if (useCollisionDetection) return;
        
        // Check if we collided with the player
        if (!broken && collider.gameObject.CompareTag(playerTag))
        {
            BreakBarrel();
        }
    }
    
    // Public method to break the barrel (can be called from other scripts too)
    public void BreakBarrel()
    {
        if (broken) return;
        
        broken = true;
        
        // Play the break animation if animator exists
        if (animator != null)
        {
            animator.SetTrigger(breakAnimationTrigger);
        }
        else
        {
            Debug.LogWarning("No Animator component attached to the barrel!");
        }
    }
}
