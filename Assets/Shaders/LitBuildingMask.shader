Shader "Custom/LitBuildingMask"
{
    Properties
    {
        _ObstacleTex ("Obstacle Map", 2D) = "white" {}
        _BaseColor ("Base Color (where lit)", Color) = (0.2, 0.2, 0.2, 1)
    }
    
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        
        Pass
        {
            Tags { "LightMode"="ForwardBase" }
            
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fwdbase
            #include "UnityCG.cginc"
            #include "Lighting.cginc"
            
            struct appdata {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float3 normal : NORMAL;
            };
            
            struct v2f {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float3 worldNormal : TEXCOORD1;
                float3 worldPos : TEXCOORD2;
            };
            
            sampler2D _ObstacleTex;
            float4 _BaseColor;
            
            v2f vert (appdata v) {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.worldNormal = UnityObjectToWorldNormal(v.normal);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                return o;
            }
            
            fixed4 frag (v2f i) : SV_Target {
                // Sample obstacle map
                float4 obs = tex2D(_ObstacleTex, i.uv);
                
                // Cyan check: high in blue/green, low in red
                float isCyan = (obs.b > 0.4 && obs.g > 0.4 && obs.r < 0.5) ? 1.0 : 0.0;
                
                // If not cyan, output pure black (no projection)
                if (isCyan < 0.5) {
                    return fixed4(0, 0, 0, 1);
                }
                
                // Simple diffuse lighting from main light
                float3 normal = normalize(i.worldNormal);
                float3 lightDir = normalize(_WorldSpaceLightPos0.xyz);
                float NdotL = saturate(dot(normal, lightDir));
                
                // Ambient + diffuse
                float3 ambient = UNITY_LIGHTMODEL_AMBIENT.rgb * _BaseColor.rgb;
                float3 diffuse = _LightColor0.rgb * _BaseColor.rgb * NdotL;
                
                float3 finalColor = ambient + diffuse;
                
                return fixed4(finalColor, 1);
            }
            ENDCG
        }
        
        // Additional pass for other lights (spotlights!)
        Pass
        {
            Tags { "LightMode"="ForwardAdd" }
            Blend One One // Additive blending
            ZWrite Off
            
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fwdadd_fullshadows
            #include "UnityCG.cginc"
            #include "Lighting.cginc"
            #include "AutoLight.cginc"
            
            struct appdata {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float3 normal : NORMAL;
            };
            
            struct v2f {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float3 worldNormal : TEXCOORD1;
                float3 worldPos : TEXCOORD2;
                LIGHTING_COORDS(3, 4)
            };
            
            sampler2D _ObstacleTex;
            float4 _BaseColor;
            
            v2f vert (appdata v) {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.worldNormal = UnityObjectToWorldNormal(v.normal);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                TRANSFER_VERTEX_TO_FRAGMENT(o);
                return o;
            }
            
            fixed4 frag (v2f i) : SV_Target {
                // Sample obstacle map
                float4 obs = tex2D(_ObstacleTex, i.uv);
                
                // Cyan check
                float isCyan = (obs.b > 0.4 && obs.g > 0.4 && obs.r < 0.5) ? 1.0 : 0.0;
                
                // If not cyan, no light contribution
                if (isCyan < 0.5) {
                    return fixed4(0, 0, 0, 0);
                }
                
                // Light direction (works for spot/point lights)
                float3 lightDir = normalize(_WorldSpaceLightPos0.xyz - i.worldPos);
                
                // Diffuse
                float3 normal = normalize(i.worldNormal);
                float NdotL = saturate(dot(normal, lightDir));
                
                // Attenuation (spotlight falloff)
                float atten = LIGHT_ATTENUATION(i);
                
                float3 diffuse = _LightColor0.rgb * _BaseColor.rgb * NdotL * atten;
                
                return fixed4(diffuse, 1);
            }
            ENDCG
        }
    }
    
    FallBack "Diffuse"
}
