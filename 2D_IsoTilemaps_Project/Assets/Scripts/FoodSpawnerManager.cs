using UnityEngine;
using System.Collections.Generic;
using System.Collections;

/// <summary>
/// Manages all food spawners in the scene
/// </summary>
public class FoodSpawnerManager : MonoBehaviour
{
    // Singleton instance
    public static FoodSpawnerManager Instance { get; private set; }
    
    // List of all spawners in the scene
    private List<CakeSpawner> spawners = new List<CakeSpawner>();
    
    [SerializeField] private float respawnDelay = 60f; // 1 minute respawn delay
    [SerializeField] private GameObject spawnEffect; // Effect to play when an item is spawned
    [SerializeField] private bool autoRespawn = true; // Whether to automatically respawn items
    
    // Public accessor for spawn effect
    public GameObject SpawnEffect => spawnEffect;
    
    private void Awake()
    {
        // Singleton pattern
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }
    
    private void Start()
    {
        if (autoRespawn)
        {
            StartCoroutine(RespawnRoutine());
        }
    }
    
    /// <summary>
    /// Register a spawner with the manager
    /// </summary>
    public void RegisterSpawner(CakeSpawner spawner)
    {
        if (!spawners.Contains(spawner))
        {
            spawners.Add(spawner);
        }
    }
    
    /// <summary>
    /// Unregister a spawner from the manager
    /// </summary>
    public void UnregisterSpawner(CakeSpawner spawner)
    {
        if (spawners.Contains(spawner))
        {
            spawners.Remove(spawner);
        }
    }
    
    /// <summary>
    /// Reset all spawners in the scene
    /// </summary>
    public void ResetAllSpawners()
    {
        foreach (var spawner in spawners)
        {
            if (spawner != null)
            {
                spawner.ResetSpawner();
            }
        }
        
        Debug.Log($"Reset {spawners.Count} food spawners");
    }
    
    /// <summary>
    /// Coroutine to handle automatic respawning of food items
    /// </summary>
    private IEnumerator RespawnRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(respawnDelay);
            
            // Get a list of all collected cakes (hasSpawned = true)
            List<CakeSpawner> collectedCakes = new List<CakeSpawner>();
            foreach (var spawner in spawners)
            {
                if (spawner != null && spawner.HasBeenCollected())
                {
                    collectedCakes.Add(spawner);
                }
            }
            
            // If there are any collected cakes, respawn one random cake
            if (collectedCakes.Count > 0)
            {
                int randomIndex = Random.Range(0, collectedCakes.Count);
                CakeSpawner spawnerToReset = collectedCakes[randomIndex];
                
                spawnerToReset.ResetSpawner();
                spawnerToReset.SpawnCake();
                
                Debug.Log($"Respawned 1 cake out of {collectedCakes.Count} collected cakes");
            }
        }
    }
    
    /// <summary>
    /// Find all spawners in the scene and register them
    /// </summary>
    public void FindAndRegisterAllSpawners()
    {
        spawners.Clear();
        CakeSpawner[] foundSpawners = FindObjectsOfType<CakeSpawner>();
        
        foreach (var spawner in foundSpawners)
        {
            RegisterSpawner(spawner);
        }
        
        Debug.Log($"Found and registered {spawners.Count} food spawners");
    }
} 