// Fake shadow strip under the belt (Built-in RP, unlit, transparent).
// Mesh contract: uv.x runs 0..1 across the strip, see BeltSweepMeshBuilder.BuildShadow.
Shader "BeltPath/Belt Shadow"
{
    Properties
    {
        _Color ("Color", Color) = (0.05, 0.1, 0.25, 1)
        _Brightness ("Brightness", Range(0, 2)) = 1
        _Saturation ("Saturation", Range(0, 2)) = 1
        _Opacity ("Opacity", Range(0, 1)) = 0.5
        _EdgeSoftness ("Edge softness", Range(0.01, 0.5)) = 0.25
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "IgnoreProjector" = "True" }

        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            ZTest LEqual
            Cull Off

            // Where two parts of the belt cross, the shadow is drawn once instead of twice.
            Stencil
            {
                Ref 1
                Comp NotEqual
                Pass Replace
            }

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            fixed4 _Color;
            half _Brightness, _Saturation, _Opacity, _EdgeSoftness;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                half across : TEXCOORD0;
                half3 rgb : TEXCOORD1;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.across = v.uv.x;
                // Depends on material values only, so it is computed once per vertex instead of once per pixel.
                half grey = dot(_Color.rgb, half3(0.299, 0.587, 0.114));
                o.rgb = saturate(lerp(grey.xxx, _Color.rgb, _Saturation) * _Brightness);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                half edge = smoothstep(0, _EdgeSoftness, min(i.across, 1 - i.across));
                return fixed4(i.rgb, _Opacity * edge * _Color.a);
            }
            ENDCG
        }
    }
}
