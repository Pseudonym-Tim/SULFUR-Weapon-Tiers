Shader "WeaponTiers/WorldGlint"
{
    Properties
    {
        _MainTex ("Shape mask", 2D) = "white" {}
        _TierColor ("Tier color", Color) = (1,1,1,1)
        _Strength ("Strength", Range(0,1)) = 0.39
        _Speed ("Speed", Float) = 0.1
        _UseAlpha ("Use texture alpha", Float) = 1
        _AlphaClipThreshold ("Shape cutoff", Float) = 0.01
        _Billboard ("Face camera", Float) = 0
        _HeldGlint ("Use held screen sweep", Float) = 0
        [Enum(UnityEngine.Rendering.CullMode)] _Cull ("Cull", Float) = 2
    }

    SubShader
    {
        Tags { "Queue"="Transparent+10" "RenderType"="Transparent" "IgnoreProjector"="True" }

        Pass
        {
            Tags { "LightMode"="SRPDefaultUnlit" }
            Blend SrcAlpha One
            ZWrite Off
            ZTest LEqual
            Cull [_Cull]
            Offset -1, -1
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "UnityCG.cginc"
            #include "EnchantmentGlint.cginc"

            struct Input
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Output
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _TierColor;
            float _Strength, _Speed, _UseAlpha, _AlphaClipThreshold, _Billboard, _HeldGlint;
            float _SwtGlintTime;

            Output vert(Input v)
            {
                Output o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                float3 position = v.vertex.xyz;

                if(_Billboard > 0.5)
                {
                    // The pickup turns in its shader, so the overlay has to turn the same way, duh...
                    float3 origin = unity_ObjectToWorld._m03_m13_m23;
                    float3 scale = float3(length(unity_ObjectToWorld._m00_m10_m20), length(unity_ObjectToWorld._m01_m11_m21), length(unity_ObjectToWorld._m02_m12_m22));
                    float3 forward = normalize(origin - _WorldSpaceCameraPos);
                    float3 up = normalize(unity_ObjectToWorld._m01_m11_m21);
                    float3 right = cross(up, forward);
                    up = cross(forward, right);
                    right = normalize(mul((float3x3)unity_WorldToObject, right));
                    up = normalize(mul((float3x3)unity_WorldToObject, up));
                    forward = normalize(mul((float3x3)unity_WorldToObject, forward));
                    float3 scaled = position * scale;
                    position = (right * scaled.x + up * scaled.y + forward * scaled.z) / max(scale, 0.00001);
                }

                float4 worldPosition = mul(unity_ObjectToWorld, float4(position, 1.0));
                o.vertex = mul(UNITY_MATRIX_VP, worldPosition);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            float4 frag(Output i) : SV_Target
            {
                float mask = lerp(1.0, tex2D(_MainTex, i.uv).a, _UseAlpha);
                clip(mask - _AlphaClipThreshold);
                float phase = _SwtGlintTime * _Speed;
                float shimmer = EnchantmentGlint(i.uv, phase, 0.16);

                if(_HeldGlint > 0.5)
                {
                    // Screen pixels keep skinning and tiny imported meshes from collapsing sweep...
                    float2 screen = i.vertex.xy / _ScreenParams.y;
                    shimmer = EnchantmentGlint(screen, phase, 0.016);
                }

                return float4(_TierColor.rgb, mask * shimmer * _Strength);
            }

            ENDHLSL
        }
    }
}
