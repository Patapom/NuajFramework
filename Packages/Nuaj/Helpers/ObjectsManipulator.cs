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
	/// This is a little object manipulator helper that you can bind to a control
	/// Use left button to pick and drag a registered object in camera plane
	/// </summary>
	public class ObjectsManipulator
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
		public delegate void	ObjectSelectionEventHandler( object _PickedObject, bool _Selected );
		public delegate void	ObjectMoveEventHandler( object _MovedObject, Vector3 _NewPosition );

		protected class MovableObject
		{
			public object	m_Object = null;
			public Vector3	m_Position = Vector3.Zero;
			public float	m_BSphereRadius = 0.0f;

			// Cached data on mouse move
			public float	m_Depth = 0.0f;
			public Vector2	m_Position2D = Vector2.Zero;
			public float	m_Radius2D = 0.0f;
		}

		#endregion

		#region FIELDS

		protected Control		m_Control = null;
		protected Camera		m_Camera = null;

		protected List<MovableObject>	m_RegisteredObjects = new List<MovableObject>();
		protected Dictionary<object,MovableObject>	m_Object2MovableObject = new Dictionary<object,MovableObject>();
		protected MovableObject			m_HoveredObject = null;
 
 		// Object motion
		protected MovableObject	m_ManipulatedObject					= null;
		protected MouseButtons	m_ButtonsDown						= MouseButtons.None;
		protected Vector2		m_ButtonDownMousePosition			= Vector2.Zero;
		protected Vector2		m_ButtonDownObjectPosition			= Vector2.Zero;
 
		#endregion

		#region PROPERTIES

		public Camera			AttachedCamera
		{
			get { return m_Camera; }
			set { m_Camera = value; }
		}

		protected MovableObject		HoveredObject
		{
			get { return m_HoveredObject; }
			set
			{
				if ( value == m_HoveredObject )
					return;	// No change

				if ( m_HoveredObject != null )
				{	// Un-select
					if ( ObjectSelected != null )
						ObjectSelected( m_HoveredObject.m_Object, false );
				}

				m_HoveredObject = value;

				if ( m_HoveredObject != null )
				{	// Un-select
					if ( ObjectSelected != null )
						ObjectSelected( m_HoveredObject.m_Object, true );
				}
			}
		}

		public event EnableMouseActionEventHandler	EnableMouseAction;

		/// <summary>
		/// Occurs on object selection or deselection
		/// </summary>
		public event ObjectSelectionEventHandler	ObjectSelected;

		/// <summary>
		/// Occurs when an object is being moved
		/// </summary>
		public event ObjectMoveEventHandler			ObjectMoving;

		#endregion

		#region METHODS

		public	ObjectsManipulator()
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

		/// <summary>
		/// Registers a new movable object
		/// </summary>
		public void		RegisterMovableObject( object _Object, Vector3 _InitialPosition, float _BSphereRadius )
		{
			MovableObject	Object = new MovableObject();
			Object.m_Object = _Object;
			Object.m_Position = _InitialPosition;
			Object.m_BSphereRadius = _BSphereRadius;
			m_RegisteredObjects.Add( Object );
			m_Object2MovableObject.Add( _Object, Object );
		}

		public void		UpdateMovableObjectPosition( object _Object, Vector3 _NewPosition )
		{
			if ( !m_Object2MovableObject.ContainsKey( _Object ) )
				return;

			m_Object2MovableObject[_Object].m_Position = _NewPosition;

			// Notify
			ObjectMoving( _Object, _NewPosition );
		}

		/// <summary>
		/// Un-registers a movable object
		/// </summary>
		/// <param name="_Object"></param>
		public void		UnRegisterMovableObject( object _Object )
		{
			if ( !m_Object2MovableObject.ContainsKey( _Object ) )
				return;

			m_RegisteredObjects.Remove( m_Object2MovableObject[_Object] );
			m_Object2MovableObject.Remove( _Object );
		}

		#endregion

		#region EVENT HANDLERS

		void Control_MouseDown( object sender, MouseEventArgs e )
		{
			if ( EnableMouseAction != null && !EnableMouseAction( e ) )
				return;	// Don't do anything

			if ( m_HoveredObject == null )
				return;	// No selection

			m_ManipulatedObject = m_HoveredObject;	// Manipulating !
			m_ButtonsDown |= e.Button;		// Add this button
			m_ButtonDownMousePosition.X = 2.0f * e.X / m_Control.Width - 1.0f;	// Left=-1 Right=+1
			m_ButtonDownMousePosition.Y = 1.0f - 2.0f * e.Y / m_Control.Height;	// Top=+1 Bottom=-1
			m_ButtonDownObjectPosition = m_ManipulatedObject.m_Position2D;
		}

		void Control_MouseUp( object sender, MouseEventArgs e )
		{
			m_ManipulatedObject = null;
			m_ButtonsDown = MouseButtons.None;	// Remove all buttons
 			m_ButtonDownMousePosition.X = 2.0f * e.X / m_Control.Width - 1.0f;	// Left=-1 Right=+1
			m_ButtonDownMousePosition.Y = 1.0f - 2.0f * e.Y / m_Control.Height;	// Top=+1 Bottom=-1
		}

		void Control_MouseMove( object sender, MouseEventArgs e )
		{
			// Perform manipulation
			m_Control.Focus();

			Vector2	MousePos = new Vector2();
 			MousePos.X = 2.0f * e.X / m_Control.Width - 1.0f;	// Left=-1 Right=+1
			MousePos.Y = 1.0f - 2.0f * e.Y / m_Control.Height;	// Top=+1 Bottom=-1

			if ( m_ManipulatedObject == null )
			{	// Look for new object to manipulate
				Matrix	World2Camera = m_Camera.World2Camera;

				float	IFOVY = 1.0f / (float) Math.Tan( 0.5 * m_Camera.PerspectiveFOV );
				float	IFOVX = IFOVY / m_Camera.AspectRatio;

				MovableObject	BestObject = null;
				foreach ( MovableObject O in m_RegisteredObjects )
				{
					// Project object in 2D
					Vector3	ObjectPosCamera = Vector3.TransformCoordinate( O.m_Position, World2Camera );
					O.m_Depth = ObjectPosCamera.Z;
					float	IDepth = 1.0f / ObjectPosCamera.Z;
					O.m_Position2D.X = ObjectPosCamera.X * IFOVX * IDepth;
					O.m_Position2D.Y = ObjectPosCamera.Y * IFOVY * IDepth;
					O.m_Radius2D = O.m_BSphereRadius * IFOVY * IDepth;

					// Check if it can be picked
					if ( BestObject != null && BestObject.m_Depth < O.m_Depth )
						continue;	// We already have a closer object !

					float	SqDistance = (MousePos - O.m_Position2D).LengthSquared();
					if ( SqDistance > O.m_Radius2D*O.m_Radius2D )
						continue;	// Too far from the mouse...

					// We have a new best object !
					BestObject = O;
				}

				// Assign our new best candidate
				HoveredObject = BestObject;
				return;
			}

			if ( EnableMouseAction != null && !EnableMouseAction( e ) )
				return;	// Don't do anything

			if ( ObjectMoving == null )
				return;

			Vector2	DeltaPos = m_ButtonDownMousePosition - m_ButtonDownObjectPosition;	// Since we may have clicked a bit afar from the center

			// Move the object in camera plane
			Vector2	NewPosition2D = MousePos - DeltaPos;

			// Un-project
			float	FOVY = (float) Math.Tan( 0.5 * m_Camera.PerspectiveFOV );
			float	FOVX = m_Camera.AspectRatio * FOVY;
			Vector3	NewPosition = new Vector3();
			NewPosition.Z = m_ManipulatedObject.m_Depth;
			NewPosition.X = NewPosition2D.X * NewPosition.Z * FOVX;
			NewPosition.Y = NewPosition2D.Y * NewPosition.Z * FOVY;

			// Transform back into WORLD space
			Matrix	Camera2World = m_Camera.Camera2World;
			m_ManipulatedObject.m_Position = Vector3.TransformCoordinate( NewPosition, Camera2World );

			// Notify
			ObjectMoving( m_ManipulatedObject.m_Object, m_ManipulatedObject.m_Position );
		}

		void Control_MouseWheel( object sender, MouseEventArgs e )
		{
			if ( EnableMouseAction != null && !EnableMouseAction( e ) )
				return;	// Don't do anything

			if ( m_HoveredObject == null )
				return;

			if ( ObjectMoving == null )
				return;

			if ( e.Delta > 0 )
				m_HoveredObject.m_Depth *= 1.0f + e.Delta * 0.001f;
			else
				m_HoveredObject.m_Depth /= 1.0f - e.Delta * 0.001f;

			// Un-project
			float	FOVY = (float) Math.Tan( 0.5 * m_Camera.PerspectiveFOV );
			float	FOVX = m_Camera.AspectRatio * FOVY;
			Vector3	NewPosition = new Vector3();
			NewPosition.Z = m_HoveredObject.m_Depth;
			NewPosition.X = m_HoveredObject.m_Position2D.X * NewPosition.Z * FOVX;
			NewPosition.Y = m_HoveredObject.m_Position2D.Y * NewPosition.Z * FOVY;

			// Transform back into WORLD space
			Matrix	Camera2World = m_Camera.Camera2World;
			m_HoveredObject.m_Position = Vector3.TransformCoordinate( NewPosition, Camera2World );

			// Notify
			ObjectMoving( m_HoveredObject.m_Object, m_HoveredObject.m_Position );
		}

		#endregion
	}
}
