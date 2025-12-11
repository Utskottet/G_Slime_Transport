Shader "Custom/SlimeShaderV2"
{
    Properties
    {
        _MainTex ("Mask Texture", 2D) = "white" {}
        _ObstacleTex ("Obstacle Map", 2D) = "black" {}
        _Color ("Slime Color", Color) = (0,1,0,1)
        _ColorDark ("Dark Color", Color) = (0,0.5,0,1)
        _EdgeColor ("Edge Glow Color", Color) = (0.5,1,0.5,1)
        _Cutoff ("Cutoff", Range(0,1)) = 0.1
        _Smoothness ("Edge Smoothness", Range(0.01, 0.2)) = 0.05
        _Bevel ("Bevel Strength", Range(0, 5)) = 2.0
        _FresnelPower ("Fresnel Power", Range(1, 5)) = 2.0
        _FresnelIntensity ("Fresnel Intensity", Range(0, 1)) = 0.5
        _SpecularPower ("Specular Power", Range(1, 64)) = 16
        _SpecularIntensity ("Specular Intensity", Range(0, 2)) = 0.8
        _PulseSpeed ("Pulse Speed", Range(0, 5)) = 1.5
        _PulseAmount ("Pulse Amount", Range(0, 0.1)) = 0.02
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            sampler2D _ObstacleTex;
            float4 _MainTex_TexelSize;
            fixed4 _Color;
            fixed4 _ColorDark;
            fixed4 _EdgeColor;
            float _Cutoff;
            float _Smoothness;
            float _Bevel;
            float _FresnelPower;
            float _FresnelIntensity;
            float _SpecularPower;
            float _SpecularIntensity;
            float _PulseSpeed;
            float _PulseAmount;

            v2f vert (appdata v) {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            // Check if position is an obstacle (red in obstacle map)
            float IsObstacle(float2 uv) {
                fixed4 obs = tex2D(_ObstacleTex, uv);
                return (obs.r > 0.5 && obs.g < 0.4 && obs.b < 0.4) ? 1.0 : 0.0;
            }

            // Smooth sampling that respects obstacle boundaries
            float SampleSmooth(float2 uv, float radius) {
                // If current pixel is obstacle, return 0
                if (IsObstacle(uv) > 0.5) return 0;
                
                float sum = 0;
                float total = 0;
                
                for (int x = -2; x <= 2; x++) {
                    for (int y = -2; y <= 2; y++) {
                        float2 offset = float2(x, y) * _MainTex_TexelSize.xy * radius;
                        float2 sampleUV = uv + offset;
                        
                        // Skip samples that land on obstacles
                        if (IsObstacle(sampleUV) > 0.5) continue;
                        
                        float weight = 1.0 / (1.0 + length(float2(x, y)));
                        sum += tex2D(_MainTex, sampleUV).r * weight;
                        total += weight;
                    }
                }
                
                if (total < 0.01) return tex2D(_MainTex, uv).r;
                return sum / total;
            }

            fixed4 frag (v2f i) : SV_Target {
                float2 uv = i.uv;
                
                // Don't render on obstacles
                if (IsObstacle(uv) > 0.5) discard;
                
                // Sample with smoothing (now respects obstacle boundaries)
                float val = SampleSmooth(uv, 1.5);
                
                // Smooth cutout edge
                float alpha = smoothstep(_Cutoff, _Cutoff + _Smoothness, val);
                
                if (alpha < 0.01) discard;

                // Calculate fake normal from height
                float delta = 0.006;
                float right = SampleSmooth(uv + float2(delta, 0), 1.0);
                float up = SampleSmooth(uv + float2(0, delta), 1.0);
                float left = SampleSmooth(uv + float2(-delta, 0), 1.0);
                float down = SampleSmooth(uv + float2(0, -delta), 1.0);
                
                float3 normal;
                normal.x = (left - right) * _Bevel;
                normal.y = (down - up) * _Bevel;
                normal.z = 1.0;
                normal = normalize(normal);
                
                // Light from top-left
                float3 lightDir = normalize(float3(-0.5, 0.5, 1.0));
                float NdotL = saturate(dot(normal, lightDir));
                
                // View direction (fake, pointing out of screen)
                float3 viewDir = float3(0, 0, 1);
                
                // Fresnel - bright rim effect
                float fresnel = pow(1.0 - saturate(dot(normal, viewDir)), _FresnelPower);
                fresnel *= _FresnelIntensity;
                
                // Specular highlight
                float3 halfDir = normalize(lightDir + viewDir);
                float spec = pow(saturate(dot(normal, halfDir)), _SpecularPower) * _SpecularIntensity;
                
                // Thickness-based color (darker in thick areas)
                float thickness = saturate(val * 1.5);
                fixed4 baseColor = lerp(_Color, _ColorDark, thickness * 0.5);
                
                // Edge glow
                float edge = smoothstep(_Cutoff + _Smoothness, _Cutoff, val);
                
                // Combine lighting
                fixed4 col = baseColor;
                col.rgb *= (0.6 + NdotL * 0.5); // Diffuse
                col.rgb += spec; // Specular
                col.rgb = lerp(col.rgb, _EdgeColor.rgb, fresnel); // Fresnel rim
                col.rgb += _EdgeColor.rgb * edge * 0.3; // Edge glow
                
                // Pulsing brightness
                col.rgb *= 1.0 + sin(_Time.y * _PulseSpeed * 2) * 0.05;
                
                col.a = alpha * 0.92;
                
                return col;
            }
            ENDCG
        }
    }
}
