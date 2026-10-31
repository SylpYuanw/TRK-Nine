#version 450
layout(binding = 1, set = 2)
uniform sampler2D _MainTex; // @default = black
layout(binding = 3, set = 2)
uniform sampler2D _StartTex; // @default = gray
layout(binding = 0, set = 2, std140) uniform block_RU_MaterialData_0
{
    float _BodyAimSide; // @default = 1
    float _Brightness; // @default = 1
    float _CanvasAspect; // @default = 1
    vec4 _Color; // @default = vec4(1, 1, 1, 1) @color
    float _Curl; // @default = 0.9
    vec4 _DeepColor; // @default = vec4(0.065, 0.002, 0.009, 1) @color
    float _DissolveNodes; // @default = 6
    float _DissolveSpeed; // @default = 1.1
    float _Erosion; // @default = 0.65
    float _FadeStart; // @default = 0.12
    vec4 _FlameColor; // @default = vec4(0.38, 0.012, 0.035, 1) @color
    float _FlowAngle; // @default = 45
    float _FlowLength; // @default = 0.68
    float _FlowSeed; // @default = 0
    float _FlowSpeed; // @default = 1.25
    float _FlowWidth; // @default = 0.17
    float _Glow; // @default = 0.12
    float _Heat; // @default = 1.15
    vec4 _HotColor; // @default = vec4(0.82, 0.065, 0.085, 1) @color
    float _NoiseScale; // @default = 1.4
    float _PreviewClockEnabled; // @default = 0
    float _PreviewNodeMode; // @default = 0
    float _PreviewSeconds; // @default = 0
    float _RibbonCount; // @default = 3
    float _RibbonWidth; // @default = 0.018
    float _SourceU; // @default = 0.66
    float _SourceV; // @default = 0.3
    float _StartFeather; // @default = 0.22
    vec4 _Time;
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


#line 168
layout(location = 1)
out vec2 entryPointParam_vert_uv_0;


#line 168
layout(location = 0) in vec4 v_vertex_0; // @vertex Position required


#line 168
layout(location = 1) in vec2 v_uv_0; // @vertex TexCoord0 optional


#line 168
struct v2f_0
{
    vec4 pos_0;
    vec2 uv_0;
};


#line 168
void main()
{

#line 168
    v2f_0 o_0;

#line 168
    o_0.pos_0 = UnityObjectToClipPos_0(v_vertex_0);

#line 168
    o_0.uv_0 = v_uv_0;

#line 168
    v2f_0 _S1 = o_0;

#line 168
    gl_Position = o_0.pos_0;

#line 168
    entryPointParam_vert_uv_0 = _S1.uv_0;

#line 168
    return;
}


// @rimunity-host-matrices 1
