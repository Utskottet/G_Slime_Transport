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
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.texcoord;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float sdf = tex2D(_FaceTex, i.uv).a;
                float noise = tex2D(_DissolveTex, i.uv).r;

                float d = noise - _Dissolve;
                float alpha = smoothstep(-_Softness, _Softness, d) * sdf;

                return fixed4(_FaceColor.rgb, alpha);
            }
            ENDCG
        }
    }
}
