Shader "Custom/SpotlightOverlay"
{
    Properties
    {
        _BaseColor ("Tint", Color) = (1, 1, 1, 1)
        _Intensity ("Intensity", Range(0, 10)) = 1
        _Softness ("Edge Softness", Range(0.1, 5)) = 1
        _Falloff ("Falloff Power", Range(0.1, 5)) = 1
    }
    
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Overlay-10" }
        
        Pass
        {
            Tags { "LightMode"="ForwardAdd" }
            Blend One One
            ZWrite Off
            ZTest Always
            
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fwdadd_fullshadows
            #include "UnityCG.cginc"
            #include "Lighting.cginc"
            #include "AutoLight.cginc"
            
            struct appdata {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };
            
            struct v2f {
                float4 vertex : SV_POSITION;
                float3 worldNormal : TEXCOORD0;
                float3 worldPos : TEXCOORD1;
                LIGHTING_COORDS(2, 3)
            };
            
            float4 _BaseColor;
            float _Intensity;
            float _Softness;
            float _Falloff;
            
            v2f vert (appdata v) {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.worldNormal = UnityObjectToWorldNormal(v.normal);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                TRANSFER_VERTEX_TO_FRAGMENT(o);
                return o;
            }
            
            fixed4 frag (v2f i) : SV_Target {
                float3 normal = normalize(i.worldNormal);
                float3 lightDir = normalize(_WorldSpaceLightPos0.xyz - i.worldPos);
                float NdotL = saturate(dot(normal, lightDir));
                
                // Attenuation from spotlight
                float atten = LIGHT_ATTENUATION(i);
                
                // Soften the edges
                atten = pow(atten, 1.0 / _Softness);
                
                // Apply falloff curve
                atten = pow(atten, _Falloff);
                
                // Final color
                float3 diffuse = _LightColor0.rgb * _BaseColor.rgb * NdotL * atten * _Intensity;
                
                return fixed4(diffuse, 1);
            }
            ENDCG
        }
    }
    
    FallBack Off
}