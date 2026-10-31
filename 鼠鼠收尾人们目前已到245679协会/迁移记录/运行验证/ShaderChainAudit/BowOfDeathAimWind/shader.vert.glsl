#version 450
layout(binding = 1, set = 2)
uniform sampler2D _MainTex; // @default = white
layout(binding = 0, set = 2, std140) uniform block_RU_MaterialData_0
{
    vec4 _Color; // @default = vec4(1, 1, 1, 1) @color
    float _EmitPeriod; // @default = 2.6
    vec4 _HeadColor; // @default = vec4(0.9, 0.09, 0.07, 1) @color
    float _HeadGlow; // @default = 0.12
    float _ParticleCount; // @default = 6
    float _PreviewClockEnabled; // @default = 0
    float _PreviewSeconds; // @default = 0
    float _SmokeCut; // @default = 0.12
    float _SmokeScroll; // @default = 0.12
    float _SmokeSoft; // @default = 0.16
    float _SmokeStrength; // @default = 0.8
    float _SmokeWarp; // @default = 0
    float _SpeedCurve; // @default = 0.45
    float _StreakAngle; // @default = 20
    float _StreakBrightness; // @default = 0.42
    vec4 _StreakColor; // @default = vec4(0.62, 0.035, 0.03, 1) @color
    float _StreakLength; // @default = 0.42
    float _StreakWidth; // @default = 0.085
    vec4 _Time;
    float _TravelDistance; // @default = 0.6
    float _Turbulence; // @default = 0.09
    float _TurbulenceSpeed; // @default = 0.35
    float _UseRealTime; // @default = 1
}RU_Material_0;


#line 15 0
struct RU_CameraData_0
{
    mat4 ViewProj_0;
};


#line 16
layout(binding = 0, std140) uniform block_RU_CameraData_0
{
    mat4 ViewProj_0;
}RU_Camera_0;

#line 17
struct RU_ObjectData_0
{
    mat4 model_0;
};


#line 18
layout(push_constant, std430) uniform block_RU_ObjectData_0
{
    mat4 model_0;
}RU_Object_0;
vec4 UnityObjectToClipPos_0(vec4 p_0)
{

#line 23
    return ((((((p_0) * (transpose(RU_Object_0.model_0))))) * (transpose(RU_Camera_0.ViewProj_0))));
}


#line 3338 1
layout(location = 1)
out vec2 entryPointParam_vert_uv_0;


#line 3338
layout(location = 0) in vec4 v_vertex_0; // @vertex Position required


#line 3338
layout(location = 1) in vec2 v_uv_0; // @vertex TexCoord0 optional


#line 135 0
struct v2f_0
{
    vec4 pos_0;
    vec2 uv_0;
};


#line 135
void main()
{

#line 135
    v2f_0 o_0;

#line 135
    o_0.pos_0 = UnityObjectToClipPos_0(v_vertex_0);

#line 135
    o_0.uv_0 = v_uv_0;

#line 135
    v2f_0 _S1 = o_0;

#line 135
    gl_Position = o_0.pos_0;

#line 135
    entryPointParam_vert_uv_0 = _S1.uv_0;

#line 135
    return;
}


// @rimunity-host-matrices 1
