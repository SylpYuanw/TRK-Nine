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
float _Brightness;
float4 _Color;
float _CoreBrightness;
float _CoreRadius;
float _Divergence;
float _Erosion;
float _FlowLength;
float _FlowSpeed;
float _HeadU;
float _HeadV;
float _NoiseScale;
float _NoiseSpeed;
float _NoiseStrength;
float _PreviewClockEnabled;
float _PreviewSeconds;
float4 _Time;
float _UseRealTime;
};
[[vk::binding(0, 2)]] ConstantBuffer<RU_MaterialData> RU_Material;
// @rimunity-material _Brightness,_Color,_CoreBrightness,_CoreRadius,_Divergence,_Erosion,_FlowLength,_FlowSpeed,_HeadU,_HeadV,_NoiseScale,_NoiseSpeed,_NoiseStrength,_PreviewClockEnabled,_PreviewSeconds,_Time,_UseRealTime
#define _PreviewSeconds (RU_Material._PreviewSeconds)
#define _PreviewClockEnabled (RU_Material._PreviewClockEnabled)
#define _UseRealTime (RU_Material._UseRealTime)
#define _Brightness (RU_Material._Brightness)
#define _CoreBrightness (RU_Material._CoreBrightness)
#define _CoreRadius (RU_Material._CoreRadius)
#define _Erosion (RU_Material._Erosion)
#define _NoiseSpeed (RU_Material._NoiseSpeed)
#define _NoiseStrength (RU_Material._NoiseStrength)
#define _NoiseScale (RU_Material._NoiseScale)
#define _FlowSpeed (RU_Material._FlowSpeed)
#define _Divergence (RU_Material._Divergence)
#define _FlowLength (RU_Material._FlowLength)
#define _HeadV (RU_Material._HeadV)
#define _HeadU (RU_Material._HeadU)
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
[[vk::binding(3, 2)]] Texture2D _StartTex;
// @rimunity-texture 3 _StartTex
[[vk::binding(4, 2)]] SamplerState RU_Sampler_StartTex;
// @rimunity-combined 3 4
float4 RU_tex2D_StartTex(float2 uv) { return _StartTex.Sample(RU_Sampler_StartTex, float2(uv.x,1-uv.y)); }
float4 RU_tex2D_StartTex(float3 uv) { return RU_tex2D_StartTex(uv.xy); }
float4 RU_tex2D_StartTex(float4 uv) { return RU_tex2D_StartTex(uv.xy); }
float4 RU_tex2Dlod_StartTex(float4 uv) { return _StartTex.SampleLevel(RU_Sampler_StartTex, float2(uv.x,1-uv.y),uv.w); }
float4 RU_tex2Dbias_StartTex(float4 uv) { return _StartTex.SampleBias(RU_Sampler_StartTex, float2(uv.x,1-uv.y),uv.w); }
float4 RU_tex2Dproj_StartTex(float4 uv) { return _StartTex.Sample(RU_Sampler_StartTex, float2(uv.x/uv.w,1-uv.y/uv.w)); }
float4 RU_tex2Dgrad_StartTex(float2 uv,float2 dx,float2 dy) { return _StartTex.SampleGrad(RU_Sampler_StartTex,float2(uv.x,1-uv.y),float2(dx.x,-dx.y),float2(dy.x,-dy.y)); }

   



 


 

 

 static const int StreamSlots = 6 ; struct appdata { [[vk::location(0)]] float4 vertex : POSITION ; [[vk::location(1)]] float2 uv : TEXCOORD0 ; } ; struct v2f { float4 pos : SV_POSITION ; [[vk::location(1)]] float2 uv : TEXCOORD0 ; } ; v2f vert ( appdata v ) { v2f o ; o . pos = UnityObjectToClipPos ( v . vertex ) ; o . uv = v . uv ; return o ; } float Hash ( float2 p ) { float3 q = frac ( float3 ( p . x , p . y , p . x ) * .1031 ) ; q += dot ( q , q . yzx + 33.33 ) ; return frac ( ( q . x + q . y ) * q . z ) ; } float Noise ( float2 p ) { float2 cell = floor ( p ) ; float2 f = frac ( p ) ; f = f * f * ( 3.0 - 2.0 * f ) ; return lerp ( lerp ( Hash ( cell ) , Hash ( cell + float2 ( 1 , 0 ) ) , f . x ) , lerp ( Hash ( cell + float2 ( 0 , 1 ) ) , Hash ( cell + 1.0 ) , f . x ) , f . y ) ; } float Fbm ( float2 p ) { return Noise ( p ) * .57 + Noise ( p * 2.03 + 7.1 ) * .28 + Noise ( p * 4.11 + 19.4 ) * .15 ; } float2 StreamCenter ( float stream , float t , float seconds , float seed ) { float branch = ( Hash ( float2 ( stream , 17.7 ) ) - 0.5 ) * 2.0 ; float bendNoise = Fbm ( float2 ( seed + t * _NoiseScale , seconds * _NoiseSpeed * 0.35 + stream * 4.7 ) ) - 0.5 ; float bendWave = sin ( t * 7.0 + seconds * _FlowSpeed * ( 0.55 + seed * 0.35 ) + seed * 13.0 ) * 0.035 ; float v = _HeadV + branch * _Divergence * t * 0.42 + ( bendNoise * _NoiseStrength + bendWave ) * t * t ; return float2 ( _HeadU - t * _FlowLength , v ) ; } float4 frag ( v2f i ) : SV_Target { float seconds = _PreviewClockEnabled > .5 ? _PreviewSeconds : ( _UseRealTime > .5 ? _Time . y : 0.0 ) ; float2 uv = i . uv ; float3 rgb = 0.0 ; float alpha = 0.0 ; float2 coreDelta = uv - float2 ( _HeadU , _HeadV ) ; float coreDistance = length ( coreDelta ) ; float coreNoise = lerp ( 0.88 , 1.08 , Fbm ( uv * 9.0 + seconds * 0.12 ) ) ; float core = exp ( - pow ( coreDistance / max ( _CoreRadius , 0.001 ) , 2.0 ) * 2.2 ) * coreNoise ; rgb += float3 ( 0.9 , 0.045 , 0.02 ) * core * _CoreBrightness ; alpha += core * _CoreBrightness ; {for ( int stream = 0 ; stream < StreamSlots ; stream ++ ) { float slot = ( float ) stream ; float seed = Hash ( float2 ( slot + 3.1 , 41.9 ) ) ; float start = Hash ( float2 ( slot + 9.4 , 13.7 ) ) ; float2 fromHead = float2 ( _HeadU - uv . x , uv . y - _HeadV ) ; float t = saturate ( fromHead . x / max ( _FlowLength , 0.001 ) ) ; float active = step ( 0.0 , fromHead . x ) * step ( fromHead . x , _FlowLength ) ; float2 center = StreamCenter ( slot , t , seconds , seed * 7.0 + start * 3.0 ) ; float2 relative = uv - center ; float width = lerp ( 0.012 , 0.026 , seed ) + _Divergence * t * lerp ( 0.032 , 0.064 , start ) ; float across = relative . y ; float strand = exp ( - pow ( across / max ( width , 0.001 ) , 2.0 ) ) ; float taper = smoothstep ( 0.0 , 0.08 , t ) * smoothstep ( 1.0 , 0.78 , t ) ; float split = smoothstep ( 0.42 , 0.78 , t ) * ( 0.6 + 0.4 * step ( 0.45 , frac ( slot * 0.37 ) ) ) ; float localNoise = Fbm ( ( uv + float2 ( seconds * _NoiseSpeed * 0.17 , - seconds * _NoiseSpeed * 0.11 ) ) * _NoiseScale * 3.2 + slot * 5.3 ) ; float edgeMargin = saturate ( 1.0 - abs ( across ) / max ( width , 0.001 ) ) ; float erosion = lerp ( 1.0 , smoothstep ( 0.16 , 0.7 , localNoise + edgeMargin * 0.42 ) , _Erosion ) ; float textureU = saturate ( lerp ( _HeadU , uv . x , 0.82 ) ) ; float textureV = saturate ( _HeadV + ( uv . y - center . y ) * 1.6 ) ; float4 bodyTex = RU_tex2D_MainTex( float2 ( textureU , textureV ) ) ; float4 headTex = RU_tex2D_StartTex( float2 ( textureU , textureV ) ) ; float textureMask = lerp ( 0.72 , 1.0 , saturate ( max ( bodyTex . a , headTex . a ) * 2.0 ) ) ; float streamMask = strand * taper * erosion * active * textureMask ; float3 streamColor = lerp ( float3 ( 0.5 , 0.012 , 0.006 ) , float3 ( 0.96 , 0.075 , 0.025 ) , saturate ( 1.0 - t * 0.55 ) ) ; rgb += streamColor * streamMask * lerp ( 0.7 , 1.0 , 1.0 - t * 0.25 ) ; alpha += streamMask * ( 0.42 + 0.18 * ( 1.0 - t ) ) ; float haze = exp ( - pow ( across / max ( width * 3.6 , 0.001 ) , 2.0 ) ) * taper * active * ( 0.08 + 0.08 * split ) ; rgb += float3 ( 0.42 , 0.012 , 0.006 ) * haze ; alpha += haze ; }} float edge = smoothstep ( 0.0 , 0.025 , uv . x ) * smoothstep ( 1.0 , 0.975 , uv . x ) * smoothstep ( 0.0 , 0.025 , uv . y ) * smoothstep ( 1.0 , 0.975 , uv . y ) ; return float4 ( rgb * _Color . rgb * _Brightness , saturate ( alpha * edge * _Color . a ) ) ; } 
