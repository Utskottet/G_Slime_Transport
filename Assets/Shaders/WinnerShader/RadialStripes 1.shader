Shader "Custom/RadialStripes"
{
    Properties
    {
        _ObstacleTex ("Obstacle Map", 2D) = "white" {}
        _Color1 ("Color 1", Color) = (1, 0.8, 0, 1)  // Yellow
        _Color2 ("Color 2", Color) = (1, 0.4, 0, 1)  // Orange
        _StripeCount ("Stripe Count", Range(4, 64)) = 16
        _RotationSpeed ("Rotation Speed", Range(-5, 5)) = 1
        _Center ("Center", Vector) = (0.5, 0.5, 0, 0)
    }
    
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Background" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            float4 _Color1;
            float4 _Color2;
            float _StripeCount;
            float _RotationSpeed;
            float2 _Center;
            sampler2D _ObstacleTex;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // Sample obstacle map
                float4 obs = tex2D(_ObstacleTex, i.uv);
                
                // Cyan check: high in blue/green, low in red
                float isCyan = (obs.b > 0.4 && obs.g > 0.4 && obs.r < 0.5) ? 1.0 : 0.0;
                
                // If not cyan (windows/red walls), output pure black
                if (isCyan < 0.5) {
                    return fixed4(0, 0, 0, 1);
                }
                
                // Get vector from center to current pixel
                float2 dir = i.uv - _Center;
                
                // Calculate angle (0 to 2*PI)
                float angle = atan2(dir.y, dir.x);
                
                // Add rotation over time
                angle += _Time.y * _RotationSpeed;
                
                // Normalize to 0-1 range
                angle = (angle + 3.14159265) / (2.0 * 3.14159265);
                
                // Create stripes
                float stripe = frac(angle * _StripeCount);
                
                // Hard edge stripes (0 or 1)
                stripe = step(0.5, stripe);
                
                // Mix colors
                return lerp(_Color1, _Color2, stripe);
            }
            ENDCG
        }
    }
}
