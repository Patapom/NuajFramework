using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using SharpDX;
using SharpDX.Direct3D10;
using SharpDX.DXGI;

namespace Nuaj
{
	/// <summary>
	/// This is the base ShaderInterface class
	/// </summary>
	public abstract class	ShaderInterfaceBase : IShaderInterface
	{
		#region FIELDS

		private Dictionary<string,Variable>	m_Semantic2Variable = new Dictionary<string,Variable>();

		#endregion

		#region METHODS

		#region IShaderInterface Members

		public void  SetEffectVariable(string _Semantic, Variable _Variable)
		{
 			m_Semantic2Variable.Add( _Semantic, _Variable );
		}

		#endregion

		protected void	SetScalar( string _Semantic, bool _Value )
		{
			if ( m_Semantic2Variable.ContainsKey( _Semantic ) )
				m_Semantic2Variable[_Semantic].AsScalar.Set( _Value );
		}

		protected void	SetScalar( string _Semantic, int _Value )
		{
			if ( m_Semantic2Variable.ContainsKey( _Semantic ) )
				m_Semantic2Variable[_Semantic].AsScalar.Set( _Value );
		}

		protected void	SetScalar( string _Semantic, float _Value )
		{
			if ( m_Semantic2Variable.ContainsKey( _Semantic ) )
				m_Semantic2Variable[_Semantic].AsScalar.Set( _Value );
		}

		protected void	SetVector( string _Semantic, Vector2 _Value )
		{
			if ( m_Semantic2Variable.ContainsKey( _Semantic ) )
				m_Semantic2Variable[_Semantic].AsVector.Set( _Value );
		}

		protected void	SetVector( string _Semantic, Vector3 _Value )
		{
// 			if ( m_Semantic2Variable == null )
// 				throw new Exception( "BOUH 0 !" );
// 			if ( !m_Semantic2Variable.ContainsKey( _Semantic ) )
// 				throw new Exception( "BOUH 1 !" );
// 			if ( m_Semantic2Variable[_Semantic].AsVector == null )
// 				throw new Exception( "BOUH 2 ! Type of variable is " + m_Semantic2Variable[_Semantic].GetType().FullName );

			if ( m_Semantic2Variable.ContainsKey( _Semantic ) )
				m_Semantic2Variable[_Semantic].AsVector.Set( _Value );
		}

		protected void	SetVector( string _Semantic, Vector4 _Value )
		{
			if ( m_Semantic2Variable.ContainsKey( _Semantic ) )
				m_Semantic2Variable[_Semantic].AsVector.Set( _Value );
		}

		protected void	SetVector( string _Semantic, Vector4[] _Value )
		{
			if ( m_Semantic2Variable.ContainsKey( _Semantic ) )
				m_Semantic2Variable[_Semantic].AsVector.Set( _Value );
		}

		protected void	SetColor( string _Semantic, Color3 _Value )
		{
			if ( m_Semantic2Variable.ContainsKey( _Semantic ) )
				m_Semantic2Variable[_Semantic].AsVector.Set( new Vector3( _Value.Red, _Value.Green, _Value.Blue ) );
		}

		protected void	SetColor( string _Semantic, Color4 _Value )
		{
			if ( m_Semantic2Variable.ContainsKey( _Semantic ) )
				m_Semantic2Variable[_Semantic].AsVector.Set( _Value );
		}

		protected void	SetMatrix( string _Semantic, Matrix _Value )
		{
			if ( m_Semantic2Variable.ContainsKey( _Semantic ) )
				m_Semantic2Variable[_Semantic].AsMatrix.SetMatrix( _Value );
		}

		protected void	SetMatrix( string _Semantic, Matrix[] _Value )
		{
			if ( m_Semantic2Variable.ContainsKey( _Semantic ) )
				m_Semantic2Variable[_Semantic].AsMatrix.SetMatrix( _Value );
		}

		protected void	SetResource( string _Semantic, ShaderResourceView _Value )
		{
			if ( m_Semantic2Variable.ContainsKey( _Semantic ) )
				m_Semantic2Variable[_Semantic].AsResource.SetResource( _Value );
		}

		protected void	SetResource( string _Semantic, ITexture2D _Value )
		{
			if ( m_Semantic2Variable.ContainsKey( _Semantic ) )
				m_Semantic2Variable[_Semantic].AsResource.SetResource( _Value != null ? _Value.TextureView : null );
		}

		protected void	SetResource( string _Semantic, ITexture3D _Value )
		{
			if ( m_Semantic2Variable.ContainsKey( _Semantic ) )
				m_Semantic2Variable[_Semantic].AsResource.SetResource( _Value != null ? _Value.TextureView : null );
		}

		#endregion
	}

	/// <summary>
	/// Standard camera interface
	/// </summary>
	public class	ICamera : ShaderInterfaceBase
	{
		[Semantic( "CAMERA2WORLD" )]
		public Matrix		Camera2World	{ set { SetMatrix( "CAMERA2WORLD", value ); } }

		[Semantic( "WORLD2CAMERA" )]
		public Matrix		World2Camera	{ set { SetMatrix( "WORLD2CAMERA", value ); } }

		[Semantic( "WORLD2PROJ" )]
		public Matrix		World2Proj		{ set { SetMatrix( "WORLD2PROJ", value ); } }

		[Semantic( "CAMERA2PROJ" )]
		public Matrix		Camera2Proj		{ set { SetMatrix( "CAMERA2PROJ", value ); } }

		[Semantic( "CAMERA_DATA" )]
		public Vector4		CameraData		{ set { SetVector( "CAMERA_DATA", value ); } }
	}

	/// <summary>
	/// Standard directional light interface
	/// Light data are :
	///	X = Range Min
	///	Y = Range Max
	///	Z = Radius Min
	///	W = Radius Max
	/// </summary>
	public class	IDirectionalLight : ShaderInterfaceBase
	{
		[Semantic( "LIGHT_POSITION" )]
		public virtual Vector3		Position	{ set { SetVector( "LIGHT_POSITION", value ); } }

		[Semantic( "LIGHT_DIRECTION" )]
		public virtual Vector3		Direction	{ set { SetVector( "LIGHT_DIRECTION", value ); } }

		[Semantic( "LIGHT_COLOR" )]
		public virtual Vector4		Color		{ set { SetVector( "LIGHT_COLOR", value ); } }

		[Semantic( "LIGHT_DATA" )]
		public virtual Vector4		Data		{ set { SetVector( "LIGHT_DATA", value ); } }
	}

	/// <summary>
	/// Standard "secondary" directional light interface
	/// Light data are :
	///	X = Range Min
	///	Y = Range Max
	///	Z = Radius Min
	///	W = Radius Max
	/// </summary>
	public class	IDirectionalLight2 : IDirectionalLight
	{
		[Semantic( "LIGHT_POSITION2" )]
		public override Vector3		Position	{ set { SetVector( "LIGHT_POSITION2", value ); } }

		[Semantic( "LIGHT_DIRECTION2" )]
		public override Vector3		Direction	{ set { SetVector( "LIGHT_DIRECTION2", value ); } }

		[Semantic( "LIGHT_COLOR2" )]
		public override Vector4		Color		{ set { SetVector( "LIGHT_COLOR2", value ); } }

		[Semantic( "LIGHT_DATA2" )]
		public override Vector4		Data		{ set { SetVector( "LIGHT_DATA2", value ); } }
	}

	/// <summary>
	/// Standard spot light interface
	/// Light data are :
	///	X = Range Min
	///	Y = Range Max
	///	Z = Cone Angle Min
	///	W = Cone Angle Max
	///	
	/// 2nd Light data are :
	/// X = cos(Cone Angle Min / 2)
	/// Y = cos(Cone Angle Max / 2)
	/// Z/W = 0
	/// </summary>
	public class	ISpotLight : ShaderInterfaceBase
	{
		[Semantic( "SPOTLIGHT_POSITION" )]
		public virtual Vector3		Position	{ set { SetVector( "SPOTLIGHT_POSITION", value ); } }

		[Semantic( "SPOTLIGHT_DIRECTION" )]
		public virtual Vector3		Direction	{ set { SetVector( "SPOTLIGHT_DIRECTION", value ); } }

		[Semantic( "SPOTLIGHT_COLOR" )]
		public virtual Vector4		Color		{ set { SetVector( "SPOTLIGHT_COLOR", value ); } }

		[Semantic( "SPOTLIGHT_DATA" )]
		public virtual Vector4		Data		{ set { SetVector( "SPOTLIGHT_DATA", value ); } }

		[Semantic( "SPOTLIGHT_DATA2" )]
		public virtual Vector4		Data2		{ set { SetVector( "SPOTLIGHT_DATA2", value ); } }
	}

	/// <summary>
	/// Standard point light interface
	/// Light data are :
	///	X = Range Min
	///	Y = Range Max
	///	Z,W = 0
	/// </summary>
	public class	IPointLight : ShaderInterfaceBase
	{
		[Semantic( "POINTLIGHT_POSITION" )]
		public virtual Vector3		Position	{ set { SetVector( "POINTLIGHT_POSITION", value ); } }

		[Semantic( "POINTLIGHT_COLOR" )]
		public virtual Vector4		Color		{ set { SetVector( "POINTLIGHT_COLOR", value ); } }

		[Semantic( "POINTLIGHT_DATA" )]
		public virtual Vector4		Data		{ set { SetVector( "POINTLIGHT_DATA", value ); } }
	}

	/// <summary>
	/// Readable ZBuffer interface
	/// </summary>
	public class	IReadableZBuffer : ShaderInterfaceBase
	{
		[Semantic( "ZBUFFER_INV_SIZE" )]
		public Vector2				ZBufferInvSize	{ set { SetVector( "ZBUFFER_INV_SIZE", value ); } }

		[Semantic( "ZBUFFER" )]
		public ITexture2D			ZBuffer		{ set { SetResource( "ZBUFFER", value ); } }
	}

	/// <summary>
	/// Linear tone mapping interface
	/// </summary>
	public class	ILinearToneMapping : ShaderInterfaceBase
	{
		[Semantic( "TONE_MAPPING_FACTOR" )]
		public float		ToneMappingFactor	{ set { SetScalar( "TONE_MAPPING_FACTOR", value ); } }

		[Semantic( "TONE_MAPPING_INV_GAMMA" )]
		public float		Gamma				{ set { SetScalar( "TONE_MAPPING_INV_GAMMA", 1.0f / value ); } }
	}
}
