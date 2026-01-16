Shader "Custom/NewEnemySlime01"
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
        _SpotlightBoost ("Spotlight Boost", Range(0, 5)) = 2.0
        _SpotSpecular ("Spot Specular", Range(0, 5)) = 3.0
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
                float3 worldPos : TEXCOORD1;
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
            float _SpotlightBoost;
            float _SpotSpecular;

            // Spotlight data from SpotlightController (global)
            float4 _Spot1Pos, _Spot2Pos, _Spot3Pos;
            float4 _Spot1Dir, _Spot2Dir, _Spot3Dir;
            float _Spot1Angle, _Spot2Angle, _Spot3Angle;
            float _Spot1Intensity, _Spot2Intensity, _Spot3Intensity;
            float4 _Spot1Color, _Spot2Color, _Spot3Color;

            v2f vert (appdata v) {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                return o;
            }

            float IsObstacle(float2 uv) {
                fixed4 obs = tex2D(_ObstacleTex, uv);
                return (obs.r > 0.5 && obs.g < 0.4 && obs.b < 0.4) ? 1.0 : 0.0;
            }

            float SampleSmooth(float2 uv, float radius) {
                if (IsObstacle(uv) > 0.5) return 0;
                
                float sum = 0;
                float total = 0;
                
                for (int x = -2; x <= 2; x++) {
                    for (int y = -2; y <= 2; y++) {
                        float2 offset = float2(x, y) * _MainTex_TexelSize.xy * radius;
                        float2 sampleUV = uv + offset;
                        
                        if (IsObstacle(sampleUV) > 0.5) continue;
                        
                        float weight = 1.0 / (1.0 + length(float2(x, y)));
                        sum += tex2D(_MainTex, sampleUV).r * weight;
                        total += weight;
                    }
                }
                
                if (total < 0.01) return tex2D(_MainTex, uv).r;
                return sum / total;
            }

            // Calculate one spotlight contribution
            float3 CalcSpotlight(float3 worldPos, float3 normal, float3 viewDir,
                                 float3 spotPos, float3 spotDir, float spotAngle, 
                                 float spotIntensity, float3 spotColor)
            {
                float3 toLight = spotPos - worldPos;
                float dist = length(toLight);
                float3 lightDir = toLight / dist;
                
                // Cone check - are we inside spotlight cone?
                float cosAngle = dot(-lightDir, spotDir);
                float coneAngle = cos(spotAngle * 0.5 * 0.0174533); // degrees to radians
                float inCone = smoothstep(coneAngle - 0.1, coneAngle + 0.1, cosAngle);
                
                if (inCone < 0.01) return float3(0,0,0);
                
                // Distance falloff
                float atten = saturate(1.0 - dist * 0.02) * inCone;
                
                // Diffuse
                float NdotL = saturate(dot(normal, lightDir));
                float3 diffuse = spotColor * NdotL * atten * spotIntensity * _SpotlightBoost * 0.1;
                
                // Specular - white shiny highlight
                float3 halfDir = normalize(lightDir + viewDir);
                float NdotH = saturate(dot(normal, halfDir));
                float spec = pow(NdotH, _SpecularPower * 2) * _SpotSpecular * atten * spotIntensity * 0.01;
                float3 specular = spotColor * spec;
                
                return diffuse + specular;
            }

            fixed4 frag (v2f i) : SV_Target {
                float2 uv = i.uv;
                
                if (IsObstacle(uv) > 0.5) discard;
                
                float val = SampleSmooth(uv, 1.5);
                float alpha = smoothstep(_Cutoff, _Cutoff + _Smoothness, val);
                
                if (alpha < 0.01) discard;

                // Calculate fake normal from height (the bevel look)
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
                
                // Ambient/fake light from top-left (base look)
                float3 fakeLightDir = normalize(float3(-0.5, 0.5, 1.0));
                float fakeNdotL = saturate(dot(normal, fakeLightDir));
                
                // View direction
                float3 viewDir = float3(0, 0, 1);
                
                // Fresnel rim
                float fresnel = pow(1.0 - saturate(dot(normal, viewDir)), _FresnelPower);
                fresnel *= _FresnelIntensity;
                
                // Fake specular
                float3 fakeHalfDir = normalize(fakeLightDir + viewDir);
                float fakeSpec = pow(saturate(dot(normal, fakeHalfDir)), _SpecularPower) * _SpecularIntensity;
                
                // Base color
                float thickness = saturate(val * 1.5);
                fixed4 baseColor = lerp(_Color, _ColorDark, thickness * 0.5);
                
                // Edge glow
                float edge = smoothstep(_Cutoff + _Smoothness, _Cutoff, val);
                
                // Combine base lighting (the good V2 look)
                fixed4 col = baseColor;
                col.rgb *= (0.6 + fakeNdotL * 0.5);
                col.rgb += fakeSpec;
                col.rgb = lerp(col.rgb, _EdgeColor.rgb, fresnel);
                col.rgb += _EdgeColor.rgb * edge * 0.3;
                
                // === ADD FAKE SPOTLIGHTS ===
                col.rgb += CalcSpotlight(i.worldPos, normal, viewDir, 
                    _Spot1Pos.xyz, _Spot1Dir.xyz, _Spot1Angle, _Spot1Intensity, _Spot1Color.rgb);
                col.rgb += CalcSpotlight(i.worldPos, normal, viewDir, 
                    _Spot2Pos.xyz, _Spot2Dir.xyz, _Spot2Angle, _Spot2Intensity, _Spot2Color.rgb);
                col.rgb += CalcSpotlight(i.worldPos, normal, viewDir, 
                    _Spot3Pos.xyz, _Spot3Dir.xyz, _Spot3Angle, _Spot3Intensity, _Spot3Color.rgb);
                
                col.a = alpha * 0.92;
                
                return col;
            }
            ENDCG
        }
    }
}