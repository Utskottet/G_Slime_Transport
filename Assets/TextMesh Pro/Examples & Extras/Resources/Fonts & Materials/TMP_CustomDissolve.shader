Shader "TextMeshPro/CustomDissolve"
{
    Properties
    {
        _FaceTex ("Font Atlas", 2D) = "white" {}
        _FaceColor ("Face Color", Color) = (1,1,1,1)

        _DissolveTex ("Dissolve Noise", 2D) = "white" {}
        _Dissolve ("Dissolve Amount", Range(0,1)) = 0
        _Softness ("Edge Softness", Range(0.001,0.2)) = 0.05
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        ZWrite Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"
            #include "TMPro.cginc"
            #include "TMPro_Properties.cginc"

            sampler2D _FaceTex;
            sampler2D _DissolveTex;
            float4 _FaceColor;
            float _Dissolve;
            float _Softness;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 texcoord0 : TEXCOORD0;
                float4 color : COLOR;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.texcoord0;
                o.color = v.color;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // SDF atlas alpha
                float sdf = tex2D(_FaceTex, i.uv).a;

                // Dissolve mask
                float noise = tex2D(_DissolveTex, i.uv).r;

                // Soft threshold
                float alphaDiss = smoothstep(_Dissolve, _Dissolve + _Softness, noise);

                fixed4 col = _FaceColor * i.color;
                col.a *= sdf * alphaDiss;

                clip(col.a - 0.01);
                return col;
            }
            ENDCG
        }
    }
}
