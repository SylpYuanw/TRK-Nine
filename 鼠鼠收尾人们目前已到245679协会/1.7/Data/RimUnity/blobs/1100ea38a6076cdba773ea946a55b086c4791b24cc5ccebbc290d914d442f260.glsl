#version 450
#extension GL_EXT_control_flow_attributes : require
float RU_CompatPow2(float x) {
    float result = float(1.0);
    x *= x;
    result *= x;
    return result;
}
float RU_CompatSmoothstep(float low, float high, float x) {
    if (low == high) return step(low, x);
    float t = clamp((x - low) / (high - low), 0.0, 1.0);
    return t * t * (3.0 - 2.0 * t);
}
vec2 RU_CompatPow2(vec2 x) {
    vec2 result = vec2(1.0);
    x *= x;
    result *= x;
    return result;
}
vec2 RU_CompatSmoothstep(vec2 low, vec2 high, vec2 x) {
    return vec2(RU_CompatSmoothstep(low.x, high.x, x.x), RU_CompatSmoothstep(low.y, high.y, x.y));
}
vec2 RU_CompatSmoothstep(float low, float high, vec2 x) { return RU_CompatSmoothstep(vec2(low), vec2(high), x); }
vec3 RU_CompatPow2(vec3 x) {
    vec3 result = vec3(1.0);
    x *= x;
    result *= x;
    return result;
}
vec3 RU_CompatSmoothstep(vec3 low, vec3 high, vec3 x) {
    return vec3(RU_CompatSmoothstep(low.x, high.x, x.x), RU_CompatSmoothstep(low.y, high.y, x.y), RU_CompatSmoothstep(low.z, high.z, x.z));
}
vec3 RU_CompatSmoothstep(float low, float high, vec3 x) { return RU_CompatSmoothstep(vec3(low), vec3(high), x); }
vec4 RU_CompatPow2(vec4 x) {
    vec4 result = vec4(1.0);
    x *= x;
    result *= x;
    return result;
}
vec4 RU_CompatSmoothstep(vec4 low, vec4 high, vec4 x) {
    return vec4(RU_CompatSmoothstep(low.x, high.x, x.x), RU_CompatSmoothstep(low.y, high.y, x.y), RU_CompatSmoothstep(low.z, high.z, x.z), RU_CompatSmoothstep(low.w, high.w, x.w));
}
vec4 RU_CompatSmoothstep(float low, float high, vec4 x) { return RU_CompatSmoothstep(vec4(low), vec4(high), x); }


#line 56 0
struct RU_MaterialData_0
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
};


#line 88
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

#line 131
layout(binding = 3, set = 2)
uniform sampler2D _StartTex; // @default = gray


#line 133



#line 120
layout(binding = 1, set = 2)
uniform sampler2D _MainTex; // @default = black


#line 122



#line 14946 1
float saturate_0(float x_0)
{

#line 14954
    return clamp(x_0, 0.0, 1.0);
}


#line 14961
vec2 saturate_1(vec2 x_1)
{

#line 14969
    return clamp(x_1, vec2(0.0), vec2(1.0));
}


#line 143 0
float WindHash_0(vec2 p_0)
{

#line 143
    float _S1 = p_0.x;

#line 143
    vec3 q_0 = fract(vec3(_S1, p_0.y, _S1) * 0.1031000018119812);

#line 143
    vec3 q_1 = q_0 + dot(q_0, q_0.yzx + 33.3300018310546875);

#line 143
    return fract((q_1.x + q_1.y) * q_1.z);
}


#line 143
float WindNoise_0(vec2 p_1)
{

#line 143
    vec2 cell_0 = floor(p_1);

#line 143
    vec2 f_0 = fract(p_1);

#line 143
    vec2 f_1 = f_0 * f_0 * (3.0 - 2.0 * f_0);

#line 143
    float _S2 = f_1.x;

#line 143
    return mix(mix(WindHash_0(cell_0), WindHash_0(cell_0 + vec2(1.0, 0.0)), _S2), mix(WindHash_0(cell_0 + vec2(0.0, 1.0)), WindHash_0(cell_0 + vec2(1.0, 1.0)), _S2), f_1.y);
}


#line 168
float BodyFbm_0(vec2 p_2)
{

#line 168
    return WindNoise_0(p_2) * 0.57999998331069946 + WindNoise_0(p_2 * 2.02999997138977051 + 7.40000009536743164) * 0.2800000011920929 + WindNoise_0(p_2 * 4.1100001335144043 + 18.20000076293945312) * 0.14000000059604645;
}


#line 135
vec4 RU_tex2D_StartTex_0(vec2 uv_0)
{

#line 135
    return (texture(_StartTex, (vec2(uv_0.x, 1.0 - uv_0.y))));
}


#line 124
vec4 RU_tex2D_MainTex_0(vec2 uv_1)
{

#line 124
    return (texture(_MainTex, (vec2(uv_1.x, 1.0 - uv_1.y))));
}


#line 168
float BodyNodeMask_0(float along_0, float across_0, float seconds_0)
{

#line 168
    bool _S3;

#line 168
    if((RU_Material_0._Erosion) <= 0.0)
    {

#line 168
        _S3 = true;

#line 168
    }
    else
    {

#line 168
        _S3 = along_0 <= (RU_Material_0._FadeStart);

#line 168
    }

#line 168
    if(_S3)
    {

#line 168
        _S3 = true;

#line 168
    }
    else
    {

#line 168
        _S3 = (RU_Material_0._FadeStart) >= 0.99900001287460327;

#line 168
    }

#line 168
    if(_S3)
    {

#line 168
        return 1.0;
    }

#line 168
    int _S4 = int(floor(RU_Material_0._DissolveNodes + 0.5));

#line 168
    int node_0 = 0;

#line 168
    float result_0 = 1.0;

#line 168
    [[dont_unroll]]
    for(;;)
    {

#line 168
        if(node_0 < 8)
        {
        }
        else
        {

#line 168
            break;
        }

#line 168
        if(node_0 >= _S4)
        {

#line 168
            break;
        }

#line 168
        float _S5 = float(node_0);

#line 168
        float clock_0 = seconds_0 * RU_Material_0._DissolveSpeed + WindHash_0(vec2(_S5 + RU_Material_0._FlowSeed, 3.71000003814697266));

#line 168
        float cycle_0 = floor(clock_0);

#line 168
        float age_0 = fract(clock_0);

#line 168
        float r1_0 = WindHash_0(vec2(_S5 * 7.13000011444091797 + RU_Material_0._FlowSeed, cycle_0 + 5.19000005722045898));

#line 168
        float _S6 = (along_0 - mix(RU_Material_0._FadeStart, 0.98000001907348633, (_S5 + 0.20000000298023224 + r1_0 * 0.60000002384185791) / max(RU_Material_0._DissolveNodes, 1.0))) / (0.15999999642372131 + r1_0 * 0.07999999821186066);

#line 168
        float _S7 = (across_0 - (WindHash_0(vec2(_S5 + 19.70000076293945312 + RU_Material_0._FlowSeed, cycle_0 * 1.90999996662139893 + 2.40000009536743164)) - 0.5) * 0.89999997615814209) / 0.44999998807907104;

#line 168
        vec2 local_0 = vec2(_S6, _S7);

#line 168
        if((abs(_S6)) > 2.5)
        {

#line 168
            _S3 = true;

#line 168
        }
        else
        {

#line 168
            _S3 = (abs(_S7)) > 2.5;

#line 168
        }

#line 168
        if(_S3)
        {

#line 168
            node_0 = node_0 + 1;

#line 168
            continue;
        }

#line 168
        float life_0 = sin(age_0 * 3.14159274101257324);

#line 168
        result_0 = result_0 * (1.0 - RU_CompatSmoothstep(0.02999999932944775, 1.0, life_0 * life_0 * (exp2((- dot(local_0, local_0) * 1.5))) * RU_Material_0._Erosion * 1.70000004768371582 * (0.85000002384185791 + WindNoise_0(vec2(along_0 * 19.0, across_0 * 9.0) + vec2(_S5 * 4.69999980926513672 + RU_Material_0._FlowSeed, cycle_0 * 3.09999990463256836)) * 0.30000001192092896)) * RU_CompatSmoothstep(RU_Material_0._FadeStart, RU_Material_0._FadeStart + 0.05999999865889549, along_0));

#line 168
        node_0 = node_0 + 1;

#line 168
    }

#line 168
    return result_0;
}


#line 168
vec4 BodyWind_0(vec2 uv_2, float facing_0, float seconds_1)
{

#line 168
    float transportSeconds_0;

#line 168
    if((RU_Material_0._PreviewNodeMode) > 0.5)
    {

#line 168
        transportSeconds_0 = 0.0;

#line 168
    }
    else
    {

#line 168
        transportSeconds_0 = seconds_1;

#line 168
    }

#line 168
    float horizontal_0 = abs(facing_0);

#line 168
    float angle_0 = mix(90.0, RU_Material_0._FlowAngle, horizontal_0) * 0.01745329238474369;

#line 168
    float _S8 = - cos(angle_0);

#line 168
    float _S9 = sin(angle_0);

#line 168
    vec2 direction_0 = vec2(_S8, _S9);

#line 168
    vec2 side_0 = vec2(_S9, - _S8);

#line 168
    vec2 relative_0 = uv_2 - vec2(0.5 + (RU_Material_0._SourceU - 0.5) * horizontal_0, RU_Material_0._SourceV);

#line 168
    float pathLength_0 = RU_Material_0._FlowLength * sin(radians(RU_Material_0._FlowAngle)) / max(_S9, 0.00999999977648258);

#line 168
    relative_0[0] = relative_0[0] * RU_Material_0._CanvasAspect;

#line 168
    float along_1 = dot(relative_0, direction_0);

#line 168
    float across_1 = dot(relative_0, side_0);

#line 168
    float rise_0 = along_1 / pathLength_0;

#line 168
    vec2 transport_0 = vec2(across_1 * 16.0, along_1 * 8.0 - transportSeconds_0 * RU_Material_0._FlowSpeed) * RU_Material_0._NoiseScale + RU_Material_0._FlowSeed * 0.07299999892711639;

#line 168
    vec2 _S10 = transport_0 * 0.55000001192092896;

#line 168
    vec2 warp_0 = vec2(BodyFbm_0(_S10 + 5.69999980926513672), BodyFbm_0(_S10 + 21.29999923706054688)) - 0.5;

#line 168
    vec2 field_0 = transport_0 + warp_0 * (RU_Material_0._Curl * 3.09999990463256836);

#line 168
    float n_0 = BodyFbm_0(field_0);

#line 168
    float tail_0 = 1.0 - RU_CompatSmoothstep(0.68000000715255737, 1.0, rise_0);

#line 168
    float _S11 = RU_Material_0._FlowWidth * (0.41999998688697815 + 0.57999998331069946 * RU_CompatSmoothstep(-0.11999999731779099, 0.40000000596046448, rise_0));

#line 168
    vec2 shapeUv_0 = vec2(1.0 - rise_0, 0.5 + across_1 / (RU_Material_0._FlowWidth * 2.0));

#line 168
    float texNoise_0 = RU_tex2D_StartTex_0(shapeUv_0 * RU_Material_0._NoiseScale + vec2(transportSeconds_0 * RU_Material_0._FlowSpeed * 0.15999999642372131, transportSeconds_0 * RU_Material_0._FlowSpeed * 0.02500000037252903)).x;

#line 168
    vec2 distorted_0 = shapeUv_0 + (vec2(texNoise_0, n_0) - 0.5) * 0.06499999761581421 * RU_Material_0._Curl;

#line 168
    vec3 sheet_0 = RU_tex2D_MainTex_0(saturate_1(distorted_0)).xyz;

#line 168
    float _S12 = distorted_0.x;

#line 168
    float _S13 = distorted_0.y;

#line 168
    float sheetMask_0 = max(sheet_0.x, max(sheet_0.y, sheet_0.z)) * (step(0.0, _S12) * step(_S12, 1.0) * step(0.0, _S13) * step(_S13, 1.0));

#line 168
    float sheetAlpha_0 = pow(saturate_0(sheetMask_0), 0.85000002384185791) * RU_CompatSmoothstep(-0.05999999865889549, 0.12999999523162842, rise_0) * tail_0 * mix(1.0, RU_CompatSmoothstep(0.11999999731779099, 0.69999998807907104, texNoise_0), RU_Material_0._Erosion) * 0.89999997615814209;

#line 168
    vec3 _S14 = mix(RU_Material_0._DeepColor.xyz * RU_Material_0._DeepColor.w, RU_Material_0._FlameColor.xyz * RU_Material_0._FlameColor.w, vec3(saturate_0(sheetMask_0 * 1.79999995231628418))) * sheetAlpha_0;

#line 168
    int _S15 = int(floor(RU_Material_0._RibbonCount + 0.5));

#line 168
    int ribbon_0 = 0;

#line 168
    float alpha_0 = sheetAlpha_0;

#line 168
    vec3 premul_0 = _S14;

#line 168
    [[dont_unroll]]
    for(;;)
    {

#line 168
        if(ribbon_0 < 8)
        {
        }
        else
        {

#line 168
            break;
        }

#line 168
        if(ribbon_0 >= _S15)
        {

#line 168
            break;
        }

#line 168
        float index_0 = float(ribbon_0);

#line 168
        float distance_0 = across_1 - (sin(transport_0.y * 1.10000002384185791 + index_0 * 2.43000006675720215 + warp_0.y * RU_Material_0._Curl * 2.20000004768371582) * _S11 * RU_Material_0._Curl * (0.69999998807907104 + 0.30000001192092896 * n_0) + warp_0.x * _S11 * RU_Material_0._Curl * 0.5);

#line 168
        float detail_0 = WindNoise_0(field_0 * 1.70000004768371582 + index_0 * 11.30000019073486328);

#line 168
        float _S16 = max(RU_Material_0._RibbonWidth * (0.64999997615814209 + detail_0 * 0.69999998807907104) * (1.0 - rise_0 * 0.34999999403953552) * (0.75 + index_0 * 0.25), (fwidth((distance_0))) * 0.80000001192092896);

#line 168
        float core_0 = (exp2((- RU_CompatPow2(distance_0 / _S16) * 2.40000009536743164)));

#line 168
        float a_0 = saturate_0(core_0 * 0.2199999988079071 + (exp2((- RU_CompatPow2(distance_0 / (_S16 * 3.20000004768371582)) * 2.0))) * (0.40000000596046448 + RU_Material_0._Glow * 0.30000001192092896)) * RU_CompatSmoothstep(- RU_Material_0._StartFeather, RU_Material_0._StartFeather, rise_0 + 0.03999999910593033 - index_0 * 0.05499999970197678 + (detail_0 - 0.5) * RU_Material_0._StartFeather * 1.5) * tail_0 * mix(1.0, RU_CompatSmoothstep(0.18000000715255737, 0.62000000476837158, detail_0), RU_Material_0._Erosion);

#line 168
        float _S17 = 1.0 - alpha_0;

#line 168
        vec3 premul_1 = premul_0 + (mix(RU_Material_0._DeepColor.xyz * RU_Material_0._DeepColor.w, RU_Material_0._FlameColor.xyz * RU_Material_0._FlameColor.w, vec3(0.40000000596046448 + 0.60000002384185791 * n_0)) + RU_Material_0._HotColor.xyz * RU_Material_0._HotColor.w * core_0 * (0.30000001192092896 + detail_0 * 0.44999998807907104) * RU_Material_0._Heat) * a_0 * _S17;

#line 168
        float alpha_1 = alpha_0 + a_0 * _S17;

#line 168
        ribbon_0 = ribbon_0 + 1;

#line 168
        alpha_0 = alpha_1;

#line 168
        premul_0 = premul_1;

#line 168
    }

#line 168
    float _S18 = uv_2.x;

#line 168
    float _S19 = uv_2.y;

#line 168
    return vec4((1.0 - (exp2((- premul_0 / max(alpha_0, 0.00000999999974738) * 1.60000002384185791 * RU_Material_0._Brightness)))) * RU_Material_0._Color.xyz, alpha_0 * BodyNodeMask_0(rise_0, across_1 / max(RU_Material_0._FlowWidth * 2.0, 0.00999999977648258), seconds_1) * (RU_CompatSmoothstep(0.0, 0.03500000014901161, _S18) * (1.0 - RU_CompatSmoothstep(0.9649999737739563, 1.0, _S18)) * RU_CompatSmoothstep(0.0, 0.03500000014901161, _S19) * (1.0 - RU_CompatSmoothstep(0.9649999737739563, 1.0, _S19))) * RU_Material_0._Color.w);
}


#line 168
layout(location = 0)
out vec4 entryPointParam_frag_0;


#line 168
layout(location = 1) in vec2 i_uv_0; // @vertex TexCoord0 optional


#line 168
void main()
{

#line 168
    float seconds_2;

#line 168
    if((RU_Material_0._PreviewClockEnabled) > 0.5)
    {

#line 168
        seconds_2 = RU_Material_0._PreviewSeconds;

#line 168
    }
    else
    {

#line 168
        if((RU_Material_0._UseRealTime) > 0.5)
        {

#line 168
            seconds_2 = RU_Material_0._Time.y;

#line 168
        }
        else
        {

#line 168
            seconds_2 = 0.0;

#line 168
        }

#line 168
    }

#line 168
    float facing_1 = clamp(RU_Material_0._BodyAimSide, -1.0, 1.0);

#line 168
    vec2 uv_3 = i_uv_0;

#line 168
    if(facing_1 < 0.0)
    {

#line 168
        uv_3[0] = 1.0 - uv_3.x;

#line 168
    }

#line 168
    vec4 a_1 = BodyWind_0(uv_3, facing_1, seconds_2);

#line 168
    float _S20 = abs(facing_1);

#line 168
    if(_S20 < 0.20000000298023224)
    {

#line 168
        vec4 b_0 = BodyWind_0(vec2(1.0 - uv_3.x, uv_3.y), facing_1, seconds_2);

#line 168
        float w_0 = 0.5 + 0.5 * RU_CompatSmoothstep(0.0, 0.20000000298023224, _S20);

#line 168
        float _S21 = b_0.w;

#line 168
        float _S22 = a_1.w;

#line 168
        float alpha_2 = mix(_S21, _S22, w_0);

#line 168
        entryPointParam_frag_0 = vec4(mix(b_0.xyz * _S21, a_1.xyz * _S22, vec3(w_0)) / max(alpha_2, 0.00000999999974738), alpha_2);

#line 168
        return;
    }

#line 168
    entryPointParam_frag_0 = a_1;

#line 168
    return;
}


// @rimunity-host-matrices 1
