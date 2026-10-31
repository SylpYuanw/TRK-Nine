#version 450
layout(binding = 1, set = 2)
uniform sampler2D _MainTex; // @default = black
layout(binding = 3, set = 2)
uniform sampler2D _StartTex; // @default = gray
layout(binding = 0, set = 2, std140) uniform block_RU_MaterialData_0
{
    float _AngleOffset; // @default = 10
    float _Brightness; // @default = 0.34
    float _CanvasAspect; // @default = 1
    vec4 _Color; // @default = vec4(1, 1, 1, 1) @color
    float _CoreBrightness; // @default = 0
    float _CoreRadius; // @default = 0.1
    vec4 _DeepColor; // @default = vec4(0.1, 0.002, 0.009, 1) @color
    float _DissolveNodes; // @default = 6
    float _DissolveSpeed; // @default = 0.75
    float _Erosion; // @default = 0.45
    float _FadeStart; // @default = 0.2
    float _FlowLength; // @default = 0.42
    float _FlowSeed; // @default = 0
    float _FlowWidth; // @default = 0.2
    float _GroupSeparation; // @default = 0
    float _HeadU; // @default = 0.79
    float _HeadV; // @default = 0.5
    vec4 _HotColor; // @default = vec4(0.8, 0.035, 0.055, 1) @color
    float _InwardCurve; // @default = 0.025
    float _LayerCount; // @default = 3
    float _NoiseScale; // @default = 2.5
    float _NoiseStrength; // @default = 0.055
    float _Opacity; // @default = 0.85
    float _PannerX; // @default = 0.3333333
    float _PannerY; // @default = 0.04
    float _PreviewClockEnabled; // @default = 0
    float _PreviewNodeMode; // @default = 0
    float _PreviewSeconds; // @default = 0
    float _ShapeContrast; // @default = 1.4
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


#line 170
layout(location = 1)
out vec2 entryPointParam_vert_uv_0;


#line 170
layout(location = 0) in vec4 v_vertex_0; // @vertex Position required


#line 170
layout(location = 1) in vec2 v_uv_0; // @vertex TexCoord0 optional


#line 170
struct v2f_0
{
    vec4 pos_0;
    vec2 uv_0;
};


#line 170
void main()
{

#line 170
    v2f_0 o_0;

#line 170
    o_0.pos_0 = UnityObjectToClipPos_0(v_vertex_0);

#line 170
    o_0.uv_0 = v_uv_0;

#line 170
    v2f_0 _S1 = o_0;

#line 170
    gl_Position = o_0.pos_0;

#line 170
    entryPointParam_vert_uv_0 = _S1.uv_0;

#line 170
    return;
}


// @rimunity-host-matrices 1
