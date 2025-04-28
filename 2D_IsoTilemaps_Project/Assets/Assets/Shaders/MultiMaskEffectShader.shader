Shader "Custom/MultiMaskEffectShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _NoiseTex ("Noise Texture", 2D) = "black" {} // For digital glitch noise

        // Max number of masks (must match script constant)
        _MaxMasks ("Max Masks", Int) = 10 // Read-only, just for info

        // Global Glitch Controls (Analog-style)
        _ScanLineJitter ("Scan Line Jitter", Range(0, 1)) = 0
        _VerticalJump ("Vertical Jump", Range(0, 1)) = 0
        _HorizontalShake ("Horizontal Shake", Range(0, 1)) = 0
        _ColorDrift ("Color Drift", Range(0, 1)) = 0

        // Global Glitch Controls (Digital-style)
        _PixelationAmount ("Pixelation", Range(0, 1)) = 0
        _ColorGlitch ("Color Channel Glitch", Range(0, 1)) = 0
        _BlockGlitch ("Block Glitch", Range(0, 1)) = 0

        // Mask Settings (used per-mask from arrays)
        _MaskSoftness ("Global Mask Softness", Range(0.01, 1)) = 0.5
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100
        ZWrite Off Cull Off // Standard for post-processing

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0 // Need shader model 3 for array support potentially

            #include "UnityCG.cginc"

            // Constants (Must match C# script)
            #define MAX_MASKS 10

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            // Textures
            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _MainTex_TexelSize;
            sampler2D _NoiseTex;

            // Mask Data (Passed from script)
            int _ActiveMaskCount;
            float4 _MaskCenters[MAX_MASKS]; // xy = center (viewport), z = radius, w = strength
            // Separate arrays might be needed if float4 array doesn't work reliably everywhere
            // float2 _MaskCenters[MAX_MASKS];
            // float _MaskRadii[MAX_MASKS];
            // float _EffectStrengths[MAX_MASKS];

            // Global Effect Params
            float _ScanLineJitter;
            float _VerticalJump;
            float _HorizontalShake;
            float _ColorDrift;
            float _PixelationAmount;
            float _ColorGlitch;
            float _BlockGlitch;
            float _MaskSoftness; // Controls the transition edge

            // Random function (from previous CustomGlitchShader)
            float rand(float2 co)
            {
                return frac(sin(dot(co.xy ,float2(12.9898,78.233))) * 43758.5453);
            }

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                // Correct UV handling for post-processing
                o.uv = v.uv;
                // Compensate for rendering differences in platforms
                #if UNITY_UV_STARTS_AT_TOP
                if (_MainTex_TexelSize.y < 0)
                    o.uv.y = 1 - o.uv.y;
                #endif
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float2 uv = i.uv;
                fixed4 originalColor = tex2D(_MainTex, uv);
                
                // Calculate maximum intensity contribution from all masks
                float maxIntensity = 0.0;
                for (int k = 0; k < _ActiveMaskCount; k++)
                {
                    float2 center = _MaskCenters[k].xy;
                    float radius = _MaskCenters[k].z;
                    float strength = _MaskCenters[k].w;

                    float dist = distance(uv, center);
                    
                    // Calculate intensity with smoothstep for softness
                    // Inner edge = radius * (1 - softness)
                    // Outer edge = radius
                    float softRadius = radius * max(0.01, 1.0 - _MaskSoftness); // Ensure non-zero softness range
                    float currentIntensity = smoothstep(radius, softRadius, dist) * strength;
                    
                    maxIntensity = max(maxIntensity, currentIntensity);
                }

                // If outside all masks, return original color
                if (maxIntensity <= 0.0)
                {
                    return originalColor;
                }
                
                // --- Apply Glitch Effect based on maxIntensity ---
                float time = _Time.y;
                float4 noise = tex2D(_NoiseTex, uv * 2.0 + time * 0.1); // Add some movement to noise

                // Calculate glitch amount scaled by intensity
                float scanLineJitter = _ScanLineJitter * maxIntensity;
                float verticalJump = _VerticalJump * maxIntensity;
                float horizontalShake = _HorizontalShake * maxIntensity;
                float colorDrift = _ColorDrift * maxIntensity;
                float pixelation = _PixelationAmount * maxIntensity;
                float colorGlitch = _ColorGlitch * maxIntensity;
                float blockGlitch = _BlockGlitch * maxIntensity;

                // --- Apply effects to UV / Color (Adapted from CustomGlitchShader) ---
                float2 glitchUV = uv;

                // Scan line jitter
                float jitter = (rand(float2(time, uv.y)) * 2.0 - 1.0) * scanLineJitter * 0.05;
                glitchUV.y += jitter;

                // Vertical jump (using time and position)
                float jumpAmount = rand(float2(floor(time * 10.0), uv.x)) * verticalJump;
                glitchUV.y = frac(glitchUV.y + jumpAmount);

                // Horizontal shake
                glitchUV.x += (rand(time + uv.y) * 2.0 - 1.0) * horizontalShake * 0.01;

                // Pixelation
                if (pixelation > 0.01) // Add threshold to avoid divisions by zero/tiny numbers
                {
                    float pixelSize = lerp(1.0, 0.05, pixelation); // Inverse relationship: higher pixelation -> smaller pixelSize
                    float pixelDensity = 1.0 / pixelSize * 100.0; // Map 0-1 pixelation to reasonable pixel density
                    glitchUV = floor(glitchUV * pixelDensity) / pixelDensity;
                }

                // Sample base color with glitched UVs
                fixed4 baseGlitchColor = tex2D(_MainTex, glitchUV);

                // Color drift (sample neighbors)
                float2 driftOffset = float2(0.005, 0.005) * colorDrift;
                fixed4 driftColor = fixed4(
                    tex2D(_MainTex, glitchUV + driftOffset).r,
                    baseGlitchColor.g,
                    tex2D(_MainTex, glitchUV - driftOffset).b,
                    baseGlitchColor.a
                );

                // Color channel glitch (using noise texture)
                fixed4 channelGlitchColor = fixed4(
                    tex2D(_MainTex, glitchUV + noise.r * colorGlitch * 0.05).r,
                    tex2D(_MainTex, glitchUV + noise.g * colorGlitch * 0.05).g,
                    tex2D(_MainTex, glitchUV + noise.b * colorGlitch * 0.05).b,
                    baseGlitchColor.a
                );

                // Block glitch (using noise texture)
                float blockNoiseAmount = pow(noise.r, 2) * blockGlitch; // Use noise^2 for sharper blocks
                fixed4 blockGlitchColor = tex2D(_MainTex, glitchUV + blockNoiseAmount * 0.1);


                // --- Combine Colors ---
                // Start with the base glitched color
                fixed4 finalColor = baseGlitchColor;
                
                // Lerp towards drifted color
                finalColor = lerp(finalColor, driftColor, colorDrift); // Use intensity again? Maybe just use parameter value
                
                // Lerp towards channel glitch color
                finalColor = lerp(finalColor, channelGlitchColor, colorGlitch);

                // Lerp towards block glitch color
                finalColor = lerp(finalColor, blockGlitchColor, blockGlitch); // Maybe use blockNoiseAmount?

                // Final blend between original and fully glitched color based on maxIntensity
                // This ensures areas with low intensity are only slightly glitched
                return lerp(originalColor, finalColor, maxIntensity);
            }
            ENDCG
        }
    }
    FallBack "Diffuse"
} 