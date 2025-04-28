//
// KinoGlitch - Video glitch effect
//
// Copyright (C) 2015 Keijiro Takahashi
//
// Permission is hereby granted, free of charge, to any person obtaining a copy of
// this software and associated documentation files (the "Software"), to deal in
// the Software without restriction, including without limitation the rights to
// use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of
// the Software, and to permit persons to whom the Software is furnished to do so,
// subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS
// FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR
// COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER
// IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN
// CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//
Shader "Hidden/Kino/Glitch/Analog"
{
    Properties
    {
        _MainTex ("Main Texture", 2D) = "white" {}
        _TimeX ("Time", Float) = 1
        _Seed ("Random Seed", Float) = 1
        _GlitchStrength ("Glitch Strength", Float) = 1
        
        // Analog parameters
        _ScanLineJitter ("Scan Line Jitter", Float) = 0
        _VerticalJump ("Vertical Jump", Float) = 0
        _HorizontalShake ("Horizontal Shake", Float) = 0
        _ColorDrift ("Color Drift", Float) = 0
        
        // Digital parameters
        _Pixelation ("Pixelation", Float) = 0
        _ColorGlitch ("Color Glitch", Float) = 0
        _BlockGlitch ("Block Glitch", Float) = 0
        
        // Masking
        _EffectCenter ("Effect Center", Vector) = (0.5, 0.5, 0, 0)
        _EffectRadius ("Effect Radius", Float) = 0.2
    }
    CGINCLUDE

    #include "UnityCG.cginc"

    sampler2D _MainTex;
    float _TimeX;
    float _Seed;
    float _GlitchStrength;
    
    // Analog parameters
    float _ScanLineJitter;
    float _VerticalJump;
    float _HorizontalShake;
    float _ColorDrift;
    
    // Digital parameters
    float _Pixelation;
    float _ColorGlitch;
    float _BlockGlitch;
    
    // Masking
    float4 _EffectCenter;
    float _EffectRadius;
    float4 _MainTex_TexelSize;

    float nrand(float x, float y)
    {
        return frac(sin(dot(float2(x, y), float2(12.9898, 78.233))) * 43758.5453);
    }

    float GetMaskMultiplier(float2 uv)
    {
        float2 center = _EffectCenter.xy;
        float dist = distance(uv, center);
        float mask = 1.0 - smoothstep(_EffectRadius * 0.8, _EffectRadius, dist);
        return mask * _GlitchStrength;
    }

    // Digital glitch helpers
    float2 GetBlockOffset(float2 uv, float blockSize)
    {
        float2 block = floor(uv * blockSize) / blockSize;
        float blockRand = nrand(block.x, block.y + _TimeX);
        
        float2 offset = float2(
            (blockRand - 0.5) * 0.5,
            (nrand(blockRand, _TimeX) - 0.5) * 0.5
        );
        
        return offset * _BlockGlitch;
    }

    float3 GetColorGlitch(float2 uv)
    {
        float colorRand = nrand(uv.y, _TimeX);
        float3 colorShift = float3(
            nrand(colorRand, 0.1),
            nrand(colorRand, 0.2),
            nrand(colorRand, 0.3)
        ) * 2 - 1;
        return colorShift * _ColorGlitch;
    }

    half4 frag(v2f_img i) : SV_Target
    {
        float maskMultiplier = GetMaskMultiplier(i.uv);
        
        // If outside the mask area, return original color
        if (maskMultiplier <= 0.001)
            return tex2D(_MainTex, i.uv);
            
        float2 uv = i.uv;
        
        // Apply analog glitch effects
        float u = uv.x;
        float v = uv.y;

        // Scan line jitter
        float jitter = nrand(v, _TimeX) * 2 - 1;
        jitter *= step(nrand(_Seed, _TimeX), _ScanLineJitter) * maskMultiplier;
        u += jitter * 0.05;

        // Vertical jump
        float jump = lerp(v, frac(v + _TimeX * 0.2), _VerticalJump * maskMultiplier);
        v = lerp(v, jump, maskMultiplier);

        // Horizontal shake
        float shake = (nrand(_TimeX, 2) - 0.5) * _HorizontalShake * maskMultiplier;
        u += shake;

        // Color drift for analog effect
        float drift = sin(_TimeX * 4) * _ColorDrift * maskMultiplier;
        
        // Apply digital effects
        float2 blockOffset = GetBlockOffset(uv, 8) * maskMultiplier;
        float3 colorGlitch = GetColorGlitch(uv) * maskMultiplier;
        
        // Pixelation
        float2 pixelSize = _MainTex_TexelSize.xy * (1 + _Pixelation * 50 * maskMultiplier);
        float2 pixelUV = floor(uv / pixelSize) * pixelSize;
        
        // Sample with all effects combined
        half4 glitched;
        float2 finalUV = float2(u, v) + blockOffset;
        float2 finalUVG = finalUV + float2(drift + colorGlitch.y * 0.1, 0);
        float2 pixelatedUV = lerp(finalUV, pixelUV, _Pixelation * maskMultiplier);
        
        glitched.r = tex2D(_MainTex, frac(pixelatedUV + float2(colorGlitch.r * 0.1, 0))).r;
        glitched.g = tex2D(_MainTex, frac(finalUVG)).g;
        glitched.b = tex2D(_MainTex, frac(pixelatedUV + float2(colorGlitch.b * 0.1, 0))).b;
        glitched.a = tex2D(_MainTex, uv).a;
        
        // Sample original color
        half4 original = tex2D(_MainTex, uv);
        
        // Blend between original and glitched based on mask
        return lerp(original, glitched, maskMultiplier);
    }

    ENDCG
    SubShader
    {
        Pass
        {
            ZTest Always Cull Off ZWrite Off
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #pragma target 3.0
            ENDCG
        }
    }
}
