using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

using SharpDX;

namespace TreeGloumibule
{
	/// <summary>
	/// This class encodes hemispherical light distribution
	/// It helps deciding which direction for growing would be best, given a pre-existing direction
	/// Light is normalized between 0 (no illumination at all) and 1 (full illumination in the given direction)
	/// 
	/// The light map is initialized as a polar map (be careful of the XYZ orientation which is NOT the same as with the tree) :
	/// 
	///              +Y
	///              ^
	///              |
	///              |
	/// 
	///           Phi=PI/2
	/// |-------------------------|
	/// |          .....          |
	/// |      ..         ..      |
	/// |    .               .    |
	/// |  .                   .  |
	/// | .                     . |
	/// |.                       .|
	/// |.         Theta=0       .|
	/// |.           O-----------.| Phi=0  ===> +X
	/// |.          +Z     Theta=PI/2
	/// |.                       .|
	/// | .                     . |
	/// |  .                   .  |
	/// |    .               .    |
	/// |      ..         ..      |
	/// |          .....          |
	/// |-------------------------|
	///           Phi=3PI/2
	/// 
	/// Typically, a cos(Theta) distribution is prefered as it obeys the diffuse equation of an horizontal plane with a vertical light.
	/// The best illumination condition is preferred. Daylight cycles being ignored for the matter.
	/// 
	/// Occlusion by other objects (trees, terrain, etc.) is the main source of light disturbance.
	/// </summary>
	public class LightHemisphere
	{
		#region CONSTANTS

		public const int		GRADIENT_STEPS_COUNT = 100;
		public const float		GRADIENT_STEP_SIZE = 0.0125f;	// Percentage of the light map we step for every gradient search

		#endregion

		#region FIELDS

		protected int			m_Size = 0;
		protected int			m_HalfSize = 0;
		protected float[,]		m_LightMap = null;
		protected bool			m_bPrepared = false;
		protected Vector2[,]	m_GradientMap = null;

		#endregion

		#region PROPERTIES

		public int		Size					{ get { return m_Size; } }
		public float	this[int _X, int _Y]	{ get { return m_LightMap[_X,_Y]; } set { m_LightMap[_X,_Y] = value; } }
		public Vector2	Grad( int _X, int _Y )	{ Prepare(); return m_GradientMap[_X,_Y]; }

		#endregion

		#region METHODS

		public LightHemisphere( int _Size )
		{
			m_Size = _Size;
			m_HalfSize = _Size / 2;
			m_LightMap = new float[m_Size,m_Size];
			m_GradientMap = new Vector2[m_Size,m_Size];
		}

		public LightHemisphere( LightHemisphere _Source )
		{
			m_Size = _Source.m_Size;
			m_HalfSize = _Source.m_Size / 2;
			m_LightMap = new float[m_Size,m_Size];
			m_GradientMap = new Vector2[m_Size,m_Size];
			SetMap( _Source.m_LightMap );
		}

		/// <summary>
		/// Clears the light map
		/// </summary>
		public void	Clear()
		{
			for ( int Y=0; Y < m_Size; Y++ )
				for ( int X=0; X < m_Size; X++ )
					m_LightMap[X,Y] = 0.0f;
		}

		/// <summary>
		/// Sets the light map from an external array
		/// </summary>
		/// <param name="_Source"></param>
		public void	SetMap( float[,] _Source )
		{
			m_bPrepared = false;
			if ( _Source.GetLength( 0 ) != m_Size || _Source.GetLength( 1 ) != m_Size)
				throw new Exception( "Source map and this map size mismatch !" );
			Array.Copy( _Source, m_LightMap, m_Size*m_Size );
		}

		/// <summary>
		/// Initializes the light map from a directional light source
		/// </summary>
		/// <param name="_LightMainDirection"></param>
		public void	SetFromLightSource( Vector3 _LightMainDirection )
		{
			_LightMainDirection.Normalize();

			m_bPrepared = false;
			for ( int Y=0; Y < m_Size; Y++ )
				for ( int X=0; X < m_Size; X++ )
//					m_LightMap[X,Y] = Math.Max( 0.0f, Vector3.Dot( XY2Direction( X, Y ), _LightMainDirection ) );
					m_LightMap[X,Y] = Vector3.Dot( XY2Direction( X, Y ), _LightMainDirection );
		}

		/// <summary>
		/// Modifies the light map by adding a spherical blocker whose solid angle will attenuate the light
		/// </summary>
		/// <param name="_Center"></param>
		/// <param name="_Radius"></param>
		/// <param name="_Opacity">1 is a fully opaque occluder</param>
		public void	AddBlockerSpherical( Vector3 _Center, float _Radius, float _Opacity )
		{
			m_bPrepared = false;

			Vector3	ToBlocker = _Center;
			float	Distance2Blocker = ToBlocker.Length();
			ToBlocker /= Distance2Blocker;	// Normalize
			float	CosSolidAngle = (float) Math.Sqrt( 1.0 - _Radius*_Radius/(Distance2Blocker*Distance2Blocker) );

			for ( int Y=0; Y < m_Size; Y++ )
				for ( int X=0; X < m_Size; X++ )
				{
					Vector3	Direction = XY2Direction( X, Y );
					float	DotBlocker = Vector3.Dot( Direction, ToBlocker );
					float	BlockFactor = CosSolidAngle - DotBlocker;	// A phase larger than the solid angle's phase will yield < 0 values...
//					m_LightMap[X,Y] *= BlockFactor > 0.0f ? 1.0f : Lerp( 1.0f, 10.0f * BlockFactor, _Opacity );
					m_LightMap[X,Y] *= BlockFactor > 0.0f ? 1.0f : Lerp( 1.0f, 0.0f, _Opacity );
				}
		}

		/// <summary>
		/// Modifies the light map by adding a cylindrical blocker whose solid angle will attenuate the light
		/// </summary>
		/// <param name="_Center">Center is a 2D vector as the cylinder is assumed to have its base on the ground</param>
		/// <param name="_Radius"></param>
		/// <param name="_Height"></param>
		/// <param name="_Opacity">1 is a fully opaque occluder</param>
		public void	AddBlockerCylindrical( Vector2 _Center, float _Radius, float _Height, float _Opacity )
		{
			m_bPrepared = false;

			Vector2	ToBlocker = _Center;
			float	Distance2Blocker = ToBlocker.Length();
			ToBlocker /= Distance2Blocker;	// Normalize
			float	CosSolidAnglePhi = (float) Math.Sqrt( 1.0 - _Radius*_Radius/(Distance2Blocker*Distance2Blocker) );
			float	CosSolidAngleTheta = Distance2Blocker / (float) Math.Sqrt( _Height*_Height + Distance2Blocker*Distance2Blocker );

			for ( int Y=0; Y < m_Size; Y++ )
				for ( int X=0; X < m_Size; X++ )
				{
					Vector3	Direction = XY2Direction( X, Y );

					// Compute block factor along phi
					Vector2	Direction2D = new Vector2( Direction.X, Direction.Y );
							Direction2D.Normalize();
					float	DotBlockerPhi = Vector2.Dot( Direction2D, ToBlocker );
					float	BlockFactorPhi = CosSolidAnglePhi - DotBlockerPhi;	// A phase larger than the solid angle's phase will yield < 0 values...

					// Compute block factor along theta
					float	DotBlockerTheta = (float) Math.Sqrt( 1.0 - Direction.Z*Direction.Z );
					float	BlockFactorTheta = CosSolidAngleTheta - DotBlockerTheta;

					// The actual block factor is a combination of the 2
//					m_LightMap[X,Y] *= BlockFactorPhi > 0.0f || BlockFactorTheta > 0.0f ? 1.0f : Lerp( 1.0f, -50.0f * Math.Abs(Math.Min( 0.0f, BlockFactorPhi ) * Math.Min( 0.0f, BlockFactorTheta )), _Opacity );
					m_LightMap[X,Y] *= BlockFactorPhi > 0.0f || BlockFactorTheta > 0.0f ? 1.0f : Lerp( 1.0f, 0.0f, _Opacity );
				}
		}

		/// <summary>
		/// Adds a branch blocker to the hemisphere
		/// NOTE: PropagateEvalFrame() must have been called on the branch prior using this method
		/// </summary>
		/// <param name="_ViewPosition">The position from which we view the branch (in WORLD space)</param>
		/// <param name="_Branch">The branch to render as a blocker</param>
		/// <param name="_Opacity"></param>
		public void		AddBlockerBranch( Vector3 _ViewPosition, Tree.Branch _Branch, float _Opacity )
		{
			Tree.Branch.Segment	Previous = _Branch.StartSegment;
			Tree.Branch.Segment	Current = Previous.Next;

			Vector3	Pos0, Pos1;
			while ( Current != null )
			{
				// Render the segment as a quad facing the view
				Vector3	Temp = Previous.WorldPosition - _ViewPosition;
				Pos0.X = Temp.X;
				Pos0.Y = -Temp.Z;
				Pos0.Z = Temp.Y;
				float	Radius0 = Previous.Radius;

				Temp = Current.WorldPosition - _ViewPosition;
				Pos1.X = Temp.X;
				Pos1.Y = -Temp.Z;
				Pos1.Z = Temp.Y;
				float	Radius1 = Current.Radius;

// 				Vector3	ToSegment0 = Pos0 - _ViewPosition;
// 				float	DistanceToSegment0 = ToSegment0.Length();
// 				ToSegment0 /= DistanceToSegment0;
// 
// 				Vector3	ToSegment1 = Pos1 - _ViewPosition;
// 				float	DistanceToSegment1 = ToSegment1.Length();
// 				ToSegment1 /= DistanceToSegment1;
// 
// 				float	Phi0 = (float) Math.Atan2( ToSegment0.Y, ToSegment0.X );
// 				float	Theta0 = (float) Math.Atan2( ToSegment0.Y, ToSegment0.X );

				for ( int StepIndex=0; StepIndex < 5; StepIndex++ )
				{
					float	t = (float) StepIndex / 5;
					Vector3	Pos = Pos0 + (Pos1-Pos0) * t;
					float	Radius = Radius0 + (Radius1-Radius0) * t;

					AddBlockerSpherical( Pos, Radius, _Opacity );
				}

				Previous = Current;
				Current = Current.Next;
			}
		}

		/// <summary>
		/// Computes the prefered light direction given a (normalized) source direction
		/// </summary>
		/// <param name="_SourceDirection"></param>
		/// <returns></returns>
		/// <remarks>This algorithm tends to be blocked if the source direction is below a blocker and both are perfectly aligned.
		/// Fortunately, this shouldn't happen as it's pretty rare to have random values aligned with main axes, anyway prefer
		///  setting the blockers not exactly aligned with axes either</remarks>
		public Vector2[]	m_MarchedPos = new Vector2[1+GRADIENT_STEPS_COUNT];
		public Vector3	ComputePreferedDirection( Vector3 _SourceDirection )
		{
			return ComputePreferedDirection( _SourceDirection, GRADIENT_STEPS_COUNT, GRADIENT_STEP_SIZE );
		}

		public Vector3	ComputePreferedDirection( Vector3 _SourceDirection, int _StepsCount, float _StepSize )
		{
			Prepare();	// Prepare the light map (does nothing if already prepared)

			// Compute initial position in light map
			int	X, Y;
			Direction2XY( _SourceDirection, out X, out Y );

			float	fX = X + 0.5f;
			float	fY = Y + 0.5f;
			float	fLight = ReadLight( fX, fY );
			m_MarchedPos[0] = new Vector2( fX, fY );

			// March along the gradient to attempt to reach strongest light
			float	StepSize = _StepSize * m_Size;	// So we march by X% of the size each step
			Vector2	PrevGrad = Vector2.Zero;
			for ( int StepIndex=0; StepIndex < _StepsCount; StepIndex++ )
			{
				Vector2	Grad = ReadGradient( fX, fY );
				float	GradLength = Grad.Length();	// As light is normalized in [0,1], the gradient can be at most sqrt(2*2 + 2*2) = 2sqrt(2)
				if ( GradLength > 1e-4f )
					Grad /= GradLength;

				float	DotPrev = Vector2.Dot( Grad, PrevGrad );
				if ( GradLength < 1e-4f || DotPrev < -0.97f )	// The second condition is here to avoid turnin back
				{	// Go toward the center
					Grad.X = m_HalfSize-fX;
					Grad.Y = m_HalfSize-fY;
					Grad.Normalize();
				}

				PrevGrad = Grad;
				Grad *= StepSize;

				// Estimate best route by checking orthogonal direction as well
				float	fNextLight0 = Math.Max( 0.3f, ReadLight( fX+Grad.X, fY+Grad.Y ) );	// Prefer main direction though
				float	fNextLight1 = Math.Max( 0.0f, ReadLight( fX+Grad.Y, fY-Grad.X ) );
				float	fNextLight2 = Math.Max( 0.0f, ReadLight( fX-Grad.Y, fY+Grad.X ) );
				Grad = (fNextLight0 * Grad
					 + (fNextLight1-fNextLight2) * new Vector2( Grad.Y, -Grad.X )
					 + (fNextLight2-fNextLight1) * new Vector2( -Grad.Y, Grad.X )) / (fNextLight0 + Math.Abs( fNextLight1 - fNextLight2));

				fX += Grad.X;
				fY += Grad.Y;
				fLight = ReadLight( fX, fY );
				m_MarchedPos[1+StepIndex] = new Vector2( fX, fY );
			}

			// Convert back into a direction
			return XY2Direction( fX, fY );
		}

		/// <summary>
		/// Builds the finalized version of the light map so it's ready to use
		/// (i.e. build the gradient map)
		/// </summary>
		protected void	Prepare()
		{
			if ( m_bPrepared )
				return;	// Already finalized !

			// Compute the gradient map
			for ( int Y=0; Y < m_Size; Y++ )
			{
				int	PY = Math.Max( 0, Y-1 );
				int	NY = Math.Min( m_Size-1, Y+1 );
				for ( int X=0; X < m_Size; X++ )
				{
					int	PX = Math.Max( 0, X-1 );
					int	NX = Math.Min( m_Size-1, X+1 );

					m_GradientMap[X,Y] = new Vector2();
					m_GradientMap[X,Y].X = m_LightMap[NX,Y] - m_LightMap[PX,Y];
					m_GradientMap[X,Y].Y = m_LightMap[X,NY] - m_LightMap[X,PY];
				}
			}

			m_bPrepared = true;
		}

		/// <summary>
		/// Bilinear light reader
		/// </summary>
		/// <param name="_X"></param>
		/// <param name="_Y"></param>
		/// <returns></returns>
		public float	ReadLight( float _X, float _Y )
		{
			int		X = (int) Math.Floor( _X );
			int		Y = (int) Math.Floor( _Y );
			float	dX = _X - X;
			float	dY = _Y - Y;
			X = Math.Max( 1, Math.Min( m_Size-2, X ) );
			Y = Math.Max( 1, Math.Min( m_Size-2, Y ) );
			int		NX = Math.Min( m_Size-2, X+1 );
			int		NY = Math.Min( m_Size-2, Y+1 );
			float	G00 = m_LightMap[X,Y];
			float	G01 = m_LightMap[NX,Y];
			float	G10 = m_LightMap[X,NY];
			float	G11 = m_LightMap[NX,NY];
			float	G0 = G00 * (1.0f - dX) + G01 * dX;
			float	G1 = G10 * (1.0f - dX) + G11 * dX;
			return G0 * (1.0f - dY) + G1 * dY;
		}

		/// <summary>
		/// Bilinear gradient reader
		/// </summary>
		/// <param name="_X"></param>
		/// <param name="_Y"></param>
		/// <returns></returns>
		public Vector2	ReadGradient( float _X, float _Y )
		{
			int		X = (int) Math.Floor( _X );
			int		Y = (int) Math.Floor( _Y );
			float	dX = _X - X;
			float	dY = _Y - Y;
			X = Math.Max( 0, Math.Min( m_Size-1, X ) );
			Y = Math.Max( 0, Math.Min( m_Size-1, Y ) );
			int		NX = Math.Min( m_Size-1, X+1 );
			int		NY = Math.Min( m_Size-1, Y+1 );
			Vector2	G00 = m_GradientMap[X,Y];
			Vector2	G01 = m_GradientMap[NX,Y];
			Vector2	G10 = m_GradientMap[X,NY];
			Vector2	G11 = m_GradientMap[NX,NY];
			Vector2	G0 = G00 * (1.0f - dX) + G01 * dX;
			Vector2	G1 = G10 * (1.0f - dX) + G11 * dX;
			return G0 * (1.0f - dY) + G1 * dY;
		}

		public Vector3	XY2Direction( int _X, int _Y )
		{
			float	X = (float) (_X - m_HalfSize) / m_HalfSize;
			float	Y = -(float) (_Y - m_HalfSize) / m_HalfSize;
			float	R = (float) Math.Sqrt( X*X+Y*Y );
			if ( R > 1.0f )
				return Vector3.Zero;

			double	Theta = 0.5 * Math.PI * R;
			float	C = (float) Math.Cos( Theta );
			float	Sr = (float) Math.Sin( Theta ) / Math.Max( 1e-6f, R );
			return new Vector3( X * Sr, Y * Sr, C );
		}

		public Vector3	XY2Direction( float _X, float _Y )
		{
			float	X = (float) (_X - m_HalfSize) / m_HalfSize;
			float	Y = -(float) (_Y - m_HalfSize) / m_HalfSize;
			float	R = (float) Math.Sqrt( X*X+Y*Y );
			if ( R > 1.0f )
				return Vector3.Zero;

			double	Theta = 0.5 * Math.PI * R;
			float	C = (float) Math.Cos( Theta );
			float	Sr = (float) Math.Sin( Theta ) / Math.Max( 1e-6f, R );
			return new Vector3( X * Sr, Y * Sr, C );
		}

		public Vector2	XY2PhiTheta( int _X, int _Y )
		{
			float	X = (float) (_X - m_HalfSize) / m_HalfSize;
			float	Y = -(float) (_Y - m_HalfSize) / m_HalfSize;
			float	R = (float) Math.Sqrt( X*X+Y*Y );
			return R > 1.0f ? Vector2.Zero : new Vector2( (float) Math.Atan2( Y, X ), (float) (0.5 * Math.PI * R) );
		}

		public void		Direction2XY( Vector3 _Direction, out int _X, out int _Y )
		{
			double	R = (float) (2.0 * Math.Acos( _Direction.Z ) / Math.PI);
			double	Phi = Math.Atan2( _Direction.Y, _Direction.X );
			_X = (int) (m_HalfSize * (1.0 + R * Math.Cos( Phi )));
			_Y = (int) (m_HalfSize * (1.0 - R * Math.Sin( Phi )));
		}

		protected float	Lerp( float x0, float x1, float t )
		{
			t = Math.Max( 0.0f, Math.Min( 1.0f, t ) );
			return x0 * (1.0f-t) + x1 * t;
		}

		public static Vector3	World2Hemisphere( Vector3 _WorldDirection )
		{
			return new Vector3( _WorldDirection.X, -_WorldDirection.Z, _WorldDirection.Y );
		}

		public static Vector3	Hemisphere2World( Vector3 _HemisphereDirection )
		{
			return new Vector3( _HemisphereDirection.X, _HemisphereDirection.Z, -_HemisphereDirection.Y );
		}

		#endregion
	}
}
