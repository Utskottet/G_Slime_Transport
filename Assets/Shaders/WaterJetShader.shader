Shader "Custom/WaterJetShader"
{
    Properties
    {
        [Header(MASTER INTENSITIES)]
        _LightingIntensity ("Lighting Intensity", Range(0, 1)) = 0.4
        _SpecularIntensity ("Specular Intensity", Range(0, 1)) = 0.8
        _FresnelIntensity ("Fresnel Rim Intensity", Range(0, 1)) = 0.4
        _CausticsIntensity ("Caustics Intensity", Range(0, 1)) = 0.3
        _FlowIntensity ("Flow/Streak Intensity", Range(0, 1)) = 0.3
        _FoamIntensity ("Edge Foam Intensity", Range(0, 3)) = 0.8
        _WallFoamIntensity ("Wall Foam Intensity", Range(0, 5)) = 2.0
        _TopFoamIntensity ("Top/Frontier Foam Intensity", Range(0, 3)) = 1.5
        _DetailIntensity ("Detail Variation Intensity", Range(0, 1)) = 0.3
        _DepthColorBlend ("Depth Color Blend", Range(0, 1)) = 1.0
        
        [Header(NOISE THRESHOLD higher equals more black)]
        _FlowThreshold ("Flow Threshold", Range(0, 1)) = 0.0
        _FlowContrast ("Flow Contrast", Range(1, 5)) = 1.0
        _CausticsThreshold ("Caustics Threshold", Range(0, 1)) = 0.0
        _CausticsContrast ("Caustics Contrast", Range(1, 5)) = 1.0
        _FoamNoiseThreshold ("Foam Noise Threshold", Range(0, 1)) = 0.0
        _FoamNoiseContrast ("Foam Noise Contrast", Range(1, 5)) = 1.0
        _DetailThreshold ("Detail Threshold", Range(0, 1)) = 0.0
        _DetailContrast ("Detail Contrast", Range(1, 5)) = 1.0
        
        [Header(Textures)]
        _MainTex ("Mask Texture", 2D) = "white" {}
        _ObstacleTex ("Obstacle Map", 2D) = "black" {}
        
        [Header(Water Colors)]
        _WaterColor ("Water Color", Color) = (0.3, 0.6, 1.0, 0.85)
        _WaterColorDeep ("Deep Water Color", Color) = (0.15, 0.4, 0.8, 0.9)
        _FoamColor ("Foam Color", Color) = (1, 1, 1, 1)
        
        [Header(Effect Colors)]
        _FlowColor ("Flow/Streak Color", Color) = (1, 1, 1, 1)
        _CausticsColor ("Caustics Color", Color) = (1, 1, 1, 1)
        
        [Header(Shape)]
        _Cutoff ("Cutoff", Range(0,1)) = 0.1
        _Smoothness ("Edge Smoothness", Range(0.01, 0.2)) = 0.05
        
        [Header(Flow and Streaks)]
        _FlowTex ("Flow Texture", 2D) = "gray" {}
        _FlowScale ("Flow Scale", Range(1, 5000)) = 15
        _FlowStretchX ("Flow Stretch X", Range(0.1, 5)) = 0.3
        _FlowStretchY ("Flow Stretch Y", Range(0.1, 5)) = 1.5
        _FlowSpeed ("Flow Speed", Range(0, 300)) = 1.0
        _FlowTexBlend ("Flow Texture Blend", Range(0, 1)) = 0.0
        
        [Header(Caustics)]
        _CausticsTex ("Caustics Texture", 2D) = "gray" {}
        _CausticsScale ("Caustics Scale", Range(1, 50)) = 15
        _CausticsStretchX ("Caustics Stretch X", Range(0.1, 5)) = 1.0
        _CausticsStretchY ("Caustics Stretch Y", Range(0.1, 5)) = 1.0
        _CausticsSpeed ("Caustics Speed", Range(0, 2)) = 0.5
        _CausticsTexBlend ("Caustics Texture Blend", Range(0, 1)) = 0.0
        
        [Header(Foam)]
        _FoamTex ("Foam Texture", 2D) = "gray" {}
        _FoamScale ("Foam Scale", Range(1, 80000)) = 40
        _FoamStretchX ("Foam Stretch X", Range(0.1, 5)) = 0.5
        _FoamStretchY ("Foam Stretch Y", Range(0.1, 5)) = 1.2
        _FoamSpeed ("Foam Animation Speed", Range(0, 5)) = 2.5
        _FoamTexBlend ("Foam Texture Blend", Range(0, 1)) = 0.0
        _FoamEdgeWidth ("Edge Foam Width", Range(0, 0.3)) = 0.15
        _FoamThreshold ("Foam Threshold", Range(0, 1)) = 0.45
        _FoamOpacity ("Foam Opacity", Range(0, 1)) = 0.85
        
        [Header(Detail Breakup)]
        _DetailTex ("Detail Texture", 2D) = "gray" {}
        _DetailScale ("Detail Scale", Range(1, 100)) = 20
        _DetailStretchX ("Detail Stretch X", Range(0.1, 5)) = 1.0
        _DetailStretchY ("Detail Stretch Y", Range(0.1, 5)) = 1.0
        _DetailSpeed ("Detail Speed", Range(0, 2)) = 0.5
        _DetailTexBlend ("Detail Texture Blend", Range(0, 1)) = 0.0
        
        [Header(Transparency)]
        _CoreAlpha ("Core Alpha", Range(0.3, 1)) = 0.8
        _EdgeAlpha ("Edge Alpha", Range(0, 1)) = 0.6
        
        [Header(Lighting Settings)]
        _FresnelPower ("Fresnel Power", Range(1, 5)) = 2.0
        _SpecularPower ("Specular Sharpness", Range(1, 128)) = 32
        
        [Header(Source Position)]
        _SourceY ("Source Y Position", Range(-10, 10)) = -5
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
            
            // Texture samplers
            sampler2D _FlowTex;
            sampler2D _CausticsTex;
            sampler2D _FoamTex;
            sampler2D _DetailTex;
            
            fixed4 _WaterColor;
            fixed4 _WaterColorDeep;
            fixed4 _FoamColor;
            fixed4 _FlowColor;
            fixed4 _CausticsColor;
            
            float _Cutoff;
            float _Smoothness;
            
            // Master intensities
            float _LightingIntensity;
            float _SpecularIntensity;
            float _FresnelIntensity;
            float _CausticsIntensity;
            float _FlowIntensity;
            float _FoamIntensity;
            float _WallFoamIntensity;
            float _TopFoamIntensity;
            float _DetailIntensity;
            float _DepthColorBlend;
            
            // Threshold and contrast
            float _FlowThreshold;
            float _FlowContrast;
            float _CausticsThreshold;
            float _CausticsContrast;
            float _FoamNoiseThreshold;
            float _FoamNoiseContrast;
            float _DetailThreshold;
            float _DetailContrast;
            
            // Flow params
            float _FlowScale;
            float _FlowStretchX;
            float _FlowStretchY;
            float _FlowSpeed;
            float _FlowTexBlend;
            
            // Caustics params
            float _CausticsScale;
            float _CausticsStretchX;
            float _CausticsStretchY;
            float _CausticsSpeed;
            float _CausticsTexBlend;
            
            // Foam params
            float _FoamScale;
            float _FoamStretchX;
            float _FoamStretchY;
            float _FoamSpeed;
            float _FoamTexBlend;
            float _FoamEdgeWidth;
            float _FoamThreshold;
            float _FoamOpacity;
            
            // Detail params
            float _DetailScale;
            float _DetailStretchX;
            float _DetailStretchY;
            float _DetailSpeed;
            float _DetailTexBlend;
            
            float _CoreAlpha;
            float _EdgeAlpha;
            
            float _FresnelPower;
            float _SpecularPower;
            
            float _SourceY;

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
            
            // Apply threshold and contrast to noise value
            float ApplyThresholdContrast(float value, float threshold, float contrast) {
                // Subtract threshold, so values below become negative (then clamped to 0)
                float v = value - threshold;
                // Remap remaining range and apply contrast
                v = saturate(v / max(1.0 - threshold, 0.001));
                // Apply contrast (power curve)
                v = pow(v, contrast);
                return v;
            }
            
            // Stretched UV helper
            float2 StretchUV(float2 uv, float stretchX, float stretchY, float scale) {
                return float2(uv.x * stretchX, uv.y * stretchY) * scale;
            }
            
            // Sample texture or procedural noise with blend
            float SampleFlowNoise(float2 uv, float time) {
                float2 stretchedUV = StretchUV(uv, _FlowStretchX, _FlowStretchY, _FlowScale);
                float2 animUV = stretchedUV + float2(0, -time * _FlowSpeed);
                
                // Procedural version - multiple layers for streaky look
                float proc1 = Noise(animUV);
                float proc2 = Noise(animUV * 2.0 + float2(17.3, 0));
                float proc3 = Noise(animUV * 4.0 + float2(0, 31.7));
                float procedural = (proc1 + proc2 * 0.5 + proc3 * 0.25) / 1.75;
                
                // Texture version
                float2 texUV = animUV * 0.1;
                float textured = tex2D(_FlowTex, texUV).r;
                
                float result = lerp(procedural, textured, _FlowTexBlend);
                return ApplyThresholdContrast(result, _FlowThreshold, _FlowContrast);
            }
            
            // Caustics pattern
            float SampleCaustics(float2 uv, float time) {
                float2 stretchedUV = StretchUV(uv, _CausticsStretchX, _CausticsStretchY, _CausticsScale);
                
                // Procedural caustics
                float2 p = stretchedUV;
                float c1 = Noise(p + float2(time * 0.3, time * 0.2));
                float c2 = Noise(p * 1.5 + float2(-time * 0.2, time * 0.4));
                float c3 = Noise(p * 2.0 + float2(time * 0.1, -time * 0.3));
                float procedural = c1 * c2 * c3;
                procedural = pow(procedural, 0.5) * 3.0;
                procedural = saturate(procedural);
                
                // Texture version
                float2 texUV1 = stretchedUV * 0.1 + float2(time * 0.05, time * 0.03);
                float2 texUV2 = stretchedUV * 0.15 + float2(-time * 0.03, time * 0.06);
                float tex1 = tex2D(_CausticsTex, texUV1).r;
                float tex2 = tex2D(_CausticsTex, texUV2).r;
                float textured = saturate(tex1 * tex2 * 3.0);
                
                float result = lerp(procedural, textured, _CausticsTexBlend);
                return ApplyThresholdContrast(result, _CausticsThreshold, _CausticsContrast);
            }
            
            // Foam noise pattern
            float SampleFoamNoise(float2 uv, float time) {
                float2 stretchedUV = StretchUV(uv, _FoamStretchX, _FoamStretchY, _FoamScale);
                
                // Procedural
                float n1 = Noise(stretchedUV + float2(0, time * _FoamSpeed));
                float n2 = Noise(stretchedUV * 2.0 + float2(time * _FoamSpeed * 0.7, 0));
                float n3 = Noise(stretchedUV * 4.0 - float2(0, time * _FoamSpeed * 0.5));
                float procedural = (n1 + n2 * 0.5 + n3 * 0.25) / 1.75;
                procedural = pow(procedural, 0.7);
                
                // Texture version
                float2 texUV = stretchedUV * 0.1 + float2(0, -time * _FoamSpeed * 0.1);
                float textured = tex2D(_FoamTex, texUV).r;
                
                float result = lerp(procedural, textured, _FoamTexBlend);
                return ApplyThresholdContrast(result, _FoamNoiseThreshold, _FoamNoiseContrast);
            }
            
            // Detail variation noise
            float SampleDetailNoise(float2 uv, float time) {
                float2 stretchedUV = StretchUV(uv, _DetailStretchX, _DetailStretchY, _DetailScale);
                
                // Procedural
                float procedural = Noise(stretchedUV + time * _DetailSpeed);
                
                // Texture version
                float2 texUV = stretchedUV * 0.1 + float2(time * _DetailSpeed * 0.05, 0);
                float textured = tex2D(_DetailTex, texUV).r;
                
                float result = lerp(procedural, textured, _DetailTexBlend);
                return ApplyThresholdContrast(result, _DetailThreshold, _DetailContrast);
            }

            // Check if pixel is an obstacle
            float IsObstacle(float2 uv) {
                fixed4 obs = tex2D(_ObstacleTex, uv);
                return (obs.r > 0.5 && obs.g < 0.4 && obs.b < 0.4) ? 1.0 : 0.0;
            }
            
            // Check if pixel is enemy slime
            float IsEnemyNearby(float2 uv) {
                fixed4 obs = tex2D(_ObstacleTex, uv);
                float isGreen = (obs.g > 0.4 && obs.r < 0.5 && obs.b < 0.5) ? 1.0 : 0.0;
                return isGreen;
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
                        
                        float w = 1.0 / (1.0 + length(float2(x, y)));
                        sum += tex2D(_MainTex, sampleUV).r * w;
                        total += w;
                    }
                }
                
                return total > 0 ? sum / total : 0;
            }

            // Detect if near obstacle (for foam on walls)
            float NearObstacle(float2 uv) {
                float near = 0;
                
                for (int r = 1; r <= 3; r++) {
                    float delta = 0.008 * r;
                    float weight = 1.0 / r;
                    
                    near += IsObstacle(uv + float2(delta, 0)) * weight;
                    near += IsObstacle(uv + float2(-delta, 0)) * weight;
                    near += IsObstacle(uv + float2(0, delta)) * weight;
                    near += IsObstacle(uv + float2(0, -delta)) * weight;
                    near += IsObstacle(uv + float2(delta, delta)) * weight * 0.7;
                    near += IsObstacle(uv + float2(-delta, delta)) * weight * 0.7;
                    near += IsObstacle(uv + float2(delta, -delta)) * weight * 0.7;
                    near += IsObstacle(uv + float2(-delta, -delta)) * weight * 0.7;
                }
                
                return saturate(near * 0.3);
            }
            
            // Detect frontier - ANY edge where water meets empty space
            float DetectFrontier(float2 uv, float val) {
                if (val <= _Cutoff) return 0;
                
                float delta = 0.012;
                float jitter = Noise(uv * 150.0) * 0.006;
                
                float frontier = 0;
                
                // Check all directions for empty space
                float right1 = SampleSmooth(uv + float2(delta + jitter, 0), 1.0);
                float right2 = SampleSmooth(uv + float2(delta * 2, jitter), 1.0);
                float left1 = SampleSmooth(uv + float2(-delta + jitter, 0), 1.0);
                float left2 = SampleSmooth(uv + float2(-delta * 2, -jitter), 1.0);
                float up1 = SampleSmooth(uv + float2(jitter, delta), 1.0);
                float up2 = SampleSmooth(uv + float2(-jitter, delta * 2), 1.0);
                float down1 = SampleSmooth(uv + float2(jitter, -delta), 1.0);
                float down2 = SampleSmooth(uv + float2(-jitter, -delta * 2), 1.0);
                
                // Soft detection - any direction bordering empty = frontier
                frontier += smoothstep(_Cutoff + 0.05, _Cutoff - 0.05, right1);
                frontier += smoothstep(_Cutoff + 0.05, _Cutoff - 0.05, right2) * 0.5;
                frontier += smoothstep(_Cutoff + 0.05, _Cutoff - 0.05, left1);
                frontier += smoothstep(_Cutoff + 0.05, _Cutoff - 0.05, left2) * 0.5;
                frontier += smoothstep(_Cutoff + 0.05, _Cutoff - 0.05, up1);
                frontier += smoothstep(_Cutoff + 0.05, _Cutoff - 0.05, up2) * 0.5;
                frontier += smoothstep(_Cutoff + 0.05, _Cutoff - 0.05, down1);
                frontier += smoothstep(_Cutoff + 0.05, _Cutoff - 0.05, down2) * 0.5;
                
                return saturate(frontier * 0.25);
            }

            fixed4 frag (v2f i) : SV_Target {
                float2 uv = i.uv;
                float time = _Time.y;
                
                // Don't render on obstacles
                if (IsObstacle(uv) > 0.5) discard;
                
                // Sample water mask
                float val = SampleSmooth(uv, 1.5);
                float alpha = smoothstep(_Cutoff, _Cutoff + _Smoothness, val);
                
                if (alpha < 0.01) discard;

                // === BASE COLOR (always starts here) ===
                float thickness = saturate(val * 2.0);
                float coreAmount = thickness * thickness;
                
                // Depth color blend - 0 means only WaterColor, 1 means full blend to deep
                fixed4 waterCol = lerp(_WaterColor, lerp(_WaterColor, _WaterColorDeep, coreAmount), _DepthColorBlend);
                
                // === FOAM DETECTION ===
                float nearWall = NearObstacle(uv);
                float frontier = DetectFrontier(uv, val);
                
                // === FOAM (only if intensities > 0) ===
                float finalFoam = 0;
                float totalFoamIntensity = _FoamIntensity + _WallFoamIntensity + _TopFoamIntensity;
                
                if (totalFoamIntensity > 0.001) {
                    float foamNoise = SampleFoamNoise(uv, time);
                    float foamVariation = SampleDetailNoise(uv, time);
                    foamNoise = foamNoise * (1.0 - _DetailIntensity) + foamVariation * _DetailIntensity;
                    
                    float safeFoamWidth = max(_FoamEdgeWidth, 1e-4);
                    float edgeBand = saturate(1.0 - (val - _Cutoff) / safeFoamWidth);
                    edgeBand *= alpha;
                    float edgeFoam = edgeBand * _FoamIntensity;
                    
                    float wallFoam = nearWall * _WallFoamIntensity;
                    float topFoam = frontier * _TopFoamIntensity;
                    
                    float foamAmount = saturate(edgeFoam + wallFoam + topFoam);
                    float foamWithNoise = foamAmount * lerp(0.65, 1.0, foamNoise);
                    finalFoam = smoothstep(_FoamThreshold, 1.0, foamWithNoise);
                    finalFoam = max(finalFoam, wallFoam * 0.35);
                    finalFoam = saturate(finalFoam);
                }
                
                // === FLOW/STREAKS (only if intensity > 0) ===
                if (_FlowIntensity > 0.001) {
                    float flowNoise = SampleFlowNoise(uv, time);
                    float jetStreaks = smoothstep(0.4, 0.6, flowNoise) * _FlowIntensity;
                    // Apply flow color - can brighten, darken, or tint
                    float3 flowEffect = (_FlowColor.rgb - 0.5) * 2.0 * jetStreaks;
                    waterCol.rgb += flowEffect * (1.0 - finalFoam);
                }
                
                // === CAUSTICS (only if intensity > 0) ===
                if (_CausticsIntensity > 0.001) {
                    float caustics = SampleCaustics(uv, time * _CausticsSpeed);
                    // Apply caustics color - can brighten, darken, or tint
                    float3 causticsEffect = (_CausticsColor.rgb - 0.5) * 2.0 * caustics * _CausticsIntensity;
                    waterCol.rgb += causticsEffect * (1.0 - finalFoam);
                }
                
                // === LIGHTING (only if intensity > 0) ===
                if (_LightingIntensity > 0.001 || _SpecularIntensity > 0.001 || _FresnelIntensity > 0.001) {
                    float delta = 0.006;
                    float right = SampleSmooth(uv + float2(delta, 0), 1.0);
                    float up = SampleSmooth(uv + float2(0, delta), 1.0);
                    float left = SampleSmooth(uv + float2(-delta, 0), 1.0);
                    float down = SampleSmooth(uv + float2(0, -delta), 1.0);
                    
                    float3 normal;
                    normal.x = (left - right) * 2.0;
                    normal.y = (down - up) * 2.0;
                    normal.z = 1.0;
                    normal = normalize(normal);
                    
                    float3 lightDir = normalize(float3(-0.3, 0.5, 1.0));
                    float3 viewDir = float3(0, 0, 1);
                    
                    // Diffuse lighting
                    if (_LightingIntensity > 0.001) {
                        float NdotL = saturate(dot(normal, lightDir));
                        float lighting = lerp(1.0, 0.7 + NdotL * 0.4, _LightingIntensity);
                        waterCol.rgb *= lighting;
                    }
                    
                    // Specular
                    if (_SpecularIntensity > 0.001) {
                        float3 halfDir = normalize(lightDir + viewDir);
                        float spec = pow(saturate(dot(normal, halfDir)), _SpecularPower);
                        waterCol.rgb += spec * _SpecularIntensity * (1.0 - finalFoam);
                    }
                    
                    // Fresnel rim
                    if (_FresnelIntensity > 0.001) {
                        float fresnel = pow(1.0 - saturate(dot(normal, viewDir)), _FresnelPower);
                        waterCol.rgb += fresnel * _FresnelIntensity * _WaterColor.rgb * 0.5 * (1.0 - finalFoam);
                    }
                }
                
                // === APPLY FOAM ===
                fixed4 col = waterCol;
                col.rgb = lerp(col.rgb, _FoamColor.rgb, finalFoam);
                
                // === ALPHA ===
                float baseAlpha = lerp(_EdgeAlpha, _CoreAlpha, thickness);
                float foamAlpha = lerp(baseAlpha, 1.0, _FoamOpacity);
                baseAlpha = lerp(baseAlpha, foamAlpha, finalFoam);
                
                col.a = baseAlpha * alpha;
                
                return col;
            }
            ENDCG
        }
    }
    
    FallBack "Transparent/Diffuse"
}
