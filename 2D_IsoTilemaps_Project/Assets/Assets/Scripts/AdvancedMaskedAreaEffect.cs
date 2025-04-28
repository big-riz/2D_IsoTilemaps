using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.Text;
using System.Diagnostics;
using Debug = UnityEngine.Debug;

/// <summary>
/// Tracks multiple GameObjects and applies a configurable shader effect (currently Glitch)
/// locally within masked screen areas around tracked objects using a single post-processing pass.
/// </summary>
[RequireComponent(typeof(Camera))]
public class AdvancedMaskedAreaEffect : MonoBehaviour
{
    #region Constants
    // Must match the shader constant and array sizes below
    private const int MAX_TRACKED_OBJECTS_LIMIT = 10;
    #endregion

    #region Enums
    // Removed MaskShape and ShaderEffect enums as the shader now handles this internally or is specific (e.g., Glitch)
    // If more effects are added to the shader, an enum could be reintroduced here and passed to the shader.

    /// <summary>
    /// Defines how to handle multiple tracked objects
    /// </summary>
    public enum TrackingMode
    {
        SingleObject,  // Track a single specified object
        ClosestTagged, // Track the closest tagged object
        ActiveTagged,  // Track the currently active tagged object
        AllTagged      // Track all objects with the specified tag
    }

    /// <summary>
    /// Debug visualization modes
    /// </summary>
    public enum DebugVisualization
    {
        None,           // No debug visualization
        ProximityZones, // Show proximity and fade zones
        // MaskAreas visualization needs rework for the new shader approach
        Performance     // Show performance metrics
    }
    #endregion
    
    #region Inspector Fields
    [Header("Target Settings")]
    [Tooltip("How to determine which objects to track")]
    [SerializeField] private TrackingMode _trackingMode = TrackingMode.SingleObject;

    [Tooltip("The GameObject to track with the mask (SingleObject mode)")]
    [SerializeField] private GameObject _targetObject;
    
    [Tooltip("Tag to track when using tag-based tracking modes")]
    [SerializeField] private string _targetTag = "Player";
    
    [Tooltip("How often to update the list of tagged objects (seconds)")]
    [SerializeField, Range(0.1f, 5f)] private float _taggedObjectsRefreshRate = 1f;
    
    [Tooltip("Index of the currently active tagged object (ActiveTagged mode)")]
    [SerializeField] private int _activeTaggedObjectIndex = 0;
    
    [Tooltip("Distance threshold at which effects become visible (units)")]
    [SerializeField, Range(1f, 50f)] private float _proximityThreshold = 10f;
    
    [Tooltip("Fade distance for effects (units)")]
    [SerializeField, Range(0.1f, 10f)] private float _fadeDistance = 2f;
    
    [Tooltip("Additional offset from the target position in viewport space (0-1)")]
    [SerializeField] private Vector2 _positionOffset = Vector2.zero;
    
    [Header("Mask Settings")]
    // MaskShape removed - controlled by shader implementation if varied
    [Tooltip("Base size of the mask in screen space (0-1)")]
    [SerializeField, Range(0.01f, 0.5f)] private float _maskSize = 0.2f;
    
    [Tooltip("Softness of the mask edge (0-1, controls fade transition)")]
    [SerializeField, Range(0.01f, 1f)] private float _maskSoftness = 0.5f;
    
    [Header("Glitch Effect Settings")] // Integrated from GlitchEffect
    [SerializeField] private Texture2D _noiseTexture; // Noise texture for digital glitch

    [Header("Analog Glitch")]
    [SerializeField] [Range(0, 1)] private float _scanLineJitter = 0;
    [SerializeField] [Range(0, 1)] private float _verticalJump = 0;
    [SerializeField] [Range(0, 1)] private float _horizontalShake = 0;
    [SerializeField] [Range(0, 1)] private float _colorDrift = 0;

    [Header("Digital Glitch")]
    [SerializeField] [Range(0, 1)] private float _pixelation = 0;
    [SerializeField] [Range(0, 1)] private float _colorGlitch = 0;
    [SerializeField] [Range(0, 1)] private float _blockGlitch = 0;
    
    [Header("Material & Shader")]
    [Tooltip("Material using the Custom/MultiMaskEffectShader")]
    [SerializeField] private Material _multiMaskMaterial;
    [Tooltip("Reference to the Custom/MultiMaskEffectShader (optional, for validation)")]
    [SerializeField] private Shader _multiMaskShader;
    
    [Tooltip("Maximum number of objects to track simultaneously (must be <= shader limit)")]
    [SerializeField, Range(1, MAX_TRACKED_OBJECTS_LIMIT)] private int _maxTrackedObjects = MAX_TRACKED_OBJECTS_LIMIT;

    [Header("Debug Settings")]
    [Tooltip("Enable detailed debug logging")]
    [SerializeField] private bool _enableDebugLogging = false;
    
    [Tooltip("Debug visualization mode")]
    [SerializeField] private DebugVisualization _debugVisualization = DebugVisualization.None;
    
    [Tooltip("Color for proximity threshold visualization")]
    [SerializeField] private Color _proximityThresholdColor = new Color(1f, 0f, 0f, 0.2f);
    
    [Tooltip("Color for fade distance visualization")]
    [SerializeField] private Color _fadeDistanceColor = new Color(0f, 1f, 0f, 0.2f);
    
    [Tooltip("Show performance metrics in editor")]
    [SerializeField] private bool _showPerformanceMetrics = false;
    #endregion

    #region Debug Fields
    private StringBuilder _debugStringBuilder = new StringBuilder();
    private Stopwatch _performanceStopwatch = new Stopwatch();
    private float _lastUpdateTime;
    private float _lastRenderTime;
    private float _averageFrameTime;
    private int _updateFrameCount;
    private const int FRAME_SAMPLE_COUNT = 60;
    private float _lastDebugLogTime;
    private const float DEBUG_LOG_INTERVAL = 0.5f; // Log every 0.5 seconds
    #endregion

    #region Private Classes
    /// <summary>
    /// Represents data needed for a tracked object's effect zone.
    /// Reduced version - no material instance needed.
    /// </summary>
    private class TrackedObjectData
    {
        public GameObject gameObject;
        public float distanceToCamera;
        public float baseEffectStrength; // Calculated based on proximity/fade
        public bool isVisible;
        public Vector2 viewportPosition; // Cache viewport position

        // Component reference to check the spawn state
        private CakeSpawner cakeSpawner; 

        // Optional: Could add per-object effect parameter overrides here if needed later
        
        public void Initialize(GameObject obj)
        {
            gameObject = obj;
            if (gameObject != null) 
            {
                // Get the component that has the 'HasBeenCollected' method
                cakeSpawner = gameObject.GetComponent<CakeSpawner>();
                // If the component doesn't exist, cakeSpawner will be null, and the check below will be skipped.
            }
        }
        
        public void UpdateProximity(Camera camera, float proximityThreshold, float fadeDistance, Vector2 positionOffset)
        {
            // --- Check Spawn State First ---
            if (cakeSpawner != null && cakeSpawner.HasBeenCollected())
            {
                isVisible = false;
                baseEffectStrength = 0f;
                viewportPosition = new Vector2(-1, -1); // Mark as invalid position too
                return; // Skip distance checks if spawned/collected
            }
            // --- End Spawn State Check ---

            if (gameObject == null || camera == null)
            {
                isVisible = false;
                baseEffectStrength = 0f;
                viewportPosition = new Vector2(-1, -1);
                return;
            }
            
            // Calculate distance to camera
            distanceToCamera = Vector3.Distance(camera.transform.position, gameObject.transform.position);
            
            // Determine visibility based on proximity
            if (distanceToCamera > proximityThreshold)
            {
                isVisible = false;
                baseEffectStrength = 0f;
                viewportPosition = new Vector2(-1, -1);
            }
            else
            {
                Vector3 objViewportPos = camera.WorldToViewportPoint(gameObject.transform.position);

                // Check if object is in front of camera
                if (objViewportPos.z <= 0) {
                    isVisible = false;
                    baseEffectStrength = 0f;
                    viewportPosition = new Vector2(-1, -1);
                    return;
                }

                isVisible = true;
                viewportPosition = new Vector2(objViewportPos.x + positionOffset.x, objViewportPos.y + positionOffset.y);

                // Calculate strength based on distance (fade in/out)
                if (fadeDistance > 0.01f) // Use a small epsilon
                {
                    float fadeStart = proximityThreshold - fadeDistance;
                    if (distanceToCamera > fadeStart)
                    {
                        // Apply fade based on distance
                        baseEffectStrength = 1f - Mathf.Clamp01((distanceToCamera - fadeStart) / fadeDistance);
                    }
                    else
                    {
                        baseEffectStrength = 1f; // Fully visible inside fade start zone
                    }
                }
                else
                {
                    baseEffectStrength = 1f; // No fade, fully visible within threshold
                }
            }
        }
    }
    #endregion

    #region Private Fields
    private Camera _camera;
    private List<GameObject> _taggedObjects = new List<GameObject>();
    private List<TrackedObjectData> _activeTrackedObjects = new List<TrackedObjectData>(); // Data for currently active/visible objects
    private float _nextTaggedObjectsRefreshTime;
    
    // Data arrays to pass to the shader
    private Vector4[] _maskDataArray; // xy=center, z=radius, w=strength
    private int _activeMaskCount = 0;
    
    // Shader property IDs
    private static readonly int ActiveMaskCountProperty = Shader.PropertyToID("_ActiveMaskCount");
    private static readonly int MaskDataProperty = Shader.PropertyToID("_MaskCenters"); // Assuming the shader uses _MaskCenters for the float4 array
    private static readonly int MaskSoftnessProperty = Shader.PropertyToID("_MaskSoftness");
    private static readonly int NoiseTexProperty = Shader.PropertyToID("_NoiseTex");
    // Glitch Property IDs
    private static readonly int ScanLineJitterProperty = Shader.PropertyToID("_ScanLineJitter");
    private static readonly int VerticalJumpProperty = Shader.PropertyToID("_VerticalJump");
    private static readonly int HorizontalShakeProperty = Shader.PropertyToID("_HorizontalShake");
    private static readonly int ColorDriftProperty = Shader.PropertyToID("_ColorDrift");
    private static readonly int PixelationAmountProperty = Shader.PropertyToID("_PixelationAmount");
    private static readonly int ColorGlitchProperty = Shader.PropertyToID("_ColorGlitch");
    private static readonly int BlockGlitchProperty = Shader.PropertyToID("_BlockGlitch");
    #endregion

    #region Unity Lifecycle Methods
    /// <summary>
    /// Initialize component references and validate configuration
    /// </summary>
    private void Awake()
    {
        _camera = GetComponent<Camera>();
        
        // Initialize data arrays (size clamped by MAX_TRACKED_OBJECTS_LIMIT)
        _maskDataArray = new Vector4[MAX_TRACKED_OBJECTS_LIMIT]; 

        ValidateConfiguration();
        RefreshTaggedObjects(); // Initial population
        RefreshActiveTrackedObjects(); // Initial setup based on mode
    }

    /// <summary>
    /// Ensures resources like the noise texture are set up if needed.
    /// </summary>
    private void Start()
    {
        EnsureNoiseTexture();
    }
    
    /// <summary>
    /// Updates the tracked objects and prepares data for the shader
    /// </summary>
    private void Update()
    {
        if (!IsConfigurationValid()) return;

        _performanceStopwatch.Restart();

        // Periodically refresh our list of *all* tagged objects if needed
        bool needsRefresh = (_trackingMode == TrackingMode.ClosestTagged || 
                             _trackingMode == TrackingMode.ActiveTagged ||
                             _trackingMode == TrackingMode.AllTagged) && 
                            Time.time > _nextTaggedObjectsRefreshTime;

        if (needsRefresh)
        {
            RefreshTaggedObjects();
            _nextTaggedObjectsRefreshTime = Time.time + _taggedObjectsRefreshRate;
            // Refreshing the active list is needed only if the underlying tagged list changed significantly
            // or if mode requires re-evaluating (e.g., ClosestTagged)
            RefreshActiveTrackedObjects(); 
        }
        else if (_trackingMode == TrackingMode.ClosestTagged) {
             // Closest object might change even if the tagged list doesn't, so we need to potentially update the active list
             RefreshActiveTrackedObjects();
        }


        // Update proximity and visibility for currently active objects
        UpdateActiveTrackedObjectsProximity();

        // Prepare data arrays for the shader based on visible objects
        PrepareShaderData();

        _performanceStopwatch.Stop();
        _lastUpdateTime = _performanceStopwatch.ElapsedMilliseconds;

        // Handle debug logging with rate limiting
        if (_enableDebugLogging && Time.time >= _lastDebugLogTime + DEBUG_LOG_INTERVAL)
        {
            LogDebugInfo();
            _lastDebugLogTime = Time.time;
        }
        
        // Update performance metrics averaging
        _updateFrameCount++;
        if (_updateFrameCount >= FRAME_SAMPLE_COUNT)
        {
            // Note: This captures only Update time, not Render time here.
            // Average calculation moved to OnRenderImage where total time is available.
            _updateFrameCount = 0; 
        }
    }
    
    /// <summary>
    /// Clean up (if needed, e.g., destroy generated noise texture)
    /// </summary>
    private void OnDestroy()
    {
        // If noise texture was generated, destroy it
        // (Assuming _noiseTexture might be assigned externally too, need check)
    }
    
    /// <summary>
    /// Apply the shader effect using the prepared data
    /// </summary>
    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        _performanceStopwatch.Restart();

        if (_multiMaskMaterial == null || _activeMaskCount == 0 || !IsConfigurationValid())
        {
            Graphics.Blit(source, destination); // Pass through if effect inactive or invalid
            _performanceStopwatch.Stop();
            _lastRenderTime = _performanceStopwatch.ElapsedMilliseconds;
            return;
        }
        
        // Set shader parameters
        _multiMaskMaterial.SetInt(ActiveMaskCountProperty, _activeMaskCount);
        _multiMaskMaterial.SetVectorArray(MaskDataProperty, _maskDataArray); // Pass center, radius, strength
        _multiMaskMaterial.SetFloat(MaskSoftnessProperty, _maskSoftness);

        // Set Glitch parameters
        if (_noiseTexture != null) {
             _multiMaskMaterial.SetTexture(NoiseTexProperty, _noiseTexture);
        }
        _multiMaskMaterial.SetFloat(ScanLineJitterProperty, _scanLineJitter);
        _multiMaskMaterial.SetFloat(VerticalJumpProperty, _verticalJump);
        _multiMaskMaterial.SetFloat(HorizontalShakeProperty, _horizontalShake);
        _multiMaskMaterial.SetFloat(ColorDriftProperty, _colorDrift);
        _multiMaskMaterial.SetFloat(PixelationAmountProperty, _pixelation);
        _multiMaskMaterial.SetFloat(ColorGlitchProperty, _colorGlitch);
        _multiMaskMaterial.SetFloat(BlockGlitchProperty, _blockGlitch);

        // Execute the shader pass
        Graphics.Blit(source, destination, _multiMaskMaterial);

        _performanceStopwatch.Stop();
        _lastRenderTime = _performanceStopwatch.ElapsedMilliseconds;
        
        // Update performance metrics average (including render time)
        if (_updateFrameCount == 0) // Use the flag set in Update
        {
             // Averaging occurs once every FRAME_SAMPLE_COUNT frames
            _averageFrameTime = (_lastUpdateTime + _lastRenderTime); // Simplistic average, could use moving average
        }
    }

    private void OnDrawGizmos()
    {
        if (!enabled || _debugVisualization == DebugVisualization.None)
            return;

        switch (_debugVisualization)
        {
            case DebugVisualization.ProximityZones:
                DrawProximityZones();
                break;
            // case DebugVisualization.MaskAreas: // Needs reimplementation
            //     DrawMaskAreas(); 
            //     break;
            case DebugVisualization.Performance:
                DrawPerformanceMetrics();
                break;
        }
    }
    #endregion

    #region Private Methods

    /// <summary>
    /// Generates a default noise texture if none is assigned.
    /// </summary>
    private void EnsureNoiseTexture()
    {
        if (_noiseTexture == null)
        {
            _noiseTexture = new Texture2D(64, 64, TextureFormat.RGB24, false);
            _noiseTexture.wrapMode = TextureWrapMode.Repeat;
            _noiseTexture.filterMode = FilterMode.Point; // Keep noise sharp
            Color[] colors = new Color[64 * 64];
            for (int i = 0; i < colors.Length; i++)
            {
                float val = UnityEngine.Random.value;
                colors[i] = new Color(val, val, val, 1);
            }
            _noiseTexture.SetPixels(colors);
            _noiseTexture.Apply();
            Debug.Log("AdvancedMaskedAreaEffect: Generated default noise texture.", this);
        }
        // Ensure material gets the texture if assigned in inspector AFTER start
        if (_multiMaskMaterial != null && _multiMaskMaterial.HasProperty(NoiseTexProperty)) {
            _multiMaskMaterial.SetTexture(NoiseTexProperty, _noiseTexture);
        }
    }

    /// <summary>
    /// Validates that all required components and settings are properly configured
    /// </summary>
    private void ValidateConfiguration()
    {
        bool isValid = true;
        if (_camera == null) {
             Debug.LogError("AdvancedMaskedAreaEffect: Camera component not found!", this);
             isValid = false;
        }

        if (_trackingMode == TrackingMode.SingleObject && _targetObject == null)
        {
            Debug.LogWarning("AdvancedMaskedAreaEffect: Target object is not assigned for SingleObject tracking mode.", this);
            // Don't disable, might be set later
        }
        
        if ((_trackingMode == TrackingMode.ClosestTagged || 
             _trackingMode == TrackingMode.ActiveTagged ||
             _trackingMode == TrackingMode.AllTagged) 
            && string.IsNullOrEmpty(_targetTag))
        {
            Debug.LogError("AdvancedMaskedAreaEffect: Target tag is not assigned for tag-based tracking mode!", this);
             isValid = false;
        }
        
        if (_multiMaskMaterial == null)
        {
            Debug.LogError("AdvancedMaskedAreaEffect: Multi Mask Material is not assigned!", this);
             isValid = false;
        } else if (_multiMaskShader != null && _multiMaskMaterial.shader != _multiMaskShader) {
             Debug.LogWarning($"AdvancedMaskedAreaEffect: Assigned material '{_multiMaskMaterial.name}' does not use the expected shader '{_multiMaskShader.name}'.", this);
             // Don't disable, might be intentional override
        } else if (_multiMaskShader == null && _multiMaskMaterial.shader.name != "Custom/MultiMaskEffectShader") {
             // If shader ref is null, check by name
             Debug.LogWarning($"AdvancedMaskedAreaEffect: Assigned material '{_multiMaskMaterial.name}' does not appear to use the 'Custom/MultiMaskEffectShader'.", this);
        }
        
        // Clamp max tracked objects based on limit
        _maxTrackedObjects = Mathf.Min(_maxTrackedObjects, MAX_TRACKED_OBJECTS_LIMIT);

        enabled = isValid; // Only fully disable if critical components missing
    }
    
    /// <summary>
    /// Checks if the current configuration allows the effect to run.
    /// </summary>
    private bool IsConfigurationValid()
    {
        if (_multiMaskMaterial == null || _camera == null) return false;

        // Check if there's *potential* for tracked objects based on mode
        if (_trackingMode == TrackingMode.SingleObject) return _targetObject != null;
        if (_trackingMode == TrackingMode.ClosestTagged || 
            _trackingMode == TrackingMode.ActiveTagged ||
            _trackingMode == TrackingMode.AllTagged) return !string.IsNullOrEmpty(_targetTag); // Tagged objects list might be empty now but populated later

        return false; // Should not happen with enum
    }

    /// <summary>
    /// Refreshes the internal list of ALL GameObjects with the target tag.
    /// </summary>
    private void RefreshTaggedObjects()
    {
        if (string.IsNullOrEmpty(_targetTag)) {
            _taggedObjects.Clear();
            return;
        }
            
        // Avoid GC alloc if possible by reusing list? FindGameObjectsWithTag always allocs new array.
        GameObject[] foundObjects = GameObject.FindGameObjectsWithTag(_targetTag);
        _taggedObjects.Clear();
        if (foundObjects != null) {
            _taggedObjects.AddRange(foundObjects);
        }
        
        // Ensure the active index is valid after refresh
        if (_trackingMode == TrackingMode.ActiveTagged)
        {
            _activeTaggedObjectIndex = _taggedObjects.Count > 0 ? Mathf.Clamp(_activeTaggedObjectIndex, 0, _taggedObjects.Count - 1) : 0;
        }

        if (_enableDebugLogging) {
             Debug.Log($"Refreshed tagged objects ({_targetTag}): Found {_taggedObjects.Count}");
        }
    }
    
    /// <summary>
    /// Updates the `_activeTrackedObjects` list based on the current tracking mode and the `_taggedObjects` list.
    /// Tries to populate up to _maxTrackedObjects based on proximity or active index.
    /// </summary>
    private void RefreshActiveTrackedObjects()
    {
        _activeTrackedObjects.Clear(); // Clear the list of objects we actually process
        
        // Temporary list to hold potential candidates and sort them
        List<GameObject> candidates = new List<GameObject>(_taggedObjects.Where(obj => obj != null)); // Filter out nulls immediately
        Vector3 camPos = _camera.transform.position;

        switch (_trackingMode)
        {
            case TrackingMode.SingleObject:
                if (_targetObject != null)
                {
                    AddTrackedObjectData(_targetObject);
                }
                break;
                
            case TrackingMode.ClosestTagged:
            case TrackingMode.AllTagged: // Treat AllTagged as tracking the N closest
                if (candidates.Count > 0)
                {
                    // Sort all valid candidates by distance
                    candidates.Sort((a, b) => 
                        Vector3.Distance(a.transform.position, camPos)
                        .CompareTo(Vector3.Distance(b.transform.position, camPos))
                    );
                    
                    // Add the closest ones up to the limit
                    int count = Mathf.Min(candidates.Count, _maxTrackedObjects);
                    for (int i = 0; i < count; i++)
                    {
                        AddTrackedObjectData(candidates[i]);
                    }
                }
                break;
                
            case TrackingMode.ActiveTagged:
                if (candidates.Count > 0)
                {
                    // Clamp index just in case
                    _activeTaggedObjectIndex = Mathf.Clamp(_activeTaggedObjectIndex, 0, candidates.Count - 1); 
                    GameObject activeObject = candidates[_activeTaggedObjectIndex];
                    
                    // Add the active object first
                    AddTrackedObjectData(activeObject);
                    
                    // If we need more objects and have more candidates
                    if (_maxTrackedObjects > 1 && candidates.Count > 1)
                    {
                        // Remove the active object from candidates
                        candidates.RemoveAt(_activeTaggedObjectIndex);
                        
                        // Sort remaining candidates by distance
                        candidates.Sort((a, b) => 
                            Vector3.Distance(a.transform.position, camPos)
                            .CompareTo(Vector3.Distance(b.transform.position, camPos))
                        );
                        
                        // Add the closest remaining ones until the limit is reached
                        int needed = _maxTrackedObjects - 1; // Already added one
                        int available = candidates.Count;
                        int countToAdd = Mathf.Min(needed, available);
                        
                        for (int i = 0; i < countToAdd; i++)
                        {
                            AddTrackedObjectData(candidates[i]);
                        }
                    }
                }
                break;
        }
         if (_enableDebugLogging) {
             Debug.Log($"Refreshed active tracked objects: Count = {_activeTrackedObjects.Count} (Mode: {_trackingMode})");
        }
    }
    
    /// <summary>
    /// Helper to add a GameObject to the `_activeTrackedObjects` list.
    /// </summary>
    private void AddTrackedObjectData(GameObject target)
    {
        if (target == null) return;

        TrackedObjectData data = new TrackedObjectData();
        data.Initialize(target);
        _activeTrackedObjects.Add(data);
    }
    
    /// <summary>
    /// Updates proximity, visibility, and position for all objects in the `_activeTrackedObjects` list.
    /// </summary>
    private void UpdateActiveTrackedObjectsProximity()
    {
        // First pass: update proximity and visibility
        foreach (TrackedObjectData objData in _activeTrackedObjects)
        {
             // Handle cases where the object might have been destroyed since the list was last refreshed
            if (objData.gameObject == null) {
                objData.isVisible = false; // Mark as invalid/invisible
                continue;
            }
            objData.UpdateProximity(_camera, _proximityThreshold, _fadeDistance, _positionOffset);
        }

        // Optional: Could prune the list of invisible objects here if performance is an issue
        // _activeTrackedObjects.RemoveAll(d => !d.isVisible); 
        // Be careful if RefreshActiveTrackedObjects relies on the full list logic
    }
    
    /// <summary>
    /// Populates the `_maskDataArray` with data from visible `_activeTrackedObjects`.
    /// </summary>
    private void PrepareShaderData()
    {
        _activeMaskCount = 0;
        foreach (TrackedObjectData objData in _activeTrackedObjects)
        {
            if (objData.isVisible && _activeMaskCount < _maxTrackedObjects) // Check visibility AND limit
            {
                _maskDataArray[_activeMaskCount] = new Vector4(
                    objData.viewportPosition.x,
                    objData.viewportPosition.y,
                    _maskSize, // Use global mask size for now
                    objData.baseEffectStrength // Pass proximity/fade strength
                );
                _activeMaskCount++;
            }
        }
        
        // Zero out remaining slots in the array (important!)
        for (int i = _activeMaskCount; i < _maxTrackedObjects; ++i) {
             _maskDataArray[i] = Vector4.zero; // Or some other indicator of inactivity if needed
        }
    }
    #endregion

    #region Public Methods
    // --- Configuration Setters ---
    
    /// <summary>Sets the tracking mode and refreshes tracked objects.</summary>
    public void SetTrackingMode(TrackingMode mode)
    {
        _trackingMode = mode;
        RefreshTaggedObjects(); // Need to update base list if switching to/from tag modes
        RefreshActiveTrackedObjects(); // Update the list being processed
        ValidateConfiguration();
    }
    
    /// <summary>Sets a new target GameObject for SingleObject mode.</summary>
    public void SetTarget(GameObject newTarget)
    {
        _targetObject = newTarget;
        if (_trackingMode == TrackingMode.SingleObject)
        {
            RefreshActiveTrackedObjects();
        }
        // No need to call ValidateConfiguration here unless _targetObject being null should disable
    }
    
    /// <summary>Sets the tag for tag-based tracking modes.</summary>
    public void SetTargetTag(string tag)
    {
        if (!string.IsNullOrEmpty(tag))
        {
            _targetTag = tag;
            // Only refresh if currently in a tag-based mode
            if (_trackingMode == TrackingMode.ClosestTagged || _trackingMode == TrackingMode.ActiveTagged || _trackingMode == TrackingMode.AllTagged)
            {
                 RefreshTaggedObjects();
                 RefreshActiveTrackedObjects();
            }
            ValidateConfiguration(); // Re-validate as tag is critical for these modes
        } else {
             Debug.LogWarning("SetTargetTag: Provided tag is null or empty.", this);
        }
    }
    
    /// <summary>Sets the proximity threshold distance.</summary>
    public void SetProximityThreshold(float threshold) { _proximityThreshold = Mathf.Max(0.1f, threshold); }
    
    /// <summary>Sets the fade distance.</summary>
    public void SetFadeDistance(float distance) { _fadeDistance = Mathf.Max(0f, distance); } // Allow zero fade
    
    /// <summary>Sets the mask size.</summary>
    public void SetMaskSize(float size) { _maskSize = Mathf.Clamp(size, 0.01f, 0.5f); }
    
    /// <summary>Sets the mask softness.</summary>
    public void SetMaskSoftness(float softness) { _maskSoftness = Mathf.Clamp(softness, 0.01f, 1f); }

    /// <summary>Sets the maximum number of simultaneously tracked objects (up to shader limit).</summary>
    public void SetMaxTrackedObjects(int max)
    {
        int newMax = Mathf.Clamp(max, 1, MAX_TRACKED_OBJECTS_LIMIT);
        if (newMax != _maxTrackedObjects) {
             _maxTrackedObjects = newMax;
            // Re-evaluate active objects if in 'AllTagged' mode and count decreased
            if (_trackingMode == TrackingMode.AllTagged)
            {
                RefreshActiveTrackedObjects();
            }
        }
    }

    // --- Glitch Parameter Setters ---
    public void SetNoiseTexture(Texture2D texture) { _noiseTexture = texture; EnsureNoiseTexture(); } // Re-apply texture
    public void SetScanLineJitter(float value) { _scanLineJitter = Mathf.Clamp01(value); }
    public void SetVerticalJump(float value) { _verticalJump = Mathf.Clamp01(value); }
    public void SetHorizontalShake(float value) { _horizontalShake = Mathf.Clamp01(value); }
    public void SetColorDrift(float value) { _colorDrift = Mathf.Clamp01(value); }
    public void SetPixelation(float value) { _pixelation = Mathf.Clamp01(value); }
    public void SetColorGlitch(float value) { _colorGlitch = Mathf.Clamp01(value); }
    public void SetBlockGlitch(float value) { _blockGlitch = Mathf.Clamp01(value); }


    // --- Control Methods ---

    /// <summary>Switches to the next tagged object in ActiveTagged mode.</summary>
    public void NextTaggedObject()
    {
        if (_trackingMode == TrackingMode.ActiveTagged && _taggedObjects.Count > 0)
        {
            _activeTaggedObjectIndex = (_activeTaggedObjectIndex + 1) % _taggedObjects.Count;
            RefreshActiveTrackedObjects();
        }
    }
    
    /// <summary>Switches to the previous tagged object in ActiveTagged mode.</summary>
    public void PreviousTaggedObject()
    {
        if (_trackingMode == TrackingMode.ActiveTagged && _taggedObjects.Count > 0)
        {
            _activeTaggedObjectIndex = (_activeTaggedObjectIndex - 1 + _taggedObjects.Count) % _taggedObjects.Count;
            RefreshActiveTrackedObjects();
        }
    }
    
    /// <summary>Force refresh of tagged object list and active tracked objects.</summary>
    public void RefreshEffects()
    {
        RefreshTaggedObjects();
        RefreshActiveTrackedObjects();
    }

    // --- Information Getters ---

    /// <summary>Gets the count of all found objects with the target tag.</summary>
    /// <returns>Number of tagged objects found.</returns>
    public int GetTaggedObjectCount() { return _taggedObjects.Count; }
    
    /// <summary>Gets the count of actively processed effect zones (visible and within limit).</summary>
    /// <returns>Number of active masks being sent to the shader.</returns>
    public int GetActiveMaskCount() { return _activeMaskCount; }
    
    /// <summary>Gets the index of the currently selected tagged object in ActiveTagged mode.</summary>
    public int GetActiveTaggedObjectIndex() { return _activeTaggedObjectIndex; }

    // Methods like GetTrackedObjectViewportPosition, GetTrackedObjectEffectStrength, GetMaskSize
    // are removed as the rendering now uses the internal arrays directly.
    // If external access to this data is needed, new getters could be added to query _activeTrackedObjects.

    #endregion

    #region Debug Methods
    /// <summary>Draws debug visualization for proximity and fade zones.</summary>
    private void DrawProximityZones()
    {
        if (_camera == null) return;

        // Draw for all potential tagged objects for context
        Gizmos.color = Color.gray * 0.5f;
        foreach(var obj in _taggedObjects) {
             if (obj != null) Gizmos.DrawWireSphere(obj.transform.position, _proximityThreshold);
        }

        // Highlight active objects and their states
        foreach (TrackedObjectData objData in _activeTrackedObjects)
        {
            if (objData.gameObject != null)
            {
                // Draw proximity threshold sphere
                Gizmos.color = objData.isVisible ? _proximityThresholdColor : Color.grey;
                Gizmos.DrawWireSphere(objData.gameObject.transform.position, _proximityThreshold);

                // Draw fade distance sphere (inner edge of fade)
                float fadeStartRadius = _proximityThreshold - _fadeDistance;
                if (fadeStartRadius > 0) {
                    Gizmos.color = objData.isVisible ? _fadeDistanceColor : Color.grey * 0.8f;
                    Gizmos.DrawWireSphere(objData.gameObject.transform.position, fadeStartRadius);
                }

                // Draw line to camera indicating strength/visibility
                if (objData.isVisible)
                {
                    Gizmos.color = Color.Lerp(Color.red, Color.green, objData.baseEffectStrength);
                    Gizmos.DrawLine(_camera.transform.position, objData.gameObject.transform.position);
                }
            }
        }
    }

    // DrawMaskAreas needs significant rework as masks are now calculated per-pixel in the shader.
    // Could potentially draw screen-space indicators using Handles/GUI, but complex.
    // private void DrawMaskAreas() { ... } 

    /// <summary>Draws debug visualization for performance metrics.</summary>
    private void DrawPerformanceMetrics()
    {
        if (!_showPerformanceMetrics || _camera == null) return;

        // Display metrics in the scene view using Handles GUI
        string metrics = $"Update: {_lastUpdateTime:F1}ms " +
                        $"Render: {_lastRenderTime:F1}ms " +
                        $"Avg Frame: {_averageFrameTime:F1}ms " +
                        $"Active Masks: {_activeMaskCount}/{_activeTrackedObjects.Count}";

#if UNITY_EDITOR
        UnityEditor.Handles.BeginGUI();
        // Position label slightly offset from top-left corner
        GUI.Box(new Rect(10, 10, 150, 70), "Perf Metrics"); // Box for background
        GUI.Label(new Rect(15, 30, 140, 60), metrics);
        UnityEditor.Handles.EndGUI();
#endif
    }

    /// <summary>Logs detailed debug information to the console.</summary>
    private void LogDebugInfo()
    {
        if (!_enableDebugLogging) return;

        _debugStringBuilder.Clear();
        _debugStringBuilder.AppendLine("===== AdvancedMaskedAreaEffect Debug Info =====");
        _debugStringBuilder.AppendLine($"Timestamp: {Time.time:F2}");
        _debugStringBuilder.AppendLine($"Tracking Mode: {_trackingMode} | Target Tag: '{_targetTag}' | Single Target: {(_targetObject != null ? _targetObject.name : "None")}");
        _debugStringBuilder.AppendLine($"Tagged Objects Found: {_taggedObjects.Count}");
        _debugStringBuilder.AppendLine($"Active Tracked Objects (Processed): {_activeTrackedObjects.Count}");
        _debugStringBuilder.AppendLine($"Active Masks Sent to Shader: {_activeMaskCount} / {_maxTrackedObjects} (Limit)");
        _debugStringBuilder.AppendLine($"Proximity Threshold: {_proximityThreshold} | Fade Distance: {_fadeDistance}");
        _debugStringBuilder.AppendLine($"Performance (ms): Update={_lastUpdateTime:F2}, Render={_lastRenderTime:F2}, AvgFrame={_averageFrameTime:F2}");
        _debugStringBuilder.AppendLine("--- Active Tracked Objects Status ---");

        int count = 0;
        foreach (TrackedObjectData objData in _activeTrackedObjects)
        {
            count++;
            if (objData.gameObject != null)
            {
                _debugStringBuilder.AppendLine($"[{count}] {objData.gameObject.name}:");
                _debugStringBuilder.AppendLine($"  Visible: {objData.isVisible} | Dist: {objData.distanceToCamera:F2} | Strength: {objData.baseEffectStrength:F2}");
                _debugStringBuilder.AppendLine($"  Viewport Pos: {objData.viewportPosition}");
            } else {
                 _debugStringBuilder.AppendLine($"[{count}] GameObject is NULL");
            }
        }
         _debugStringBuilder.AppendLine("----------------------------------------");

        Debug.Log(_debugStringBuilder.ToString());
    }
    
    // Gizmo drawing helpers (Circle, Square, Diamond) are removed as DrawMaskAreas is disabled/needs rework.
    // Re-add them if implementing screen-space gizmos for masks.

    #endregion

    #region Public Debug Methods
    /// <summary>Enables or disables detailed debug logging.</summary>
    public void SetDebugLogging(bool enable)
    {
        _enableDebugLogging = enable;
        if (enable) LogDebugInfo(); // Log immediately when enabled
    }

    /// <summary>Sets the debug visualization mode.</summary>
    public void SetDebugVisualization(DebugVisualization mode) { _debugVisualization = mode; }

    /// <summary>Toggles the display of performance metrics overlay.</summary>
    public void TogglePerformanceMetrics() { _showPerformanceMetrics = !_showPerformanceMetrics; }

    /// <summary>Gets a string containing detailed debug information.</summary>
    public string GetDebugInfo()
    {
        // Reuse the LogDebugInfo logic but return the string
        LogDebugInfo(); // Populates the string builder
        return _debugStringBuilder.ToString();
    }
    #endregion
}
