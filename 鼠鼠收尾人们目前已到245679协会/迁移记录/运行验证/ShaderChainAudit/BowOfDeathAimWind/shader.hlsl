// @rimunity-vertex 0 Position required
// @rimunity-vertex 1 TexCoord0 optional
// @rimunity-vertex 2 Color optional
// @rimunity-vertex 3 Normal optional
// @rimunity-vertex 4 Tangent optional
// @rimunity-vertex 5 TexCoord1 optional
// @rimunity-vertex 6 TexCoord2 optional
// @rimunity-vertex 7 TexCoord3 optional
// @rimunity-vertex 8 TexCoord4 optional
// @rimunity-vertex 9 TexCoord5 optional
// @rimunity-vertex 10 TexCoord6 optional
// @rimunity-vertex 11 TexCoord7 optional
// @rimunity-vertex 12 BlendWeight optional
// @rimunity-vertex 13 BlendIndices optional
struct RU_CameraData { float4x4 ViewProj; };
[[vk::binding(0, 0)]] ConstantBuffer<RU_CameraData> RU_Camera;
struct RU_ObjectData { float4x4 model; };
[[vk::push_constant]] ConstantBuffer<RU_ObjectData> RU_Object;
#define unity_ObjectToWorld RU_Object.model
#define UNITY_MATRIX_M RU_Object.model
#define UNITY_MATRIX_VP RU_Camera.ViewProj
#define UNITY_MATRIX_MVP mul(RU_Camera.ViewProj, RU_Object.model)
float4 UnityObjectToClipPos(float4 p) { return mul(RU_Camera.ViewProj, mul(RU_Object.model, p)); }
float4 UnityObjectToClipPos(float3 p) { return UnityObjectToClipPos(float4(p, 1)); }
float3 TransformObjectToWorld(float3 p) { return mul(RU_Object.model, float4(p, 1)).xyz; }
float4 TransformWorldToHClip(float3 p) { return mul(RU_Camera.ViewProj, float4(p, 1)); }
float4 UnityWorldToClipPos(float3 p) { return TransformWorldToHClip(p); }
float3 UnityObjectToWorldNormal(float3 normal) {
    float3x3 m = (float3x3)RU_Object.model;
    float3x3 cofactors = float3x3(cross(m[1],m[2]), cross(m[2],m[0]), cross(m[0],m[1]));
    return normalize(mul(cofactors, normal) * sign(dot(m[0],cofactors[0])));
}
float3 UnityObjectToWorldDir(float3 direction) { return normalize(mul((float3x3)RU_Object.model, direction)); }
float4 TransformObjectToHClip(float3 p) { return UnityObjectToClipPos(p); }
float4 ComputeScreenPos(float4 p) { return float4(p.xy * 0.5 + p.w * 0.5, p.zw); }
#define TRANSFORM_TEX(uv,name) ((uv).xy * name##_ST.xy + name##_ST.zw)
#define UNITY_INITIALIZE_OUTPUT(type,name) name = (type)0
//非雾、非实例化、单眼变体在 Unity 中也不产生这些字段和调用。
#define UNITY_FOG_COORDS(index)
#define UNITY_TRANSFER_FOG(output,position)
#define UNITY_APPLY_FOG(coord,color)
#define UNITY_VERTEX_INPUT_INSTANCE_ID
#define UNITY_SETUP_INSTANCE_ID(input)
#define UNITY_TRANSFER_INSTANCE_ID(input,output)
#define UNITY_VERTEX_OUTPUT_STEREO
#define UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output)
#define SAMPLE_TEXTURE2D(tex,samp,uv) tex.Sample(samp,float2((uv).x,1-(uv).y))
#define SAMPLE_TEXTURE2D_LOD(tex,samp,uv,lod) tex.SampleLevel(samp,float2((uv).x,1-(uv).y),lod)
#define SAMPLE_TEXTURE2D_BIAS(tex,samp,uv,bias) tex.SampleBias(samp,float2((uv).x,1-(uv).y),bias)
float4 ComputeGrabScreenPos(float4 p) { float4 r = p * 0.5; r.xy = float2(r.x, -r.y) + r.w; r.zw = p.zw; return r; }
float3 UnpackNormal(float4 n) { n.x *= n.w; float2 xy = n.xy * 2 - 1; return float3(xy, sqrt(1 - saturate(dot(xy, xy)))); }
float UnityGet2DClipping(float2 p, float4 r) { float2 v = step(r.xy,p)*step(p,r.zw); return v.x*v.y; }
struct appdata_base { [[vk::location(0)]] float4 vertex : POSITION; [[vk::location(3)]] float3 normal : NORMAL; [[vk::location(1)]] float4 texcoord : TEXCOORD0; };
struct appdata_tan { [[vk::location(0)]] float4 vertex : POSITION; [[vk::location(4)]] float4 tangent : TANGENT; [[vk::location(3)]] float3 normal : NORMAL; [[vk::location(1)]] float4 texcoord : TEXCOORD0; };
struct appdata_full { [[vk::location(0)]] float4 vertex : POSITION; [[vk::location(4)]] float4 tangent : TANGENT; [[vk::location(3)]] float3 normal : NORMAL; [[vk::location(1)]] float4 texcoord : TEXCOORD0; [[vk::location(5)]] float4 texcoord1 : TEXCOORD1; [[vk::location(6)]] float4 texcoord2 : TEXCOORD2; [[vk::location(7)]] float4 texcoord3 : TEXCOORD3; [[vk::location(2)]] float4 color : COLOR; };
struct RU_MaterialData {
float4 _Color;
float _EmitPeriod;
float4 _HeadColor;
float _HeadGlow;
float _ParticleCount;
float _PreviewClockEnabled;
float _PreviewSeconds;
float _SmokeCut;
float _SmokeScroll;
float _SmokeSoft;
float _SmokeStrength;
float _SmokeWarp;
float _SpeedCurve;
float _StreakAngle;
float _StreakBrightness;
float4 _StreakColor;
float _StreakLength;
float _StreakWidth;
float4 _Time;
float _TravelDistance;
float _Turbulence;
float _TurbulenceSpeed;
float _UseRealTime;
};
[[vk::binding(0, 2)]] ConstantBuffer<RU_MaterialData> RU_Material;
// @rimunity-material _Color,_EmitPeriod,_HeadColor,_HeadGlow,_ParticleCount,_PreviewClockEnabled,_PreviewSeconds,_SmokeCut,_SmokeScroll,_SmokeSoft,_SmokeStrength,_SmokeWarp,_SpeedCurve,_StreakAngle,_StreakBrightness,_StreakColor,_StreakLength,_StreakWidth,_Time,_TravelDistance,_Turbulence,_TurbulenceSpeed,_UseRealTime
#define _PreviewSeconds (RU_Material._PreviewSeconds)
#define _PreviewClockEnabled (RU_Material._PreviewClockEnabled)
#define _UseRealTime (RU_Material._UseRealTime)
#define _SmokeWarp (RU_Material._SmokeWarp)
#define _SmokeSoft (RU_Material._SmokeSoft)
#define _SmokeCut (RU_Material._SmokeCut)
#define _SmokeScroll (RU_Material._SmokeScroll)
#define _SmokeStrength (RU_Material._SmokeStrength)
#define _TurbulenceSpeed (RU_Material._TurbulenceSpeed)
#define _Turbulence (RU_Material._Turbulence)
#define _StreakBrightness (RU_Material._StreakBrightness)
#define _HeadGlow (RU_Material._HeadGlow)
#define _SpeedCurve (RU_Material._SpeedCurve)
#define _TravelDistance (RU_Material._TravelDistance)
#define _StreakAngle (RU_Material._StreakAngle)
#define _StreakWidth (RU_Material._StreakWidth)
#define _StreakLength (RU_Material._StreakLength)
#define _EmitPeriod (RU_Material._EmitPeriod)
#define _ParticleCount (RU_Material._ParticleCount)
#define _HeadColor (RU_Material._HeadColor)
#define _StreakColor (RU_Material._StreakColor)
#define _Color (RU_Material._Color)
#define _Time (RU_Material._Time)
[[vk::binding(1, 2)]] Texture2D _MainTex;
// @rimunity-texture 1 _MainTex
[[vk::binding(2, 2)]] SamplerState RU_Sampler_MainTex;
// @rimunity-combined 1 2
float4 RU_tex2D_MainTex(float2 uv) { return _MainTex.Sample(RU_Sampler_MainTex, float2(uv.x,1-uv.y)); }
float4 RU_tex2D_MainTex(float3 uv) { return RU_tex2D_MainTex(uv.xy); }
float4 RU_tex2D_MainTex(float4 uv) { return RU_tex2D_MainTex(uv.xy); }
float4 RU_tex2Dlod_MainTex(float4 uv) { return _MainTex.SampleLevel(RU_Sampler_MainTex, float2(uv.x,1-uv.y),uv.w); }
float4 RU_tex2Dbias_MainTex(float4 uv) { return _MainTex.SampleBias(RU_Sampler_MainTex, float2(uv.x,1-uv.y),uv.w); }
float4 RU_tex2Dproj_MainTex(float4 uv) { return _MainTex.Sample(RU_Sampler_MainTex, float2(uv.x/uv.w,1-uv.y/uv.w)); }
float4 RU_tex2Dgrad_MainTex(float2 uv,float2 dx,float2 dy) { return _MainTex.SampleGrad(RU_Sampler_MainTex,float2(uv.x,1-uv.y),float2(dx.x,-dx.y),float2(dy.x,-dy.y)); }

 

 




 



 



 

 static const int ParticleSlots = 12 ; static const float EmitX = 0.9 ; struct appdata { [[vk::location(0)]] float4 vertex : POSITION ; [[vk::location(1)]] float2 uv : TEXCOORD0 ; } ; struct v2f { float4 pos : SV_POSITION ; [[vk::location(1)]] float2 uv : TEXCOORD0 ; } ; v2f vert ( appdata v ) { v2f o ; o . pos = UnityObjectToClipPos ( v . vertex ) ; o . uv = v . uv ; return o ; } float Hash ( float2 p ) { float3 q = frac ( float3 ( p . x , p . y , p . x ) * .1031 ) ; q += dot ( q , q . yzx + 33.33 ) ; return frac ( ( q . x + q . y ) * q . z ) ; } float Noise ( float2 p ) { float2 c = floor ( p ) , f = frac ( p ) ; f = f * f * ( 3.0 - 2.0 * f ) ; return lerp ( lerp ( Hash ( c ) , Hash ( c + float2 ( 1 , 0 ) ) , f . x ) , lerp ( Hash ( c + float2 ( 0 , 1 ) ) , Hash ( c + 1.0 ) , f . x ) , f . y ) ; } float Fbm ( float2 p ) { return Noise ( p ) * .57 + Noise ( p * 2.03 + 7.1 ) * .28 + Noise ( p * 4.11 + 19.4 ) * .15 ; } float4 frag ( v2f i ) : SV_Target { float seconds = _PreviewClockEnabled > .5 ? _PreviewSeconds : ( _UseRealTime > .5 ? _Time . y : 0.0 ) ; float2 uv = i . uv ; float angleRadians = _StreakAngle * 0.0174532924 ; float2 drift = float2 ( - cos ( angleRadians ) , sin ( angleRadians ) ) ; float2 cross = float2 ( - drift . y , drift . x ) ; float3 rgb = 0.0 ; float alpha = 0.0 ; {for ( int slot = 0 ; slot < ParticleSlots ; slot ++ ) { float index = ( float ) slot ; float enabled = step ( index + 0.5 , _ParticleCount ) ; float seedY = Hash ( float2 ( index , 7.3 ) ) ; float seedPhase = Hash ( float2 ( index , 19.1 ) ) ; float seedLength = Hash ( float2 ( index , 31.7 ) ) ; float life = frac ( seconds / max ( _EmitPeriod , 0.001 ) + seedPhase ) ; float travelCurve = lerp ( life , 1.0 - ( 1.0 - life ) * ( 1.0 - life ) , _SpeedCurve ) ; float2 head = float2 ( EmitX , 0.16 + seedY * 0.5 ) + drift * ( _TravelDistance * travelCurve ) ; float2 turbulence = float2 ( Fbm ( float2 ( index * 3.1 , seconds * _TurbulenceSpeed ) ) - 0.5 , Noise ( float2 ( index * 3.1 + 13.7 , seconds * _TurbulenceSpeed + 7.1 ) ) - 0.5 ) ; head += turbulence * ( _Turbulence * 2.0 ) ; float2 relative = uv - head ; float along = dot ( relative , drift ) ; float across = dot ( relative , cross ) ; float streakLength = _StreakLength * ( 0.7 + 0.6 * seedLength ) ; float tail = saturate ( - along / max ( streakLength , 0.001 ) ) ; float body = sin ( 3.14159265 * saturate ( 0.08 + tail * 0.86 ) ) ; float section = exp ( - pow ( across / max ( _StreakWidth , 0.001 ) , 2.0 ) ) ; float headDot = exp ( - pow ( length ( relative ) / max ( _StreakWidth * 1.4 , 0.001 ) , 2.0 ) ) ; float fade = smoothstep ( 0.0 , 0.16 , life ) * smoothstep ( 1.0 , 0.74 , life ) ; float streak = ( body * section * _StreakBrightness + headDot * _HeadGlow ) * fade * enabled ; rgb += lerp ( _StreakColor . rgb , _HeadColor . rgb , saturate ( headDot * 1.5 ) ) * streak ; alpha += streak ; }} float scroll = frac ( seconds * _SmokeScroll ) ; float2 warp = ( float2 ( Fbm ( uv * 2.5 + seconds * 0.2 ) , Fbm ( uv * 2.5 + float2 ( 31.7 , - seconds * 0.15 ) ) ) - 0.5 ) * _SmokeWarp ; float4 smoke = RU_tex2D_MainTex( uv + warp + float2 ( scroll , 0.0 ) ) ; float smokeAlpha = smoothstep ( _SmokeCut , _SmokeCut + max ( _SmokeSoft , 0.001 ) , smoke . a ) * _SmokeStrength ; rgb += smoke . rgb * smokeAlpha ; alpha += smokeAlpha ; float edge = smoothstep ( 0.0 , 0.03 , uv . x ) * smoothstep ( 1.0 , 0.97 , uv . x ) * smoothstep ( 0.0 , 0.03 , uv . y ) * smoothstep ( 1.0 , 0.97 , uv . y ) ; return float4 ( rgb * _Color . rgb , saturate ( alpha * edge ) * _Color . a ) ; } 
