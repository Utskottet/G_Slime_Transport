Shader "Custom/SlimeShader"
{
    Properties
    {
        _MainTex ("Mask Texture", 2D) = "white" {}
        _Color ("Slime Color", Color) = (0,1,0,1)
        _Cutoff ("Cutoff", Range(0,1)) = 0.1
        _Bevel ("Bevel Strength", Range(0, 5)) = 3.0
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
            float4 _MainTex_TexelSize;
            fixed4 _Color;
            float _Cutoff;
            float _Bevel;

            v2f vert (appdata v) {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target {
                // Sample center thickness
                float val = tex2D(_MainTex, i.uv).r;
                
                // Cutout shape
                // smoothstep creates the sharp edge from the fuzzy bilinear texture
                float alpha = smoothstep(_Cutoff, _Cutoff + 0.05, val);
                
                // If transparent, stop here
                if (alpha < 0.01) discard;

                // FAKE 3D LIGHTING
                // Sample neighbors to calculate "slope"
                float delta = 0.005; 
                float up = tex2D(_MainTex, i.uv + float2(0, delta)).r;
                float right = tex2D(_MainTex, i.uv + float2(delta, 0)).r;
                
                // Slope: difference between center and neighbor
                float slopeX = (val - right);
                float slopeY = (val - up);
                
                // Light comes from top-left (positive slope in both X and Y)
                float light = (slopeX + slopeY) * _Bevel;

                fixed4 col = _Color;
                
                // Apply light: Brighten highlight, darken shadow
                col.rgb += light;
                
                // Extra Rim: Darken the very edge for contrast
                if (val < _Cutoff + 0.1) col.rgb *= 0.8;

                col.a = alpha * 0.95; // Slightly transparent
                
                return col;
            }
            ENDCG
        }
    }
}