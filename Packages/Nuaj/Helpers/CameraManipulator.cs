using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using SharpDX;
using SharpDX.Direct3D10;
using SharpDX.DXGI;

namespace Nuaj.Helpers
{
	/// <summary>
	/// This is a little camera manipulator helper that you can bind to a control
	/// Use left button to rotate, middle to pan and right/wheel to zoom
	/// Use Shift to switch to "Unreal Editor first person mode"
	/// </summary>
	public class CameraManipulator
	{
		#region	CONSTANTS

		// The minimal distance we can't go under (decreasing any further would push the target along with the camera)
		protected const float	MIN_TARGET_DISTANCE					= 0.1f;

		// The MAX denormalized distance
		// NOTE: The MAX normalized distance is deduced from this value and the zoom acceleration)
		protected const float	TARGET_DISTANCE_DENORMALIZED_MAX	= 100.0f;	// At MAX normalized distance, denormalized distance should equal to this

		// The power at which the denormalized distance should increase
		protected const float	TARGET_DISTANCE_POWER				= 4.0f;

		#endregion

		#region NESTED TYPES

		public delegate bool	EnableMouseActionEventHandler( MouseEventArgs _e );

		#endregion

		#region FIELDS

		protected Control		m_Control = null;
		protected Camera		m_Camera = null;

		// Camera manipulation parameters
		protected float			m_ManipulationRotationSpeed			= 1.0f;
		protected float			m_ManipulationPanSpeed				= 1.0f;
		protected float			m_ManipulationZoomSpeed				= 0.8f;
		protected float			m_ManipulationZoomAcceleration		= 0.8f;

		// Target object matrix
		protected Matrix		m_CameraTransform						= Matrix.Identity;
		protected float			m_CameraTargetDistance				= 5.0f;

		// Camera motion
		protected MouseButtons	m_ButtonsDown						= MouseButtons.None;
		protected Matrix		m_ButtonDownTransform				= Matrix.Identity;
		protected Matrix		m_ButtonDownTargetObjectMatrix		= Matrix.Identity;
		protected Matrix		m_InvButtonDownTargetObjectMatrix	= Matrix.Identity;
		protected Vector2		m_ButtonDownMousePosition			= Vector2.Zero;
		protected float			m_ButtonDownCameraTargetDistance	= 0.0f;
		protected bool			m_bRotationEnabled					= true;
		protected float			m_NormalizedTargetDistance			= 5.0f;
		protected float			m_ButtonDownNormalizedTargetDistance = 0.0f;
		protected bool			m_bPushingTarget					= false;

		protected bool			m_bLastManipulationWasFirstPerson	= false;

		#endregion

		#region PROPERTIES

		public Camera		ManipulatedCamera
		{
			get { return m_Camera; }
			set { m_Camera = value; CameraTransform = m_CameraTransform; }
		}

		protected Matrix	CameraTransform
		{
			get { return m_CameraTransform; }
			set
			{
				m_CameraTransform = value;
				if ( m_Camera == null )
					return;

				Matrix	Result = value;
			    Result.Row1 = -Result.Row1;

				m_Camera.Camera2World = Result;
			}
		}

		protected float		CameraTargetDistance
		{
			get { return m_CameraTargetDistance; }
			set
			{
				Matrix	TargetMat = TargetObjectMatrix;		// Get current target matrix before changing distance
				Matrix	Temp = CameraTransform;				// Get current camera matrix before changing distance

				m_CameraTargetDistance = value;

				// Move the camera along its axis to match the new distance
				Temp.Row4  =  TargetMat.Row4 - m_CameraTargetDistance * Temp.Row3;

				CameraTransform = Temp;

				m_NormalizedTargetDistance = NormalizeTargetDistance( m_CameraTargetDistance );
			}
		}

		protected Matrix	TargetObjectMatrix
		{
			get
			{
				Matrix	CamMat = CameraTransform;
				Matrix	TargetMat = Matrix.Identity;
						TargetMat.Row4 =  CamMat.Row4  + m_CameraTargetDistance * CamMat.Row3;

				return	TargetMat;
			}
			set
			{
				Matrix	CamMat = CameraTransform;
						CamMat.Row4 = value.Row4 - m_CameraTargetDistance * value.Row3;

 				CameraTransform = CamMat;
			}
		}

		protected bool		FirstPersonKeyDown
		{
			get { return (Control.ModifierKeys & Keys.Shift) != 0; }
		}

		public event EnableMouseActionEventHandler	EnableMouseAction;

		#endregion

		#region METHODS

		public	CameraManipulator()
		{
		}

		public void		Attach( Control _Control, Camera _Camera )
		{
			m_Control = _Control;
			m_Camera = _Camera;
			m_Control.MouseDown += new MouseEventHandler( Control_MouseDown );
			m_Control.MouseUp += new MouseEventHandler( Control_MouseUp );
			m_Control.MouseMove += new MouseEventHandler( Control_MouseMove );
			m_Control.MouseWheel += new MouseEventHandler( Control_MouseWheel );
		}

		public void		Detach( Control _Control )
		{
			m_Control.MouseDown += new MouseEventHandler( Control_MouseDown );
			m_Control.MouseUp += new MouseEventHandler( Control_MouseUp );
			m_Control.MouseMove += new MouseEventHandler( Control_MouseMove );
			m_Control.MouseWheel += new MouseEventHandler( Control_MouseWheel );
			m_Control = null;
			m_Camera = null;
		}

		public void		InitializeCamera( Vector3 _Position, Vector3 _Target, Vector3 _Up )
		{
			// Build the camera matrix
			Vector3	At = _Target - _Position;
			if ( At.LengthSquared() > 1e-2f )
			{	// Normal case
				m_CameraTargetDistance = At.Length();
				At /= m_CameraTargetDistance;
			}
			else
			{	// Special bad case
				m_CameraTargetDistance = 0.01f;
				At = new Vector3( 0.0f, 0.0f, -1.0f );
			}

			Vector3	Ortho = Vector3.Cross( _Up, At );
					Ortho.Normalize();

			Matrix		CameraMat = Matrix.Identity;
						CameraMat.Row4 = new Vector4( _Position, 1.0f );
						CameraMat.Row3 = new Vector4( At, 0.0f );
						CameraMat.Row1 = new Vector4( Ortho, 0.0f );
						CameraMat.Row2 = new Vector4( Vector3.Cross( At, Ortho ), 0.0f );

			CameraTransform = CameraMat;

			// Setup the normalized target distance
			m_NormalizedTargetDistance = NormalizeTargetDistance( m_CameraTargetDistance );
		}

		protected Vector2	ComputeNormalizedScreenPosition( int _X, int _Y, float _fCameraAspectRatio )
		{
			return new Vector2( _fCameraAspectRatio * (2.0f * (float) _X - m_Control.Width) / m_Control.Width, 1.0f - 2.0f * (float) _Y / m_Control.Height );
		}

		protected float		GetDenormalizationFactor()
		{
			float	fMaxDeNormalizedDistance = TARGET_DISTANCE_DENORMALIZED_MAX / m_ManipulationZoomSpeed;			// Here, we reduce the max denormalized distance based on the zoom speed
			float	fMaxNormalizedDistance = fMaxDeNormalizedDistance * (1.0f - m_ManipulationZoomAcceleration);	// This line deduces the max normalized distance from the max denormalized distance

			return	fMaxDeNormalizedDistance / (float) Math.Pow( fMaxNormalizedDistance, TARGET_DISTANCE_POWER );
		}

		protected float		NormalizeTargetDistance( float _fDeNormalizedTargetDistance )
		{
			return	(float) Math.Pow( _fDeNormalizedTargetDistance / GetDenormalizationFactor(), 1.0 / TARGET_DISTANCE_POWER );
		}

		protected float		DeNormalizeTargetDistance( float _fNormalizedTargetDistance )
		{
			return	GetDenormalizationFactor() * (float) Math.Pow( _fNormalizedTargetDistance, TARGET_DISTANCE_POWER );
		}

		/// <summary>
		/// Converts an angle+axis into a plain rotation matrix
		/// </summary>
		/// <param name="_Angle"></param>
		/// <param name="_Axis"></param>
		/// <returns></returns>
		protected Matrix	AngleAxis2Matrix( float _Angle, Vector3 _Axis )
		{
			// Convert into a quaternion
			Vector3	qv = (float) System.Math.Sin( .5f * _Angle ) * _Axis;
			float	qs = (float) System.Math.Cos( .5f * _Angle );

			// Then into a matrix
			float	xs, ys, zs, wx, wy, wz, xx, xy, xz, yy, yz, zz;

// 			Quat	q = new Quat( _Source );
// 			q.Normalize();		// A cast to a matrix only works with normalized quaternions!

			xs = 2.0f * qv.X;	ys = 2.0f * qv.Y;	zs = 2.0f * qv.Z;

			wx = qs * xs;		wy = qs * ys;		wz = qs * zs;
			xx = qv.X * xs;	xy = qv.X * ys;	xz = qv.X * zs;
			yy = qv.Y * ys;	yz = qv.Y * zs;	zz = qv.Z * zs;

			Matrix	Ret = Matrix.Identity;

			Ret.M11 = 1.0f -	yy - zz;
			Ret.M12 =			xy + wz;
			Ret.M13 =			xz - wy;

			Ret.M21 =			xy - wz;
			Ret.M22 = 1.0f -	xx - zz;
			Ret.M23 =			yz + wx;

			Ret.M31 =			xz + wy;
			Ret.M32 =			yz - wx;
			Ret.M33 = 1.0f -	xx - yy;

			return	Ret;
		}

		/// <summary>
		/// Extracts Euler angles from a rotation matrix
		/// </summary>
		/// <param name="_Matrix"></param>
		/// <returns></returns>
		protected Vector3	GetEuler( Matrix _Matrix )
		{
			Vector3	Ret = new Vector3();
			float	fSinY = Math.Min( +1.0f, Math.Max( -1.0f, _Matrix[0, 2] ) ),
					fCosY = (float) Math.Sqrt( 1.0f - fSinY*fSinY );

			if ( _Matrix[0, 0] < 0.0 && _Matrix[2, 2] < 0.0 )
				fCosY = -fCosY;

			if ( (float) Math.Abs( fCosY ) > float.Epsilon )
			{
				Ret.X = (float)  Math.Atan2( _Matrix[1, 2] / fCosY, _Matrix[2, 2] / fCosY );
				Ret.Y = (float) -Math.Atan2( fSinY, fCosY );
				Ret.Z = (float)  Math.Atan2( _Matrix[0, 1] / fCosY, _Matrix[0, 0] / fCosY );
			}
			else
			{
				Ret.X = (float)  Math.Atan2( -_Matrix[2, 1], _Matrix[1, 1] );
				Ret.Y = (float) -Math.Asin( fSinY );
				Ret.Z = 0.0f;
			}

			return	Ret;
		}

		#endregion

		#region EVENT HANDLERS

		void Control_MouseDown( object sender, MouseEventArgs e )
		{
			if ( EnableMouseAction != null && !EnableMouseAction( e ) )
				return;	// Don't do anything

			m_ButtonsDown |= e.Button;		// Add this button

			// Keep a track of the mouse and camera states when button was pressed
			m_ButtonDownTransform = CameraTransform;
			m_ButtonDownTargetObjectMatrix = TargetObjectMatrix;
			m_InvButtonDownTargetObjectMatrix = m_ButtonDownTargetObjectMatrix;
			m_InvButtonDownTargetObjectMatrix.Invert();
			m_ButtonDownMousePosition = ComputeNormalizedScreenPosition( e.X, e.Y, (float) m_Control.Width / m_Control.Height );
			m_ButtonDownCameraTargetDistance = CameraTargetDistance;
			m_ButtonDownNormalizedTargetDistance = NormalizeTargetDistance( m_ButtonDownCameraTargetDistance );
		}

		void Control_MouseUp( object sender, MouseEventArgs e )
		{
			m_ButtonsDown = MouseButtons.None;	// Remove all buttons

			// Update the mouse and camera states when button is released
			m_ButtonDownTransform = CameraTransform;
			m_ButtonDownTargetObjectMatrix = TargetObjectMatrix;
			m_ButtonDownMousePosition = ComputeNormalizedScreenPosition( e.X, e.Y, (float) m_Control.Width / m_Control.Height );
			m_ButtonDownCameraTargetDistance = CameraTargetDistance;
			m_ButtonDownNormalizedTargetDistance = NormalizeTargetDistance( m_ButtonDownCameraTargetDistance );
		}

		void Control_MouseMove( object sender, MouseEventArgs e )
		{
			if ( EnableMouseAction != null && !EnableMouseAction( e ) )
				return;	// Don't do anything

			Matrix	CameraMatrixBeforeBaseCall = CameraTransform;

//			base.OnMouseMove( e );

			m_Control.Focus();

			if ( m_ButtonDownTransform == null )
				return;		// Can't manipulate...

			Vector2	MousePos = ComputeNormalizedScreenPosition( e.X, e.Y, (float) m_Control.Width / m_Control.Height );

			// Check for FIRST PERSON switch
			if ( m_bLastManipulationWasFirstPerson ^ FirstPersonKeyDown )
			{	// There was a switch so we need to copy the current matrix and make it look like the button was just pressed...
				Control_MouseDown(  sender, e );
			}
			m_bLastManipulationWasFirstPerson = FirstPersonKeyDown;

			if ( !FirstPersonKeyDown )
			{
				//////////////////////////////////////////////////////////////////////////
				// MAYA MANIPULATION MODE
				//////////////////////////////////////////////////////////////////////////
				//
				switch ( m_ButtonsDown )
				{
						// ROTATE
					case	MouseButtons.Left:
					{
						if ( !m_bRotationEnabled )
							break;	// Rotation is disabled!

						float	fAngleX = (MousePos.Y - m_ButtonDownMousePosition.Y) * 2.0f * (float) Math.PI * m_ManipulationRotationSpeed;
						float	fAngleY = (MousePos.X - m_ButtonDownMousePosition.X) * 2.0f * (float) Math.PI * m_ManipulationRotationSpeed;

						Vector4	AxisX = m_ButtonDownTransform.Row1;
						Matrix	Rot = AngleAxis2Matrix( fAngleX, -new Vector3( AxisX.X, AxisX.Y, AxisX.Z ) )
									* AngleAxis2Matrix( fAngleY, new Vector3( 0f, -1.0f, 0.0f ) );

						Matrix	Rotated = m_ButtonDownTransform * m_InvButtonDownTargetObjectMatrix * Rot * TargetObjectMatrix;

						CameraTransform = Rotated;

						break;
					}

						// DOLLY => Simply translate along the AT axis
					case	MouseButtons.Right:
					case	MouseButtons.Left | MouseButtons.Middle:
					{
						float	fTrans = m_ButtonDownMousePosition.X - m_ButtonDownMousePosition.Y - MousePos.X + MousePos.Y;

						m_NormalizedTargetDistance = m_ButtonDownNormalizedTargetDistance + 4.0f * m_ManipulationZoomSpeed * fTrans;
						float	fTargetDistance = Math.Sign( m_NormalizedTargetDistance ) * DeNormalizeTargetDistance( m_NormalizedTargetDistance );
						if ( fTargetDistance > MIN_TARGET_DISTANCE )
						{	// Okay! We're far enough so we can reduce the distance anyway
							CameraTargetDistance = fTargetDistance;
							m_bPushingTarget = false;
						}
						else
						{	// Too close! Let's move the camera forward and clamp the target distance... That will push the target along.
							m_CameraTargetDistance = MIN_TARGET_DISTANCE;
							m_NormalizedTargetDistance = NormalizeTargetDistance( m_CameraTargetDistance );

							if ( !m_bPushingTarget )
							{
								m_ButtonDownNormalizedTargetDistance = m_NormalizedTargetDistance;
								fTrans = 0.0f;
								m_bPushingTarget = true;
							}

							m_ButtonDownMousePosition = MousePos;

							Matrix	DollyCam = CameraTransform;
									DollyCam.Row4 = DollyCam.Row4 - 2.0f * m_ManipulationZoomSpeed * fTrans * DollyCam.Row3;

							CameraTransform = DollyCam;
						}
						break;
					}

						// PAN
					case	MouseButtons.Middle:
					{
						Vector2	Trans = new Vector2(	-(MousePos.X - m_ButtonDownMousePosition.X),
														MousePos.Y - m_ButtonDownMousePosition.Y
													);

						float		fTransFactor = m_ManipulationPanSpeed * Math.Max( 2.0f, m_CameraTargetDistance );

						// Make the camera pan
						Matrix	PanCam = m_ButtonDownTransform;
								PanCam.Row4 = m_ButtonDownTransform.Row4
											- fTransFactor * Trans.X * m_ButtonDownTransform.Row1
											- fTransFactor * Trans.Y * m_ButtonDownTransform.Row2;

						CameraTransform = PanCam;
						break;
					}
				}
			}
			else
			{
				//////////////////////////////////////////////////////////////////////////
				// UNREAL MANIPULATION MODE
				//////////////////////////////////////////////////////////////////////////
				//
				switch ( m_ButtonsDown )
				{
					// TRANSLATE IN THE ZX PLANE (WORLD SPACE)
					case	MouseButtons.Left :
					{
						float	fTransFactor = m_ManipulationPanSpeed * System.Math.Max( 4.0f, CameraTargetDistance );

						// Compute translation in the view direction
					    Vector4 Trans = CameraMatrixBeforeBaseCall.Row3;
								Trans.Y = 0.0f;
						if ( Trans.LengthSquared() < 1e-4f )
						{	// Better use Y instead...
						    Trans = CameraMatrixBeforeBaseCall.Row2;
							Trans.Y = 0.0f;
						}

						Trans.Normalize();

						Vector4	NewPosition = CameraMatrixBeforeBaseCall.Row4 + Trans * fTransFactor * (MousePos.Y - m_ButtonDownMousePosition.Y);

						m_ButtonDownMousePosition.Y = MousePos.Y;	// The translation is a cumulative operation...

						// Compute rotation about the the Y WORLD axis
						float		fAngleY = (m_ButtonDownMousePosition.X - MousePos.X) * 2.0f * (float) Math.PI * m_ManipulationRotationSpeed * 0.2f;	// [PATAPATCH] Multiplied by 0.2 as it's REALLY too sensitive otherwise!
						if ( m_ButtonDownTransform.Row2.Y < 0.0f )
							fAngleY = -fAngleY;		// Special "head down" case...

						Matrix	RotY = Matrix.RotationY( fAngleY );

						Matrix	FinalMatrix = m_ButtonDownTransform;
								FinalMatrix.Row4 = Vector4.Zero;	// Clear translation...
								FinalMatrix = FinalMatrix * RotY;
								FinalMatrix.Row4 = NewPosition;

						CameraTransform = FinalMatrix;

						break;
					}

					// ROTATE ABOUT CAMERA
					case	MouseButtons.Right :
					{
						float fAngleY = (m_ButtonDownMousePosition.X - MousePos.X) * 2.0f * (float) Math.PI * m_ManipulationRotationSpeed;
						float fAngleX = (m_ButtonDownMousePosition.Y - MousePos.Y) * 2.0f * (float) Math.PI * m_ManipulationRotationSpeed;

						Vector3	Euler = GetEuler( m_ButtonDownTransform );
						Matrix	CamRotYMatrix = Matrix.RotationY( fAngleY + Euler.Y );
						Matrix	CamRotXMatrix = Matrix.RotationX( fAngleX + Euler.X );
						Matrix	CamRotZMatrix = Matrix.RotationZ( Euler.Z );

						Matrix	RotateMatrix = CamRotXMatrix * CamRotYMatrix * CamRotZMatrix;

						RotateMatrix.Row4 = CameraTransform.Row4;
						CameraTransform = RotateMatrix;

						break;
					}

						// Translate in the ( Z-world Y-camera ) plane
					case	MouseButtons.Middle :
					case	MouseButtons.Left | MouseButtons.Right:
					{
						float		fTransFactor = m_ManipulationPanSpeed * System.Math.Max( 4.0f, CameraTargetDistance );

						Vector4		NewPosition =	m_ButtonDownTransform.Row4 + fTransFactor *
													( (MousePos.Y - m_ButtonDownMousePosition.Y) * Vector4.UnitY
													+ (m_ButtonDownMousePosition.X - MousePos.X) * m_ButtonDownTransform.Row1 );

						Matrix	NewMatrix = m_ButtonDownTransform;
								NewMatrix.Row4 =  NewPosition ;

						CameraTransform = NewMatrix;

						break;
					}
				}
			}
		}

		void Control_MouseWheel( object sender, MouseEventArgs e )
		{
			if ( EnableMouseAction != null && !EnableMouseAction( e ) )
				return;	// Don't do anything

			if ( m_ButtonDownTransform == null )
				return;		// Can't manipulate...

			m_NormalizedTargetDistance -= 0.004f * m_ManipulationZoomSpeed * e.Delta;
			float	fTargetDistance = DeNormalizeTargetDistance( m_NormalizedTargetDistance );
			if ( fTargetDistance > MIN_TARGET_DISTANCE )
			{	// Okay! We're far enough so we can reduce the distance anyway
				CameraTargetDistance = fTargetDistance;

				// Update "cached" data
				Control_MouseDown( sender, e );
			}
			else
			{
				// Too close! Let's move the camera forward without changing the target distance...
				m_CameraTargetDistance = MIN_TARGET_DISTANCE;
				m_NormalizedTargetDistance = NormalizeTargetDistance( m_CameraTargetDistance );

				Matrix	DollyCam = CameraTransform;
						DollyCam.Row4 = DollyCam.Row4 + 0.004f * m_ManipulationZoomSpeed * e.Delta * DollyCam.Row3;

				CameraTransform = DollyCam;

				// Update "cached" data
				Control_MouseDown( sender, e );
			}
		}

		#endregion
	}
}
