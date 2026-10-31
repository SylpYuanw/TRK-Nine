#version 450
#extension GL_EXT_control_flow_attributes : require
float RU_CompatSmoothstep(float low, float high, float x) {
    if (low == high) return step(low, x);
    float t = clamp((x - low) / (high - low), 0.0, 1.0);
    return t * t * (3.0 - 2.0 * t);
}
vec2 RU_CompatSmoothstep(vec2 low, vec2 high, vec2 x) {
    return vec2(RU_CompatSmoothstep(low.x, high.x, x.x), RU_CompatSmoothstep(low.y, high.y, x.y));
}
vec2 RU_CompatSmoothstep(float low, float high, vec2 x) { return RU_CompatSmoothstep(vec2(low), vec2(high), x); }
vec3 RU_CompatSmoothstep(vec3 low, vec3 high, vec3 x) {
    return vec3(RU_CompatSmoothstep(low.x, high.x, x.x), RU_CompatSmoothstep(low.y, high.y, x.y), RU_CompatSmoothstep(low.z, high.z, x.z));
}
vec3 RU_CompatSmoothstep(float low, float high, vec3 x) { return RU_CompatSmoothstep(vec3(low), vec3(high), x); }
vec4 RU_CompatSmoothstep(vec4 low, vec4 high, vec4 x) {
    return vec4(RU_CompatSmoothstep(low.x, high.x, x.x), RU_CompatSmoothstep(low.y, high.y, x.y), RU_CompatSmoothstep(low.z, high.z, x.z), RU_CompatSmoothstep(low.w, high.w, x.w));
}
vec4 RU_CompatSmoothstep(float low, float high, vec4 x) { return RU_CompatSmoothstep(vec4(low), vec4(high), x); }


#line 56 0
struct RU_MaterialData_0
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
};


#line 89
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

#line 133
layout(binding = 3, set = 2)
uniform sampler2D _StartTex; // @default = gray


#line 135



#line 122
layout(binding = 1, set = 2)
uniform sampler2D _MainTex; // @default = black


#line 124



#line 14961 1
vec2 saturate_0(vec2 x_0)
{

#line 14969
    return clamp(x_0, vec2(0.0), vec2(1.0));
}


#line 14946
float saturate_1(float x_1)
{

#line 14954
    return clamp(x_1, 0.0, 1.0);
}


#line 145 0
float WindHash_0(vec2 p_0)
{

#line 145
    float _S1 = p_0.x;

#line 145
    vec3 q_0 = fract(vec3(_S1, p_0.y, _S1) * 0.1031000018119812);

#line 145
    vec3 q_1 = q_0 + dot(q_0, q_0.yzx + 33.3300018310546875);

#line 145
    return fract((q_1.x + q_1.y) * q_1.z);
}


#line 137
vec4 RU_tex2D_StartTex_0(vec2 uv_0)
{

#line 137
    return (texture(_StartTex, (vec2(uv_0.x, 1.0 - uv_0.y))));
}


#line 126
vec4 RU_tex2D_MainTex_0(vec2 uv_1)
{

#line 126
    return (texture(_MainTex, (vec2(uv_1.x, 1.0 - uv_1.y))));
}


#line 145
float WindTextureMask_0(vec4 c_0)
{

#line 145
    return saturate_1(max(c_0.x, max(c_0.y, c_0.z)) * c_0.w);
}


#line 170
float FlowPacket_0(float age_0, float t_0)
{

#line 170
    float front_0 = age_0 * 1.54999995231628418 - 0.25;

#line 170
    return RU_CompatSmoothstep(0.0, 0.10000000149011612, age_0) * (1.0 - RU_CompatSmoothstep(0.87999999523162842, 1.0, age_0)) * RU_CompatSmoothstep(front_0 - 0.5, front_0 - 0.05000000074505806, t_0) * (1.0 - RU_CompatSmoothstep(front_0 + 0.05000000074505806, front_0 + 0.55000001192092896, t_0));
}


#line 145
float WindNoise_0(vec2 p_1)
{

#line 145
    vec2 cell_0 = floor(p_1);

#line 145
    vec2 f_0 = fract(p_1);

#line 145
    vec2 f_1 = f_0 * f_0 * (3.0 - 2.0 * f_0);

#line 145
    float _S2 = f_1.x;

#line 145
    return mix(mix(WindHash_0(cell_0), WindHash_0(cell_0 + vec2(1.0, 0.0)), _S2), mix(WindHash_0(cell_0 + vec2(0.0, 1.0)), WindHash_0(cell_0 + vec2(1.0, 1.0)), _S2), f_1.y);
}


#line 145
float WindNodeMask_0(float along_0, float across_0, float seconds_0, float speed_0, float seed_0, float start_0, float erosion_0, float count_0)
{

#line 145
    bool _S3;

#line 145
    if(erosion_0 <= 0.0)
    {

#line 145
        _S3 = true;

#line 145
    }
    else
    {

#line 145
        _S3 = along_0 <= start_0;

#line 145
    }

#line 145
    if(_S3)
    {

#line 145
        _S3 = true;

#line 145
    }
    else
    {

#line 145
        _S3 = start_0 >= 0.99900001287460327;

#line 145
    }

#line 145
    if(_S3)
    {

#line 145
        return 1.0;
    }

#line 145
    int _S4 = int(floor(count_0 + 0.5));

#line 145
    int n_0 = 0;

#line 145
    float result_0 = 1.0;

#line 145
    [[dont_unroll]]
    for(;;)
    {

#line 145
        if(n_0 < 8)
        {
        }
        else
        {

#line 145
            break;
        }

#line 145
        if(n_0 >= _S4)
        {

#line 145
            break;
        }

#line 145
        float node_0 = float(n_0);

#line 145
        float clock_0 = seconds_0 * speed_0 + WindHash_0(vec2(node_0 + seed_0, 3.71000003814697266));

#line 145
        float cycle_0 = floor(clock_0);

#line 145
        float age_1 = fract(clock_0);

#line 145
        float r1_0 = WindHash_0(vec2(node_0 * 7.13000011444091797 + seed_0, cycle_0 + 5.19000005722045898));

#line 145
        float r2_0 = WindHash_0(vec2(node_0 + 19.70000076293945312 + seed_0, cycle_0 * 1.90999996662139893 + 2.40000009536743164));

#line 145
        float center_0 = mix(start_0, 0.98000001907348633, (node_0 + 0.20000000298023224 + r1_0 * 0.60000002384185791) / max(count_0, 1.0));

#line 145
        float side_0;

#line 145
        if(r2_0 < 0.5)
        {

#line 145
            side_0 = -1.0;

#line 145
        }
        else
        {

#line 145
            side_0 = 1.0;

#line 145
        }

#line 145
        float lateral_0 = 0.01999999955296516 + abs(r2_0 - 0.5) * 0.44999998807907104;

#line 145
        float life_0 = RU_CompatSmoothstep(0.01999999955296516, 0.2199999988079071, age_1) * (1.0 - RU_CompatSmoothstep(0.64999997615814209, 0.98000001907348633, age_1));

#line 145
        float radius_0 = (0.11999999731779099 + r1_0 * 0.10999999940395355) * life_0 * sqrt(max(erosion_0, 0.0));

#line 145
        if(radius_0 < 0.00009999999747379)
        {

#line 145
            _S3 = true;

#line 145
        }
        else
        {

#line 145
            _S3 = (abs(along_0 - center_0)) > (radius_0 * 1.64999997615814209 + 0.03500000014901161);

#line 145
        }

#line 145
        if(_S3)
        {

#line 145
            n_0 = n_0 + 1;

#line 145
            continue;
        }

#line 145
        float entry_0 = start_0 + WindNoise_0(vec2(across_0 * 6.0 + node_0, seed_0)) * 0.02500000037252903;

#line 145
        result_0 = result_0 * mix(1.0, RU_CompatSmoothstep(0.2199999988079071, 1.35000002384185791, length(vec2((along_0 - center_0 + (WindNoise_0(vec2(across_0 * 5.0 + node_0, seed_0 + cycle_0)) - 0.5) * 0.07000000029802322 * life_0) / max(radius_0, 0.00009999999747379), max(lateral_0 - side_0 * across_0, 0.0) / max(radius_0 * 0.75, 0.00009999999747379))) + (WindNoise_0(vec2(along_0 * 31.0, across_0 * 15.0) + vec2(seed_0, node_0 * 3.70000004768371582)) - 0.5) * 0.60000002384185791), RU_CompatSmoothstep(0.07999999821186066, 0.44999998807907104, life_0) * RU_CompatSmoothstep(entry_0, min(entry_0 + 0.05999999865889549, 1.05999994277954102), along_0));

#line 145
        n_0 = n_0 + 1;

#line 145
    }

#line 145
    return result_0;
}


#line 145
layout(location = 0)
out vec4 entryPointParam_frag_0;


#line 145
layout(location = 1) in vec2 i_uv_0; // @vertex TexCoord0 optional


#line 170
void main()
{

#line 170
    float _S5;

#line 170
    if((RU_Material_0._PreviewClockEnabled) > 0.5)
    {

#line 170
        _S5 = RU_Material_0._PreviewSeconds;

#line 170
    }
    else
    {

#line 170
        if((RU_Material_0._UseRealTime) > 0.5)
        {

#line 170
            _S5 = RU_Material_0._Time.y;

#line 170
        }
        else
        {

#line 170
            _S5 = 0.0;

#line 170
        }

#line 170
    }

#line 170
    vec2 delta_0 = vec2((i_uv_0.x - RU_Material_0._HeadU) * RU_Material_0._CanvasAspect, i_uv_0.y - RU_Material_0._HeadV);

#line 170
    vec3 _S6 = vec3(ivec3(0));

#line 170
    int _S7 = int(floor(RU_Material_0._LayerCount + 0.5));

#line 170
    int layer_0 = 0;

#line 170
    float alpha_0 = 0.0;

#line 170
    vec3 premul_0 = _S6;

#line 170
    [[dont_unroll]]
    for(;;)
    {

#line 170
        if(layer_0 < 6)
        {
        }
        else
        {

#line 170
            break;
        }

#line 170
        if(layer_0 >= _S7)
        {

#line 170
            break;
        }

#line 170
        float index_0 = float(layer_0);

#line 170
        float r1_1 = WindHash_0(vec2(index_0 + RU_Material_0._FlowSeed, 12.47000026702880859));

#line 170
        float side_1;

#line 170
        if((layer_0 % 2) == 1)
        {

#line 170
            side_1 = -1.0;

#line 170
        }
        else
        {

#line 170
            side_1 = 1.0;

#line 170
        }

#line 170
        float baseDegrees_0;

#line 170
        if(layer_0 == 0)
        {

#line 170
            baseDegrees_0 = 22.0;

#line 170
        }
        else
        {

#line 170
            if(layer_0 == 1)
            {

#line 170
                baseDegrees_0 = 27.0;

#line 170
            }
            else
            {

#line 170
                if(layer_0 == 2)
                {

#line 170
                    baseDegrees_0 = 13.0;

#line 170
                }
                else
                {

#line 170
                    if(layer_0 == 3)
                    {

#line 170
                        baseDegrees_0 = 18.0;

#line 170
                    }
                    else
                    {

#line 170
                        if(layer_0 == 4)
                        {

#line 170
                            baseDegrees_0 = 31.0;

#line 170
                        }
                        else
                        {

#line 170
                            baseDegrees_0 = 35.0;

#line 170
                        }

#line 170
                    }

#line 170
                }

#line 170
            }

#line 170
        }

#line 170
        float angle_0 = radians(baseDegrees_0 + RU_Material_0._AngleOffset);

#line 170
        float _S8 = cos(angle_0);

#line 170
        float _S9 = sin(angle_0);

#line 170
        vec2 outward_0 = vec2(_S9, side_1 * _S8);

#line 170
        vec2 groupDelta_0 = delta_0 - vec2(0.0, side_1 * RU_Material_0._GroupSeparation);

#line 170
        float along_1 = dot(groupDelta_0, vec2(- _S8, side_1 * _S9));

#line 170
        float _S10 = RU_Material_0._FlowLength;

#line 170
        bool _S11 = layer_0 == 2;

#line 170
        float _S12;

#line 170
        if(_S11)
        {

#line 170
            _S12 = 0.8399999737739563;

#line 170
        }
        else
        {

#line 170
            _S12 = 1.0;

#line 170
        }

#line 170
        float t_1 = along_1 / (_S10 * _S12);

#line 170
        float _S13 = saturate_1(t_1);

#line 170
        float _S14 = 1.0 - _S13;

#line 170
        float across_1 = dot(groupDelta_0, outward_0) - - RU_Material_0._InwardCurve * 4.0 * _S13 * _S14;

#line 170
        float _S15 = RU_Material_0._FlowWidth;

#line 170
        float _S16;

#line 170
        if(_S11)
        {

#line 170
            _S16 = 0.75;

#line 170
        }
        else
        {

#line 170
            _S16 = 1.0;

#line 170
        }

#line 170
        float width_0 = _S15 * _S16;

#line 170
        bool _S17;

#line 170
        if(t_1 <= -0.00999999977648258)
        {

#line 170
            _S17 = true;

#line 170
        }
        else
        {

#line 170
            _S17 = t_1 >= 1.0;

#line 170
        }

#line 170
        bool _S18;

#line 170
        if(_S17)
        {

#line 170
            _S18 = true;

#line 170
        }
        else
        {

#line 170
            _S18 = (abs(across_1)) > (width_0 * (0.5 + RU_Material_0._NoiseStrength * 0.5));

#line 170
        }

#line 170
        if(_S18)
        {

#line 170
            layer_0 = layer_0 + 1;

#line 170
            continue;
        }

#line 170
        vec2 shapeUv_0 = vec2(_S14 - 0.14000000059604645 * sin(6.28318548202514648 * _S13), 0.5 + across_1 / width_0);

#line 170
        vec2 noiseUv_0 = shapeUv_0 * RU_Material_0._NoiseScale + _S5 * vec2(RU_Material_0._PannerX, RU_Material_0._PannerY) + index_0 * vec2(0.17000000178813934, 0.28999999165534973);

#line 170
        vec2 distorted_0 = shapeUv_0 + (vec2(RU_tex2D_StartTex_0(noiseUv_0).x, RU_tex2D_StartTex_0(noiseUv_0 + vec2(0.37000000476837158, 0.61000001430511475)).x) - 0.5) * RU_Material_0._NoiseStrength * RU_CompatSmoothstep(0.01999999955296516, 0.25, t_1);

#line 170
        float mask_0 = pow(WindTextureMask_0(RU_tex2D_MainTex_0(saturate_0(distorted_0))), RU_Material_0._ShapeContrast * 0.80000001192092896);

#line 170
        float _S19 = distorted_0.x;

#line 170
        float _S20 = distorted_0.y;

#line 170
        float inside_0 = step(0.0, _S19) * step(_S19, 1.0) * step(0.0, _S20) * step(_S20, 1.0);

#line 170
        float phase_0 = _S5 * RU_Material_0._DissolveSpeed * 0.64999997615814209 + (index_0 + 0.20000000298023224) / max(RU_Material_0._LayerCount, 1.0) + r1_1 * 0.11999999731779099;

#line 170
        float _S21 = max(FlowPacket_0(fract(phase_0), t_1), FlowPacket_0(fract(phase_0 + 0.5), t_1) * 0.75);

#line 170
        float _S22 = RU_CompatSmoothstep(RU_Material_0._FadeStart, min(RU_Material_0._FadeStart + 0.07999999821186066, 1.00999999046325684), t_1) * saturate_1(RU_Material_0._Erosion / 0.44999998807907104);

#line 170
        float region_0;

#line 170
        if((RU_Material_0._PreviewNodeMode) > 0.5)
        {

#line 170
            region_0 = 0.0;

#line 170
        }
        else
        {

#line 170
            region_0 = _S22;

#line 170
        }

#line 170
        float a_0 = mask_0 * inside_0 * RU_CompatSmoothstep(-0.00999999977648258, 0.07000000029802322, t_1) * (1.0 - RU_CompatSmoothstep(0.77999997138977051, 1.0, t_1)) * mix(1.0, 0.30000001192092896 + 0.69999998807907104 * _S21, region_0);

#line 170
        if(a_0 <= 0.0)
        {

#line 170
            layer_0 = layer_0 + 1;

#line 170
            continue;
        }

#line 170
        float nodes_0 = WindNodeMask_0(t_1, across_1 / max(width_0, 0.00999999977648258), _S5, RU_Material_0._DissolveSpeed, RU_Material_0._FlowSeed + index_0 * 7.19000005722045898, RU_Material_0._FadeStart, RU_Material_0._Erosion, RU_Material_0._DissolveNodes);

#line 170
        float _S23;

#line 170
        if(_S11)
        {

#line 170
            _S23 = 0.64999997615814209;

#line 170
        }
        else
        {

#line 170
            _S23 = 1.0;

#line 170
        }

#line 170
        float a_1 = a_0 * (nodes_0 * _S23);

#line 170
        float _S24 = 1.0 - alpha_0;

#line 170
        vec3 premul_1 = premul_0 + mix(RU_Material_0._DeepColor.xyz, RU_Material_0._HotColor.xyz, vec3(pow(saturate_1(mask_0), 0.69999998807907104))) * a_1 * _S24;

#line 170
        alpha_0 = alpha_0 + a_1 * _S24;

#line 170
        premul_0 = premul_1;

#line 170
        layer_0 = layer_0 + 1;

#line 170
    }

#line 170
    float core_0 = exp(- dot(delta_0, delta_0) / (RU_Material_0._CoreRadius * RU_Material_0._CoreRadius) * 2.20000004768371582) * RU_Material_0._CoreBrightness;

#line 170
    entryPointParam_frag_0 = vec4((premul_0 / max(alpha_0, 0.00000999999974738) + RU_Material_0._HotColor.xyz * core_0) * (RU_Material_0._Brightness / 0.40000000596046448) * RU_Material_0._Color.xyz, saturate_1(alpha_0 + core_0) * (RU_CompatSmoothstep(0.0, 0.02500000037252903, i_uv_0.x) * (1.0 - RU_CompatSmoothstep(0.97500002384185791, 1.0, i_uv_0.x)) * RU_CompatSmoothstep(0.0, 0.02500000037252903, i_uv_0.y) * (1.0 - RU_CompatSmoothstep(0.97500002384185791, 1.0, i_uv_0.y))) * RU_Material_0._Opacity * RU_Material_0._Color.w);

#line 170
    return;
}


// @rimunity-host-matrices 1
