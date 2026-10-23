Shader "Masu/UI/MandalaCameraOrbit"
{
    Properties
    {
        [PerRendererData] _MainTex ("Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _YawDeg ("Yaw Degrees", Float) = 0
        _PitchDeg ("Pitch Degrees", Float) = 0
        _Focal ("Perspective Focal", Float) = 3.2
        _Pivot ("Pivot UV", Vector) = (0.5, 0.5, 0, 0)
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "True"
        }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            struct appdata_t
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float2 texcoord : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            fixed4 _Color;
            float4 _MainTex_ST;
            float _YawDeg;
            float _PitchDeg;
            float _Focal;
            float4 _Pivot;
            float4 _ClipRect;

            float3 RotateYawPitch(float3 p, float yaw, float pitch)
            {
                float cy = cos(yaw);
                float sy = sin(yaw);
                float3 py = float3(
                    p.x * cy + p.z * sy,
                    p.y,
                    -p.x * sy + p.z * cy);

                float cp = cos(pitch);
                float sp = sin(pitch);
                return float3(
                    py.x,
                    py.y * cp - py.z * sp,
                    py.y * sp + py.z * cp);
            }

            v2f vert(appdata_t v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                float yaw = _YawDeg * 0.0174532925;
                float pitch = _PitchDeg * 0.0174532925;
                float2 uv = v.texcoord;
                float2 centered = float2(uv.x - _Pivot.x, uv.y - _Pivot.y);
                float3 p = float3(centered.x, centered.y, 0);
                float3 r = RotateYawPitch(p, yaw, pitch);
                float persp = _Focal / (_Focal + r.z);
                float2 warped = float2(r.x, r.y) * persp + _Pivot.xy;

                o.worldPosition = v.vertex;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.texcoord = warped;
                o.color = v.color * _Color;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                return tex2D(_MainTex, i.texcoord) * i.color;
            }
            ENDCG
        }
    }
}
