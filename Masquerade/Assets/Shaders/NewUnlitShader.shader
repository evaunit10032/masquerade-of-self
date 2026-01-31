// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Unlit/NewUnlitShader"
{
       Properties {
        _Color ("Tint", Color) = (0, 0, 0, 1)
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader {
        Tags{ "RenderType"="CutOut" "Queue"="Geometry" }

        CGPROGRAM

        #pragma surface surf Standard fullforwardshadows
        #pragma target 3.0

        sampler2D _MainTex;
        float4 _MainTex_ST;
        fixed4 _Color;

        struct Input {
            float4 screenPos;
        };

        void surf (Input i, inout SurfaceOutputStandard o) {
            float2 textureCoordinate = i.screenPos.xy / i.screenPos.w;
            float aspect = _ScreenParams.x / _ScreenParams.y;
            textureCoordinate.x = textureCoordinate.x * aspect;
            textureCoordinate = TRANSFORM_TEX(textureCoordinate, _MainTex);

            fixed4 col = tex2D(_MainTex, textureCoordinate);
            col *= _Color;
            o.Emission = col.rgb;
        }
        ENDCG
    }
    FallBack "Standard"

}
/*Shader "Unlit/NewUnlitShader"
{
    Properties
    {
        _Color ("Tint", Color) = (1, 1, 1, 1)
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" "Queue"="Geometry" }

        CGPROGRAM
        #pragma vertex vert
        #pragma fragment frag

        #include "UnityCG.cginc"

        sampler2D _MainTex;
        fixed4 _Color;

        
        struct appdata_t
        {
            float4 vertex : POSITION;
            float2 uv : TEXCOORD0;
        }; 

        struct v2f
        {
            float2 uv : TEXCOORD0;
            float3 pos : TEXCOORD1; // World position
            float4 vertex : SV_POSITION;
            UNITY_FOG_COORDS(2); // For fog
        };

        v2f vert(appdata_t v)
        {
            v2f o;
            o.vertex = UnityObjectToClipPos(v.vertex);
            o.uv = v.uv;

            // Transform vertex position to world space
            o.pos = mul(unity_ObjectToWorld, v.vertex).xyz;

            UNITY_TRANSFER_FOG(o, o.vertex); // Pass fog coordinates
            return o;
        }
        

        void frag(v2f i, out float4 o)
        {
            // Sample the main texture using uv_MainTex
            fixed4 texColor = tex2D(_MainTex, i.uv);
            texColor *= _Color; // Apply tint

            // Get light information
            half3 lightPos = _WorldSpaceLightPos0.xyz; // Position of the point light
            half3 lightDir = lightPos - i.pos; // Direction to the light
            half distance = length(lightDir);
            lightDir = normalize(lightDir);

            // Calculate attenuation based on distance
            half attenuation = 1.0 / (distance * distance + 0.1); // Avoid division by zero
            half NdotL = max(0, dot(float3(0, 0, 1), lightDir)); // Assuming normal pointing up

            // Calculate final color based on light intensity and attenuation
            fixed3 finalColor = texColor.rgb * NdotL * attenuation;

            // Output the final color
            o = fixed4(finalColor, texColor.a); // Preserve alpha
        }
        
        ENDCG
    }
    FallBack "Standard"
    
    
}*/


    