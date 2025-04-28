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
Shader "Hidden/Kino/Glitch/Digital"
{
    Properties
    {
        _MainTex  ("Main Texture", 2D) = "white" {}
        _NoiseTex ("Noise Texture", 2D) = "white" {}
        _TrashTex ("Trash Texture", 2D) = "white" {}
        _Intensity ("Intensity", Float) = 0
        _GlitchStrength ("Glitch Strength", Float) = 1
        _EffectCenter ("Effect Center", Vector) = (0.5, 0.5, 0, 0)
        _EffectRadius ("Effect Radius", Float) = 0.2
    }

    CGINCLUDE

    #include "UnityCG.cginc"

    sampler2D _MainTex;
    sampler2D _NoiseTex;
    sampler2D _TrashTex;
    float _Intensity;
    float _GlitchStrength;
    float4 _EffectCenter;
    float _EffectRadius;

    float GetMaskMultiplier(float2 uv)
    {
        float2 center = _EffectCenter.xy;
        float dist = distance(uv, center);
        float mask = 1.0 - smoothstep(_EffectRadius * 0.8, _EffectRadius, dist);
        return mask * _GlitchStrength;
    }

    float4 frag(v2f_img i) : SV_Target 
    {
        // Calculate mask for this pixel
        float maskMultiplier = GetMaskMultiplier(i.uv);
        
        // If outside the mask area, return original color
        if (maskMultiplier <= 0.001)
            return tex2D(_MainTex, i.uv);
            
        // Original texture at this position
        float4 source = tex2D(_MainTex, i.uv);
        
        // Sample noise texture for glitch effect
        float4 glitch = tex2D(_NoiseTex, i.uv);

        // Calculate effect thresholds with intensity and mask
        float effectiveIntensity = _Intensity * maskMultiplier;
        float thresh = 1.001 - effectiveIntensity * 1.001;
        float w_d = step(thresh, pow(glitch.z, 2.5)); // displacement glitch
        float w_f = step(thresh, pow(glitch.w, 2.5)); // frame glitch
        float w_c = step(thresh, pow(glitch.z, 3.5)); // color glitch

        // Displacement - scaled by mask
        float2 offset = glitch.xy * w_d * maskMultiplier;
        float2 uv = frac(i.uv + offset);
        float4 distortedSource = tex2D(_MainTex, uv);

        // Mix with trash frame based on mask
        float trashMix = w_f * maskMultiplier;
        float3 color = lerp(distortedSource.rgb, tex2D(_TrashTex, uv).rgb, trashMix);

        // Shuffle color components based on mask
        float colorMix = w_c * maskMultiplier;
        float3 neg = saturate(color.grb + (1 - dot(color, 1)) * 0.5);
        color = lerp(color, neg, colorMix);

        // Final blend between original and glitched based on mask
        float4 result = float4(color, distortedSource.a);
        return lerp(source, result, maskMultiplier);
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
