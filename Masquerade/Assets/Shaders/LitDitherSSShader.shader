Shader "Custom/LitDotSSDitherShader"
{
    Properties
    {
        _MainTex ("Bright Texture", 2D) = "white" {}
        _DarkTex ("Dark Texture", 2D) = "white" {}
        _CutoutTex ("Cutout Texture", 2D) = "white" {}
        _DotPattern ("Dot Pattern", 2D) = "white" {}
        _ShadowTex ("Shadow Texture", 2D) = "white" {}
        
        _BrightTint ("Bright Texture Tint", Color) = (1,1,1,1)
        _DarkTint ("Dark Texture Tint", Color) = (1,1,1,1)
        _CutoutTint ("Cutout Texture Tint", Color) = (1,1,1,1)
        _ShadowTint ("Shadow Texture Tint", Color) = (1,1,1,1)
        
        _BrightTiling ("Bright Tiling", Vector) = (1,1,0,0)
        _DarkTiling ("Dark Tiling", Vector) = (1,1,0,0)
        _CutoutTiling ("Cutout Tiling", Vector) = (1,1,0,0)
        _DotTiling ("Dot Pattern Tiling", Vector) = (1,1,0,0)
        _ShadowTiling ("Shadow Tiling", Vector) = (1,1,0,0)
        
        _DitherThreshold ("Dither Threshold", Range(0, 1)) = 0.5
        _DitherScale ("Dither Scale", Range(0, 100)) = 50
        _DarkThreshold ("Dark Threshold", Range(0, 1)) = 0.2
        _CutoutThreshold ("Cutout Threshold", Range(0, 1)) = 0.5
        _DotSize ("Dot Size", Range(0.1, 5.0)) = 1.0
        _MaxDotSize ("Max Dot Size", Range(0.1, 5.0)) = 2.0
        _MinDotSize ("Min Dot Size", Range(0.1, 1.0)) = 0.2
        _DotSizeGradient ("Dot Size Gradient", Range(0.5, 5.0)) = 1.0
        _RowOffset ("Row Offset", Range(0.0, 1.0)) = 0.5
        _DotSpacing ("Dot Spacing", Range(0.5, 2.0)) = 1.0
        
        _OutlineWidth ("Outline Width", Range(0, 10)) = 1.0
        [Toggle(USE_SCREENSPACE_OUTLINE)] _ScreenSpaceOutline("Screen Space Outline", Float) = 1
        [Toggle(_RECEIVE_SHADOWS)] _ReceiveShadows("Receive Shadows", Float) = 1
    }
    
    SubShader
    {
        Tags 
        {
            "RenderPipeline"="UniversalPipeline"
            "RenderType"="TransparentCutout"
            "Queue"="AlphaTest"
        }
        
        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
        
        TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
        TEXTURE2D(_DarkTex); SAMPLER(sampler_DarkTex);
        TEXTURE2D(_CutoutTex); SAMPLER(sampler_CutoutTex);
        TEXTURE2D(_DotPattern); SAMPLER(sampler_DotPattern);
        TEXTURE2D(_ShadowTex); SAMPLER(sampler_ShadowTex);
        
        CBUFFER_START(UnityPerMaterial)
            float4 _MainTex_ST;
            float4 _CutoutTex_ST;
            float4 _DotPattern_ST;
            float4 _ShadowTex_ST;
            float4 _BrightTint;
            float4 _DarkTint;
            float4 _CutoutTint;
            float4 _ShadowTint;
            float2 _BrightTiling;
            float2 _DarkTiling;
            float2 _CutoutTiling;
            float2 _DotTiling;
            float2 _ShadowTiling;
            float _DitherThreshold;
            float _DitherScale;
            float _DarkThreshold;
            float _CutoutThreshold;
            float _DotSize;
            float _MaxDotSize;
            float _MinDotSize;
            float _DotSizeGradient;
            float _RowOffset;
            float _DotSpacing;
            float4 _OutlineColor;
            float _OutlineWidth;
        CBUFFER_END

        //pattern
        static const float BAYER_PATTERN[16] = {    
            0.9998f, 1.0000f, 0.9999f, 0.9999f,
            1.0000f, 0.9997f, 0.9999f, 0.9998f,
            0.9998f, 1.0000f, 0.9996f, 0.9999f,
            0.9997f, 0.9998f, 0.9995f, 0.9997f
        };
        

        struct AppData
        {
            float4 positionOS : POSITION;
            float3 normalOS : NORMAL;
            float2 uv : TEXCOORD0;
        };
        
        struct V2F
        {
            float4 positionCS : SV_POSITION;
            float3 normalWS : NORMAL;
            float2 uv : TEXCOORD0;
            float4 screenPos : TEXCOORD1;
            float3 positionWS : TEXCOORD2;
            float4 shadowCoord : TEXCOORD3;
        };

        float hash(float2 p) {
            float3 p3 = frac(float3(p.xyx) * 0.13); 
            p3 += dot(p3, p3.yzx + 3.333);
            return frac((p3.x + p3.y) * p3.z);
        }

        // Modified function to check if dots are fully within the gradient area
        float GetCustomDotPattern(float2 pixelPos, float scale, float lightIntensity, float isInGradientArea) {
            // Don't render dots that are at least partially in fully lit areas
            if (isInGradientArea < 1.0) {
                return 0.0; // Return 0 (no dot) for dots that would be partly in the fully lit area
            }

            float dotSizeFactor = pow(lightIntensity, _DotSizeGradient);
            float dynamicDotSize = lerp(_MinDotSize, _MaxDotSize, dotSizeFactor) * _DotSize;

            // Scale the position by the pattern scaling factor
            float2 scaledPos = pixelPos * scale * _DotTiling / (_ScreenParams.xy * _DotSpacing);
            
            // Apply row-based staggering to scaledPos BEFORE calculating the cell
            // This way the entire grid is offset, not just the centers within cells
            float rowIndex = floor(scaledPos.y);
            float randOffset = lerp(_RowOffset - 0.2f, _RowOffset + 0.2f, lightIntensity);
            float xOffset = fmod(rowIndex, 2.0) * randOffset;
            
            // Apply the offset to create the staggered grid
            float2 staggeredPos = float2(scaledPos.x + xOffset, scaledPos.y);
            
            // Now determine cell and position within cell
            float2 cell = floor(staggeredPos);
            float2 cellUV = frac(staggeredPos);
            
            float2 center = float2(0.5, 0.5);
            float radius = 0.4 * dynamicDotSize;
            float dist = length(cellUV - center);
            float dot = step(dist, radius);
            
            // Use a different bayer pattern offset for each cell to add variety
            uint x = fmod(cell.x, 4);
            uint y = fmod(cell.y, 4);
            float bayerValue =  BAYER_PATTERN[y * 4 + x];
            
            // Add slight noise variation per cell
            float noise = hash(cell) * 0.3;
            bayerValue = saturate(bayerValue - 0.075);
       
            return dot * bayerValue;
        }
        ENDHLSL
        
        Pass
        {
            Name "ForwardLit"
            Tags {"LightMode"="UniversalForward"}
            
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile _ _MIXED_LIGHTING_SUBTRACTIVE
            #pragma shader_feature_local _RECEIVE_SHADOWS
            
            V2F vert(AppData input)
            {
                V2F output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.uv = input.uv;
                output.screenPos = ComputeScreenPos(output.positionCS);
                output.shadowCoord = TransformWorldToShadowCoord(output.positionWS);
                return output;
            }
            
            half4 frag(V2F input) : SV_Target
            {
                float2 screenUV = input.screenPos.xy / input.screenPos.w;
                
                float4 cutoutColor = SAMPLE_TEXTURE2D(_CutoutTex, sampler_CutoutTex, 
                    TRANSFORM_TEX(input.uv, _MainTex) * _CutoutTiling) * _CutoutTint;
                
                clip(cutoutColor.a - _CutoutThreshold);
                
                float4 shadowCoord = input.shadowCoord;
                #if defined(MAIN_LIGHT_CALCULATE_SHADOWS)
                    shadowCoord = TransformWorldToShadowCoord(input.positionWS);
                #endif
                
                Light mainLight = GetMainLight(shadowCoord);
                float NdotL = dot(input.normalWS, mainLight.direction);

                float lightIntensity = 0; // smoothstep(0, 0.01, NdotL * mainLight.shadowAttenuation);
                
                float3 additionalLighting = 0;
                #ifdef _ADDITIONAL_LIGHTS
                    uint additionalLightsCount = GetAdditionalLightsCount();
                    for (uint lightIndex = 0u; lightIndex < additionalLightsCount; ++lightIndex)
                    {
                        Light light = GetAdditionalLight(lightIndex, input.positionWS, shadowCoord);
                        float NdotL = dot(input.normalWS, light.direction);
                        float shadowAtten = light.shadowAttenuation * light.distanceAttenuation;
                        #ifdef _RECEIVE_SHADOWS
                        additionalLighting += light.color * shadowAtten * smoothstep(0, 0.01, NdotL);
                        #else
                        additionalLighting += light.color * light.distanceAttenuation * smoothstep(0, 0.01, NdotL);
                        #endif
                    }
                #endif
                
                lightIntensity += length(additionalLighting);
                lightIntensity = saturate(lightIntensity);
                
                float4 brightColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, screenUV * _BrightTiling) * _BrightTint;
                float4 darkColor = SAMPLE_TEXTURE2D(_DarkTex, sampler_DarkTex, screenUV * _DarkTiling) * _DarkTint;
                float4 shadowColor = SAMPLE_TEXTURE2D(_ShadowTex, sampler_ShadowTex, screenUV * _ShadowTiling) * _ShadowTint;
                
                float4 finalColor;
                if (lightIntensity <= _DarkThreshold) {
                    finalColor = shadowColor;
                }
                else {
                    float2 pixelPos = input.positionCS.xy;
                    float ditherIntensity = saturate((lightIntensity - _DarkThreshold) / (_DitherThreshold - _DarkThreshold) * 2);
                    
                    // Check if the current position is fully within the gradient dithering area
                    // 1.0 means it's fully in the gradient area, 0.0 means it's at least partially in the fully lit area
                    float isInGradientArea = step(ditherIntensity, 1.0);

                    
                    
                    // Get dot pattern with the actual light intensity and pass the gradient area flag
                    float dotPattern = GetCustomDotPattern(pixelPos, _DitherScale, ditherIntensity, isInGradientArea);
                    
                    // If ditherIntensity >= 1.0, we're in the fully lit area, so use brightColor
                    // If ditherIntensity < 1.0, we're in the gradient area, so apply dot pattern
                    float isBright = dotPattern < (1.0 - ditherIntensity);
                    
                    // For areas that are fully lit (ditherIntensity >= 1.0), always use brightColor
                    // Otherwise use the dot pattern to determine the color
                    if (ditherIntensity >= 1.0) {
                        finalColor = brightColor;
                    } else {
                        finalColor = lerp(brightColor, darkColor, isBright);
                    }
                }
                
                float totalShadow = mainLight.shadowAttenuation;
                #ifdef _ADDITIONAL_LIGHTS
                    #ifdef _RECEIVE_SHADOWS
                        for (uint lightIndex = 0u; lightIndex < additionalLightsCount; ++lightIndex)
                        {
                            Light light = GetAdditionalLight(lightIndex, input.positionWS, shadowCoord);
                            totalShadow = min(totalShadow, light.shadowAttenuation);
                        }
                    #endif
                #endif
                
                #ifdef _RECEIVE_SHADOWS
                    if(totalShadow > 1) 
                    {
                        finalColor = shadowColor;
                    }
                    //finalColor = lerp(shadowColor, finalColor, totalShadow);
                #endif
                finalColor = lerp(finalColor, shadowColor, cutoutColor.a);
                
                return finalColor;
            }
            ENDHLSL
        }
        
        Pass
        {
            Name "Outline"
            Cull Front
            
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma shader_feature USE_SCREENSPACE_OUTLINE
            
            float3 GetScreenSpaceNormal(float4 screenPos)
            {
                float2 screenUV = screenPos.xy / screenPos.w;
                float2 offset = float2(1.0, 1.0) / _ScreenParams.xy;
                
                float2 uv0 = screenUV + float2(-offset.x, 0);
                float2 uv1 = screenUV + float2(offset.x, 0);
                float2 uv2 = screenUV + float2(0, -offset.y);
                float2 uv3 = screenUV + float2(0, offset.y);
                
                float3 p0 = float3(uv0, 0);
                float3 p1 = float3(uv1, 0);
                float3 p2 = float3(uv2, 0);
                float3 p3 = float3(uv3, 0);
                
                float3 normal = normalize(cross(p1 - p0, p3 - p2));
                return normal;
            }
            
            V2F vert(AppData input)
            {
                V2F output;
                
                float3 normalOS = input.normalOS;
                float4 posOS = input.positionOS;
                
                float3 normalVS = TransformWorldToViewDir(TransformObjectToWorldNormal(normalOS));
                normalVS.z = -0.5;
                
                float3 posVS = TransformWorldToView(TransformObjectToWorld(posOS.xyz));
                posVS += normalize(normalVS) * _OutlineWidth * 0.001;
                
                output.positionCS = TransformWViewToHClip(posVS);
                output.normalWS = TransformObjectToWorldNormal(normalOS);
                output.positionWS = TransformObjectToWorld(posOS.xyz);
                output.uv = input.uv;
                output.screenPos = ComputeScreenPos(output.positionCS);
                output.shadowCoord = TransformWorldToShadowCoord(output.positionWS);
                
                return output;
            }
            
            half4 frag(V2F input) : SV_Target
            {
                float4 cutoutColor = SAMPLE_TEXTURE2D(_CutoutTex, sampler_CutoutTex, 
                    TRANSFORM_TEX(input.uv, _MainTex) * _CutoutTiling) * _CutoutTint;
                clip(cutoutColor.a - _CutoutThreshold);
                
                float2 screenUV = input.screenPos.xy / input.screenPos.w;
                
                #ifdef USE_SCREENSPACE_OUTLINE
                    return SAMPLE_TEXTURE2D(_ShadowTex, sampler_ShadowTex, screenUV * _ShadowTiling) * _ShadowTint;
                #else
                    return float4(0, 0, 0, 1);
                #endif
            }
            ENDHLSL
        }
        
        
        // Shadow casting pass
        Pass
        {
            Name "ShadowCaster"
            Tags{"LightMode" = "ShadowCaster"}
            
            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull Back
            
            HLSLPROGRAM
            #pragma vertex ShadowPassVertex
            #pragma fragment ShadowPassFragment
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
            
            #include "Packages/com.unity.render-pipelines.universal/Shaders/LitInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/ShadowCasterPass.hlsl"
            ENDHLSL
        }
    }
}