#version 450
float RU_CompatPow2(float x) {
    float result = float(1.0);
    x *= x;
    result *= x;
    return result;
}
float RU_CompatSmoothstep(float low, float high, float x) {
    float t = clamp((x - low) / (high - low), float(0.0), float(1.0));
    return t * t * (3.0 - 2.0 * t);
}
vec2 RU_CompatPow2(vec2 x) {
    vec2 result = vec2(1.0);
    x *= x;
    result *= x;
    return result;
}
vec2 RU_CompatSmoothstep(vec2 low, vec2 high, vec2 x) {
    vec2 t = clamp((x - low) / (high - low), vec2(0.0), vec2(1.0));
    return t * t * (3.0 - 2.0 * t);
}
vec2 RU_CompatSmoothstep(float low, float high, vec2 x) { return RU_CompatSmoothstep(vec2(low), vec2(high), x); }
vec3 RU_CompatPow2(vec3 x) {
    vec3 result = vec3(1.0);
    x *= x;
    result *= x;
    return result;
}
vec3 RU_CompatSmoothstep(vec3 low, vec3 high, vec3 x) {
    vec3 t = clamp((x - low) / (high - low), vec3(0.0), vec3(1.0));
    return t * t * (3.0 - 2.0 * t);
}
vec3 RU_CompatSmoothstep(float low, float high, vec3 x) { return RU_CompatSmoothstep(vec3(low), vec3(high), x); }
vec4 RU_CompatPow2(vec4 x) {
    vec4 result = vec4(1.0);
    x *= x;
    result *= x;
    return result;
}
vec4 RU_CompatSmoothstep(vec4 low, vec4 high, vec4 x) {
    vec4 t = clamp((x - low) / (high - low), vec4(0.0), vec4(1.0));
    return t * t * (3.0 - 2.0 * t);
}
vec4 RU_CompatSmoothstep(float low, float high, vec4 x) { return RU_CompatSmoothstep(vec4(low), vec4(high), x); }


#line 56 0
struct RU_MaterialData_0
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
};


#line 75
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

#line 94
layout(binding = 1, set = 2)
uniform sampler2D _MainTex; // @default = white


#line 96



#line 105
layout(binding = 3, set = 2)
uniform sampler2D _StartTex; // @default = white


#line 107



#line 14946 1
float saturate_0(float x_0)
{

#line 14954
    return clamp(x_0, 0.0, 1.0);
}


#line 128 0
float Hash_0(vec2 p_0)
{

#line 128
    float _S1 = p_0.x;

#line 128
    vec3 q_0 = fract(vec3(_S1, p_0.y, _S1) * 0.1031000018119812);

#line 128
    vec3 q_1 = q_0 + dot(q_0, q_0.yzx + 33.3300018310546875);

#line 128
    return fract((q_1.x + q_1.y) * q_1.z);
}


#line 128
float Noise_0(vec2 p_1)
{

#line 128
    vec2 cell_0 = floor(p_1);

#line 128
    vec2 f_0 = fract(p_1);

#line 128
    vec2 f_1 = f_0 * f_0 * (3.0 - 2.0 * f_0);

#line 128
    float _S2 = f_1.x;

#line 128
    return mix(mix(Hash_0(cell_0), Hash_0(cell_0 + vec2(1.0, 0.0)), _S2), mix(Hash_0(cell_0 + vec2(0.0, 1.0)), Hash_0(cell_0 + 1.0), _S2), f_1.y);
}


#line 128
float Fbm_0(vec2 p_2)
{

#line 128
    return Noise_0(p_2) * 0.56999999284744263 + Noise_0(p_2 * 2.02999997138977051 + 7.09999990463256836) * 0.2800000011920929 + Noise_0(p_2 * 4.1100001335144043 + 19.39999961853027344) * 0.15000000596046448;
}


#line 128
vec2 StreamCenter_0(float stream_0, float t_0, float seconds_0, float seed_0)
{

#line 128
    return vec2(RU_Material_0._HeadU - t_0 * RU_Material_0._FlowLength, RU_Material_0._HeadV + (Hash_0(vec2(stream_0, 17.70000076293945312)) - 0.5) * 2.0 * RU_Material_0._Divergence * t_0 * 0.41999998688697815 + ((Fbm_0(vec2(seed_0 + t_0 * RU_Material_0._NoiseScale, seconds_0 * RU_Material_0._NoiseSpeed * 0.34999999403953552 + stream_0 * 4.69999980926513672)) - 0.5) * RU_Material_0._NoiseStrength + sin(t_0 * 7.0 + seconds_0 * RU_Material_0._FlowSpeed * (0.55000001192092896 + seed_0 * 0.34999999403953552) + seed_0 * 13.0) * 0.03500000014901161) * t_0 * t_0);
}


#line 98
vec4 RU_tex2D_MainTex_0(vec2 uv_0)
{

#line 98
    return (texture(_MainTex, (vec2(uv_0.x, 1.0 - uv_0.y))));
}


#line 109
vec4 RU_tex2D_StartTex_0(vec2 uv_1)
{

#line 109
    return (texture(_StartTex, (vec2(uv_1.x, 1.0 - uv_1.y))));
}


#line 109
layout(location = 0)
out vec4 entryPointParam_frag_0;


#line 109
layout(location = 1) in vec2 i_uv_0; // @vertex TexCoord0 optional


#line 128
void main()
{

#line 128
    float seconds_1;

#line 128
    if((RU_Material_0._PreviewClockEnabled) > 0.5)
    {

#line 128
        seconds_1 = RU_Material_0._PreviewSeconds;

#line 128
    }
    else
    {

#line 128
        if((RU_Material_0._UseRealTime) > 0.5)
        {

#line 128
            seconds_1 = RU_Material_0._Time.y;

#line 128
        }
        else
        {

#line 128
            seconds_1 = 0.0;

#line 128
        }

#line 128
    }

#line 128
    float core_0 = exp(- RU_CompatPow2(length(i_uv_0 - vec2(RU_Material_0._HeadU, RU_Material_0._HeadV)) / max(RU_Material_0._CoreRadius, 0.00100000004749745)) * 2.20000004768371582) * mix(0.87999999523162842, 1.08000004291534424, Fbm_0(i_uv_0 * 9.0 + seconds_1 * 0.11999999731779099));

#line 128
    vec3 _S3 = vec3(0.89999997615814209, 0.04500000178813934, 0.01999999955296516) * core_0 * RU_Material_0._CoreBrightness;

#line 128
    float _S4 = core_0 * RU_Material_0._CoreBrightness;

#line 128
    int stream_1 = 0;

#line 128
    vec3 rgb_0 = _S3;

#line 128
    float alpha_0 = _S4;

#line 128
    for(;;)
    {

#line 128
        if(stream_1 < 6)
        {
        }
        else
        {

#line 128
            break;
        }

#line 128
        float slot_0 = float(stream_1);

#line 128
        float seed_1 = Hash_0(vec2(slot_0 + 3.09999990463256836, 41.90000152587890625));

#line 128
        float start_0 = Hash_0(vec2(slot_0 + 9.39999961853027344, 13.69999980926513672));

#line 128
        float _S5 = i_uv_0.x;

#line 128
        float _S6 = RU_Material_0._HeadU - _S5;

#line 128
        float t_1 = saturate_0(_S6 / max(RU_Material_0._FlowLength, 0.00100000004749745));

#line 128
        float active_0 = step(0.0, _S6) * step(_S6, RU_Material_0._FlowLength);

#line 128
        vec2 center_0 = StreamCenter_0(slot_0, t_1, seconds_1, seed_1 * 7.0 + start_0 * 3.0);

#line 128
        float width_0 = mix(0.01200000010430813, 0.0260000005364418, seed_1) + RU_Material_0._Divergence * t_1 * mix(0.03200000151991844, 0.06400000303983688, start_0);

#line 128
        float across_0 = (i_uv_0 - center_0).y;

#line 128
        float _S7 = max(width_0, 0.00100000004749745);

#line 128
        float taper_0 = RU_CompatSmoothstep(0.0, 0.07999999821186066, t_1) * RU_CompatSmoothstep(1.0, 0.77999997138977051, t_1);

#line 128
        vec2 _S8 = vec2(saturate_0(mix(RU_Material_0._HeadU, _S5, 0.81999999284744263)), saturate_0(RU_Material_0._HeadV + (i_uv_0.y - center_0.y) * 1.60000002384185791));

#line 128
        float streamMask_0 = exp(- RU_CompatPow2(across_0 / _S7)) * taper_0 * mix(1.0, RU_CompatSmoothstep(0.15999999642372131, 0.69999998807907104, Fbm_0((i_uv_0 + vec2(seconds_1 * RU_Material_0._NoiseSpeed * 0.17000000178813934, - seconds_1 * RU_Material_0._NoiseSpeed * 0.10999999940395355)) * RU_Material_0._NoiseScale * 3.20000004768371582 + slot_0 * 5.30000019073486328) + saturate_0(1.0 - abs(across_0) / _S7) * 0.41999998688697815), RU_Material_0._Erosion) * active_0 * mix(0.72000002861022949, 1.0, saturate_0(max(RU_tex2D_MainTex_0(_S8).w, RU_tex2D_StartTex_0(_S8).w) * 2.0));

#line 128
        float haze_0 = exp(- RU_CompatPow2(across_0 / max(width_0 * 3.59999990463256836, 0.00100000004749745))) * taper_0 * active_0 * (0.07999999821186066 + 0.07999999821186066 * (RU_CompatSmoothstep(0.41999998688697815, 0.77999997138977051, t_1) * (0.60000002384185791 + 0.40000000596046448 * step(0.44999998807907104, fract(slot_0 * 0.37000000476837158)))));

#line 128
        vec3 rgb_1 = rgb_0 + mix(vec3(0.5, 0.01200000010430813, 0.00600000005215406), vec3(0.95999997854232788, 0.07500000298023224, 0.02500000037252903), vec3(saturate_0(1.0 - t_1 * 0.55000001192092896))) * streamMask_0 * mix(0.69999998807907104, 1.0, 1.0 - t_1 * 0.25) + vec3(0.41999998688697815, 0.01200000010430813, 0.00600000005215406) * haze_0;

#line 128
        float alpha_1 = alpha_0 + streamMask_0 * (0.41999998688697815 + 0.18000000715255737 * (1.0 - t_1)) + haze_0;

#line 128
        stream_1 = stream_1 + 1;

#line 128
        rgb_0 = rgb_1;

#line 128
        alpha_0 = alpha_1;

#line 128
    }

#line 128
    float _S9 = i_uv_0.x;

#line 128
    float _S10 = i_uv_0.y;

#line 128
    entryPointParam_frag_0 = vec4(rgb_0 * RU_Material_0._Color.xyz * RU_Material_0._Brightness, saturate_0(alpha_0 * (RU_CompatSmoothstep(0.0, 0.02500000037252903, _S9) * RU_CompatSmoothstep(1.0, 0.97500002384185791, _S9) * RU_CompatSmoothstep(0.0, 0.02500000037252903, _S10) * RU_CompatSmoothstep(1.0, 0.97500002384185791, _S10)) * RU_Material_0._Color.w));

#line 128
    return;
}


// @rimunity-host-matrices 1
