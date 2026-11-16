// Channel wall for the belt (Built-in RP, unlit, opaque). Fakes a soft lit volume without scene lights.
// Mesh contract: smooth object-space normals; uv2.x is the height inside the channel profile
// (0 = floor, 1 = wall crest), see BeltSweepMeshBuilder.
Shader "BeltPath/Channel Wall"
{
    Properties
    {
        _ShadeColor ("Shade color (facing away from light)", Color) = (0.12, 0.22, 0.5, 1)
        _BaseColor ("Base color (side-on)", Color) = (0.2, 0.37, 0.74, 1)
        _HighlightColor ("Highlight color (facing the light)", Color) = (0.33, 0.55, 0.9, 1)
        _CrestColor ("Crest color (top ridge)", Color) = (0.6, 0.8, 1, 1)
        _OcclusionColor ("Occlusion color (wall feet)", Color) = (0.07, 0.14, 0.36, 1)
        _FloorColor ("Floor color", Color) = (0.12, 0.35, 0.72, 1)

        _LightDir ("Light direction (object space)", Vector) = (-0.35, 0.8, -0.5, 0)
        _RampSoftness ("Light ramp softness", Range(0.1, 1)) = 0.6
        _CrestPower ("Crest tightness", Range(1, 16)) = 5
        _CrestStrength ("Crest strength", Range(0, 1)) = 0.55
        _OcclusionHeight ("Occlusion height", Range(0.01, 1)) = 0.4
        _OcclusionStrength ("Occlusion strength", Range(0, 1)) = 0.8
        _FloorHeight ("Floor band height", Range(0.01, 0.5)) = 0.06
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "Queue" = "Geometry" }

        Pass
        {
            Cull Back
            ZWrite On

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            fixed4 _ShadeColor, _BaseColor, _HighlightColor, _CrestColor, _OcclusionColor, _FloorColor;
            float4 _LightDir;
            half _RampSoftness, _CrestPower, _CrestStrength;
            half _OcclusionHeight, _OcclusionStrength, _FloorHeight;

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float2 uv2 : TEXCOORD1;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 normal : TEXCOORD0;
                half height : TEXCOORD1;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.normal = v.normal;
                o.height = v.uv2.x;
                return o;
            }

            // Three-tone ramp: shade -> base -> highlight, eased so the steps read as a smooth volume.
            fixed3 LightRamp(float3 n)
            {
                half facing = dot(n, normalize(_LightDir.xyz));
                half s = smoothstep(0, 1, saturate(facing / (2 * _RampSoftness) + 0.5));
                fixed3 col = lerp(_ShadeColor.rgb, _BaseColor.rgb, saturate(s * 2));
                return lerp(col, _HighlightColor.rgb, saturate(s * 2 - 1));
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float3 n = normalize(i.normal);
                fixed3 col = LightRamp(n);

                half crest = pow(saturate(n.y), _CrestPower) * _CrestStrength;
                col = lerp(col, _CrestColor.rgb, crest);

                half foot = 1 - smoothstep(0, _OcclusionHeight, i.height);
                col = lerp(col, _OcclusionColor.rgb, foot * _OcclusionStrength);

                half floorMask = (1 - smoothstep(0, _FloorHeight, i.height)) * saturate(n.y);
                col = lerp(col, _FloorColor.rgb, floorMask);

                return fixed4(col, 1);
            }
            ENDCG
        }
    }
}
