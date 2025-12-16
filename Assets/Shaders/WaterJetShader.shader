Shader "Custom/WaterJetShader"
{
    Properties
    {
        _MainTex ("Mask Texture", 2D) = "white" {}
        _ObstacleTex ("Obstacle Map", 2D) = "black" {}
        
        [Header(Water Colors)]
        _WaterColor ("Water Color", Color) = (0.3, 0.6, 1.0, 0.85)
        _WaterColorDeep ("Deep Water Color", Color) = (0.15, 0.4, 0.8, 0.9)
        _FoamColor ("Foam Color", Color) = (1, 1, 1, 1)
        
        [Header(Shape)]
        _Cutoff ("Cutoff", Range(0,1)) = 0.1
        _Smoothness ("Edge Smoothness", Range(0.01, 0.2)) = 0.05
        
        [Header(Water Animation)]
        _FlowSpeed ("Flow Speed", Range(0, 3)) = 1.0
        _CausticsScale ("Caustics Scale", Range(1, 50)) = 15
        _CausticsSpeed ("Caustics Speed", Range(0, 2)) = 0.5
        _CausticsIntensity ("Caustics Intensity", Range(0, 1)) = 0.3
        
        [Header(Foam Settings)]
        _FoamEdgeWidth ("Edge Foam Width", Range(0, 0.3)) = 0.15
        _FoamNoiseScale ("Foam Noise Scale", Range(1, 100)) = 40
        _FoamSpeed ("Foam Animation Speed", Range(0, 5)) = 2.5
        _FoamIntensity ("Foam Intensity", Range(0, 3)) = 0.8
        _WallFoamIntensity ("Wall Foam Intensity", Range(0, 5)) = 2.0
        _TopFoamIntensity ("Top/Frontier Foam", Range(0, 3)) = 1.5
        
        [Header(Transparency)]
        _CoreAlpha ("Core Alpha", Range(0.3, 1)) = 0.8
        _EdgeAlpha ("Edge Alpha", Range(0, 1)) = 0.6
        
        [Header(Fresnel Rim)]
        _FresnelPower ("Fresnel Power", Range(1, 5)) = 2.0
        _FresnelIntensity ("Fresnel Intensity", Range(0, 1)) = 0.4
        
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
            
            fixed4 _WaterColor;
            fixed4 _WaterColorDeep;
            fixed4 _FoamColor;
            
            float _Cutoff;
            float _Smoothness;
            
            float _FlowSpeed;
            float _CausticsScale;
            float _CausticsSpeed;
            float _CausticsIntensity;
            
            float _FoamEdgeWidth;
            float _FoamNoiseScale;
            float _FoamSpeed;
            float _FoamIntensity;
            float _WallFoamIntensity;
            float _TopFoamIntensity;
            float _FoamThreshold;
            
            float _CoreAlpha;
            float _EdgeAlpha;
            
            float _FresnelPower;
            float _FresnelIntensity;
            
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
            
            // Caustics pattern
            float Caustics(float2 uv, float time) {
                float2 p = uv * _CausticsScale;
                
                // Two layers of animated noise for caustic look
                float c1 = Noise(p + float2(time * 0.3, time * 0.2));
                float c2 = Noise(p * 1.5 + float2(-time * 0.2, time * 0.4));
                float c3 = Noise(p * 2.0 + float2(time * 0.1, -time * 0.3));
                
                // Combine and create bright caustic lines
                float caustic = c1 * c2 * c3;
                caustic = pow(caustic, 0.5) * 3.0;
                
                return saturate(caustic);
            }
            
            // Foam noise pattern
            float FoamNoise(float2 uv, float time) {
                float2 p = uv * _FoamNoiseScale;
                
                // Animated bubble-like noise
                float n1 = Noise(p + float2(0, time * _FoamSpeed));
                float n2 = Noise(p * 2.0 + float2(time * _FoamSpeed * 0.7, 0));
                float n3 = Noise(p * 4.0 - float2(0, time * _FoamSpeed * 0.5));
                
                // Create bubbly foam pattern
                float foam = (n1 + n2 * 0.5 + n3 * 0.25) / 1.75;
                foam = pow(foam, 0.7);
                
                return foam;
            }

            // Check if pixel is an obstacle
            float IsObstacle(float2 uv) {
                fixed4 obs = tex2D(_ObstacleTex, uv);
                return (obs.r > 0.5 && obs.g < 0.4 && obs.b < 0.4) ? 1.0 : 0.0;
            }
            
            // Check if pixel is enemy slime (green in obstacle map or we need separate data)
            float IsEnemyNearby(float2 uv) {
                // For now, check obstacle map for non-red, non-black areas
                // We'll improve this with actual enemy cell data later
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
                        
                        float weight = 1.0 / (1.0 + length(float2(x, y)));
                        sum += tex2D(_MainTex, sampleUV).r * weight;
                        total += weight;
                    }
                }
                
                if (total < 0.01) return tex2D(_MainTex, uv).r;
                return sum / total;
            }
            
            // Detect edge of water mass - returns how much this pixel is on the edge
            float DetectEdge(float2 uv, float val) {
                float delta = 0.008;
                
                float right = SampleSmooth(uv + float2(delta, 0), 1.0);
                float left = SampleSmooth(uv + float2(-delta, 0), 1.0);
                float up = SampleSmooth(uv + float2(0, delta), 1.0);
                float down = SampleSmooth(uv + float2(0, -delta), 1.0);
                
                // Strong edge where we border empty space
                float emptyRight = (right < _Cutoff) ? 1.0 : 0.0;
                float emptyLeft = (left < _Cutoff) ? 1.0 : 0.0;
                float emptyUp = (up < _Cutoff) ? 1.0 : 0.0;
                float emptyDown = (down < _Cutoff) ? 1.0 : 0.0;
                
                float edge = (emptyRight + emptyLeft + emptyUp + emptyDown) * 0.4;
                
                return saturate(edge);
            }
            
            // Detect if near obstacle (for foam on walls) - multi-sample for thicker detection
            float NearObstacle(float2 uv) {
                float near = 0;
                
                // Multiple radii for gradient foam
                for (int r = 1; r <= 3; r++) {
                    float delta = 0.008 * r;
                    float weight = 1.0 / r; // Closer = stronger
                    
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
            
            // Detect frontier - top edge where water is pushing into empty/enemy space
            float DetectFrontier(float2 uv, float val) {
                float delta = 0.012;
                
                // Check if there's less water above us
                float up1 = SampleSmooth(uv + float2(0, delta), 1.0);
                float up2 = SampleSmooth(uv + float2(0, delta * 2), 1.0);
                float up3 = SampleSmooth(uv + float2(0, delta * 3), 1.0);
                
                // Frontier if we have water but above is empty
                float frontier = 0;
                if (val > _Cutoff) {
                    frontier += (up1 < _Cutoff) ? 1.0 : 0.0;
                    frontier += (up2 < _Cutoff) ? 0.7 : 0.0;
                    frontier += (up3 < _Cutoff) ? 0.4 : 0.0;
                }
                
                return saturate(frontier * 0.6);
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

                // === DISTANCE FROM SOURCE ===
                float distFromSource = uv.y;
                float sourceInfluence = 1.0 - saturate(distFromSource * 0.8);
                
                // === FLOW ANIMATION ===
                float2 flowUV = uv;
                flowUV.y -= time * _FlowSpeed * 0.1;
                
                // === CAUSTICS ===
                float caustics = Caustics(flowUV, time * _CausticsSpeed);

                // === VERTICAL JET STREAKS ===
                float jetStreaks = Noise(float2(uv.x * 50.0, uv.y * 5.0 - time * _FlowSpeed * 2.0));
                jetStreaks = smoothstep(0.4, 0.6, jetStreaks) * 0.15;
                
                // === FOAM DETECTION ===
                float edge = DetectEdge(uv, val);
                float nearWall = NearObstacle(uv);
                float frontier = DetectFrontier(uv, val);
                
                // === FOAM NOISE ===
                float foamNoise = FoamNoise(uv, time);
                // Add variation - some areas more foamy than others
                float foamVariation = Noise(uv * 20.0 + time * 0.5);
                foamNoise = foamNoise * 0.7 + foamVariation * 0.3;
                
                // === CALCULATE FOAM AMOUNTS ===
                // Edge foam (sides of water stream)
                float edgeFoam = edge * _FoamIntensity;
                
                // Wall/obstacle foam (around windows) - should be WHITE and strong
                float wallFoam = nearWall * _WallFoamIntensity;
                
                // Top frontier foam (where water pushes up)
                float topFoam = frontier * _TopFoamIntensity;
                
                // Combine all foam sources
                float foamAmount = saturate(edgeFoam + wallFoam + topFoam);
                
                // Apply noise to foam (but keep minimum foam where detected)
                float foamWithNoise = foamAmount * (0.5 + foamNoise * 0.5);
                
                // Threshold for crisp foam edges
                float finalFoam = smoothstep(_FoamThreshold * 0.5, _FoamThreshold, foamWithNoise);
                
                // Extra boost for wall foam - always visible
                finalFoam = max(finalFoam, wallFoam * 0.5);
                finalFoam = saturate(finalFoam * 0.8); // Overall foam reduction
                
                // === BASE COLOR ===
                float thickness = saturate(val * 2.0);
                // Core detection - how surrounded by water are we?
                float coreAmount = thickness * thickness; // Simple: thicker = more core
                float edginess = saturate(edge + nearWall * 0.5 + frontier * 0.5);
                fixed4 waterCol = lerp(_WaterColor, _WaterColorDeep, coreAmount);
                // Lighten edges slightly but keep blue
                waterCol.rgb = lerp(waterCol.rgb, waterCol.rgb * 1.3, edginess * 0.3);
                
                // Add caustics to water (not on foam)
                waterCol.rgb += caustics * _CausticsIntensity * (1.0 - finalFoam);
                waterCol.rgb += jetStreaks * (1.0 - finalFoam) * coreAmount;
                
                // === FAKE NORMAL FOR SHADING ===
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
                
                // Simple lighting
                float3 lightDir = normalize(float3(-0.3, 0.5, 1.0));
                float NdotL = saturate(dot(normal, lightDir));
                
                // Fresnel rim
                float3 viewDir = float3(0, 0, 1);
                float fresnel = pow(1.0 - saturate(dot(normal, viewDir)), _FresnelPower);
                fresnel *= _FresnelIntensity;
                
                // Specular highlight
                float3 halfDir = normalize(lightDir + viewDir);
                float spec = pow(saturate(dot(normal, halfDir)), 32) * 0.8;
                
                // === COMBINE ===
                fixed4 col = waterCol;
                
                // Apply lighting to water
                col.rgb *= (0.7 + NdotL * 0.4);
                col.rgb += spec * (1.0 - finalFoam);
                
                // Add fresnel rim
                col.rgb += fresnel * _WaterColor.rgb * 0.5 * (1.0 - finalFoam);
                
                // === APPLY FOAM ===
                // Foam is white and opaque
                col.rgb = lerp(col.rgb, float3(1,1,1), finalFoam * 0.95);
                
                // === ALPHA ===
                float baseAlpha = lerp(_EdgeAlpha, _CoreAlpha, thickness);
                // Foam is more opaque
                baseAlpha = lerp(baseAlpha, 1.0, finalFoam * 0.9);
                
                col.a = baseAlpha * alpha;
                
                return col;
            }
            ENDCG
        }
    }
    
    FallBack "Transparent/Diffuse"
}
