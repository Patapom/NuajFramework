// This shader downscales the ZBuffer by 2, keeping the MAX value each time
//
#include "../Camera.fx"
#include "../Samplers.fx"
#include "../ReadableZBufferSupport.fx"

float2		_BufferInvSize;

struct VS_IN
{
	float4	Position : SV_POSITION;
};

VS_IN	VS( VS_IN _In ) { return _In; }

// ===================================================================================
// First pass reads back linear Z from ZBuffer
float	ReadDepthNearest( float2 _UV )
{
	float	Zproj = ZBuffer.SampleLevel( NearestClamp, _UV, 0 ).x;
	float	Q = CameraData.w / (CameraData.w - CameraData.z);	// Zf / (Zf-Zn)
	return (Q * CameraData.z) / (Q - Zproj);
}

float	PS0( VS_IN _In ) : SV_TARGET0
{
	_In.Position.xy = 0.5 + 2.0 * floor( _In.Position.xy );
	float2	UV = _In.Position.xy * ZBufferInvSize;

	float	Z00 = ReadDepthNearest( UV );	UV.x += ZBufferInvSize.x;
	float	Z01 = ReadDepthNearest( UV );	UV.y += ZBufferInvSize.y;
	float	Z11 = ReadDepthNearest( UV );	UV.x -= ZBufferInvSize.x;
	float	Z10 = ReadDepthNearest( UV );

	return max( max( max( Z00, Z01 ), Z10 ), Z11 );
}

// ===================================================================================
// Standard pass reads back linear Z from previous pass
Texture2D	_SourceBuffer;

float	PS1( VS_IN _In ) : SV_TARGET0
{
	_In.Position.xy = 0.5 + 2.0 * floor( _In.Position.xy );
	float2	UV = _In.Position.xy * _BufferInvSize;

	float	Z00 = _SourceBuffer.SampleLevel( NearestClamp, UV, 0.0 ).x;	UV.x += _BufferInvSize.x;
	float	Z01 = _SourceBuffer.SampleLevel( NearestClamp, UV, 0.0 ).x;	UV.y += _BufferInvSize.y;
	float	Z11 = _SourceBuffer.SampleLevel( NearestClamp, UV, 0.0 ).x;	UV.x -= _BufferInvSize.x;
	float	Z10 = _SourceBuffer.SampleLevel( NearestClamp, UV, 0.0 ).x;

	return max( max( max( Z00, Z01 ), Z10 ), Z11 );
}

// ===================================================================================
technique10 DownscaleFirstPass
{
	pass P0
	{
		SetVertexShader( CompileShader( vs_4_0, VS() ) );
		SetGeometryShader( 0 );
		SetPixelShader( CompileShader( ps_4_0, PS0() ) );
	}
}

technique10 Downscale
{
	pass P0
	{
		SetVertexShader( CompileShader( vs_4_0, VS() ) );
		SetGeometryShader( 0 );
		SetPixelShader( CompileShader( ps_4_0, PS1() ) );
	}
}
