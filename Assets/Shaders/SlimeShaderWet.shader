Shader "Custom/SlimeShaderWet"
{
    Properties
    {
        _MainTex ("Mask Texture", 2D) = "white" {}
        _ObstacleTex ("Obstacle Map", 2D) = "black" {}
        
        [Header(Base Colors)]
        _Color ("Slime Color", Color) = (0,1,0,1)
        _ColorDark ("Dark Color", Color) = (0,0.5,0,1)
        _EdgeColor ("Edge Glow Color", Color) = (0.5,1,0.5,1)
        
        [Header(Shape)]
        _Cutoff ("Cutoff", Range(0,1)) = 0.1
        _Smoothness ("Edge Smoothness", Range(0.01, 0.2)) = 0.05
        
        [Header(Emission Self Glow)]
        _EmissionStrength ("Emission Strength", Range(0, 1)) = 0.15
        
        [Header(Wet and Shiny)]
        _Glossiness ("Glossiness", Range(0, 1)) = 0.8
        _SpecularPower ("Specular Sharpness", Range(1, 128)) = 64
        _SpecularIntensity ("Specular Intensity", Range(0, 3)) = 1.5
        _FresnelPower ("Fresnel Power", Range(1, 5)) = 2.5
        _FresnelIntensity ("Fresnel Intensity", Range(0, 1)) = 0.6
        
        [Header(Noise Bumpiness)]
        _NoiseScale ("Noise Scale Large", Range(1, 50)) = 15
        _NoiseScaleSmall ("Noise Scale Small", Range(10, 200)) = 80
        _BumpStrength ("Bump Strength", Range(0, 2)) = 0.5
        _NoiseSpeed ("Noise Animation Speed", Range(0, 2)) = 0.3
        
        [Header(Transparency)]
        _BaseAlpha ("Base Alpha", Range(0.5, 1)) = 0.85
        _EdgeAlpha ("Edge Alpha", Range(0, 1)) = 0.6
    }
    
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "LightMode"="ForwardBase" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fwdbase
            #include "UnityCG.cginc"
            #include "Lighting.cginc"

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
            
            float _EmissionStrength;
            
            float _Glossiness;
            float _SpecularPower;
            float _SpecularIntensity;
            float _FresnelPower;
            float _FresnelIntensity;
            
            float _NoiseScale;
            float _NoiseScaleSmall;
            float _BumpStrength;
            float _NoiseSpeed;
            
            float _BaseAlpha;
            float _EdgeAlpha;

            v2f vert (appdata v) {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                return o;
            }

            // Noise functions
            float Hash(float2 p) {
                return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453);
            }
            
            float Noise(float2 p) {
                float2 i = floor(p);
                float2 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);
                
                float a = Hash(i);
                float b = Hash(i + float2(1, 0));
                float c = Hash(i + float2(0, 1));
                float d = Hash(i + float2(1, 1));
                
                return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
            }
            
            float FBM(float2 p) {
                float value = 0.0;
                float amplitude = 0.5;
                for (int i = 0; i < 3; i++) {
                    value += amplitude * Noise(p);
                    p *= 2.0;
                    amplitude *= 0.5;
                }
                return value;
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

            fixed4 frag (v2f i) : SV_Target {
                float2 uv = i.uv;
                float time = _Time.y * _NoiseSpeed;
                
                if (IsObstacle(uv) > 0.5) discard;
                
                float val = SampleSmooth(uv, 1.5);
                float alpha = smoothstep(_Cutoff, _Cutoff + _Smoothness, val);
                
                if (alpha < 0.01) discard;

                // === NOISE FOR BUMPINESS ===
                float2 noiseUV = uv * _NoiseScale + time * 0.5;
                float2 noiseUVSmall = uv * _NoiseScaleSmall + time * 0.8;
                
                float noiseLarge = FBM(noiseUV) * 2.0 - 1.0;
                float noiseSmall = (Noise(noiseUVSmall) * 2.0 - 1.0) * 0.5;
                float combinedNoise = (noiseLarge + noiseSmall) * _BumpStrength;
                
                // === CALCULATE NORMAL FROM SLIME MASK + NOISE ===
                float delta = 0.005;
                float right = SampleSmooth(uv + float2(delta, 0), 1.0);
                float up = SampleSmooth(uv + float2(0, delta), 1.0);
                float left = SampleSmooth(uv + float2(-delta, 0), 1.0);
                float down = SampleSmooth(uv + float2(0, -delta), 1.0);
                
                // Add noise to normals for that bumpy wet look
                float noiseRight = FBM((uv + float2(delta, 0)) * _NoiseScale + time * 0.5);
                float noiseUp = FBM((uv + float2(0, delta)) * _NoiseScale + time * 0.5);
                
                float3 normal;
                normal.x = (left - right) * 3.0 + (combinedNoise - noiseRight) * _BumpStrength;
                normal.y = (down - up) * 3.0 + (combinedNoise - noiseUp) * _BumpStrength;
                normal.z = 1.0;
                normal = normalize(normal);
                
                // === LIGHTING ===
                float3 lightDir = normalize(_WorldSpaceLightPos0.xyz);
                float3 viewDir = float3(0, 0, 1);
                
                // Diffuse
                float NdotL = saturate(dot(normal, lightDir));
                float3 diffuse = _LightColor0.rgb * NdotL;
                
                // Specular - wet shiny highlight
                float3 halfDir = normalize(lightDir + viewDir);
                float NdotH = saturate(dot(normal, halfDir));
                float spec = pow(NdotH, _SpecularPower) * _SpecularIntensity * _Glossiness;
                float3 specular = _LightColor0.rgb * spec;
                
                // Fresnel - wet rim glow
                float fresnel = pow(1.0 - saturate(dot(normal, viewDir)), _FresnelPower);
                fresnel *= _FresnelIntensity;
                
                // === BASE COLOR ===
                float thickness = saturate(val * 1.5);
                fixed4 baseColor = lerp(_Color, _ColorDark, thickness * 0.4);
                
                // Add noise variation to color
                baseColor.rgb *= 0.9 + combinedNoise * 0.2;
                
                // === COMBINE ===
                fixed4 col = baseColor;
                
                // Ambient + emission (self glow)
                float3 ambient = UNITY_LIGHTMODEL_AMBIENT.rgb + baseColor.rgb * _EmissionStrength;
                col.rgb = ambient + col.rgb * diffuse;
                
                // Add specular (shiny wet highlights)
                col.rgb += specular;
                
                // Fresnel rim
                col.rgb = lerp(col.rgb, _EdgeColor.rgb * 1.5, fresnel * 0.5);
                
                // Edge glow
                float edge = smoothstep(_Cutoff + _Smoothness, _Cutoff, val);
                col.rgb += _EdgeColor.rgb * edge * 0.2;
                
                // Alpha: more transparent at edges
                col.a = lerp(_EdgeAlpha, _BaseAlpha, smoothstep(0, 0.3, val - _Cutoff));
                col.a *= alpha;
                
                return col;
            }
            ENDCG
        }
        
        // Additional pass for spotlights
        Pass
        {
            Tags { "LightMode"="ForwardAdd" }
            Blend One One
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
            };

            struct v2f {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float3 worldPos : TEXCOORD1;
                LIGHTING_COORDS(2, 3)
            };

            sampler2D _MainTex;
            sampler2D _ObstacleTex;
            float4 _MainTex_TexelSize;
            
            fixed4 _Color;
            fixed4 _ColorDark;
            
            float _Cutoff;
            float _Smoothness;
            float _Glossiness;
            float _SpecularPower;
            float _SpecularIntensity;
            float _NoiseScale;
            float _NoiseScaleSmall;
            float _BumpStrength;
            float _NoiseSpeed;

            v2f vert (appdata v) {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                TRANSFER_VERTEX_TO_FRAGMENT(o);
                return o;
            }

            float Hash(float2 p) {
                return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453);
            }
            
            float Noise(float2 p) {
                float2 i = floor(p);
                float2 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);
                
                float a = Hash(i);
                float b = Hash(i + float2(1, 0));
                float c = Hash(i + float2(0, 1));
                float d = Hash(i + float2(1, 1));
                
                return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
            }
            
            float FBM(float2 p) {
                float value = 0.0;
                float amplitude = 0.5;
                for (int i = 0; i < 3; i++) {
                    value += amplitude * Noise(p);
                    p *= 2.0;
                    amplitude *= 0.5;
                }
                return value;
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

            fixed4 frag (v2f i) : SV_Target {
                float2 uv = i.uv;
                float time = _Time.y * _NoiseSpeed;
                
                if (IsObstacle(uv) > 0.5) discard;
                
                float val = SampleSmooth(uv, 1.5);
                float alpha = smoothstep(_Cutoff, _Cutoff + _Smoothness, val);
                
                if (alpha < 0.01) discard;

                // Noise for bumpiness
                float2 noiseUV = uv * _NoiseScale + time * 0.5;
                float noiseLarge = FBM(noiseUV) * 2.0 - 1.0;
                float noiseSmall = (Noise(uv * _NoiseScaleSmall + time * 0.8) * 2.0 - 1.0) * 0.5;
                float combinedNoise = (noiseLarge + noiseSmall) * _BumpStrength;
                
                // Normal
                float delta = 0.005;
                float right = SampleSmooth(uv + float2(delta, 0), 1.0);
                float up = SampleSmooth(uv + float2(0, delta), 1.0);
                float left = SampleSmooth(uv + float2(-delta, 0), 1.0);
                float down = SampleSmooth(uv + float2(0, -delta), 1.0);
                
                float noiseRight = FBM((uv + float2(delta, 0)) * _NoiseScale + time * 0.5);
                float noiseUp = FBM((uv + float2(0, delta)) * _NoiseScale + time * 0.5);
                
                float3 normal;
                normal.x = (left - right) * 3.0 + (combinedNoise - noiseRight) * _BumpStrength;
                normal.y = (down - up) * 3.0 + (combinedNoise - noiseUp) * _BumpStrength;
                normal.z = 1.0;
                normal = normalize(normal);
                
                // Light direction for point/spot lights
                float3 lightDir = normalize(_WorldSpaceLightPos0.xyz - i.worldPos);
                float3 viewDir = float3(0, 0, 1);
                
                // Attenuation
                float atten = LIGHT_ATTENUATION(i);
                
                // Diffuse
                float NdotL = saturate(dot(normal, lightDir));
                float3 diffuse = _LightColor0.rgb * NdotL * atten;
                
                // Specular - shiny wet
                float3 halfDir = normalize(lightDir + viewDir);
                float NdotH = saturate(dot(normal, halfDir));
                float spec = pow(NdotH, _SpecularPower) * _SpecularIntensity * _Glossiness;
                float3 specular = _LightColor0.rgb * spec * atten;
                
                // Base color
                float thickness = saturate(val * 1.5);
                fixed4 baseColor = lerp(_Color, _ColorDark, thickness * 0.4);
                
                fixed4 col;
                col.rgb = baseColor.rgb * diffuse + specular;
                col.a = alpha * 0.5; // Additive pass, reduce alpha
                
                return col;
            }
            ENDCG
        }
    }
    
    FallBack "Transparent/Diffuse"
}
