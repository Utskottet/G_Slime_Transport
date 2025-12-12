Shader "TextMeshPro/Dissolve"
{
    Properties
    {
        // TMP font atlas comes in here:
        _MainTex ("Font Atlas", 2D) = "white" {}
        _FaceColor ("Face Color", Color) = (1,1,1,1)

        _DissolveTex ("Dissolve Noise", 2D) = "white" {}
        _Dissolve ("Dissolve Amount", Range(0,1)) = 0
        _Softness ("Edge Softness", Range(0,0.2)) = 0.05
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        ZWrite Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            sampler2D _DissolveTex;
            fixed4 _FaceColor;
            float _Dissolve;
            float _Softness;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv     : TEXCOORD0;
                fixed4 color  : COLOR;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv     : TEXCOORD0;
                fixed4 color  : COLOR;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = v.color;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float glyph = tex2D(_MainTex, i.uv).a;     // <-- TMP glyph alpha
                float noise = tex2D(_DissolveTex, i.uv).r;

                float edge = smoothstep(_Dissolve, _Dissolve + _Softness, noise);

                fixed4 col = _FaceColor * i.color;
                col.a *= glyph * edge;

                clip(col.a - 0.01);
                return col;
            }
            ENDCG
        }
    }
}
