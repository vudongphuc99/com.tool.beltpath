// Flowing water surface for the belt channel (Built-in RP, unlit, opaque).
// Mesh contract: uv.x runs 0..1 across the channel, uv.y is the distance along the belt in UV units.
// Closed belts end on a whole number of UV units, so the noise (period of one UV unit) has no seam.
// Every effect has an "Enabled" toggle. A toggle that is switched off removes its code from the shader variant,
// so it costs nothing. All toggles are on by default (a material without the toggle keywords keeps every effect).
Shader "BeltPath/Water Flow"
{
    Properties
    {
        [Header(Tint)]
        _Color ("Tint", Color) = (1, 1, 1, 1)

        [Space(8)]
        [Header(Base Colors)]
        _WaterOuterColor ("Outer color (channel sides)", Color) = (0, 0.652, 0.957, 1)
        _WaterInnerColor ("Inner color (middle)", Color) = (0, 0.776, 0.98, 1)
        _WaterCenterColor ("Center color (bright band)", Color) = (0.063, 0.828, 1, 1)

        [Space(8)]
        [Header(Center Bright Band)]
        [ToggleOff(_CENTER_BAND_OFF)] _CenterBandOn ("Enabled", Float) = 1
        _CenterBright ("Strength", Range(0, 1)) = 0.8
        _CenterBrightPosition ("Position", Range(0, 1)) = 0.5
        _CenterBrightWidth ("Width", Range(0.1, 1)) = 0.6

        [Space(8)]
        [Header(Edge Darkening)]
        [ToggleOff(_EDGE_DARK_OFF)] _EdgeDarkOn ("Enabled", Float) = 1
        _EdgeDark ("Strength", Range(0, 1)) = 0.29
        _EdgeDarkWidth ("Width", Range(0.01, 0.45)) = 0.127

        [Space(8)]
        [Header(Flow Pattern   moving light and dark patches)]
        [ToggleOff(_FLOW_PATTERN_OFF)] _FlowPatternOn ("Enabled", Float) = 1
        _FlowSpeed ("Speed", Range(0, 2)) = 0.105
        _PathTiling ("Repeats per UV unit", Range(1, 12)) = 2
        _FlowContrast ("Contrast", Range(0, 1)) = 0.35

        [Space(8)]
        [Header(Flow Distortion   warps the flow pattern)]
        [ToggleOff(_FLOW_DISTORT_OFF)] _FlowDistortOn ("Enabled", Float) = 1
        _DistortStrength ("Distortion", Range(0, 0.12)) = 0.079

        [Space(8)]
        [Header(Foam and Current Motion   shared by Edge Foam and Inner Current)]
        _FoamSpeed ("Speed", Range(0, 3)) = 0.26
        _FoamTiling ("Repeats per UV unit", Range(1, 16)) = 5

        [Space(8)]
        [Header(Edge Foam)]
        [ToggleOff(_EDGE_FOAM_OFF)] _EdgeFoamOn ("Enabled", Float) = 1
        _FoamWidth ("Width", Range(0.01, 0.48)) = 0.063
        _FoamSoftness ("Softness", Range(0.001, 0.3)) = 0.061
        _FoamStrength ("Strength", Range(0, 1)) = 0.546

        [Space(8)]
        [Header(Inner Current)]
        [ToggleOff(_INNER_CURRENT_OFF)] _InnerCurrentOn ("Enabled", Float) = 1
        _InnerCurrentStrength ("Strength", Range(0, 1)) = 0

        [Space(8)]
        [Header(Flow Arrows)]
        [ToggleOff(_FLOW_ARROWS_OFF)] _FlowArrowsOn ("Enabled", Float) = 1
        _ArrowStrength ("Strength", Range(0, 1)) = 0
        _ArrowSpeed ("Speed", Range(0, 3)) = 0.62
        _ArrowTiling ("Arrows per UV unit", Range(1, 16)) = 3
        _ArrowWidth ("Thickness", Range(0.05, 0.5)) = 0.275
        _ArrowSoftness ("Softness", Range(0.001, 0.3)) = 0.137
    }

    SubShader
    {
        Tags { "Queue" = "Geometry" "RenderType" = "Opaque" }

        Pass
        {
            ZWrite On
            Cull Off

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // Both stages read these keywords (the vertex shader builds the noise coordinates).
            #pragma shader_feature_local _CENTER_BAND_OFF
            #pragma shader_feature_local _EDGE_DARK_OFF
            #pragma shader_feature_local _FLOW_PATTERN_OFF
            #pragma shader_feature_local _FLOW_DISTORT_OFF
            #pragma shader_feature_local _EDGE_FOAM_OFF
            #pragma shader_feature_local _INNER_CURRENT_OFF
            #pragma shader_feature_local _FLOW_ARROWS_OFF
            #include "UnityCG.cginc"
            #include "BeltWaterNoise.cginc"

            // Effect switches resolved once, so the code below reads as "if the effect is on".
            #if !defined(_CENTER_BAND_OFF)
            #define BELT_CENTER_BAND 1
            #endif
            #if !defined(_EDGE_DARK_OFF)
            #define BELT_EDGE_DARK 1
            #endif
            #if !defined(_FLOW_PATTERN_OFF)
            #define BELT_FLOW_PATTERN 1
            #endif
            #if defined(BELT_FLOW_PATTERN) && !defined(_FLOW_DISTORT_OFF)
            #define BELT_FLOW_DISTORT 1
            #endif
            #if !defined(_EDGE_FOAM_OFF)
            #define BELT_EDGE_FOAM 1
            #endif
            #if !defined(_INNER_CURRENT_OFF)
            #define BELT_INNER_CURRENT 1
            #endif
            #if !defined(_FLOW_ARROWS_OFF)
            #define BELT_FLOW_ARROWS 1
            #endif
            // The foam noise feeds both the edge foam jitter and the inner current.
            #if defined(BELT_EDGE_FOAM) || defined(BELT_INNER_CURRENT)
            #define BELT_FOAM_NOISE 1
            #endif

            fixed4 _Color, _WaterOuterColor, _WaterInnerColor, _WaterCenterColor;
            half _CenterBright, _CenterBrightPosition, _CenterBrightWidth, _EdgeDark, _EdgeDarkWidth;
            half _FlowSpeed, _PathTiling, _FlowContrast, _DistortStrength;
            half _FoamSpeed, _FoamTiling, _FoamWidth, _FoamSoftness, _FoamStrength, _InnerCurrentStrength;
            half _ArrowStrength, _ArrowSpeed, _ArrowTiling, _ArrowWidth, _ArrowSoftness;

            // Across-channel noise lattice size; along the belt the lattice size is the tiling property.
            static const float AcrossCells = 3.0;
            static const float FoamAcrossCells = 6.0;
            static const float DistortAcrossCells = 4.0;
            static const float DistortRows = 3.0;
            static const float DistortAcrossOffset = 7.0;
            static const float FoamAcrossOffset = 13.0;
            static const float FoamEdgeJitter = 0.05;
            static const float ArrowTipSlope = 0.5;

            struct Attributes { float4 vertex : POSITION; float2 uv : TEXCOORD0; };

            // The noise coordinates are straight lines in uv, so the vertex shader builds them and the rasterizer
            // interpolates them. The `.z` of a three-component coordinate is the lattice row count (its period).
            struct Varyings
            {
                float4 pos : SV_POSITION;
                float across : TEXCOORD0;
            #if defined(BELT_FLOW_PATTERN)
                float3 patternCoord : TEXCOORD1;
            #endif
            #if defined(BELT_FLOW_DISTORT)
                float2 warpCoord : TEXCOORD2;
            #endif
            #if defined(BELT_FOAM_NOISE)
                float3 foamCoord : TEXCOORD3;
            #endif
            #if defined(BELT_FLOW_ARROWS)
                float arrowPhase : TEXCOORD4;
            #endif
            };

            float RowCount(float cellsAlong)
            {
                return max(1.0, round(cellsAlong));
            }

            // Lattice coordinate of a noise scrolling toward +v at `speed`.
            float2 LatticeCoord(float2 uv, float cellsAcross, float rows, float speed, float acrossOffset)
            {
                return float2(uv.x * cellsAcross + acrossOffset, (uv.y - _Time.y * speed) * rows);
            }

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.pos = UnityObjectToClipPos(input.vertex);
                output.across = input.uv.x;
            #if defined(BELT_FLOW_PATTERN)
                float patternRows = RowCount(_PathTiling);
                output.patternCoord = float3(LatticeCoord(input.uv, AcrossCells, patternRows, _FlowSpeed, 0.0), patternRows);
            #endif
            #if defined(BELT_FLOW_DISTORT)
                output.warpCoord = LatticeCoord(input.uv, DistortAcrossCells, DistortRows, _FlowSpeed * 0.5, DistortAcrossOffset);
            #endif
            #if defined(BELT_FOAM_NOISE)
                float foamRows = RowCount(_FoamTiling);
                output.foamCoord = float3(LatticeCoord(input.uv, FoamAcrossCells, foamRows, _FoamSpeed, FoamAcrossOffset), foamRows);
            #endif
            #if defined(BELT_FLOW_ARROWS)
                output.arrowPhase = input.uv.y * round(_ArrowTiling) - _Time.y * _ArrowSpeed;
            #endif
                return output;
            }

            // Horizontal gradient: dark sides, base middle, bright center band.
            fixed3 ChannelGradient(float across)
            {
                half edgeDistance = min(across, 1.0 - across);
                fixed3 color = lerp(_WaterOuterColor.rgb, _WaterInnerColor.rgb, smoothstep(0.0, 0.5, edgeDistance));
            #if defined(BELT_CENTER_BAND)
                half band = 1.0 - smoothstep(0.0, _CenterBrightWidth * 0.5, abs(across - _CenterBrightPosition));
                color = lerp(color, _WaterCenterColor.rgb, band * _CenterBright);
            #endif
            #if defined(BELT_EDGE_DARK)
                half edgeMask = 1.0 - smoothstep(0.0, _EdgeDarkWidth, edgeDistance);
                color *= 1.0 - edgeMask * _EdgeDark;
            #endif
                return color;
            }

        #if defined(BELT_FLOW_PATTERN)
            // Moving light and dark patches, warped by a second slower noise.
            half FlowShade(Varyings input)
            {
                float2 p = input.patternCoord.xy;
            #if defined(BELT_FLOW_DISTORT)
                float warp = (BeltPeriodicNoise(input.warpCoord, DistortRows) - 0.5) * 2.0;
                p += warp * _DistortStrength * float2(AcrossCells, input.patternCoord.z);
            #endif
                half pattern = BeltPeriodicNoise(p, input.patternCoord.z);
                return 1.0 + (pattern - 0.5) * 2.0 * _FlowContrast;
            }
        #endif

        #if defined(BELT_EDGE_FOAM)
            // Bright rim along both banks, broken up by the shared foam noise (`jitter`, 0..1).
            half EdgeFoam(float across, half jitter)
            {
                half edgeDistance = min(across, 1.0 - across);
                half shifted = edgeDistance + (jitter - 0.5) * FoamEdgeJitter;
                return (1.0 - smoothstep(_FoamWidth, _FoamWidth + _FoamSoftness, shifted)) * (0.5 + jitter);
            }
        #endif

        #if defined(BELT_FLOW_ARROWS)
            // Chevron pointing toward +v (the flow direction), repeated along the belt.
            half FlowArrow(float across, float phase)
            {
                float2 local = float2((across - 0.5) * 2.0, frac(phase) - 0.5);
                half offset = abs(local.y + abs(local.x) * ArrowTipSlope);
                half thickness = _ArrowWidth * 0.4;
                return 1.0 - smoothstep(thickness, thickness + _ArrowSoftness, offset);
            }
        #endif

            fixed4 frag(Varyings input) : SV_Target
            {
                fixed3 color = ChannelGradient(input.across);
            #if defined(BELT_FLOW_PATTERN)
                color *= FlowShade(input);
            #endif
            #if defined(BELT_FOAM_NOISE)
                half foamNoise = BeltPeriodicNoise(input.foamCoord.xy, input.foamCoord.z);
            #endif
            #if defined(BELT_INNER_CURRENT)
                color *= 1.0 - foamNoise * _InnerCurrentStrength;
            #endif
            #if defined(BELT_EDGE_FOAM)
                color = lerp(color, fixed3(1, 1, 1), saturate(EdgeFoam(input.across, foamNoise)) * _FoamStrength);
            #endif
            #if defined(BELT_FLOW_ARROWS)
                color = lerp(color, fixed3(1, 1, 1), FlowArrow(input.across, input.arrowPhase) * _ArrowStrength);
            #endif
                return fixed4(color * _Color.rgb, 1);
            }
            ENDCG
        }
    }
}
