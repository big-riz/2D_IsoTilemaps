using UnityEngine;

public class BreakableBarrel : MonoBehaviour
{
    [SerializeField] private Animation anim;
    [SerializeField] private string breakAnimationName = "BarrelBreak";
    [SerializeField] private bool useCollisionDetection = true;
    [SerializeField] private string playerTag = "Player";
    
    private bool broken = false;

    void Start()
    {
        // If animation component isn't assigned, try to get it from this GameObject
        if (anim == null)
        {
            anim = GetComponent<Animation>();
        }
    }

    void Update()
    {
        // Keep the update method for potential future functionality
    }
    
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!useCollisionDetection) return;
        
        // Check if we collided with the player
        if (!broken && collision.gameObject.CompareTag(playerTag))
        {
            BreakBarrel();
        }
    }
    
    private void OnTriggerEnter2D(Collider2D collider)
    {
        if (useCollisionDetection) return;
        
        // Check if we collided with the player
        if (!broken && collider.gameObject.CompareTag(playerTag))
        {
            BreakBarrel();
        }
    }
    
    public void BreakBarrel()
    {
        if (broken) return;
        
        broken = true;
        
        // Play the break animation if animation component exists
        if (anim != null && anim.GetClip(breakAnimationName) != null)
        {
            anim.Play(breakAnimationName);
        }
        else
        {
            Debug.LogWarning("No Animation component attached to the barrel or animation clip not found!");
        }
    }
}