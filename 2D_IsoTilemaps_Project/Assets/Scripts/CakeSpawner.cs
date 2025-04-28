using UnityEngine;

/// <summary>
/// Spawns a cake prefab once when the game starts or when triggered
/// </summary>
public class CakeSpawner : MonoBehaviour
{
    [SerializeField] private GameObject cakePrefab;
    [SerializeField] private bool spawnOnStart = true;
    [SerializeField] private bool hasSpawned = false;
    
    // Unique ID to identify this spawner for saving/loading state
    [SerializeField] private string spawnerID;
    
    private void Awake()
    {
        // Generate a unique ID if none exists
        if (string.IsNullOrEmpty(spawnerID))
        {
            spawnerID = System.Guid.NewGuid().ToString();
        }
        
        // Load the spawned state
        hasSpawned = PlayerPrefs.GetInt("CakeSpawner_" + spawnerID, 0) == 1;
    }
    
    private void Start()
    {
        // Register with the manager
        if (FoodSpawnerManager.Instance != null)
        {
            FoodSpawnerManager.Instance.RegisterSpawner(this);
        }
        
        if (spawnOnStart && !hasSpawned)
        {
            SpawnCake();
        }
    }
    
    private void OnDestroy()
    {
        // Unregister from the manager
        if (FoodSpawnerManager.Instance != null)
        {
            FoodSpawnerManager.Instance.UnregisterSpawner(this);
        }
    }
    
    /// <summary>
    /// Spawns the cake if it hasn't been spawned yet
    /// </summary>
    public void SpawnCake()
    {
        if (hasSpawned || cakePrefab == null)
            return;
            
        Instantiate(cakePrefab, transform.position, Quaternion.identity);
        hasSpawned = true;
        
        // Play spawn effect if available
        if (FoodSpawnerManager.Instance != null && FoodSpawnerManager.Instance.SpawnEffect != null)
        {
            Instantiate(FoodSpawnerManager.Instance.SpawnEffect, transform.position, Quaternion.identity);
        }
        
        // Save the spawner state
        SaveSpawnerState();
    }
    
    /// <summary>
    /// Reset the spawner to allow spawning again
    /// </summary>
    public void ResetSpawner()
    {
        hasSpawned = false;
        
        PlayerPrefs.SetInt("CakeSpawner_" + spawnerID, 0);
        PlayerPrefs.Save();
    }
    
    private void SaveSpawnerState()
    {
        PlayerPrefs.SetInt("CakeSpawner_" + spawnerID, hasSpawned ? 1 : 0);
        PlayerPrefs.Save();
    }
    
    /// <summary>
    /// Returns whether this cake has been collected
    /// </summary>
    public bool HasBeenCollected()
    {
        return hasSpawned;
    }
    
    // Draw a visual marker in the editor
    private void OnDrawGizmos()
    {
        Gizmos.color = hasSpawned ? Color.grey : Color.cyan;
        Gizmos.DrawWireSphere(transform.position, 0.2f);
        
        // Draw an icon to represent the cake
        Gizmos.DrawIcon(transform.position, "food", true);
    }
} 