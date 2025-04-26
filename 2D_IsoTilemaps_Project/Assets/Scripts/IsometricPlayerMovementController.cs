using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IsometricPlayerMovementController : MonoBehaviour
{
    public float movementSpeed = 1f;
    IsometricCharacterRenderer isoRenderer;

    Rigidbody2D rbody;

    // Direction constants for 8-way movement
    public enum CardinalDirection
    {
        North,
        NorthEast,
        East,
        SouthEast,
        South,
        SouthWest,
        West,
        NorthWest
    }

    private void Awake()
    {
        rbody = GetComponent<Rigidbody2D>();
        isoRenderer = GetComponentInChildren<IsometricCharacterRenderer>();
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        Vector2 currentPos = rbody.position;
        float horizontalInput = Input.GetAxis("Horizontal");
        float verticalInput = Input.GetAxis("Vertical");
        Vector2 inputVector = new Vector2(horizontalInput, verticalInput);
        inputVector = Vector2.ClampMagnitude(inputVector, 1);
        Vector2 movement = inputVector * movementSpeed;
        Vector2 newPos = currentPos + movement * Time.fixedDeltaTime;
        
        // Get cardinal direction based on movement vector
        CardinalDirection direction = GetCardinalDirection(movement);
        
        // Set direction in renderer and pass the enum value
        isoRenderer.SetDirection(movement, direction);
        
        rbody.MovePosition(newPos);
    }
    
    // Determine which of the 8 cardinal directions the movement vector is closest to
    public CardinalDirection GetCardinalDirection(Vector2 movement)
    {
        // If no significant movement, keep the last direction
        if (movement.magnitude < 0.1f)
            return isoRenderer.GetLastDirection();
            
        // Convert to angle in degrees (0 is right, goes counter-clockwise)
        float angle = Mathf.Atan2(movement.y, movement.x) * Mathf.Rad2Deg;
        
        // Make sure angle is positive (0-360)
        if (angle < 0) angle += 360f;
        
        // Determine cardinal direction based on angle
        // Each direction covers 45 degrees, centered on the main direction
        if (angle >= 337.5f || angle < 22.5f) return CardinalDirection.East;        // 0
        if (angle >= 22.5f && angle < 67.5f) return CardinalDirection.NorthEast;    // 45
        if (angle >= 67.5f && angle < 112.5f) return CardinalDirection.North;       // 90
        if (angle >= 112.5f && angle < 157.5f) return CardinalDirection.NorthWest;  // 135
        if (angle >= 157.5f && angle < 202.5f) return CardinalDirection.West;       // 180
        if (angle >= 202.5f && angle < 247.5f) return CardinalDirection.SouthWest;  // 225
        if (angle >= 247.5f && angle < 292.5f) return CardinalDirection.South;      // 270
        return CardinalDirection.SouthEast;                                         // 315
    }
}
