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
};


#line 81
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

#line 106
layout(binding = 1, set = 2)
uniform sampler2D _MainTex; // @default = white


#line 108



#line 14946 1
float saturate_0(float x_0)
{

#line 14954
    return clamp(x_0, 0.0, 1.0);
}


#line 135 0
float Hash_0(vec2 p_0)
{

#line 135
    float _S1 = p_0.x;

#line 135
    vec3 q_0 = fract(vec3(_S1, p_0.y, _S1) * 0.1031000018119812);

#line 135
    vec3 q_1 = q_0 + dot(q_0, q_0.yzx + 33.3300018310546875);

#line 135
    return fract((q_1.x + q_1.y) * q_1.z);
}


#line 135
float Noise_0(vec2 p_1)
{

#line 135
    vec2 c_0 = floor(p_1);

#line 135
    vec2 f_0 = fract(p_1);

#line 135
    vec2 f_1 = f_0 * f_0 * (3.0 - 2.0 * f_0);

#line 135
    float _S2 = f_1.x;

#line 135
    return mix(mix(Hash_0(c_0), Hash_0(c_0 + vec2(1.0, 0.0)), _S2), mix(Hash_0(c_0 + vec2(0.0, 1.0)), Hash_0(c_0 + 1.0), _S2), f_1.y);
}


#line 135
float Fbm_0(vec2 p_2)
{

#line 135
    return Noise_0(p_2) * 0.56999999284744263 + Noise_0(p_2 * 2.02999997138977051 + 7.09999990463256836) * 0.2800000011920929 + Noise_0(p_2 * 4.1100001335144043 + 19.39999961853027344) * 0.15000000596046448;
}


#line 110
vec4 RU_tex2D_MainTex_0(vec2 uv_0)
{

#line 110
    return (texture(_MainTex, (vec2(uv_0.x, 1.0 - uv_0.y))));
}


#line 110
layout(location = 0)
out vec4 entryPointParam_frag_0;


#line 110
layout(location = 1) in vec2 i_uv_0; // @vertex TexCoord0 optional


#line 135
void main()
{

#line 135
    float seconds_0;

#line 135
    if((RU_Material_0._PreviewClockEnabled) > 0.5)
    {

#line 135
        seconds_0 = RU_Material_0._PreviewSeconds;

#line 135
    }
    else
    {

#line 135
        if((RU_Material_0._UseRealTime) > 0.5)
        {

#line 135
            seconds_0 = RU_Material_0._Time.y;

#line 135
        }
        else
        {

#line 135
            seconds_0 = 0.0;

#line 135
        }

#line 135
    }

#line 135
    float angleRadians_0 = RU_Material_0._StreakAngle * 0.01745329238474369;

#line 135
    float _S3 = - cos(angleRadians_0);

#line 135
    float _S4 = sin(angleRadians_0);

#line 135
    vec2 drift_0 = vec2(_S3, _S4);

#line 135
    vec2 _S5 = vec2(- _S4, _S3);

#line 135
    const vec3 _S6 = vec3(0.0);

#line 135
    int slot_0 = 0;

#line 135
    vec3 rgb_0 = _S6;

#line 135
    float alpha_0 = 0.0;

#line 135
    for(;;)
    {

#line 135
        if(slot_0 < 12)
        {
        }
        else
        {

#line 135
            break;
        }

#line 135
        float index_0 = float(slot_0);

#line 135
        float life_0 = fract(seconds_0 / max(RU_Material_0._EmitPeriod, 0.00100000004749745) + Hash_0(vec2(index_0, 19.10000038146972656)));

#line 135
        float _S7 = 1.0 - life_0;

#line 135
        float _S8 = index_0 * 3.09999990463256836;

#line 135
        vec2 relative_0 = i_uv_0 - (vec2(0.89999997615814209, 0.15999999642372131 + Hash_0(vec2(index_0, 7.30000019073486328)) * 0.5) + drift_0 * (RU_Material_0._TravelDistance * mix(life_0, 1.0 - _S7 * _S7, RU_Material_0._SpeedCurve)) + vec2(Fbm_0(vec2(_S8, seconds_0 * RU_Material_0._TurbulenceSpeed)) - 0.5, Noise_0(vec2(_S8 + 13.69999980926513672, seconds_0 * RU_Material_0._TurbulenceSpeed + 7.09999990463256836)) - 0.5) * (RU_Material_0._Turbulence * 2.0));

#line 135
        float headDot_0 = exp(- RU_CompatPow2(length(relative_0) / max(RU_Material_0._StreakWidth * 1.39999997615814209, 0.00100000004749745)));

#line 135
        float streak_0 = (sin(3.14159274101257324 * saturate_0(0.07999999821186066 + saturate_0(- dot(relative_0, drift_0) / max(RU_Material_0._StreakLength * (0.69999998807907104 + 0.60000002384185791 * Hash_0(vec2(index_0, 31.70000076293945312))), 0.00100000004749745)) * 0.86000001430511475)) * exp(- RU_CompatPow2(dot(relative_0, _S5) / max(RU_Material_0._StreakWidth, 0.00100000004749745))) * RU_Material_0._StreakBrightness + headDot_0 * RU_Material_0._HeadGlow) * (RU_CompatSmoothstep(0.0, 0.15999999642372131, life_0) * RU_CompatSmoothstep(1.0, 0.74000000953674316, life_0)) * step(index_0 + 0.5, RU_Material_0._ParticleCount);

#line 135
        vec3 rgb_1 = rgb_0 + mix(RU_Material_0._StreakColor.xyz, RU_Material_0._HeadColor.xyz, vec3(saturate_0(headDot_0 * 1.5))) * streak_0;

#line 135
        float alpha_1 = alpha_0 + streak_0;

#line 135
        slot_0 = slot_0 + 1;

#line 135
        rgb_0 = rgb_1;

#line 135
        alpha_0 = alpha_1;

#line 135
    }

#line 135
    vec2 _S9 = i_uv_0 * 2.5;

#line 135
    vec4 smoke_0 = RU_tex2D_MainTex_0(i_uv_0 + (vec2(Fbm_0(_S9 + seconds_0 * 0.20000000298023224), Fbm_0(_S9 + vec2(31.70000076293945312, - seconds_0 * 0.15000000596046448))) - 0.5) * RU_Material_0._SmokeWarp + vec2(fract(seconds_0 * RU_Material_0._SmokeScroll), 0.0));

#line 135
    float smokeAlpha_0 = RU_CompatSmoothstep(RU_Material_0._SmokeCut, RU_Material_0._SmokeCut + max(RU_Material_0._SmokeSoft, 0.00100000004749745), smoke_0.w) * RU_Material_0._SmokeStrength;

#line 135
    float _S10 = i_uv_0.x;

#line 135
    float _S11 = i_uv_0.y;

#line 135
    entryPointParam_frag_0 = vec4((rgb_0 + smoke_0.xyz * smokeAlpha_0) * RU_Material_0._Color.xyz, saturate_0((alpha_0 + smokeAlpha_0) * (RU_CompatSmoothstep(0.0, 0.02999999932944775, _S10) * RU_CompatSmoothstep(1.0, 0.97000002861022949, _S10) * RU_CompatSmoothstep(0.0, 0.02999999932944775, _S11) * RU_CompatSmoothstep(1.0, 0.97000002861022949, _S11))) * RU_Material_0._Color.w);

#line 135
    return;
}


// @rimunity-host-matrices 1
