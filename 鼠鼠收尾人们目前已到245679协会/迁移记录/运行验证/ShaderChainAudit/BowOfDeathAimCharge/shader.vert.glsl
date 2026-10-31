#version 450
layout(binding = 3, set = 2)
uniform sampler2D _StartTex; // @default = white
layout(binding = 1, set = 2)
uniform sampler2D _MainTex; // @default = white
layout(binding = 0, set = 2, std140) uniform block_RU_MaterialData_0
{
    float _Brightness; // @default = 1
    vec4 _Color; // @default = vec4(1, 1, 1, 1) @color
    float _CoreBrightness; // @default = 1.25
    float _CoreRadius; // @default = 0.1
    float _Divergence; // @default = 0.48
    float _Erosion; // @default = 0.5
    float _FlowLength; // @default = 0.68
    float _FlowSpeed; // @default = 1.2
    float _HeadU; // @default = 0.79
    float _HeadV; // @default = 0.5
    float _NoiseScale; // @default = 2.5
    float _NoiseSpeed; // @default = 0.9
    float _NoiseStrength; // @default = 0.24
    float _PreviewClockEnabled; // @default = 0
    float _PreviewSeconds; // @default = 0
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


#line 128
layout(location = 1)
out vec2 entryPointParam_vert_uv_0;


#line 128
layout(location = 0) in vec4 v_vertex_0; // @vertex Position required


#line 128
layout(location = 1) in vec2 v_uv_0; // @vertex TexCoord0 optional


#line 128
struct v2f_0
{
    vec4 pos_0;
    vec2 uv_0;
};


#line 128
void main()
{

#line 128
    v2f_0 o_0;

#line 128
    o_0.pos_0 = UnityObjectToClipPos_0(v_vertex_0);

#line 128
    o_0.uv_0 = v_uv_0;

#line 128
    v2f_0 _S1 = o_0;

#line 128
    gl_Position = o_0.pos_0;

#line 128
    entryPointParam_vert_uv_0 = _S1.uv_0;

#line 128
    return;
}


// @rimunity-host-matrices 1
