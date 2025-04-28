using UnityEngine;
using UnityEngine.Tilemaps; // Added for TilemapCollider2D
#if UNITY_EDITOR // Include editor-specific code only in the editor
using UnityEditor;
#endif

[RequireComponent(typeof(TilemapCollider2D))] // Ensure the component exists
public class CollisionScaler : MonoBehaviour
{
    #region Inspector Fields
    [Tooltip("The collider offset when the GameObject's scale is (1, 1, 1).")]
    [SerializeField] private Vector2 _baseOffsetAtScaleOne = Vector2.zero; 
    #endregion

    #region Private Fields
    // No longer caching components as we get them on demand in the editor method
    #endregion

    #region Unity Lifecycle
    // Removed Awake and Update as logic is now editor-only and on-demand
    #endregion

    #region Editor Methods
    #if UNITY_EDITOR
    [ContextMenu("Apply Scaled Collider Offset")]
    private void ApplyScaledOffsetInEditor()
    {
        TilemapCollider2D tilemapCollider = GetComponent<TilemapCollider2D>();
        if (tilemapCollider == null)
        {
            Debug.LogError("CollisionScaler: No TilemapCollider2D found on this GameObject.", this);
            return;
        }

        Vector3 currentScale = transform.localScale;
        
        // Calculate the new offset based on the current scale relative to (1, 1, 1)
        Vector2 newOffset = new Vector2(_baseOffsetAtScaleOne.x * currentScale.x, _baseOffsetAtScaleOne.y * currentScale.y);

        // Apply the new offset
        Undo.RecordObject(tilemapCollider, "Apply Scaled Collider Offset"); // Register for Undo
        tilemapCollider.offset = newOffset;
        EditorUtility.SetDirty(tilemapCollider); // Mark the component as dirty to ensure changes are saved

        Debug.Log($"CollisionScaler: Applied scaled offset {newOffset} based on scale {currentScale}.", this);
    }

    // Optional: Helper to set the base offset based on current values
    [ContextMenu("Set Base Offset from Current Values (at Scale 1)")]
    private void SetBaseOffsetFromCurrent()
    {
         if (!Mathf.Approximately(transform.localScale.x, 1f) || !Mathf.Approximately(transform.localScale.y, 1f))
         {
              Debug.LogWarning("CollisionScaler: Set the GameObject's scale to (1, 1, 1) before setting the base offset.", this);
              return;
         }

        TilemapCollider2D tilemapCollider = GetComponent<TilemapCollider2D>();
        if (tilemapCollider == null)
        {
            Debug.LogError("CollisionScaler: No TilemapCollider2D found on this GameObject.", this);
            return;
        }

        Undo.RecordObject(this, "Set Base Collider Offset"); // Record change to this script component
        _baseOffsetAtScaleOne = tilemapCollider.offset;
        EditorUtility.SetDirty(this); // Mark this script component as dirty

        Debug.Log($"CollisionScaler: Set base offset to {tilemapCollider.offset} (assuming current scale is 1).", this);
    }
    #endif
    #endregion
}
