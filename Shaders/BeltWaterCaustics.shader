// Additive light web drawn over the water surface (Built-in RP, unlit). Same mesh contract as Water Flow:
// uv.x across the channel, uv.y along the belt in UV units, a closed belt ends on a whole UV unit.
// The softening and mask noises have an "Enabled" toggle; a toggle that is switched off removes its code from the
// shader variant. Both are on by default (a material without the toggle keywords keeps both).
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
        [ToggleOff(_CAUSTIC_SOFTEN_OFF)] _SoftenOn ("Enabled", Float) = 1
        _Softness ("Amount", Range(0, 1)) = 1
        _DissolveScroll ("Scroll (across, along)", Vector) = (-0.1, -0.2, 0, 0)

        [Space(8)]
        [Header(Mask Noise)]
        [ToggleOff(_CAUSTIC_MASK_OFF)] _MaskOn ("Enabled", Float) = 1
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
            // Both stages read these keywords (the vertex shader builds the noise coordinates).
            #pragma shader_feature_local _CAUSTIC_SOFTEN_OFF
            #pragma shader_feature_local _CAUSTIC_MASK_OFF
            #include "UnityCG.cginc"
            #include "BeltWaterNoise.cginc"

            #if !defined(_CAUSTIC_SOFTEN_OFF)
            #define BELT_CAUSTIC_SOFTEN 1
            #endif
            #if !defined(_CAUSTIC_MASK_OFF)
            #define BELT_CAUSTIC_MASK 1
            #endif

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

            // The noise coordinates are straight lines in uv, so the vertex shader builds them and the rasterizer
            // interpolates them. The `.z` of the web coordinate is its lattice row count (its period).
            struct Varyings
            {
                float4 pos : SV_POSITION;
                float3 webCoord : TEXCOORD0;
            #if defined(BELT_CAUSTIC_SOFTEN)
                float2 softenCoord : TEXCOORD1;
            #endif
            #if defined(BELT_CAUSTIC_MASK)
                float2 maskCoord : TEXCOORD2;
            #endif
            };

            // Lattice coordinate of a noise scrolling by `scroll` (across, along) per second.
            float2 ScrollingCoord(float2 uv, float cells, float rows, float2 scroll)
            {
                return float2(uv.x * cells + _Time.y * scroll.x, (uv.y - _Time.y * scroll.y) * rows);
            }

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.pos = UnityObjectToClipPos(input.vertex);
                // The cell count along the belt shrinks as the stretch grows.
                float webRows = max(1.0, round(_CausticTiling.y / _PathStretch));
                output.webCoord = float3(ScrollingCoord(input.uv, _CausticTiling.x, webRows, _WebScroll.xy), webRows);
            #if defined(BELT_CAUSTIC_SOFTEN)
                output.softenCoord = ScrollingCoord(input.uv, SoftenCells, SoftenRows, _DissolveScroll.xy);
            #endif
            #if defined(BELT_CAUSTIC_MASK)
                output.maskCoord = ScrollingCoord(input.uv, MaskCells, MaskRows, _MaskScroll.xy);
            #endif
                return output;
            }

            fixed4 frag(Varyings input) : SV_Target
            {
                // Bright web of cell borders.
                half intensity = 1.0 - smoothstep(0.0, _WebWidth, BeltPeriodicWeb(input.webCoord.xy, input.webCoord.z));
            #if defined(BELT_CAUSTIC_SOFTEN)
                intensity *= lerp(1.0, BeltPeriodicNoise(input.softenCoord, SoftenRows), _Softness);
            #endif
            #if defined(BELT_CAUSTIC_MASK)
                intensity *= smoothstep(MaskLow, MaskHigh, BeltPeriodicNoise(input.maskCoord, MaskRows));
            #endif
                return fixed4(_Color.rgb * (_Color.a * intensity), 1);
            }
            ENDCG
        }
    }
}
