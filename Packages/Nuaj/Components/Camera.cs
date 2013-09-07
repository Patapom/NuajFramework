using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using SharpDX;

namespace Nuaj
{
	/// <summary>
	/// The Camera class doesn't wrap any DirectX component per-se but helps a lot to handle
	///  basic displacement and projections
	///  
	/// NOTES :
	/// _ The projection matrix is Left Handed
	/// _ The Local2World matrix is left handed (all other matrices in Nuaj are right handed !)
	/// 
	/// A typical camera matrix looks like this :
	/// 
	///     Y (Up)
	///     ^
	///     |    Z (At)
	///     |   /
	///     |  /
	///     | /
	///     |/
	///     o---------> X (Right)
	/// 
	/// </summary>
	public class Camera : Component, IShaderInterfaceProvider
	{
		#region FIELDS

		protected Matrix	m_Camera2World = Matrix.Identity;	// Transform matrix
		protected Matrix	m_World2Camera = Matrix.Identity;
		protected Matrix	m_Camera2Proj = Matrix.Identity;	// Projection matrix
		protected Matrix	m_World2Proj = Matrix.Identity;		// Transform + Projection matrix

		protected float		m_Near = 0.0f;
		protected float		m_Far = 0.0f;
		protected float		m_AspectRatio = 0.0f;
		protected Frustum	m_Frustum = null;

		// Perspective/Orthogonal informations
		protected bool		m_bIsPerspective = true;
		protected float		m_PerspFOV = 0.0f;
		protected float		m_OrthoHeight = 0.0f;

		protected bool		m_bActive = false;

		protected Vector4	m_CachedCameraData = new Vector4();

		#endregion

		#region PROPERTIES

		/// <summary>
		/// Gets or sets the camera transform (CAMERA => WORLD)
		/// </summary>
		public Matrix			Camera2World
		{
			get { return m_Camera2World; }
			set
			{
				m_Camera2World = m_World2Camera = value;
				m_World2Camera.Invert();
				m_World2Proj = m_World2Camera * m_Camera2Proj;

				// Notify of the change
				if ( CameraTransformChanged != null )
					CameraTransformChanged( this, EventArgs.Empty );
			}
		}

		/// <summary>
		/// Gets the inverse camera transform (WORLD => CAMERA)
		/// </summary>
		public Matrix			World2Camera
		{
			get { return m_World2Camera; }
		}

		/// <summary>
		/// Gets the projection transform
		/// </summary>
		[System.ComponentModel.Browsable( false )]
		public Matrix			Camera2Proj
		{
			get { return m_Camera2Proj; }
			private set
			{
				m_Camera2Proj = value;
				m_World2Proj = m_World2Camera * m_Camera2Proj;

				// Notify of the change
				if ( CameraProjectionChanged != null )
					CameraProjectionChanged( this, EventArgs.Empty );
			}
		}

		/// <summary>
		/// Gets the world to projection transform
		/// </summary>
		[System.ComponentModel.Browsable( false )]
		public Matrix			World2Proj	{ get { return m_World2Proj; } }

		/// <summary>
		/// Gets the near clip plane distance
		/// </summary>
		public float			Near		{ get { return m_Near; } set { m_Far = value; RebuildProjection(); } }

		/// <summary>
		/// Gets the far clip plane distance
		/// </summary>
		public float			Far			{ get { return m_Far; } set { m_Far = value; RebuildProjection(); } }

		/// <summary>
		/// Gets the aspect ratio
		/// </summary>
		public float			AspectRatio	{ get { return m_AspectRatio; } }

		/// <summary>
		/// Gets the camera frustum
		/// </summary>
		[System.ComponentModel.Browsable( false )]
		public Frustum			Frustum		{ get { return m_Frustum; } }

		/// <summary>
		/// Tells if the camera was initialized as a perspective camera
		/// </summary>
		public bool				IsPerspective	{ get { return m_bIsPerspective; } }

		/// <summary>
		/// Gets the vertical FOV value used for perspective init
		/// </summary>
		public float			PerspectiveFOV	{ get { return m_PerspFOV; } }

		/// <summary>
		/// Gets the vertical height value used for orthographic init
		/// </summary>
		public float			OrthographicHeight	{ get { return m_OrthoHeight; } }

		public Vector3			Right		{ get { return new Vector3( m_Camera2World.M11, m_Camera2World.M12, m_Camera2World.M13 ); } }
		public Vector3			Up			{ get { return new Vector3( m_Camera2World.M21, m_Camera2World.M22, m_Camera2World.M23 ); } }
		public Vector3			At			{ get { return new Vector3( m_Camera2World.M31, m_Camera2World.M32, m_Camera2World.M33 ); } }
		public Vector3			Position	{ get { return new Vector3( m_Camera2World.M41, m_Camera2World.M42, m_Camera2World.M43 ); } }

		public event EventHandler	CameraTransformChanged;
		public event EventHandler	CameraProjectionChanged;

		#endregion

		#region METHODS

		/// <summary>
		/// Creates a default camera
		/// </summary>
		/// <remarks>IMPORTANT : Don't forget to ACTIVATE the camera once it's created otherwise materials won't get their projection matrices !</remarks>
		/// <param name="_Device"></param>
		/// <param name="_Name"></param>
		public	Camera( Device _Device, string _Name ) : base( _Device, _Name )
		{
		}

		/// <summary>
		/// Makes that camera the active one (i.e. now providing projection matrices)
		/// </summary>
		public void		Activate()
		{
			if ( !m_bActive )
				m_Device.RegisterShaderInterfaceProvider( typeof(ICamera), this );
			m_bActive = true;
		}

		/// <summary>
		/// Makes that camera inactive (i.e. not providing projection matrices anymore)
		/// </summary>
		public void		DeActivate()
		{
			if ( m_bActive )
				m_Device.UnRegisterShaderInterfaceProvider( typeof(ICamera), this );
			m_bActive = false;
		}

		public override void Dispose()
		{
			DeActivate();	// De-activate if active...
			base.Dispose();
		}

		/// <summary>
		/// Creates a perspective projection matrix for the camera
		/// </summary>
		/// <param name="_FOV"></param>
		/// <param name="_AspectRatio"></param>
		/// <param name="_Near"></param>
		/// <param name="_Far"></param>
		public void		CreatePerspectiveCamera( float _FOV, float _AspectRatio, float _Near, float _Far )
		{
			m_Near = _Near;
			m_Far = _Far;
			m_AspectRatio = _AspectRatio;
			m_PerspFOV = _FOV;
			m_Frustum = Frustum.FromPerspective( _FOV, _AspectRatio, _Near, _Far );
			this.Camera2Proj = Matrix.PerspectiveFovLH( _FOV, _AspectRatio, _Near, _Far );
			m_bIsPerspective = true;

			// Build camera data
			m_CachedCameraData.X = (float) Math.Tan( 0.5 * m_PerspFOV );
			m_CachedCameraData.Y = m_AspectRatio;
			m_CachedCameraData.Z = m_Near;
			m_CachedCameraData.W = m_Far;
		}

		/// <summary>
		/// Creates an orthogonal projection matrix for the camera
		/// </summary>
		/// <param name="_Height"></param>
		/// <param name="_AspectRatio"></param>
		/// <param name="_Near"></param>
		/// <param name="_Far"></param>
		public void		CreateOrthoCamera( float _Height, float _AspectRatio, float _Near, float _Far )
		{
			m_Near = _Near;
			m_Far = _Far;
			m_AspectRatio = _AspectRatio;
			m_OrthoHeight = _Height;
			m_Frustum = Frustum.FromOrtho( _Height, _AspectRatio, _Near, _Far );
			this.Camera2Proj = Matrix.OrthoLH( _AspectRatio * _Height, _Height, _Near, _Far );
			m_bIsPerspective = false;

			// Build camera data
			m_CachedCameraData.X = 0.5f * _Height;
			m_CachedCameraData.Y = m_AspectRatio;
			m_CachedCameraData.Z = m_Near;
			m_CachedCameraData.W = m_Far;
		}

		/// <summary>
		/// Rebuilds the camera projection data after a change
		/// </summary>
		protected void	RebuildProjection()
		{
			if ( m_bIsPerspective )
				CreatePerspectiveCamera( m_PerspFOV, m_AspectRatio, m_Near, m_Far );
			else
				CreateOrthoCamera( m_OrthoHeight, m_AspectRatio, m_Near, m_Far );
		}

		/// <summary>
		/// Makes the camera look at the specified target from the specified eye position
		/// </summary>
		/// <param name="_Eye"></param>
		/// <param name="_Target"></param>
		/// <param name="_Up"></param>
		public void		LookAt( Vector3 _Eye, Vector3 _Target, Vector3 _Up )
		{
			this.Camera2World = Camera.CreateLookAt( _Eye, _Target, _Up );
		}

		/// <summary>
		/// Projects a 3D point in 2D
		/// </summary>
		/// <param name="_Position"></param>
		/// <returns></returns>
		public Vector2	ProjectPoint( Vector3 _Position )
		{
			Vector4	Temp = Vector4.Transform( new Vector4( _Position, 1.0f ), m_World2Proj );
			Temp /= Temp.W;
			return new Vector2( Temp.X, Temp.Y );
		}

		/// <summary>
		/// Projects a 3D vector in 2D
		/// </summary>
		/// <param name="_Position"></param>
		/// <returns></returns>
		public Vector2	ProjectVector( Vector3 _Vector )
		{
			Vector4	Temp = Vector4.Transform( new Vector4( _Vector, 0.0f ), m_World2Proj );
			Temp /= Temp.W;
			return new Vector2( Temp.X, Temp.Y );
		}

		/// <summary>
		/// Builds a camera ray in WORLD space
		/// </summary>
		/// <param name="_X">The normalized X coordinate in [0,1] (0 is left screen border and 1 is right screen border)</param>
		/// <param name="_Y">The normalized Y coordinate in [0,1] (0 is top screen border and 1 is bottom screen border)</param>
		/// <param name="_Position"></param>
		/// <param name="_Direction"></param>
		public void		BuildWorldRay( float _X, float _Y, out Vector3 _Position, out Vector3 _Direction )
		{
			Vector3	P, V;
			BuildCameraRay( _X, _Y, out P, out V );

			_Position = Vector3.TransformCoordinate( P, m_Camera2World );
			_Direction = Vector3.TransformNormal( V, m_Camera2World );
		}

		/// <summary>
		/// Builds a camera ray in CAMERA space
		/// </summary>
		/// <param name="_X">The normalized X coordinate in [0,1] (0 is left screen border and 1 is right screen border)</param>
		/// <param name="_Y">The normalized Y coordinate in [0,1] (0 is top screen border and 1 is bottom screen border)</param>
		/// <param name="_Position"></param>
		/// <param name="_Direction"></param>
		public void		BuildCameraRay( float _X, float _Y, out Vector3 _Position, out Vector3 _Direction )
		{
			if ( m_bIsPerspective )
			{
				_Position = Vector3.Zero;
				_Direction = new Vector3(
						(2.0f * _X - 1.0f) * m_AspectRatio * (float) Math.Tan( 0.5f * m_PerspFOV ),
						(1.0f - 2.0f * _Y) * (float) Math.Tan( 0.5f * m_PerspFOV ),
						1.0f
					);
				_Direction.Normalize();
			}
			else
			{
				_Direction = Vector3.UnitZ;
				_Position = new Vector3(
						(_X - 0.5f) * m_AspectRatio * m_OrthoHeight,
						(0.5f - _Y) * m_OrthoHeight,
						0.0f
					);
			}
		}

		/// <summary>
		/// Creates a LookAt matrix
		/// </summary>
		/// <param name="_Eye"></param>
		/// <param name="_Target"></param>
		/// <param name="_Up"></param>
		/// <returns></returns>
		public static Matrix	CreateLookAt( Vector3 _Eye, Vector3 _Target, Vector3 _Up )
		{
			Vector3	At = _Target-_Eye;
					At.Normalize();
			Vector3	Right = Vector3.Cross( At, _Up );
					Right.Normalize();
			Vector3	Up = Vector3.Cross( Right, At );

			Matrix	Camera2World = new Matrix();
					Camera2World.Row1 = new Vector4( Right, 0.0f );
					Camera2World.Row2 = new Vector4( Up, 0.0f ) ;
					Camera2World.Row3 = new Vector4( At, 0.0f ) ;
					Camera2World.Row4 = new Vector4( _Eye, 1.0f ) ;

			return Camera2World;
		}

		#region IShaderInterfaceProvider Members

		public void ProvideData( IShaderInterface _Interface )
		{
			ICamera	Cam = _Interface as ICamera;

			Cam.Camera2World = m_Camera2World;
			Cam.World2Camera = m_World2Camera;
			Cam.World2Proj = m_World2Proj;
			Cam.Camera2Proj = m_Camera2Proj;
			Cam.CameraData = m_CachedCameraData;
		}

		#endregion

		#endregion
	}
}
