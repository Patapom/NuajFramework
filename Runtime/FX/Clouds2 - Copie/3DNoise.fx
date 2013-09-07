// ===================================================================================
// Noise & Random sampling
// (Requires Samplers.fx)

float3		_NoiseSize;
float2		_NoiseAmplitudeFactor;						// X=Amplitude Factor Y=1/SumAmplitudes for noise normalizing
float2		_NoiseFrequencyFactor;						// X=Frequency Factor Y=Mip level increment
float		_CloudTime;
float		_CloudSpeed;
float		_CloudEvolutionSpeed;

Texture3D	_NoiseTexture0 : NOISE3D_TEX0;
// Texture3D	_NoiseTexture1 : NOISE3D_TEX1;
// Texture3D	_NoiseTexture2 : NOISE3D_TEX2;
// Texture3D	_NoiseTexture3 : NOISE3D_TEX3;


SamplerState VolumeSampler
{
	Filter = MIN_MAG_MIP_LINEAR;
	AddressU = Wrap;
//	AddressV = Clamp;	// Vertically, the noise doesn't wrap so clouds have a solid top and bottom limit
	AddressV = Wrap;
	AddressW = Wrap;
};

// ======== This one is shit BUT remember we're having a 200% acceleration !!! ========
// Can't we use a better one, pure ALU, no texture ?
float	Noise_( Texture3D _Texture, float3 _UVW, float _MipLevel )
{
	_UVW *= 0.000001;
//	return frac( sin( dot( _UVW, float3( 12.9898, 18.233, 17.987 ) ) ) * 43758.5453 );
	return frac( (sin( _UVW.x * 12.9898 ) * sin( _UVW.z * 17.987 )) * 43758.5453 );

// return 0.5 * (1.0 + NoiseTable( 100.0 * _UVW.xz ));
// 
// 	_UVW *= 1.0;
// 
// 	// Generate wrapping positions on 3 circles
// 	float2	SinCosX;
// 	sincos( _UVW.x, SinCosX.x, SinCosX.y );
// 	float2	SinCosY;
// 	sincos( _UVW.y, SinCosY.x, SinCosY.y );
// 	float2	SinCosZ;
// 	sincos( _UVW.z, SinCosZ.x, SinCosZ.y );
// 
// 	// Grab a pseudo-random value for each of the 3 circles
// 	float	Radius = 1.0;
// 	float	V = NoiseTable( Radius * SinCosX );
// 			  + NoiseTable( Radius * SinCosY );
// 			  + NoiseTable( Radius * SinCosZ );
// 
// 	return 0.5 * (1.0 + 0.333333 * V);
}

// ======== 3D Perlin noise without textures using noise arrays ========
// NOTE: At some point, it's no more a question of the table size but rather the integer operations that take precedence
//
// 
// // 64 values table
// // float2	NoiseTable[65] = { float2( 0.07932694, 0.5272702 ), float2( 0.8850821, 0.744983 ), float2( 0.606181, 0.9214102 ), float2( 0.9045827, 0.826494 ), float2( 0.7351174, 0.6003946 ), float2( 0.3819631, 0.4133375 ), float2( 0.5348898, 0.1018706 ), float2( 0.04198993, 0.08142311 ), float2( 0.2809114, 0.5369409 ), float2( 0.5272702, 0.5592554 ), float2( 0.3411145, 0.1594635 ), float2( 0.6367293, 0.6677479 ), float2( 0.173324, 0.4330191 ), float2( 0.6677479, 0.2809114 ), float2( 0.1013956, 0.954134 ), float2( 0.6003946, 0.9620884 ), float2( 0.3394723, 0.08251163 ), float2( 0.826494, 0.3394723 ), float2( 0.4133375, 0.9850275 ), float2( 0.7742423, 0.9565349 ), float2( 0.3871005, 0.1819537 ), float2( 0.08251163, 0.3431326 ), float2( 0.9565349, 0.9900361 ), float2( 0.1819537, 0.5348898 ), float2( 0.8159267, 0.5779178 ), float2( 0.9900361, 0.3411145 ), float2( 0.5592554, 0.4473825 ), float2( 0.4330191, 0.1458325 ), float2( 0.1458325, 0.07932694 ), float2( 0.8335289, 0.253925 ), float2( 0.744983, 0.04198993 ), float2( 0.253925, 0.7351174 ), float2( 0.7106014, 0.1179126 ), float2( 0.5369409, 0.4153739 ), float2( 0.1179126, 0.8886978 ), float2( 0.9850275, 0.2163403 ), float2( 0.1594635, 0.8850821 ), float2( 0.2163403, 0.9333043 ), float2( 0.9884116, 0.1013956 ), float2( 0.425655, 0.7450813 ), float2( 0.9333043, 0.8961574 ), float2( 0.3431326, 0.6367293 ), float2( 0.09972321, 0.425655 ), float2( 0.4474381, 0.173324 ), float2( 0.6102111, 0.09972321 ), float2( 0.9620884, 0.2202573 ), float2( 0.4153739, 0.7106014 ), float2( 0.2727067, 0.4474381 ), float2( 0.5779178, 0.8335289 ), float2( 0.2202573, 0.3819631 ), float2( 0.08142311, 0.08865602 ), float2( 0.03863042, 0.9968153 ), float2( 0.8886978, 0.2727067 ), float2( 0.954134, 0.3871005 ), float2( 0.9214102, 0.7742423 ), float2( 0.9968153, 0.8159267 ), float2( 0.9285472, 0.03863042 ), float2( 0.4242272, 0.9045827 ), float2( 0.08865602, 0.8227077 ), float2( 0.7450813, 0.606181 ), float2( 0.8227077, 0.9884116 ), float2( 0.1018706, 0.4242272 ), float2( 0.8961574, 0.6102111 ), float2( 0.4473825, 0.9285472 ), float2( 0.07932694, 0.5272702 ) };
// // int		PermutationsTable[128] = { 16, 21, 41, 11, 13, 8, 33, 46, 32, 34, 52, 47, 43, 12, 27, 28, 0, 9, 26, 63, 56, 51, 55, 24, 48, 29, 31, 4, 15, 45, 49, 5, 18, 35, 37, 40, 62, 44, 42, 39, 59, 2, 54, 19, 22, 25, 10, 36, 1, 30, 7, 50, 58, 60, 38, 14, 53, 20, 23, 6, 61, 57, 3, 17, 16, 21, 41, 11, 13, 8, 33, 46, 32, 34, 52, 47, 43, 12, 27, 28, 0, 9, 26, 63, 56, 51, 55, 24, 48, 29, 31, 4, 15, 45, 49, 5, 18, 35, 37, 40, 62, 44, 42, 39, 59, 2, 54, 19, 22, 25, 10, 36, 1, 30, 7, 50, 58, 60, 38, 14, 53, 20, 23, 6, 61, 57, 3, 17 };
// 
// // 16 values table
// float2	NoiseTable[17] = { float2( 0.008474346, 0.2424364 ), float2( 0.5353357, 0.07570706 ), float2( 0.1373777, 0.3660391 ), float2( 0.8970748, 0.550023 ), float2( 0.07570706, 0.772223 ), float2( 0.01877564, 0.8970748 ), float2( 0.8649874, 0.5353357 ), float2( 0.9715052, 0.2209556 ), float2( 0.3660391, 0.9312432 ), float2( 0.4603953, 0.01877564 ), float2( 0.550023, 0.4631462 ), float2( 0.2209556, 0.8649874 ), float2( 0.2424364, 0.9715052 ), float2( 0.772223, 0.4603953 ), float2( 0.4631462, 0.1373777 ), float2( 0.9312432, 0.008474346 ), float2( 0.008474346, 0.2424364 ) };
// int		PermutationsTable[32] = { 9, 5, 3, 10, 14, 2, 8, 15, 0, 12, 7, 11, 6, 1, 4, 13, 9, 5, 3, 10, 14, 2, 8, 15, 0, 12, 7, 11, 6, 1, 4, 13 };
// 
// // 8 values table
// // float2	NoiseTable[9] = { float2( 0.08073366, 0.3193271 ), float2( 0.5424731, 0.881673 ), float2( 0.8163252, 0.5424731 ), float2( 0.7394603, 0.3138514 ), float2( 0.3193271, 0.7394603 ), float2( 0.3138514, 0.8163252 ), float2( 0.881673, 0.6472222 ), float2( 0.6472222, 0.08073366 ), float2( 0.08073366, 0.3193271 ) };
// // int		PermutationsTable[16] = { 4, 3, 5, 2, 1, 6, 7, 0, 4, 3, 5, 2, 1, 6, 7, 0 };
// 
// // 4 values table
// // float2	NoiseTable[5] = { float2( 0.8812907, 0.5229909 ), float2( 0.4151475, 0.9708127 ), float2( 0.5229909, 0.4151475 ), float2( 0.9708127, 0.8812907 ), float2( 0.8812907, 0.5229909 ) };
// // int		PermutationsTable[8] = { 1, 3, 0, 2, 1, 3, 0, 2 };
// 
// float	Noise__( Texture3D _Texture, float3 _UVW, float _MipLevel )
// {
// 	_UVW = 10.0 * (1000.0 + _UVW);
// 
// 	int3	I0 = int3( floor( _UVW ) );
// 	float3	f = _UVW - I0;
// 	I0 &= 15;
// 	int3	I1 = I0 + 1;
// 
// 	f = f * f * f * (10.0 + f * (-15.0 + f * 6.0));
// 
// 	// Here we're using correlated noise tables
// 	// The float2 will contain (Noise[p[a+z]], Noise[p[a+z+1]]) where a is unknown (a mix of permutations between x and y that we don't know of)
// 	float2	V00 = NoiseTable[PermutationsTable[PermutationsTable[PermutationsTable[I0.x]+I0.y]+I0.z]];
// 	float2	V01 = NoiseTable[PermutationsTable[PermutationsTable[PermutationsTable[I1.x]+I0.y]+I0.z]];
// 	float2	V11 = NoiseTable[PermutationsTable[PermutationsTable[PermutationsTable[I1.x]+I1.y]+I0.z]];
// 	float2	V10 = NoiseTable[PermutationsTable[PermutationsTable[PermutationsTable[I0.x]+I1.y]+I0.z]];
// 
// 	// X lerp
// 	float2	V0 = lerp( V00, V01, f.x );
// 	float2	V1 = lerp( V10, V11, f.x );
// 
// 	// Y lerp
// 	float2	V = lerp( V0, V1, f.y );
// 
// 	// Z lerp
// 	return lerp( V.x, V.y, f.z );
// }
// 
// // ======== Another noise without textures ========
// float	Rand( float x )
// {
// 	return frac( sin( 12.9898 * x ) * 43758.5453 );
//// 	return frac( x * 43758.5453 );
// }
// 
// float	Noise( Texture3D _Texture, float3 _UVW, float _MipLevel )
// {
// 	_UVW = 10.0 * (1000.0 + _UVW);
// 
// 	float3	I0 = floor( _UVW );
// 	float3	f = _UVW - I0;
//			f = f * f * f * (10.0 + f * (-15.0 + f * 6.0));
// 
// 			I0 = fmod( I0, 16.0 );
// 	float3	I1 = fmod( I0+1, 16.0 );
// 
// 	float	V000 = Rand( I0.x + 16.0 * (I0.y + 16.0 * I0.z) );
// 	float	V001 = Rand( I1.x + 16.0 * (I0.y + 16.0 * I0.z) );
// 	float	V011 = Rand( I1.x + 16.0 * (I1.y + 16.0 * I0.z) );
// 	float	V010 = Rand( I0.x + 16.0 * (I1.y + 16.0 * I0.z) );
// 	float	V100 = Rand( I0.x + 16.0 * (I0.y + 16.0 * I1.z) );
// 	float	V101 = Rand( I1.x + 16.0 * (I0.y + 16.0 * I1.z) );
// 	float	V111 = Rand( I1.x + 16.0 * (I1.y + 16.0 * I1.z) );
// 	float	V110 = Rand( I0.x + 16.0 * (I1.y + 16.0 * I1.z) );
// 
// 	float	V00 = lerp( V000, V001, f.x );
// 	float	V01 = lerp( V010, V011, f.x );
// 	float	V10 = lerp( V100, V101, f.x );
// 	float	V11 = lerp( V110, V111, f.x );
// 
// 	float	V0 = lerp( V00, V01, f.y );
// 	float	V1 = lerp( V10, V11, f.y );
// 
// 	return lerp( V0, V1, f.z );
// }

// ======== And another noise without textures that attempts to return 4 octaves at the same time !========
// But that doesn't work...
// float4	Rand( float x )
// {
// 	float4	x4 = float4( x, 3.0 * x, 9.0 * x, 27.0 * x );
// //	x4 = fmod( x4, 16.0*16.0*16.0 );
// 
// 	return frac( sin( 12.9898 * x4 ) * 43758.5453 );
// }
// 
// float4	Noise4( float3 _UVW, float _MipLevel )
// {
// 	_UVW = 10.0 * (1000.0 + _UVW);
// 
// 	float3	I0 = floor( _UVW );
// 	float3	f = _UVW - I0;
// 			f = f * f * f * (10.0 + f * (-15.0 + f * 6.0));
// 
// 			I0 = fmod( I0, 16.0 );
// 	float3	I1 = fmod( I0+1, 16.0 );
// 
// 	float4	V000 = Rand( I0.x + 16.0 * (I0.y + 16.0 * I0.z) );
// 	float4	V001 = Rand( I1.x + 16.0 * (I0.y + 16.0 * I0.z) );
// 	float4	V011 = Rand( I1.x + 16.0 * (I1.y + 16.0 * I0.z) );
// 	float4	V010 = Rand( I0.x + 16.0 * (I1.y + 16.0 * I0.z) );
// 	float4	V100 = Rand( I0.x + 16.0 * (I0.y + 16.0 * I1.z) );
// 	float4	V101 = Rand( I1.x + 16.0 * (I0.y + 16.0 * I1.z) );
// 	float4	V111 = Rand( I1.x + 16.0 * (I1.y + 16.0 * I1.z) );
// 	float4	V110 = Rand( I0.x + 16.0 * (I1.y + 16.0 * I1.z) );
// 
// 	float4	V00 = lerp( V000, V001, f.x );
// 	float4	V01 = lerp( V010, V011, f.x );
// 	float4	V10 = lerp( V100, V101, f.x );
// 	float4	V11 = lerp( V110, V111, f.x );
// 
// 	float4	V0 = lerp( V00, V01, f.y );
// 	float4	V1 = lerp( V10, V11, f.y );
// 
// 	return lerp( V0, V1, f.z );
// }
// 
// float	fbm( float3 _UVW, float _MipLevel )
// {
// 	float4	Value  = Noise4( _UVW, _MipLevel );
// 	return _NoiseAmplitudeFactor.y * (Value.x + _NoiseAmplitudeFactor.x * (Value.y + _NoiseAmplitudeFactor.x * (Value.z + _NoiseAmplitudeFactor.x * Value.w)));
// }


// This one uses a 16x16x16 3D texture
float4	Noise( Texture3D _Texture, float3 _UVW, float _MipLevel )
{
    return _Texture.SampleLevel( VolumeSampler, _UVW, _MipLevel );
}

// Dynamic version (4 octaves)
float	fbm( float3 _UVW, float _MipLevel )
{
	float	Value  = Noise( _NoiseTexture0, _UVW, _MipLevel ).x;
	float	Amplitude = _NoiseAmplitudeFactor.x;
	_UVW *= _NoiseFrequencyFactor.x;
//	_MipLevel += _NoiseFrequencyFactor.y;
	_UVW.x += _CloudEvolutionSpeed * _CloudTime;

	Value += Amplitude * Noise( _NoiseTexture0, _UVW, _MipLevel ).y;
	Amplitude *= _NoiseAmplitudeFactor.x;
	_UVW *= _NoiseFrequencyFactor.x;
//	_MipLevel += _NoiseFrequencyFactor.y;
	_UVW.x += _CloudEvolutionSpeed * _CloudTime;

	Value += Amplitude * Noise( _NoiseTexture0, _UVW, _MipLevel ).z;
	Amplitude *= _NoiseAmplitudeFactor.x;
	_UVW *= _NoiseFrequencyFactor.x;
//	_MipLevel += _NoiseFrequencyFactor.y;
	_UVW.x += _CloudEvolutionSpeed * _CloudTime;

	Value += Amplitude * Noise( _NoiseTexture0, _UVW, _MipLevel ).w;
//	Value += 0.25 * _NoiseTexture3.SampleLevel( VolumeSampler, _UVW, _MipLevel ).x;

/*	// ======= Try more octaves ? =======
	Amplitude *= _NoiseAmplitudeFactor.x;
	_UVW *= _NoiseFrequencyFactor.x;
//	_MipLevel += _NoiseFrequencyFactor.y;
	_UVW.x += _CloudEvolutionSpeed * _CloudTime;

	Value += Amplitude * _NoiseTexture3.SampleLevel( VolumeSampler, _UVW, _MipLevel ).x;
	Amplitude *= _NoiseAmplitudeFactor.x;
	_UVW *= _NoiseFrequencyFactor.x;
//	_MipLevel += _NoiseFrequencyFactor.y;
	_UVW.x += _CloudEvolutionSpeed * _CloudTime;

	Value += Amplitude * _NoiseTexture3.SampleLevel( VolumeSampler, _UVW, _MipLevel ).x;
	Amplitude *= _NoiseAmplitudeFactor.x;
	_UVW *= _NoiseFrequencyFactor.x;
//	_MipLevel += _NoiseFrequencyFactor.y;
	_UVW.x += _CloudEvolutionSpeed * _CloudTime;
//*/

	return Value * _NoiseAmplitudeFactor.y;	// Normalize value
}
