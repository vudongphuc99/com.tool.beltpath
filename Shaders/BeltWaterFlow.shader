// Flowing water surface for the belt channel (Built-in RP, unlit).
// Mesh contract: uv.x runs 0..1 across the channel, uv.y is the distance along the belt in UV units.
// Closed belts end on a whole number of UV units, so the noise (period of one UV unit) has no seam.
Shader "BeltPath/Water Flow"
{
    Properties
    {
        [Header(Opacity)]
        _Color ("Tint (alpha = opacity)", Color) = (1, 1, 1, 0.7)

        [Space(8)]
        [Header(Base Colors)]
        _WaterOuterColor ("Outer color (channel sides)", Color) = (0, 0.652, 0.957, 1)
        _WaterInnerColor ("Inner color (middle)", Color) = (0, 0.776, 0.98, 1)
        _WaterCenterColor ("Center color (bright band)", Color) = (0.063, 0.828, 1, 1)

        [Space(8)]
        [Header(Center Bright Band)]
        _CenterBright ("Strength", Range(0, 1)) = 0.8
        _CenterBrightPosition ("Position", Range(0, 1)) = 0.5
        _CenterBrightWidth ("Width", Range(0.1, 1)) = 0.6

        [Space(8)]
        [Header(Edge Darkening)]
        _EdgeDark ("Strength", Range(0, 1)) = 0.29
        _EdgeDarkWidth ("Width", Range(0.01, 0.45)) = 0.127

        [Space(8)]
        [Header(Flow Pattern   moving light and dark patches)]
        _FlowSpeed ("Speed", Range(0, 2)) = 0.105
        _PathTiling ("Repeats per UV unit", Range(1, 12)) = 2
        _FlowContrast ("Contrast", Range(0, 1)) = 0.35
        _DistortStrength ("Distortion", Range(0, 0.12)) = 0.079

        [Space(8)]
        [Header(Foam and Current Motion   shared by Edge Foam and Inner Current)]
        _FoamSpeed ("Speed", Range(0, 3)) = 0.26
        _FoamTiling ("Repeats per UV unit", Range(1, 16)) = 5

        [Space(8)]
        [Header(Edge Foam)]
        _FoamWidth ("Width", Range(0.01, 0.48)) = 0.063
        _FoamSoftness ("Softness", Range(0.001, 0.3)) = 0.061
        _FoamStrength ("Strength", Range(0, 1)) = 0.546

        [Space(8)]
        [Header(Inner Current)]
        _InnerCurrentStrength ("Strength", Range(0, 1)) = 0

        [Space(8)]
        [Header(Flow Arrows)]
        _ArrowStrength ("Strength (0 = off)", Range(0, 1)) = 0
        _ArrowSpeed ("Speed", Range(0, 3)) = 0.62
        _ArrowTiling ("Arrows per UV unit", Range(1, 16)) = 3
        _ArrowWidth ("Thickness", Range(0.05, 0.5)) = 0.275
        _ArrowSoftness ("Softness", Range(0.001, 0.3)) = 0.137
    }

    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" "IgnoreProjector" = "True" }

        Pass
        {
            ZWrite Off
            Cull Off
            Blend SrcAlpha OneMinusSrcAlpha

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            #include "BeltWaterNoise.cginc"

            fixed4 _Color, _WaterOuterColor, _WaterInnerColor, _WaterCenterColor;
            half _CenterBright, _CenterBrightPosition, _CenterBrightWidth, _EdgeDark, _EdgeDarkWidth;
            half _FlowSpeed, _PathTiling, _FlowContrast, _DistortStrength;
            half _FoamSpeed, _FoamTiling, _FoamWidth, _FoamSoftness, _FoamStrength, _InnerCurrentStrength;
            half _ArrowStrength, _ArrowSpeed, _ArrowTiling, _ArrowWidth, _ArrowSoftness;

            // Across-channel noise lattice size; along the belt the lattice size is the tiling property.
            static const float AcrossCells = 3.0;
            static const float FoamAcrossCells = 6.0;
            static const float DistortAcrossCells = 4.0;
            static const float DistortAlongCells = 3.0;
            static const float FoamEdgeJitter = 0.05;
            static const float ArrowTipSlope = 0.5;

            struct Attributes { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; };

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.pos = UnityObjectToClipPos(input.vertex);
                output.uv = input.uv;
                return output;
            }

            // Noise whose lattice has `cellsAlong` rows per UV unit, scrolling toward +v at `speed`.
            float FlowNoise(float2 uv, float cellsAcross, float cellsAlong, float speed, float acrossOffset)
            {
                float rows = max(1.0, round(cellsAlong));
                float2 p = float2(uv.x * cellsAcross + acrossOffset, (uv.y - _Time.y * speed) * rows);
                return BeltPeriodicNoise(p, rows);
            }

            // Horizontal gradient: dark sides, base middle, bright center band.
            fixed3 ChannelGradient(float across)
            {
                half edgeDistance = min(across, 1.0 - across);
                fixed3 color = lerp(_WaterOuterColor.rgb, _WaterInnerColor.rgb, smoothstep(0.0, 0.5, edgeDistance));
                half band = 1.0 - smoothstep(0.0, _CenterBrightWidth * 0.5, abs(across - _CenterBrightPosition));
                color = lerp(color, _WaterCenterColor.rgb, band * _CenterBright);
                half edgeMask = 1.0 - smoothstep(0.0, _EdgeDarkWidth, edgeDistance);
                return color * (1.0 - edgeMask * _EdgeDark);
            }

            // Moving light and dark patches, warped by a second slower noise.
            half FlowShade(float2 uv)
            {
                float warp = (FlowNoise(uv, DistortAcrossCells, DistortAlongCells, _FlowSpeed * 0.5, 7.0) - 0.5) * 2.0;
                float2 warped = float2(uv.x + warp * _DistortStrength, uv.y + warp * _DistortStrength);
                half pattern = FlowNoise(warped, AcrossCells, _PathTiling, _FlowSpeed, 0.0);
                return 1.0 + (pattern - 0.5) * 2.0 * _FlowContrast;
            }

            // Bright rim along both banks, broken up by the shared foam noise (`jitter`, 0..1).
            half EdgeFoam(float2 uv, half jitter)
            {
                half edgeDistance = min(uv.x, 1.0 - uv.x);
                half shifted = edgeDistance + (jitter - 0.5) * FoamEdgeJitter;
                return (1.0 - smoothstep(_FoamWidth, _FoamWidth + _FoamSoftness, shifted)) * (0.5 + jitter);
            }

            // Chevron pointing toward +v (the flow direction), repeated along the belt.
            half FlowArrow(float2 uv)
            {
                float2 local = float2((uv.x - 0.5) * 2.0, frac(uv.y * round(_ArrowTiling) - _Time.y * _ArrowSpeed) - 0.5);
                half offset = abs(local.y + abs(local.x) * ArrowTipSlope);
                half thickness = _ArrowWidth * 0.4;
                return 1.0 - smoothstep(thickness, thickness + _ArrowSoftness, offset);
            }

            fixed4 frag(Varyings input) : SV_Target
            {
                fixed3 color = ChannelGradient(input.uv.x) * FlowShade(input.uv);
                // One noise shared by the inner current and the edge foam jitter.
                half foamNoise = FlowNoise(input.uv, FoamAcrossCells, _FoamTiling, _FoamSpeed, 13.0);
                color *= 1.0 - foamNoise * _InnerCurrentStrength;
                color = lerp(color, fixed3(1, 1, 1), saturate(EdgeFoam(input.uv, foamNoise)) * _FoamStrength);
                color = lerp(color, fixed3(1, 1, 1), FlowArrow(input.uv) * _ArrowStrength);
                return fixed4(color * _Color.rgb, _Color.a);
            }
            ENDCG
        }
    }
}
