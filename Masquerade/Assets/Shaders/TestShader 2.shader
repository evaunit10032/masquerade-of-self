Shader "Custom/CelShaderURPScreenSpace"
{
    Properties
    {
        _MainTex ("Bright Texture", 2D) = "white" {}
        _MidTex ("Mid Texture", 2D) = "white" {}
        _DarkTex ("Dark Texture", 2D) = "white" {}
        _CutoutTex ("Cutout Texture", 2D) = "white" {}
        
        _BrightTiling ("Bright Tiling", Vector) = (1,1,0,0)
        _MidTiling ("Mid Tiling", Vector) = (1,1,0,0)
        _DarkTiling ("Dark Tiling", Vector) = (1,1,0,0)
        _CutoutTiling ("Cutout Tiling", Vector) = (1,1,0,0)
        
        _MidThreshold ("Mid Threshold", Range(0, 1)) = 0.5
        _DarkThreshold ("Dark Threshold", Range(0, 1)) = 0.2
        _CutoutThreshold ("Cutout Threshold", Range(0, 1)) = 0.5
        
        _OutlineColor ("Outline Color", Color) = (0,0,0,1)
        _OutlineWidth ("Outline Width", Range(0, 10)) = 1.0
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
        TEXTURE2D(_MidTex); SAMPLER(sampler_MidTex);
        TEXTURE2D(_DarkTex); SAMPLER(sampler_DarkTex);
        TEXTURE2D(_CutoutTex); SAMPLER(sampler_CutoutTex);
        
        CBUFFER_START(UnityPerMaterial)
            float4 _MainTex_ST;
            float4 _CutoutTex_ST;
            float2 _BrightTiling;
            float2 _MidTiling;
            float2 _DarkTiling;
            float2 _CutoutTiling;
            float _MidThreshold;
            float _DarkThreshold;
            float _CutoutThreshold;
            float4 _OutlineColor;
            float _OutlineWidth;
        CBUFFER_END
        
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
        ENDHLSL
        
        // Main Pass
        Pass
        {
            Name "ForwardLit"
            Tags {"LightMode"="UniversalForward"}
            
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            // Shadow keywords
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile _ _MIXED_LIGHTING_SUBTRACTIVE
            
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
                
                // Sample cutout texture in UV space
                float4 cutoutColor = SAMPLE_TEXTURE2D(_CutoutTex, sampler_CutoutTex, TRANSFORM_TEX(input.uv, _MainTex) * _CutoutTiling);
                
                // Apply alpha cutout
                clip(cutoutColor.a - _CutoutThreshold);
                
                // Get shadow coordinates
                float4 shadowCoord = input.shadowCoord;
                #if defined(MAIN_LIGHT_CALCULATE_SHADOWS)
                    shadowCoord = TransformWorldToShadowCoord(input.positionWS);
                #endif
                
                // Main directional light with shadows
                Light mainLight = GetMainLight(shadowCoord);
                float NdotL = dot(input.normalWS, mainLight.direction);
                float lightIntensity = smoothstep(0, 0.01, NdotL * mainLight.shadowAttenuation);
                
                // Additional point/spot lights with shadows
                float3 additionalLighting = 0;
                #ifdef _ADDITIONAL_LIGHTS
                    uint additionalLightsCount = GetAdditionalLightsCount();
                    for (uint lightIndex = 0u; lightIndex < additionalLightsCount; ++lightIndex)
                    {
                        Light light = GetAdditionalLight(lightIndex, input.positionWS, shadowCoord);
                        float NdotL = dot(input.normalWS, light.direction);
                        float shadowAtten = light.shadowAttenuation * light.distanceAttenuation;
                        additionalLighting += light.color * shadowAtten * smoothstep(0, 0.01, NdotL);
                    }
                #endif
                
                lightIntensity += length(additionalLighting);
                lightIntensity = saturate(lightIntensity);
                
                // Sample textures in screen space
                float4 brightColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, screenUV * _BrightTiling);
                float4 midColor = SAMPLE_TEXTURE2D(_MidTex, sampler_MidTex, screenUV * _MidTiling);
                float4 darkColor = SAMPLE_TEXTURE2D(_DarkTex, sampler_DarkTex, screenUV * _DarkTiling);
                
                float4 finalColor;
                if (lightIntensity > _MidThreshold)
                    finalColor = brightColor;
                else if (lightIntensity > _DarkThreshold)
                    finalColor = midColor;
                else
                    finalColor = darkColor;
                
                // Apply shadows using dark texture
                float totalShadow = mainLight.shadowAttenuation;
                #ifdef _ADDITIONAL_LIGHTS
                    for (uint lightIndex = 0u; lightIndex < additionalLightsCount; ++lightIndex)
                    {
                        Light light = GetAdditionalLight(lightIndex, input.positionWS, shadowCoord);
                        totalShadow = min(totalShadow, light.shadowAttenuation);
                    }
                #endif
                
                finalColor = lerp(darkColor, finalColor, totalShadow);
                
                // Blend with cutout texture
                finalColor = lerp(finalColor, cutoutColor, cutoutColor.a);
                
                return finalColor;
            }
            ENDHLSL
        }
        
        // Outline Pass
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
                
                // Sample depths at neighboring pixels
                float2 uv0 = screenUV + float2(-offset.x, 0);
                float2 uv1 = screenUV + float2(offset.x, 0);
                float2 uv2 = screenUV + float2(0, -offset.y);
                float2 uv3 = screenUV + float2(0, offset.y);
                
                // Get position differences
                float3 p0 = float3(uv0, 0);
                float3 p1 = float3(uv1, 0);
                float3 p2 = float3(uv2, 0);
                float3 p3 = float3(uv3, 0);
                
                // Calculate normal from cross product of position differences
                float3 normal = normalize(cross(p1 - p0, p3 - p2));
                return normal;
            }
            
            V2F vert(AppData input)
            {
                V2F output;
                
                float3 normalOS = input.normalOS;
                float4 posOS = input.positionOS;
                
                // Calculate view-space normal
                float3 normalVS = TransformWorldToViewDir(TransformObjectToWorldNormal(normalOS));
                normalVS.z = -0.5;  // Adjust normal to face camera more
                
                // Convert position to view space
                float3 posVS = TransformWorldToView(TransformObjectToWorld(posOS.xyz));
                
                // Expand in view space
                posVS += normalize(normalVS) * _OutlineWidth * 0.001;
                
                // Convert back to clip space
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
                // Sample cutout texture for outline
                float4 cutoutColor = SAMPLE_TEXTURE2D(_CutoutTex, sampler_CutoutTex, 
                    TRANSFORM_TEX(input.uv, _MainTex) * _CutoutTiling);
                clip(cutoutColor.a - _CutoutThreshold);
                
                float2 screenUV = input.screenPos.xy / input.screenPos.w;
                
                #ifdef USE_SCREENSPACE_OUTLINE
                    // Sample the dark texture in screen space for the outline
                    return SAMPLE_TEXTURE2D(_DarkTex, sampler_DarkTex, screenUV * _DarkTiling);
                #else
                    // Use solid color for outline if screen space is disabled
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
            
            // Shadow casting support
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
            
            #include "Packages/com.unity.render-pipelines.universal/Shaders/LitInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/ShadowCasterPass.hlsl"
            ENDHLSL
        }
    }
}