Shader "Custom/VictoryShader"
{
    Properties
    {
        _ObstacleTex ("Obstacle Map", 2D) = "black" {}
        _HouseTex ("House Texture", 2D) = "black" {}
        
        [Header(Color Animation)]
        _ColorSpeed ("Color Cycle Speed", Range(0.1, 5)) = 1.0
        _PulseSpeed ("Pulse Speed", Range(0.5, 10)) = 2.0
        _PulseIntensity ("Pulse Intensity", Range(0, 1)) = 0.4
        _SwirlScale ("Swirl Scale", Range(1, 20)) = 6.0
        _SwirlSpeed ("Swirl Speed", Range(0.1, 3)) = 0.8
        _DarkAmount ("Dark Amount", Range(0, 1)) = 0.4
        _Contrast ("Contrast", Range(0.5, 3)) = 1.5
        
        [Header(Window Glow)]
        _WindowGlowWidth ("Window Glow Width", Range(0.001, 0.03)) = 0.008
        _WindowGlowIntensity ("Window Glow Intensity", Range(0, 3)) = 1.5
        
        [Header(Stars)]
        _StarDensity ("Star Density", Range(10, 200)) = 60
        _StarSpeed ("Star Fall Speed", Range(0.1, 3)) = 0.8
        _StarSize ("Star Size", Range(0.001, 0.02)) = 0.005
        _StarBrightness ("Star Brightness", Range(0.5, 3)) = 1.2
        
        [Header(Overall)]
        _Brightness ("Overall Brightness", Range(0, 2)) = 0.8
    }
    
    SubShader
    {
        Tags { "Queue"="Transparent+100" "RenderType"="Transparent" }
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

            sampler2D _ObstacleTex;
            sampler2D _HouseTex;
            float4 _ObstacleTex_TexelSize;
            
            float _ColorSpeed;
            float _PulseSpeed;
            float _PulseIntensity;
            float _SwirlScale;
            float _SwirlSpeed;
            
            float _WindowGlowWidth;
            float _WindowGlowIntensity;
            
            float _StarDensity;
            float _StarSpeed;
            float _StarSize;
            float _StarBrightness;
            
            float _Brightness;
            float _DarkAmount;
            float _Contrast;

            v2f vert (appdata v) {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            // Check if pixel is a window (red in obstacle map)
            float IsWindow(float2 uv) {
                fixed4 obs = tex2D(_ObstacleTex, uv);
                return (obs.r > 0.5 && obs.g < 0.4 && obs.b < 0.4) ? 1.0 : 0.0;
            }
            
            // Rainbow color from hue
            float3 HSVtoRGB(float h) {
                float3 rgb = saturate(abs(fmod(h * 6.0 + float3(0, 4, 2), 6.0) - 3.0) - 1.0);
                return rgb;
            }
            
            // Pseudo-random
            float Random(float2 st) {
                return frac(sin(dot(st, float2(12.9898, 78.233))) * 43758.5453);
            }
            
            // 2D Noise
            float Noise(float2 st) {
                float2 i = floor(st);
                float2 f = frac(st);
                
                float a = Random(i);
                float b = Random(i + float2(1.0, 0.0));
                float c = Random(i + float2(0.0, 1.0));
                float d = Random(i + float2(1.0, 1.0));
                
                float2 u = f * f * (3.0 - 2.0 * f);
                
                return lerp(a, b, u.x) + (c - a) * u.y * (1.0 - u.x) + (d - b) * u.x * u.y;
            }
            
            // Swirly plasma pattern
            float Plasma(float2 uv, float time) {
                float v = 0.0;
                float2 c = uv * _SwirlScale;
                
                v += sin(c.x + time);
                v += sin(c.y + time);
                v += sin(c.x + c.y + time);
                
                float2 c2 = c + float2(sin(time * 0.5), cos(time * 0.3));
                v += sin(sqrt(c2.x * c2.x + c2.y * c2.y) + time);
                
                return v * 0.25 + 0.5;
            }
            
            // Edge detection for window outlines
            float WindowEdge(float2 uv) {
                float center = IsWindow(uv);
                
                float edge = 0.0;
                float w = _WindowGlowWidth;
                
                // Sample neighbors
                float left = IsWindow(uv + float2(-w, 0));
                float right = IsWindow(uv + float2(w, 0));
                float up = IsWindow(uv + float2(0, w));
                float down = IsWindow(uv + float2(0, -w));
                
                // Diagonal samples for smoother edges
                float tl = IsWindow(uv + float2(-w, w));
                float tr = IsWindow(uv + float2(w, w));
                float bl = IsWindow(uv + float2(-w, -w));
                float br = IsWindow(uv + float2(w, -w));
                
                // Edge if center is different from neighbors
                float neighbors = (left + right + up + down + tl + tr + bl + br) / 8.0;
                
                // Edge strength: high when center differs from average of neighbors
                edge = abs(center - neighbors);
                
                // Also glow just outside windows
                if (center < 0.5 && neighbors > 0.1) {
                    edge = max(edge, neighbors * 0.8);
                }
                
                return saturate(edge * 3.0);
            }
            
            // Falling stars
            float Stars(float2 uv, float time) {
                float stars = 0.0;
                
                // Multiple star layers for depth
                for (int layer = 0; layer < 3; layer++) {
                    float layerOffset = layer * 0.3;
                    float layerSpeed = 1.0 + layer * 0.5;
                    float layerSize = _StarSize * (1.0 - layer * 0.2);
                    
                    // Grid for star placement
                    float2 gridUV = uv * _StarDensity * (1.0 + layer * 0.3);
                    gridUV.y += time * _StarSpeed * layerSpeed;
                    
                    float2 gridID = floor(gridUV);
                    float2 gridLocal = frac(gridUV) - 0.5;
                    
                    // Random offset within cell
                    float2 randOffset = float2(
                        Random(gridID + layerOffset) - 0.5,
                        Random(gridID.yx + layerOffset) - 0.5
                    ) * 0.8;
                    
                    float2 starPos = gridLocal - randOffset;
                    float dist = length(starPos);
                    
                    // Star brightness with twinkle
                    float twinkle = sin(time * 10.0 + Random(gridID) * 6.28) * 0.3 + 0.7;
                    
                    // Star shape
                    float star = smoothstep(layerSize, 0.0, dist) * twinkle;
                    
                    // Random star visibility
                    star *= step(0.7, Random(gridID + float2(layer, layer)));
                    
                    stars += star;
                }
                
                return saturate(stars) * _StarBrightness;
            }

            fixed4 frag (v2f i) : SV_Target {
                float time = _Time.y;
                float2 uv = i.uv;
                
                // Is this pixel inside a window?
                float isWindow = IsWindow(uv);
                
                // === BASE COLOR: Swirly RGB plasma ===
                float plasma = Plasma(uv, time * _SwirlSpeed);
                float hue = plasma + time * _ColorSpeed * 0.1;
                float3 plasmaColor = HSVtoRGB(hue);
                
                // === PULSING ===
                float pulse = sin(time * _PulseSpeed) * 0.5 + 0.5;
                float radialPulse = sin(length(uv - 0.5) * 10.0 - time * _PulseSpeed * 2.0) * 0.5 + 0.5;
                float combinedPulse = lerp(pulse, radialPulse, 0.5);
                
                plasmaColor = lerp(plasmaColor, plasmaColor * 1.5, combinedPulse * _PulseIntensity);
                
                // === WINDOW GLOW OUTLINES ===
                float edge = WindowEdge(uv);
                float edgeHue = hue + 0.5; // Complementary color
                float3 edgeColor = HSVtoRGB(edgeHue) * _WindowGlowIntensity;
                
                // Add extra shimmer to edges
                edgeColor *= 1.0 + sin(time * 8.0 + uv.x * 20.0) * 0.3;
                
                // === FALLING STARS ===
                float stars = Stars(uv, time);
                float3 starColor = float3(1, 1, 1) * stars;
                
                // Add color tint to some stars
                starColor += HSVtoRGB(frac(time * 0.2 + Random(floor(uv * _StarDensity)))) * stars * 0.5;
                
                // === COMBINE ===
                float3 finalColor = plasmaColor;
                
                // Add dark zones - use noise to create dark patches
                float darkNoise = Noise(uv * 3.0 + time * 0.2);
                float darkMask = smoothstep(0.3, 0.7, darkNoise);
                darkMask = lerp(1.0, darkMask, _DarkAmount);
                
                // Apply darkness
                finalColor *= darkMask;
                
                // Add contrast - push colors toward black or bright
                finalColor = pow(finalColor, _Contrast);
                
                // Add edge glow (on top of darkness)
                finalColor = lerp(finalColor, edgeColor, edge * 0.8);
                
                // Add stars on top
                finalColor += starColor;
                
                // Apply brightness
                finalColor *= _Brightness;
                
                // === ALPHA: Don't render inside windows ===
                // Full opacity outside windows, transparent inside
                float alpha = 1.0 - isWindow;
                
                // Keep edge glow visible even at window borders
                alpha = max(alpha, edge * 0.8);
                
                // Stars visible everywhere
                alpha = max(alpha, stars * 0.9);
                
                return fixed4(finalColor, alpha);
            }
            ENDCG
        }
    }
}
