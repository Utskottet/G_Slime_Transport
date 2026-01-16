Shader "Custom/AxelSlime01"
{
    Properties
    {
        _MainTex ("Mask Texture", 2D) = "white" {}
        _ObstacleTex ("Obstacle Map", 2D) = "black" {}
        _Color ("Slime Color", Color) = (0,1,0,1)
        _ColorDark ("Dark Color", Color) = (0,0.5,0,1)
        _EdgeColor ("Edge Glow Color", Color) = (0.5,1,0.5,1)
        
        [Header(Shape)]
        _Cutoff ("Cutoff", Range(0,1)) = 0.1
        _Smoothness ("Edge Smoothness", Range(0.01, 0.2)) = 0.05
        _Bevel ("Bevel Strength", Range(0, 5)) = 2.0
        
        [Header(Lighting)]
        _FresnelPower ("Fresnel Power", Range(1, 5)) = 2.0
        _FresnelIntensity ("Fresnel Intensity", Range(0, 1)) = 0.5
        _SpecularPower ("Specular Power", Range(1, 64)) = 16
        _SpecularIntensity ("Specular Intensity", Range(0, 2)) = 0.8
        _SpotlightBoost ("Spotlight Boost", Range(0, 5)) = 2.0
        _SpotSpecular ("Spot Specular", Range(0, 5)) = 3.0

        [Header(Alive Animation)]
        // UPDATED: Increased max range from 20 to 200
        _NoiseScale ("Ripple Scale", Range(1, 200)) = 50.0 
        _NoiseSpeed ("Ripple Speed", Range(0, 5)) = 2.0
        _NoiseStrength ("Ripple Height", Range(0, 0.2)) = 0.05
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
            
            // Animation Variables
            float _NoiseScale;
            float _NoiseSpeed;
            float _NoiseStrength;

            // Spotlight data
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

            float Random(float2 uv) {
                return frac(sin(dot(uv, float2(12.9898, 78.233))) * 43758.5453123);
            }

            float Noise(float2 uv) {
                float2 i = floor(uv);
                float2 f = frac(uv);
                f = f * f * (3.0 - 2.0 * f);

                float a = Random(i);
                float b = Random(i + float2(1.0, 0.0));
                float c = Random(i + float2(0.0, 1.0));
                float d = Random(i + float2(1.0, 1.0));

                return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
            }

            float SampleHeight(float2 uv) {
                if (IsObstacle(uv) > 0.5) return 0;
                
                float h = tex2D(_MainTex, uv).r;
                
                if (h > 0.05) {
                    // Apply Ripple
                    float ripple = Noise(uv * _NoiseScale + _Time.y * _NoiseSpeed);
                    h += (ripple - 0.5) * _NoiseStrength * smoothstep(0, 1, h); 
                }
                
                return h;
            }

            float SampleSmooth(float2 uv, float radius) {
                float sum = 0;
                float total = 0;
                sum += SampleHeight(uv) * 4.0; total += 4.0;
                float d = _MainTex_TexelSize.x * radius;
                sum += SampleHeight(uv + float2(d, 0)); total += 1.0;
                sum += SampleHeight(uv - float2(d, 0)); total += 1.0;
                sum += SampleHeight(uv + float2(0, d)); total += 1.0;
                sum += SampleHeight(uv - float2(0, d)); total += 1.0;
                return sum / total;
            }

            float3 CalcSpotlight(float3 worldPos, float3 normal, float3 viewDir,
                                 float3 spotPos, float3 spotDir, float spotAngle, 
                                 float spotIntensity, float3 spotColor)
            {
                float3 toLight = spotPos - worldPos;
                float dist = length(toLight);
                float3 lightDir = toLight / dist;
                float cosAngle = dot(-lightDir, spotDir);
                float coneAngle = cos(spotAngle * 0.5 * 0.0174533);
                float inCone = smoothstep(coneAngle - 0.1, coneAngle + 0.1, cosAngle);
                if (inCone < 0.01) return float3(0,0,0);
                float atten = saturate(1.0 - dist * 0.02) * inCone;
                float NdotL = saturate(dot(normal, lightDir));
                float3 diffuse = spotColor * NdotL * atten * spotIntensity * _SpotlightBoost * 0.1;
                float3 halfDir = normalize(lightDir + viewDir);
                float NdotH = saturate(dot(normal, halfDir));
                float spec = pow(NdotH, _SpecularPower * 2) * _SpotSpecular * atten * spotIntensity * 0.01;
                return diffuse + spotColor * spec;
            }

            fixed4 frag (v2f i) : SV_Target {
                float2 uv = i.uv;
                if (IsObstacle(uv) > 0.5) discard;
                
                float val = SampleSmooth(uv, 1.5);
                float alpha = smoothstep(_Cutoff, _Cutoff + _Smoothness, val);
                if (alpha < 0.01) discard;

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

                float3 lightDir = normalize(float3(-0.5, 0.5, 1.0));
                float NdotL = saturate(dot(normal, lightDir));
                float3 viewDir = float3(0, 0, 1);
                float fresnel = pow(1.0 - saturate(dot(normal, viewDir)), _FresnelPower) * _FresnelIntensity;
                float3 halfDir = normalize(lightDir + viewDir);
                float spec = pow(saturate(dot(normal, halfDir)), _SpecularPower) * _SpecularIntensity;
                
                float thickness = saturate(val * 1.5);
                fixed4 baseColor = lerp(_Color, _ColorDark, thickness * 0.5);
                float edge = smoothstep(_Cutoff + _Smoothness, _Cutoff, val);

                fixed4 col = baseColor;
                col.rgb *= (0.6 + NdotL * 0.5);
                col.rgb += spec;
                col.rgb = lerp(col.rgb, _EdgeColor.rgb, fresnel);
                col.rgb += _EdgeColor.rgb * edge * 0.3;
                
                col.rgb += CalcSpotlight(i.worldPos, normal, viewDir, _Spot1Pos.xyz, _Spot1Dir.xyz, _Spot1Angle, _Spot1Intensity, _Spot1Color.rgb);
                col.rgb += CalcSpotlight(i.worldPos, normal, viewDir, _Spot2Pos.xyz, _Spot2Dir.xyz, _Spot2Angle, _Spot2Intensity, _Spot2Color.rgb);
                col.rgb += CalcSpotlight(i.worldPos, normal, viewDir, _Spot3Pos.xyz, _Spot3Dir.xyz, _Spot3Angle, _Spot3Intensity, _Spot3Color.rgb);
                
                col.a = alpha * 0.92;
                return col;
            }
            ENDCG
        }
    }
}