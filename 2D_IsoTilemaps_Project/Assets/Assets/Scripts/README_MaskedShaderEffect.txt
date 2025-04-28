# Masked Camera Shader Effect

This package includes components to create a camera shader effect that only appears in a masked area tracking GameObjects, with each effect only visible when near its corresponding object.

## Files Included

- `MaskedAreaShader.shader`: Basic shader that renders a grayscale effect in a circular mask
- `AdvancedMaskedAreaShader.shader`: Advanced shader with multiple mask shapes and effect types
- `MaskedAreaTracker.cs`: Basic script to track GameObjects with the shader mask
- `AdvancedMaskedAreaEffect.cs`: Advanced script with more configuration options
- `MaskedAreaEffectDemo.cs`: Demo script showing how to control the effect at runtime

## Setup Instructions

### Basic Setup

1. Create a new Material:
   - In the Project window, right-click and select Create > Material
   - Name it "MaskedAreaMaterial"
   - In the Inspector, set the Shader to "Custom/MaskedAreaShader"

2. Set up your Camera:
   - Select your main Camera in the Scene
   - Add the "MaskedAreaTracker" component
   - Choose the tracking type:
     - SingleObject: Assign a GameObject to the "Target Object" field
     - ByTag: Enter a tag name in the "Target Tag" field
   - Drag your "MaskedAreaMaterial" into the "Effect Material" field
   - Adjust the Mask Radius and Effect Strength as needed

### Advanced Setup

1. Create a new Material:
   - In the Project window, right-click and select Create > Material
   - Name it "AdvancedMaskedAreaMaterial"
   - In the Inspector, set the Shader to "Custom/AdvancedMaskedAreaShader"

2. Set up your Camera:
   - Select your main Camera in the Scene
   - Add the "AdvancedMaskedAreaEffect" component
   - Choose the tracking mode:
     - SingleObject: Assign a GameObject to the "Target Object" field
     - ClosestTagged: Enter a tag to track the closest object with that tag
     - ActiveTagged: Enter a tag to track one of many tagged objects, with ability to switch between them
     - AllTagged: Track all objects with the specified tag simultaneously
   - Drag your "AdvancedMaskedAreaMaterial" into the "Effect Material Template" field
   - Configure the following settings:
     - Proximity Threshold: Distance at which effects become visible (units)
     - Fade Distance: Distance over which effects fade in/out (units)
     - Max Tracked Objects: Maximum number of objects to track (AllTagged mode)
     - Position Offset: Adjust the offset of the mask from the target
     - Mask Shape: Choose between Circle, Square, or Diamond
     - Mask Size: Set the size of the mask
     - Mask Softness: Adjust the softness of the mask edges
     - Effect Type: Select from Grayscale, Sepia, Invert, Pixelate, or Edge Detection
     - Effect Strength: Control the intensity of the effect

## Proximity-Based Effects

A key feature of this system is that shader effects are only visible when near the corresponding objects:

1. **Proximity Threshold**: Each effect is only visible when the camera is within this distance of the tracked object.

2. **Fade Distance**: Effects smoothly fade in and out over this distance as you approach or move away from objects.

3. **Per-Object Instances**: Each tracked object gets its own shader instance, allowing for independent visibility.

4. **Benefits**:
   - Reduced visual clutter - effects only appear for nearby objects
   - Performance optimization - distant effects are disabled
   - Contextual awareness - focus on nearby objects

## Runtime Control

You can dynamically change the effect parameters at runtime using the following methods:

### Basic Script (MaskedAreaTracker)
```csharp
// Get a reference to the component
MaskedAreaTracker tracker = Camera.main.GetComponent<MaskedAreaTracker>();

// Change tracking type
tracker.SetTrackingType(MaskedAreaTracker.TrackingType.SingleObject);
// or
tracker.SetTrackingType(MaskedAreaTracker.TrackingType.ByTag);

// Track a specific object
tracker.SetTarget(newTarget);

// Track objects with a specific tag
tracker.SetTargetTag("Player");

// Get the count of objects with the target tag
int count = tracker.GetTaggedObjectCount();

// Adjust mask size
tracker.SetMaskRadius(0.3f);

// Adjust effect strength
tracker.SetEffectStrength(0.5f);
```

### Advanced Script (AdvancedMaskedAreaEffect)
```csharp
// Get a reference to the component
AdvancedMaskedAreaEffect effect = Camera.main.GetComponent<AdvancedMaskedAreaEffect>();

// Set tracking mode
effect.SetTrackingMode(AdvancedMaskedAreaEffect.TrackingMode.SingleObject);
// or
effect.SetTrackingMode(AdvancedMaskedAreaEffect.TrackingMode.ClosestTagged);
// or
effect.SetTrackingMode(AdvancedMaskedAreaEffect.TrackingMode.ActiveTagged);
// or
effect.SetTrackingMode(AdvancedMaskedAreaEffect.TrackingMode.AllTagged);

// Track a specific object
effect.SetTarget(newTarget);

// Track objects with a specific tag
effect.SetTargetTag("Enemy");

// Set proximity parameters
effect.SetProximityThreshold(15f); // Effects visible within 15 units
effect.SetFadeDistance(3f); // Effects fade in/out over 3 units

// Set maximum tracked objects (for AllTagged mode)
effect.SetMaxTrackedObjects(10);

// Navigate through tagged objects (in ActiveTagged mode)
effect.NextTaggedObject();
effect.PreviousTaggedObject();

// Get information about tagged objects
int totalCount = effect.GetTaggedObjectCount();
int visibleCount = effect.GetVisibleTrackedObjectCount();
int activeIndex = effect.GetActiveTaggedObjectIndex();

// Change mask shape
effect.SetMaskShape(AdvancedMaskedAreaEffect.MaskShape.Diamond);

// Change effect type
effect.SetEffectType(AdvancedMaskedAreaEffect.ShaderEffect.Sepia);

// Adjust mask parameters
effect.SetMaskSize(0.25f);
effect.SetMaskSoftness(0.2f);

// Adjust effect strength
effect.SetEffectStrength(0.8f);

// Force refresh all tracked objects and effects
effect.RefreshEffects();
```

## Demo Script

The included `MaskedAreaEffectDemo.cs` script demonstrates how to use the effect:

1. Attach it to any GameObject in your scene
2. Assign a reference to your AdvancedMaskedAreaEffect component
3. Set initial tracking settings (object or tag)
4. Run the scene and use these keyboard controls:
   - `1-5` keys: Change effect type
   - `Q/E` keys: Change mask shape
   - `+/-` keys: Adjust mask size
   - `T` key: Cycle between tracking modes
   - `N/P` keys: Navigate to next/previous tagged object (in ActiveTagged mode)
   - `R` key: Refresh the list of tagged objects
   - `F/G` keys: Increase/decrease proximity threshold
   - `V/B` keys: Increase/decrease fade distance
   - `SPACE`: Display information about active tracked objects

## Performance Considerations

- Each tracked object requires its own material instance, so limit the Max Tracked Objects value in high-density scenes
- Effects are only processed for objects within the proximity threshold, improving performance
- For best performance, disable MSAA on the camera that uses this effect
- When using tag-based tracking, the refresh rate of tagged objects is configurable
- Consider using a lower resolution render texture for the effect if performance is a concern

## Troubleshooting

- If no effects are visible:
  - Check that objects are within the Proximity Threshold distance
  - Try increasing the Proximity Threshold or decreasing the Fade Distance
  - Verify that tracked objects are active and have the correct tag

- If effects aren't updating properly:
  - Press the 'R' key in the demo to force refresh all effects
  - Check that your game objects have proper tags assigned
  - Ensure the material template is correctly assigned to the component

- If you see errors about "_MaskCenter", ensure you're using the correct shader with the corresponding script 