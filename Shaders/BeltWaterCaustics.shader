// Additive light web drawn over the water surface (Built-in RP, unlit). Same mesh contract as Water Flow:
// uv.x across the channel, uv.y along the belt in UV units, a closed belt ends on a whole UV unit.
Shader "BeltPath/Water Caustics"
{
    Properties
    {
        [Header(Color)]
        _Color ("Color (alpha = intensity)", Color) = (1, 1, 1, 0.333)

        [Space(8)]
        [Header(Web Pattern)]
        _CausticTiling ("Cells (across, along) per UV unit", Vector) = (3, 8, 0, 0)
        _PathStretch ("Stretch along the belt", Range(1, 8)) = 4
        _WebWidth ("Line width", Range(0.02, 0.5)) = 0.18
        _WebScroll ("Scroll (across, along)", Vector) = (0, 0.3, 0, 0)

        [Space(8)]
        [Header(Softening Noise)]
        _Softness ("Amount", Range(0, 1)) = 1
        _DissolveScroll ("Scroll (across, along)", Vector) = (-0.1, -0.2, 0, 0)

        [Space(8)]
        [Header(Mask Noise)]
        _MaskScroll ("Scroll (across, along)", Vector) = (0.3, 0.3, 0, 0)
    }

    SubShader
    {
        Tags { "Queue" = "Transparent+1" "RenderType" = "Transparent" "IgnoreProjector" = "True" }

        Pass
        {
            ZWrite Off
            Cull Off
            Blend One One

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            #include "BeltWaterNoise.cginc"

            fixed4 _Color;
            float4 _CausticTiling, _WebScroll, _DissolveScroll, _MaskScroll;
            half _PathStretch, _WebWidth, _Softness;

            static const float SoftenCells = 3.0;
            static const float SoftenRows = 3.0;
            static const float MaskCells = 2.0;
            static const float MaskRows = 2.0;
            static const float MaskLow = 0.3;
            static const float MaskHigh = 0.7;

            struct Attributes { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; };

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.pos = UnityObjectToClipPos(input.vertex);
                output.uv = input.uv;
                return output;
            }

            // Bright web of cell borders; the cell count along the belt shrinks as the stretch grows.
            float Web(float2 uv)
            {
                float rows = max(1.0, round(_CausticTiling.y / _PathStretch));
                float2 p = float2(uv.x * _CausticTiling.x, (uv.y - _Time.y * _WebScroll.y) * rows);
                p.x += _Time.y * _WebScroll.x;
                return 1.0 - smoothstep(0.0, _WebWidth, BeltPeriodicWeb(p, rows));
            }

            float ScrollingNoise(float2 uv, float cells, float rows, float2 scroll)
            {
                float2 p = float2(uv.x * cells + _Time.y * scroll.x, (uv.y - _Time.y * scroll.y) * rows);
                return BeltPeriodicNoise(p, rows);
            }

            fixed4 frag(Varyings input) : SV_Target
            {
                float web = Web(input.uv);
                float soft = lerp(1.0, ScrollingNoise(input.uv, SoftenCells, SoftenRows, _DissolveScroll.xy), _Softness);
                float mask = smoothstep(MaskLow, MaskHigh, ScrollingNoise(input.uv, MaskCells, MaskRows, _MaskScroll.xy));
                return fixed4(_Color.rgb * (_Color.a * web * soft * mask), 1);
            }
            ENDCG
        }
    }
}
