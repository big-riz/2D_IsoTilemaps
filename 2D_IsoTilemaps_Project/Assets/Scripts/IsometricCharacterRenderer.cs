using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IsometricCharacterRenderer : MonoBehaviour
{
    private SpriteRenderer spriteRenderer;
    
    [Header("Direction Sprites")]
    public Sprite northSprite;
    public Sprite northEastSprite;
    public Sprite eastSprite;
    public Sprite southEastSprite;
    public Sprite southSprite;
    public Sprite southWestSprite;
    public Sprite westSprite;
    public Sprite northWestSprite;
    
    private IsometricPlayerMovementController.CardinalDirection lastDirection = 
        IsometricPlayerMovementController.CardinalDirection.South; // Default direction
    
    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        
        // Set default sprite
        UpdateSprite();
    }
    
    // Original method can be kept for compatibility
    public void SetDirection(Vector2 direction)
    {
        // This might be called from other places, so we'll handle it
        if (direction != Vector2.zero)
        {
            // Determine direction and update sprite
            IsometricPlayerMovementController movementController = 
                GetComponentInParent<IsometricPlayerMovementController>();
            if (movementController != null)
            {
                lastDirection = movementController.GetCardinalDirection(direction);
                UpdateSprite();
            }
        }
    }
    
    // New method that accepts both the movement vector and the calculated direction
    public void SetDirection(Vector2 direction, IsometricPlayerMovementController.CardinalDirection cardinalDirection)
    {
        if (direction != Vector2.zero)
        {
            lastDirection = cardinalDirection;
            UpdateSprite();
        }
    }
    
    // Get the last used direction
    public IsometricPlayerMovementController.CardinalDirection GetLastDirection()
    {
        return lastDirection;
    }
    
    // Update sprite based on current direction
    private void UpdateSprite()
    {
        switch (lastDirection)
        {
            case IsometricPlayerMovementController.CardinalDirection.North:
                spriteRenderer.sprite = northSprite;
                break;
            case IsometricPlayerMovementController.CardinalDirection.NorthEast:
                spriteRenderer.sprite = northEastSprite;
                break;
            case IsometricPlayerMovementController.CardinalDirection.East:
                spriteRenderer.sprite = eastSprite;
                break;
            case IsometricPlayerMovementController.CardinalDirection.SouthEast:
                spriteRenderer.sprite = southEastSprite;
                break;
            case IsometricPlayerMovementController.CardinalDirection.South:
                spriteRenderer.sprite = southSprite;
                break;
            case IsometricPlayerMovementController.CardinalDirection.SouthWest:
                spriteRenderer.sprite = southWestSprite;
                break;
            case IsometricPlayerMovementController.CardinalDirection.West:
                spriteRenderer.sprite = westSprite;
                break;
            case IsometricPlayerMovementController.CardinalDirection.NorthWest:
                spriteRenderer.sprite = northWestSprite;
                break;
        }
    }
}
