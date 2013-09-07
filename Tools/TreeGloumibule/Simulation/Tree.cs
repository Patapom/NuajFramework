using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

using SharpDX;

namespace TreeGloumibule
{
	public class Tree
	{
		#region CONSTANTS

		// Gravity should be equal to -9.8 here but I want torques to have lengths be consistent with masses
		//	and I don't want to bother multiplying masses by 9.8 to deal with weight forces instead...
		// So, I normalized gravity
		public const float				GRAVITY_LENGTH = 1.0f;
		public static readonly Vector3	GRAVITY = new Vector3( 0.0f, -GRAVITY_LENGTH, 0.0f );

		#endregion

		#region NESTED TYPES

		// TODO: Ground nutrients map

		/// <summary>
		/// This defines the base "branch" class used by both acropetal (trunk & branches) and basopetal (roots) branch types
		/// </summary>
		[System.Diagnostics.DebuggerDisplay( "ID={ID} Lv={Level} L={Length} M={Mass} BrCnt={BranchesCount}" )]
		public class Branch : IComparer<Branch>
		{
			#region NESTED TYPES

			/// <summary>
			/// This class is only a helper and is not used in the growing process of the tree
			/// </summary>
			public class	Leaf
			{
				#region FIELDS

				protected Segment	m_Parent = null;

				#endregion

				#region PROPERTIES

				public Vector3		Position	{ get { return m_Parent.Position; } }
				public Vector3		Direction	{ get { return m_Parent.Y; } }

				#endregion

				#region METHODS

				public Leaf( Segment _Parent )
				{
					m_Parent = _Parent;
				}

				#endregion
			}
			
			/// <summary>
			/// Describes a ray hit
			/// </summary>
			[System.Diagnostics.DebuggerDisplay( "Distance={m_HitDistance} ID={m_HitSegment.ID}" )]
			public class RayHit : IComparable
			{
				public Segment	m_HitSegment = null;
				public float	m_HitDistance = float.MaxValue;

				#region IComparable Members

				public int CompareTo( object obj )
				{
					RayHit	Other = obj as RayHit;
					return m_HitDistance < Other.m_HitDistance ? -1 : (m_HitDistance > Other.m_HitDistance ? +1 : 0);
				}

				#endregion
			}

			/// <summary>
			/// This is the base segment class representing a small segment of the branch
			/// </summary>
			[System.Diagnostics.DebuggerDisplay( "ID={m_Parent.ID}.{ID} R={Radius} L={Length} M={Mass} Y={Y}" )]
			public class	Segment
			{
				#region FIELDS

				protected Branch	m_Parent = null;
				protected Segment	m_Previous = null;
				protected Segment	m_Next = null;

				protected Vector3	m_Position = Vector3.Zero;				// LOCAL frame relative to PARENT frame
				protected Vector3	m_X = Vector3.Zero;
				protected Vector3	m_Y = Vector3.Zero;						// Growing direction => the next segment is along this axis
				protected Vector3	m_Z = Vector3.Zero;

				protected Matrix	m_Local2World = Matrix.Identity;		// LOCAL -> WORLD frame (evaluated by the PropagateEvalFrame() method)
				protected Matrix	m_World2Local = Matrix.Identity;		// WORLD -> LOCAL frame (evaluated by the PropagateEvalFrame() method)

				protected float		m_Radius = 0.0f;
				protected bool		m_bActiveLengthGrowth = true;			// We try to grow in length as long as this is true

				// Grow cached parameters per simulation step
				protected float		m_AccumulatedMass = 0.0f;				// Mass of this segment + sub-segments and branches
				protected Vector3	m_Torque = Vector3.Zero;				// Torque applied to this segment

					// Target length/radius & volume growths
				protected float		m_TargetLength = 0.0f;					// The target length to reach on next growth step
				protected float		m_TargetVolumeForLengthGrowth = 0.0f;	// The target volume to reach the target length on next growth step
				protected float		m_TargetRadius = 0.0f;					// The target radius to reach on next growth step
				protected float		m_TargetVolumeForRadiusGrowth = 0.0f;	// The target volume to reach the target radius on next growth step

				protected float		m_ActualTargetVolume = 0.0f;			// The actual target volume which is a mix of radius & length target volumes
				protected float		m_DeltaVolume = 0.0f;					// The difference in volume to acquire (will guide the nutrients & light needs)
				protected float		m_RadiusVolumeGrowthImportance = 0.0f;	// The percentage of volume that should be used to grow the radius
				protected float		m_LengthVolumeGrowthImportance = 0.0f;	// The percentage of volume that should be used to grow the length

					// Light & nutrient needs
				protected float		m_LightNeeds = 0.0f;					// The absolute need in light
				protected float		m_NutrientsNeeds = 0.0f;				// The absolute need in nutrients
				protected float		m_AccumulatedLightNeeds = 0.0f;			// The accumulated need in light accounting for child branches (basopetal accumulation)
				protected float		m_AccumulatedNutrientsNeeds = 0.0f;		// The accumulated need in nutrients accounting for parent branches (acropetal accumulation)

					// Light & nutrient collected for that simulation step
				protected float		m_CollectedLight = 0.0f;
				protected float		m_CollectedNutrients = 0.0f;

				#endregion

				#region PROPERTIES

				public Branch	Parent			{ get { return m_Parent; } }
				public int		ID				{ get { return PreviousSegmentsCount; } }

				public Segment	Previous		{ get { return m_Previous; } }
				public Segment	Next			{ get { return m_Next; } }
				public Segment	Start			{ get { return m_Previous == null ? this : m_Previous.Start; } }
				public Segment	End				{ get { return m_Next == null ? this : m_Next.End; } }

				public Vector3	Position		{ get { return m_Position; } }
				public Vector3	X				{ get { return m_X; } }
				public Vector3	Y				{ get { return m_Y; } }
				public Vector3	Z				{ get { return m_Z; } }
				public float	Radius			{ get { return m_Radius; } }

				// These data are valid only after PropagateEvalFrame()
				public Matrix	Local2World		{ get { return m_Local2World; } }
				public Vector3	WorldPosition	{ get { return new Vector3( m_Local2World.M41, m_Local2World.M42, m_Local2World.M43 ); } }
				public Vector3	WorldX			{ get { return new Vector3( m_Local2World.M11, m_Local2World.M12, m_Local2World.M13 );; } }
				public Vector3	WorldY			{ get { return new Vector3( m_Local2World.M21, m_Local2World.M22, m_Local2World.M23 );; } }
				public Vector3	WorldZ			{ get { return new Vector3( m_Local2World.M31, m_Local2World.M32, m_Local2World.M33 );; } }

				/// <summary>
				/// Gets the mass of that segment
				/// </summary>
				public float	Mass
				{
					get { return m_Parent.m_Owner.m_Density * Volume; }
				}

				/// <summary>
				/// Gets the length of that segment
				/// </summary>
				public float	Length
				{
					get { return m_Next != null ? m_Next.m_Position.Length() : 0.0f; }
				}

				/// <summary>
				/// Gets the volume of that segment (a tapered cylinder)
				/// </summary>
				public float	Volume
				{
					get
					{
						if ( m_Next == null )
							return 0.0f;

						// If this radius or next radius are 0 then we're back with the volume of a cone
						float	Nr = m_Next.m_Radius;
						return (float) (Math.PI * (m_Radius*m_Radius + Nr*Nr + m_Radius*Nr) * Length / 3.0);
					}
				}

				/// <summary>
				/// Gets the area of the segment (a tapered cylinder)
				/// </summary>
				public float	Area
				{
					get
					{
						if ( m_Next == null )
							return 0.0f;

						float	L = Length;
						float	Dr = m_Next.m_Radius - m_Radius;	// If this is 0 we retrieve the area of a cylinder
						return (float) (Math.PI * Math.Sqrt( L*L + Dr*Dr ) * (m_Radius + m_Next.m_Radius));
					}
				}

				/// <summary>
				/// Gets the total area of the segment and its children
				/// </summary>
				public virtual float	AreaWithChildren
				{
					get { return Area + (m_Next != null ? m_Next.AreaWithChildren : 0.0f); }
				}

				/// <summary>
				/// Gets the amount of branches, including this one
				/// </summary>
				public virtual int		BranchesCount
				{
					get { return m_Next != null ? m_Next.BranchesCount : 0; }
				}

				/// <summary>
				/// Gets the amount of segments before us
				/// </summary>
				public int		PreviousSegmentsCount
				{
					get { return m_Previous != null ? 1+m_Previous.PreviousSegmentsCount : 0; }
				}

				/// <summary>
				/// Gets the amount of segments after us
				/// </summary>
				public int		NextSegmentsCount
				{
					get { return m_Next != null ? 1+m_Next.NextSegmentsCount : 0; }
				}

				/// <summary>
				/// Gets the torque applied to this segment
				/// NOTE: You must call PropagateComputeTorque() before that property is valid
				/// </summary>
				public Vector3	Torque
				{
					get { return m_Torque; }
				}

				/// <summary>
				/// Gets the mass of the segment and its children
				/// NOTE: You must call PropagateComputeTorque() before that property is valid
				/// </summary>
				public float	AccumulatedMass
				{
					get { return m_AccumulatedMass; }
				}

				/// <summary>
				/// Gets the light needs of the segment and its children
				/// NOTE: You must call PropagateAccumulateLightAndNutrientNeeds() before that property is valid
				/// </summary>
				public float	AccumulatedLightNeeds
				{
					get { return m_AccumulatedLightNeeds; }
				}

				#endregion

				#region METHODS

				public Segment( Branch _Parent )
				{
					m_Parent = _Parent;
				}

				/// <summary>
				/// Initializes the segment's reference frame and radius
				/// </summary>
				/// <param name="_Position"></param>
				/// <param name="_X"></param>
				/// <param name="_Y"></param>
				/// <param name="_Z"></param>
				/// <param name="_Radius"></param>
				public virtual void		Initialize( Vector3 _Position, Vector3 _X, Vector3 _Y, Vector3 _Z, float _Radius )
				{
					m_Position = _Position;
					m_X = _X;
					m_Y = _Y;
					m_Z = _Z;
					m_Radius = _Radius;
				}

				/// <summary>
				/// Adds a new segment to that one
				/// </summary>
				/// <param name="_New"></param>
				public void	AddSegment( Segment _New )
				{
					m_Next = _New;
					_New.m_Previous = this;
				}

				#region Growth Algorithm

				/// <summary>
				/// Evaluates the LOCAL2WORLD frame by propagating from the root to the leaves
				/// </summary>
				public virtual void	PropagateEvalFrame( Matrix _Parent2World )
				{
					Vector3	P = Vector3.TransformCoordinate( m_Position, _Parent2World );
					Vector3	X = Vector3.TransformNormal( m_X, _Parent2World );
					Vector3	Y = Vector3.TransformNormal( m_Y, _Parent2World );
					Vector3	Z = Vector3.TransformNormal( m_Z, _Parent2World );

					m_Local2World.Row1 = new Vector4( X, 0.0f );
					m_Local2World.Row2 = new Vector4( Y, 0.0f );
					m_Local2World.Row3 = new Vector4( Z, 0.0f );
					m_Local2World.Row4 = new Vector4( P, 1.0f );

					// Build the inverse
					m_World2Local = m_Local2World;
					m_World2Local.Invert();

					// Propagate to next segment
					if ( m_Next != null )
						m_Next.PropagateEvalFrame( m_Local2World );
				}

				/// <summary>
				/// Clears the growth parameters before propagation (called by the parent segment/branch)
				/// </summary>
				public void		ClearGrowthParameters()
				{
					m_AccumulatedMass = 0.0f;
					m_Torque = Vector3.Zero;
				}

				/// <summary>
				/// Evaluates the torque applied to that segment
				/// NOTE: The computation is performed in WORLD space so PropagateEvalFrame() must have been called prior calling this method
				/// </summary>
				public virtual void	PropagateComputeTorque()
				{
					if ( m_Next == null )
						return;	// Tip has no mass, hence no torque...

					// Start by recursing to next segment as we need its torque to compute ours
					m_Next.ClearGrowthParameters();
					m_Next.PropagateComputeTorque();

					// Accumulate mass
					m_AccumulatedMass += Mass + m_Next.m_AccumulatedMass;

					// The torque formula is "home-made" :
					// ===================================
					// We first compute the torque on the segment :
					//	T = integral[0,L]( (x*L)^(dV(x) * density * G) dx
					// Then you add the torque from the next segment :
					//	T += Next.T
					// Then you add the torque the child branches apply to the tip of that segment :
					//	T += L ^ (TotalMassOfChildren * G)

					// Compute this segment's torque
					int		TorqueMaxStepsCount = 8;

					Vector3	Pn = m_Next.WorldPosition;
					Vector3	P = WorldPosition;
					Vector3	Direction = Pn - P;
					float	L = Direction.Length();
							Direction /= Math.Max( 1e-6f, L );

					float	Density = m_Parent.m_Owner.m_Density;
					float	R0 = m_Radius;
					float	R1 = m_Next.m_Radius;
					float	DR = R1-R0;
					float	dL = L / TorqueMaxStepsCount;

					m_Torque = Vector3.Zero;
					for ( int TorqueStepIndex=0; TorqueStepIndex < TorqueMaxStepsCount; TorqueStepIndex++ )
					{
						float	t0 = (float) TorqueStepIndex / TorqueMaxStepsCount;
						float	t1 = (float) (TorqueStepIndex+1) / TorqueMaxStepsCount;
						float	Li = L * (TorqueStepIndex+0.5f) / TorqueMaxStepsCount;			// Length of lever

						float	r0 = R0 + DR * t0;
						float	r1 = R0 + DR * t1;
						float	dV = (float) Math.PI * (r0*r0 + r0*r1 + r1*r1) * dL / 3.0f;		// Small volume element
						float	dm = dV * Density;												// Small mass element
						Vector3	dT = Vector3.Cross( dm * GRAVITY, Li * Direction );				// Small torque element
						m_Torque += dT;
					}

					// Compute the torque exerted by the mass of the child segments + branches as a point mass at the tip of this segment
					Vector3	ChildTorque = Vector3.Cross( m_Next.m_AccumulatedMass * GRAVITY, L * Direction );
					m_Torque += ChildTorque;

					// Add the next segment's torque
					m_Torque += m_Next.m_Torque;
				}

				/// <summary>
				/// Computes the growth in length or radius for a single segment, based on segment torque and mass parameters
				/// Also computes the need in light & nutrients based on the estimate target volume to grow
				/// </summary>
				public virtual void PropagateComputeGrowthAndNeeds()
				{
					if ( m_Next == null )
						return;

					// Propagate to next segment first
					m_Next.PropagateComputeGrowthAndNeeds();

					Tree	T = m_Parent.m_Owner;
					float	CurrentLength = Length;
					float	CurrentVolume = Volume;

					//////////////////////////////////////////////////////////////////////////
					// Compute the target volume based on torque applied to the segment and total mass to support
					float	TorqueResistance = T.m_TorqueResistancePerVolume;
					float	WeightResistance = T.m_WeightResistancePerVolume;
					float	TorqueAmplitude = m_Torque.Length();			// The torque to support
					float	Weight = m_AccumulatedMass * GRAVITY_LENGTH;	// The weight to support

					float	VolumeToSupportTorque = TorqueAmplitude / TorqueResistance;							// The volume required to support the torque
					float	VolumeToSupportWeight = Weight / WeightResistance;									// The volume required to support the weight
					float	Nr = m_Radius + T.m_RadiusGrowth;													// The volume required to grow steadily in radius
					float	VolumeConstantGrowth = (float) Math.PI * (Nr*Nr + m_Next.m_Radius*m_Next.m_Radius + Nr*m_Next.m_Radius) * CurrentLength / 3.0f;

					m_TargetVolumeForRadiusGrowth = Math.Max( VolumeToSupportTorque, VolumeToSupportWeight );		// Required volume is the greatest of both
					m_TargetVolumeForRadiusGrowth = Math.Max( m_TargetVolumeForRadiusGrowth, VolumeConstantGrowth );// Required volume is the greatest of both
					m_TargetVolumeForRadiusGrowth = Math.Max( CurrentVolume, m_TargetVolumeForRadiusGrowth );		// We never grow back to a lesser volume

					// Compute the target radius by inverting the volume equation
					// V = PI * (r0² + r1² + r0*r1) * Length / 3;
					// so:
					// r0² + r1/3 . r0 - (V / (PI*Length) - r1²) = 0
					// and solve for r0...
					//
					float	b = m_Next.m_TargetRadius;
					float	c = m_Next.m_TargetRadius*m_Next.m_TargetRadius - 3.0f * m_TargetVolumeForRadiusGrowth / ((float) Math.PI * CurrentLength);
					float	Delta = (float) Math.Sqrt( b*b-4*c );
					m_TargetRadius = 0.5f * (-b + Delta);
 
// Should be equal to the target volume for radius
float	Check = (float) Math.PI * CurrentLength * (m_TargetRadius*m_TargetRadius + m_Next.m_TargetRadius*m_Next.m_TargetRadius + m_TargetRadius*m_Next.m_TargetRadius) / 3.0f;

					//////////////////////////////////////////////////////////////////////////
					// Compute the target length which is a constant growth requirement per cycle
//					m_TargetLength = CurrentLength * T.m_LengthGrowthFactor;
					m_TargetLength = CurrentLength + T.m_LengthGrowth;
					m_TargetVolumeForLengthGrowth = (float) Math.PI * m_TargetLength * (m_Radius*m_Radius + m_Next.m_Radius*m_Next.m_Radius + m_Radius*m_Next.m_Radius ) / 3.0f;
					m_TargetVolumeForLengthGrowth = Math.Max( CurrentVolume, m_TargetVolumeForLengthGrowth );	// We never grow back to a lesser volume

					//////////////////////////////////////////////////////////////////////////
					// The actual volume to reach is a mix of the length and radius target volumes
					float	DeltaVolumeRadius = m_TargetVolumeForRadiusGrowth - CurrentVolume;					// The amount to grow to reach new radius volume
					float	DeltaVolumeLength = m_TargetVolumeForLengthGrowth - CurrentVolume;					// The amount to grow to reach new length volume
					if ( m_bActiveLengthGrowth )
					{	// Still growing in length, we must decide which need is more important between radius & length
						float	Radius2LengthRatio = T.m_RadiusToLengthVolumeGrowthRatio;

						float	InverseVolumeGrowthSum = Radius2LengthRatio * DeltaVolumeRadius + DeltaVolumeLength;
						if ( InverseVolumeGrowthSum > 1e-8f )
						{
							InverseVolumeGrowthSum = 1.0f / InverseVolumeGrowthSum;
							m_LengthVolumeGrowthImportance = DeltaVolumeLength * InverseVolumeGrowthSum;
							m_LengthVolumeGrowthImportance *= m_LengthVolumeGrowthImportance;					// So radius importance grows quadratically
							m_RadiusVolumeGrowthImportance = 1.0f - m_LengthVolumeGrowthImportance;
						}
						else
						{	// Take drastic measures to choose between length & radius
							if ( Radius2LengthRatio * DeltaVolumeRadius < DeltaVolumeLength )
							{
								m_RadiusVolumeGrowthImportance = 0.0f;
								m_LengthVolumeGrowthImportance = 1.0f;
							}
							else
							{
								m_RadiusVolumeGrowthImportance = 1.0f;
								m_LengthVolumeGrowthImportance = 0.0f;
							}
						}
					}
					else
					{	// We're only growing in radius from now on...
						m_RadiusVolumeGrowthImportance = 1.0f;
						m_LengthVolumeGrowthImportance = 0.0f;
					}

					m_DeltaVolume = m_RadiusVolumeGrowthImportance * DeltaVolumeRadius
								  + m_LengthVolumeGrowthImportance * DeltaVolumeLength;							// The actual difference in volume to grow
					m_DeltaVolume = Math.Min( m_DeltaVolume, T.m_MaxVolumeGrowthPerSimulationStep );			// But we can't grow more than this per simulation step anyway...
					m_ActualTargetVolume = CurrentVolume + m_DeltaVolume;

					//////////////////////////////////////////////////////////////////////////
					// Compute the need in light & nutrients based on the target volume to acquire
					m_LightNeeds = m_DeltaVolume * T.m_LightNeedUnitPerVolumeGrowth;
					m_NutrientsNeeds = m_DeltaVolume * T.m_NutrientsNeedUnitPerVolumeGrowth;
				}

				/// <summary>
				/// Clears the accumulated light needs before propagation (called by the parent segment/branch)
				/// </summary>
				public void		ClearAccumulatedLightNeeds()
				{
					m_AccumulatedLightNeeds = 0.0f;
				}

				/// <summary>
				/// Propagate the sum of light & nutrient needs
				/// Light needs are accumulated from leaves to roots (basopetal transport)
				/// Nutrient needs are accumulated from roots to leaves (acropetal transport)
				/// 
				/// This method also globally accumulates ligt & nutrient needs of each segment to the entire parent tree
				/// </summary>
				/// <param name="_AccumulatedParentNutrientNeeds">The needs in nutrients by the entire parent hierarchy</param>
				public virtual void PropagateAccumulateLightAndNutrientNeeds( float _AccumulatedParentNutrientNeeds, ref float _AccumulatedLightNeeds, ref float _AccumulatedNutrientNeeds )
				{
					if ( m_Next == null )
						return;	// Leaves don't need anything...
					
					//////////////////////////////////////////////////////////////////////////
					// Accumulate global light & nutrient needs for the entire tree
					_AccumulatedLightNeeds += m_LightNeeds;
					_AccumulatedNutrientNeeds += m_NutrientsNeeds;

					//////////////////////////////////////////////////////////////////////////
					// Accumulate nutrient needs from the roots
					m_AccumulatedNutrientsNeeds = m_NutrientsNeeds					// Our needs
												+ _AccumulatedParentNutrientNeeds;	// + needs of our parent

					//////////////////////////////////////////////////////////////////////////
					// Accumulate light needs from the leaves
					m_Next.ClearAccumulatedLightNeeds();
					m_Next.PropagateAccumulateLightAndNutrientNeeds( m_AccumulatedNutrientsNeeds, ref _AccumulatedLightNeeds, ref _AccumulatedNutrientNeeds );

					m_AccumulatedLightNeeds += m_LightNeeds;						// Our needs
					m_AccumulatedLightNeeds += m_Next.m_AccumulatedLightNeeds;		// + needs of our children
				}

				/// <summary>
				/// Transports light from leaves to root (basopetal)
				/// </summary>
				public virtual float TransportLight()
				{
					if ( m_Next == null )
						return m_Parent.m_Owner.m_LightUnitsPerLeaf;	// Leaf returns its energy
	
					// Compute remaining energy from children
					float	RemainingLight = m_Next.TransportLight();

					// Collect some light based on own need (deduced from the amount of volume we need to grow)
					m_CollectedLight = Math.Min( Math.Max( 0.0f, RemainingLight ), m_LightNeeds );
					RemainingLight -= m_LightNeeds;	// The remaining light can be negative and that's important as a final remaining negative quantity decides for branch splitting !

					return RemainingLight;
				}

				/// <summary>
				/// Transports nutrients from roots to leaves (acropetal)
				/// </summary>
				public virtual void TransportNutrients( float _RemainingNutrients, ref float _GlobalRemainingNutrients )
				{
					if ( m_Next == null )
						return;	// Leaves don't need nutrients...

					// Collect some nutrients based on own need (deduced from the amount of volume we need to grow)
					m_CollectedNutrients = Math.Min( Math.Max( 0.0f, _RemainingNutrients ), m_NutrientsNeeds );
					_RemainingNutrients -= m_NutrientsNeeds;	// The remaining nutrients can be negative and that's important as a final remaining negative quantity decides for root splitting !
					_GlobalRemainingNutrients -= m_NutrientsNeeds;

					// Continue transport toward leaves
					m_Next.TransportNutrients( _RemainingNutrients, ref _GlobalRemainingNutrients );
				}

				/// <summary>
				/// Grows the segment's geometry as a branch (i.e. above ground branch that utilizes light & nutrient inputs)
				/// </summary>
				public virtual void	GrowAsBranch()
				{
					if ( m_Next == null )
						return;	// Leaves don't grow...

					Tree	T = m_Parent.m_Owner;

					// Grow next segment first as we'll need its new radius for our volume computations
					m_Next.GrowAsBranch();

					// Compute the volume we can really grow given what we collected in light & nutrients
					float	GrowableVolumeLight = m_CollectedLight / T.m_LightNeedUnitPerVolumeGrowth;
					float	GrowableVolumeNutrients = m_CollectedNutrients / T.m_NutrientsNeedUnitPerVolumeGrowth;
					float	GrowableVolume = Math.Min( GrowableVolumeLight, GrowableVolumeNutrients );
					float	TargetVolume = Volume + GrowableVolume;

					float	CurrentLength = Length;
					float	CurrentRadius = m_Radius;
					float	NewLength, NewRadius;
					SolveRadiusAndLengthGrowth( CurrentLength, CurrentRadius, m_TargetLength, m_TargetRadius, TargetVolume, m_LengthVolumeGrowthImportance, out NewLength, out NewRadius );

// Should be equal to target volume
double	Check = Math.PI * NewLength * (NewRadius*NewRadius + NewRadius*m_Next.m_Radius + m_Next.m_Radius*m_Next.m_Radius) / 3.0;

// 					m_TargetVolumeForLengthGrowth = (float) Math.PI * m_TargetLength * (m_Radius*m_Radius + m_Next.m_Radius*m_Next.m_Radius + m_Radius*m_Next.m_Radius/3.0f);
// 					float	NewLength = 3.0f * TargetVolume / ((float) Math.PI * (m_Radius*m_Radius + m_Next.m_Radius*m_Next.m_Radius + m_Radius*m_Next.m_Radius));

					// Grow in radius
					m_Radius = NewRadius;

					// Grow in length as long as we're active
					if ( !m_bActiveLengthGrowth )
						return;

					if ( NewLength > T.m_MaxSegmentLength )
					{	// We reached our maximum length
						// We must create a new child segment after ourselves
						Segment	NewChild = new Segment( m_Parent );
						NewChild.m_X = m_X;
						NewChild.m_Y = m_Y;
						NewChild.m_Z = m_Z;
						NewChild.m_Radius = m_Radius + (m_Next.m_Radius-m_Radius) * T.m_MaxSegmentLength / NewLength;	// Its radius is a lerp of current and child radius based on length growth
						NewChild.m_Position = m_Next.m_Position * T.m_MaxSegmentLength / CurrentLength;	// Its position is our current child's position
						NewChild.m_Torque = m_Torque;

						// Update current child's position so it's offset by the excessive length
						m_Next.m_Position *= (NewLength - T.m_MaxSegmentLength) / NewLength;

						// Clamp this segment to max length
						NewLength = T.m_MaxSegmentLength;

						// Link new child
						NewChild.m_Previous = this;
						NewChild.m_Next = m_Next;
						m_Next.m_Previous = NewChild;
						this.m_Next = NewChild;

						// Propagate WORLD frame evaluation again...
						m_Next.PropagateEvalFrame( m_Local2World );

						// We're not active for growing length anymore, we can only grow in radius now !
						m_bActiveLengthGrowth = false;
					}

					// We must limit the rotation angle based on the curvilinear distance a branch can move in a single simulation step
					// The branch has a length L so the curvilinear distance is s=L*Angle (from the well known S=2PI.R perimeter equation of a circle)
					// The maximum angle we can rotate by is then S/L
					float	MaxAngle = Math.Min( 0.5f * (float) Math.PI, T.m_MaxBranchSegmentMotion / NewLength );

					// Update next segment's position to match the new length
					Vector3	LocalDirection = m_Next.m_Position;
							LocalDirection.Normalize();
					Vector3	NewDirection = EvaluateNewBranchGrowthDirection( LocalDirection, NewLength, MaxAngle );
					m_Next.m_Position = NewLength * NewDirection;
				}

				/// <summary>
				/// Grows the segment's geometry as a root (i.e. below ground root that does not utilizes light & nutrient inputs) (although real tree roots do)
				/// </summary>
				public virtual void	GrowAsRoot()
				{
					if ( m_Next == null )
						return;	// Root tips don't grow...

					Tree	T = m_Parent.m_Owner;

					// Grow next segment first as we'll need its new radius for our volume computations
					m_Next.GrowAsRoot();

					// Compute the volume we can really grow given what we collected in light & nutrients
// 					float	GrowableVolumeLight = m_CollectedLight / T.m_LightNeedUnitPerVolumeGrowth;
// 					float	GrowableVolumeNutrients = m_CollectedNutrients / T.m_NutrientsNeedUnitPerVolumeGrowth;
// 					float	GrowableVolume = Math.Min( GrowableVolumeLight, GrowableVolumeNutrients );
// 					float	TargetVolume = Volume + GrowableVolume;

					// We don't use collected light & nutrients here but a fixed growable volume
					// TODO: Either simulate light & nutrients collection or some other strategy
 					float	TargetVolume = Volume * 1.1f;

					float	CurrentLength = Length;
					float	CurrentRadius = m_Radius;

					// As we didn't collect nutrients & light, we need to compute our target radius & length, and their relative importance at this point
					float	b = m_Next.m_TargetRadius;
					float	c = m_Next.m_TargetRadius*m_Next.m_TargetRadius - 3.0f * TargetVolume / ((float) Math.PI * CurrentLength);
					float	Delta = (float) Math.Sqrt( b*b-4*c );
					m_TargetRadius = 0.5f * (-b + Delta);
//					m_TargetLength = CurrentLength * T.m_LengthGrowthFactor;
					m_TargetLength = CurrentLength + T.m_LengthGrowth;

					m_LengthVolumeGrowthImportance = 1.0f / (1.0f+T.m_RadiusToLengthVolumeGrowthRatio);
					m_LengthVolumeGrowthImportance *= m_LengthVolumeGrowthImportance;
					m_RadiusVolumeGrowthImportance = 1.0f - m_LengthVolumeGrowthImportance;

					float	NewLength, NewRadius;
					SolveRadiusAndLengthGrowth( CurrentLength, CurrentRadius, m_TargetLength, m_TargetRadius, TargetVolume, m_LengthVolumeGrowthImportance, out NewLength, out NewRadius );

// Should be equal to target volume
double	Check = Math.PI * NewLength * (NewRadius*NewRadius + NewRadius*m_Next.m_Radius + m_Next.m_Radius*m_Next.m_Radius) / 3.0;

// 					m_TargetVolumeForLengthGrowth = (float) Math.PI * m_TargetLength * (m_Radius*m_Radius + m_Next.m_Radius*m_Next.m_Radius + m_Radius*m_Next.m_Radius/3.0f);
// 					float	NewLength = 3.0f * TargetVolume / ((float) Math.PI * (m_Radius*m_Radius + m_Next.m_Radius*m_Next.m_Radius + m_Radius*m_Next.m_Radius));

					// Grow in radius
					m_Radius = NewRadius;

					// Grow in length as long as we're active
					if ( !m_bActiveLengthGrowth )
						return;

					if ( NewLength > T.m_MaxSegmentLength )
					{	// We reached our maximum length
						// We must create a new child segment after ourselves
						Segment	NewChild = new Segment( m_Parent );
						NewChild.m_X = m_X;
						NewChild.m_Y = m_Y;
						NewChild.m_Z = m_Z;
						NewChild.m_Radius = m_Next.m_Radius;	// Its radius is our current child's radius
						NewChild.m_Position = m_Next.m_Position * T.m_MaxSegmentLength / CurrentLength;	// Its position is our current child's position
						NewChild.m_Torque = m_Torque;

						// Update current child's position so it's offset by the excessive length
						m_Next.m_Position *= (NewLength - T.m_MaxSegmentLength) / NewLength;

						// Clamp this segement to max length
						NewLength = T.m_MaxSegmentLength;

						// Link new child
						NewChild.m_Previous = this;
						NewChild.m_Next = m_Next;
						m_Next.m_Previous = NewChild;
						this.m_Next = NewChild;

						// Propagate WORLD frame evaluation again...
						m_Next.PropagateEvalFrame( m_Local2World );

						// We're not active for growing length anymore, we can only grow in radius now !
						m_bActiveLengthGrowth = false;
					}

					// We must limit the rotation angle based on the curvilinear distance a root can move in a single simulation step
					// The root has a length L so the curvilinear distance is s=L*Angle (from the well known S=2PI.R perimeter equation of a circle)
					// The maximum angle we can rotate by is then S/L
					float	MaxAngle = Math.Min( 0.5f * (float) Math.PI, T.m_MaxRootSegmentMotion / NewLength );

					// Update next segment's position to match the new length
					Vector3	LocalDirection = m_Next.m_Position;
							LocalDirection.Normalize();
					Vector3	NewDirection = EvaluateNewRootGrowthDirection( LocalDirection, NewLength, MaxAngle );
					m_Next.m_Position = NewLength * NewDirection;
				}

				/// <summary>
				/// Splits the segment
				/// </summary>
				/// <returns>The new split segment</returns>
				public virtual Segment	SplitAsBranch()
				{
					Tree	T = m_Parent.m_Owner;

					//////////////////////////////////////////////////////////////////////////
					// Determine orientation the new branch splitting off
					float	BranchingPosition = T.m_BaseBranchingPosition * (1.0f + T.m_BaseBranchingPositionVariance * T.NewSignedRandom() );
					Vector3	SplitPosition = BranchingPosition * m_Next.m_Position;
					float	SplitRadius = m_Radius + (m_Next.m_Radius - m_Radius) * BranchingPosition;

					// First, determine a possible initial direction for the new branch that we don't limit in angle except a maximum 90° off angle
					Vector3	LocalDirection = m_Next.m_Position;
							LocalDirection.Normalize();
					Vector3	NewDirection = EvaluateNewBranchGrowthDirection( LocalDirection, 0.1f, 0.5f * (float) Math.PI );

					// It's very likely the new direction is pretty close to this segment's direction
					// In that case, we must force the direction off our segment's direction
					// We first determine the angle covered by the base of the segment as seen from split position :
					//
					//       . SplitPosition
					//       |.
					//       | .
					//       | .
					//       |  .
					//    _ -|- _.
					//   (   o...) Radius
					//    '-----'
					//
					float	SqSplitDistance = SplitPosition.LengthSquared();
					float	CosSolidAngle = (float) Math.Sqrt( 1.0 - Math.Min( 1.0, m_Radius*m_Radius / SqSplitDistance ) );
					float	Dot = Vector3.Dot( LocalDirection, NewDirection );
					if ( Dot > CosSolidAngle )
					{
						if ( Dot > 1.0f-1e-5f )
						{	// So close we need to draw a entirely new random direction altogether
							float	Theta = T.m_BaseBranchingAngle * (1.0f + T.m_BaseBranchingAngleVariance * T.NewSignedRandom() );
							float	Phi = 2.0f * (float) Math.PI * T.NewRandom();
							NewDirection.X = (float) (Math.Sin( Phi ) * Math.Sin( Theta ));
							NewDirection.Y = (float) Math.Cos( Theta );
							NewDirection.Z = (float) (Math.Cos( Phi ) * Math.Sin( Theta ));
						}
						else
						{	// Make sure the branch goes in a direction clearly away from ours
							Vector3	Ortho = Vector3.Cross( LocalDirection, NewDirection );
							Ortho.Normalize();
							float	MinAngle = (float) Math.Acos( CosSolidAngle );	// The minimum branching angle to escape our direction
							float	MaxAngle = T.m_BaseBranchingAngle;				// The prefered branching angle
							float	Angle = MinAngle + (MaxAngle - MinAngle) * T.NewRandom();
							NewDirection = RotateAxisByAngle( LocalDirection, Angle, Ortho );
// Should be equal to "Angle"
float	Check = (float) Math.Acos( Vector3.Dot( LocalDirection, NewDirection ) );
						}
					}

					//////////////////////////////////////////////////////////////////////////
					// Balance splitting angle between the new branch and that segment

					// This is the default branching balancing
					float			BranchingBalancingFactor = T.m_BranchingBalancing * (1.0f + T.m_BranchingBalancingVariance * T.NewSignedRandom());

					// Also bring that balancing toward 0 based on that segment's position within the branch
					// (indeed, we don't want a root segment and all its sub-branches bend all of a sudden)
					BranchingBalancingFactor *= (float) (0.5f+PreviousSegmentsCount) / (m_Parent.BranchSegmentsCount-1);

					Vector3			BranchingRotationAxis = Vector3.Cross( LocalDirection, NewDirection );
					float			AxisLength = BranchingRotationAxis.Length();
					BranchingRotationAxis /= AxisLength > 1e-6f ? AxisLength : 1.0f;
					float			BranchingAngle = (float) Math.Asin( AxisLength );
					WMath.AngleAxis	NewBranchAA = new WMath.AngleAxis( BranchingAngle * (1.0f - BranchingBalancingFactor), BranchingRotationAxis.X, BranchingRotationAxis.Y, BranchingRotationAxis.Z );
					WMath.Matrix4x4	NewBranchRotation = (WMath.Matrix4x4) (WMath.Quat) NewBranchAA;

					WMath.AngleAxis	ThisAA = new WMath.AngleAxis( BranchingAngle * BranchingBalancingFactor, BranchingRotationAxis.X, BranchingRotationAxis.Y, BranchingRotationAxis.Z );
					WMath.Matrix4x4	ThisRotation = (WMath.Matrix4x4) (WMath.Quat) ThisAA;

					// Build rotated axes for new branch
					WMath.Matrix4x4	InitialMatrix = new WMath.Matrix4x4();
					InitialMatrix.MakeIdentity();
					InitialMatrix.m[0,0] = m_X.X; InitialMatrix.m[0,1] = m_X.Y; InitialMatrix.m[0,2] = m_X.Z;
					InitialMatrix.m[1,0] = m_Y.X; InitialMatrix.m[1,1] = m_Y.Y; InitialMatrix.m[1,2] = m_Y.Z;
					InitialMatrix.m[2,0] = m_Z.X; InitialMatrix.m[2,1] = m_Z.Y; InitialMatrix.m[2,2] = m_Z.Z;

					WMath.Matrix4x4	BranchAxes = InitialMatrix * NewBranchRotation;
					WMath.Matrix4x4	ThisAxes = InitialMatrix * ThisRotation;

					//////////////////////////////////////////////////////////////////////////
					// Create a new branch and transform this segment into a split segment
					Branch			B = new Branch( T, T.m_TrunkBranchesCount, m_Parent.m_Level+1 );
					B.Initialize(	SplitPosition,
									new Vector3( BranchAxes.m[0,0], BranchAxes.m[0,1], BranchAxes.m[0,2] ),
									new Vector3( BranchAxes.m[1,0], BranchAxes.m[1,1], BranchAxes.m[1,2] ),
									new Vector3( BranchAxes.m[2,0], BranchAxes.m[2,1], BranchAxes.m[2,2] ),
									SplitRadius,
									0.001f
								);
					SplitSegment	S = new SplitSegment( m_Parent, B );
					S.Initialize(	m_Position,
									new Vector3( ThisAxes.m[0,0], ThisAxes.m[0,1], ThisAxes.m[0,2] ),
									new Vector3( ThisAxes.m[1,0], ThisAxes.m[1,1], ThisAxes.m[1,2] ),
									new Vector3( ThisAxes.m[2,0], ThisAxes.m[2,1], ThisAxes.m[2,2] ),
									m_Radius
								);

					S.m_Previous = m_Previous;
					S.m_Next = m_Next;
					if ( m_Next != null )
						m_Next.m_Previous = S;
					if ( m_Previous != null )
						m_Previous.m_Next = S;
					else
						m_Parent.m_StartSegment = S;	// We're the new start segment !

					// Add one more branch to the tree
					T.m_TrunkBranchesCount++;

					return S;
				}

				/// <summary>
				/// Splits the segment
				/// </summary>
				/// <returns>The new split segment</returns>
				public virtual Segment	SplitAsRoot()
				{
					Tree	T = m_Parent.m_Owner;

					//////////////////////////////////////////////////////////////////////////
					// Determine orientation of the new branch splitting off
					float	BranchingPosition = T.m_BaseBranchingPosition * (1.0f + T.m_BaseBranchingPositionVariance * T.NewSignedRandom() );
					Vector3	SplitPosition = BranchingPosition * m_Next.m_Position;
					float	SplitRadius = m_Radius + (m_Next.m_Radius - m_Radius) * BranchingPosition;

					// First, determine a possible initial direction for the new root that we don't limit in angle except a maximum 90° off angle
					Vector3	LocalDirection = m_Next.m_Position;
							LocalDirection.Normalize();
					Vector3	NewDirection = EvaluateNewRootGrowthDirection( LocalDirection, 0.1f, 0.5f * (float) Math.PI );

					// It's very likely the new direction is pretty close to this segment's direction
					// In that case, we must force the direction off our segment's direction
					// We first determine the angle covered by the base of the segment as seen from split position :
					//
					//       . P
					//       |
					//       |
					//       |
					//       |
					//    _ -|- _
					//   (   o...) R
					//    '-----'
					//
					float	SqSplitDistance = SplitPosition.LengthSquared();
					float	CosSolidAngle = (float) Math.Sqrt( 1.0 - Math.Min( 1.0, m_Radius*m_Radius / SqSplitDistance ) );
					float	Dot = Vector3.Dot( LocalDirection, NewDirection );
					if ( Dot > CosSolidAngle )
					{
						if ( Dot > 1.0f-1e-5f )
						{	// So close we need to draw a new random direction altogether
							float	Theta = T.m_BaseBranchingAngle * (1.0f + T.m_BaseBranchingAngleVariance * T.NewSignedRandom() );
							float	Phi = 2.0f * (float) Math.PI * T.NewRandom();
							NewDirection.X = (float) (Math.Sin( Phi ) * Math.Sin( Theta ));
							NewDirection.Y = (float) Math.Cos( Theta );
							NewDirection.Z = (float) (Math.Cos( Phi ) * Math.Sin( Theta ));
						}
						else
						{	// Make sure the branch goes in a direction clearly away from ours
							Vector3	Ortho = Vector3.Cross( LocalDirection, NewDirection );
							Ortho.Normalize();
							float	MinAngle = (float) Math.Acos( CosSolidAngle );	// The minimum branching angle to escape our direction
							float	MaxAngle = T.m_BaseBranchingAngle;				// The prefered branching angle
							float	Angle = MinAngle + (MaxAngle - MinAngle) * T.NewRandom();
							NewDirection = RotateAxisByAngle( LocalDirection, Angle, Ortho );
// Should be equal to "Angle"
double	Check = Math.Acos( Vector3.Dot( LocalDirection, NewDirection ) );
						}
					}

					//////////////////////////////////////////////////////////////////////////
					// Balance splitting angle between the new branch and that segment

					// This is the default branching balancing
					float			BranchingBalancingFactor = T.m_BranchingBalancing * (1.0f + T.m_BranchingBalancingVariance * T.NewSignedRandom());

					// Also bring that balancing toward 0 based on that segment's position within the branch
					// (indeed, we don't want a root segment and all its sub-branches bend all of a sudden)
					BranchingBalancingFactor *= (float) (0.5f+PreviousSegmentsCount) / (m_Parent.BranchSegmentsCount-1);

					Vector3			BranchingRotationAxis = Vector3.Cross( LocalDirection, NewDirection );
					float			AxisLength = BranchingRotationAxis.Length();
					BranchingRotationAxis /= AxisLength > 1e-6f ? AxisLength : 1.0f;
					float			BranchingAngle = (float) Math.Asin( AxisLength );
					WMath.AngleAxis	NewBranchAA = new WMath.AngleAxis( BranchingAngle * (1.0f - BranchingBalancingFactor), BranchingRotationAxis.X, BranchingRotationAxis.Y, BranchingRotationAxis.Z );
					WMath.Matrix4x4	NewBranchRotation = (WMath.Matrix4x4) (WMath.Quat) NewBranchAA;

					WMath.AngleAxis	ThisAA = new WMath.AngleAxis( BranchingAngle * BranchingBalancingFactor, BranchingRotationAxis.X, BranchingRotationAxis.Y, BranchingRotationAxis.Z );
					WMath.Matrix4x4	ThisRotation = (WMath.Matrix4x4) (WMath.Quat) ThisAA;

					// Build rotated axes for new branch
					WMath.Matrix4x4	InitialMatrix = new WMath.Matrix4x4();
					InitialMatrix.MakeIdentity();
					InitialMatrix.m[0,0] = m_X.X; InitialMatrix.m[0,1] = m_X.Y; InitialMatrix.m[0,2] = m_X.Z;
					InitialMatrix.m[1,0] = m_Y.X; InitialMatrix.m[1,1] = m_Y.Y; InitialMatrix.m[1,2] = m_Y.Z;
					InitialMatrix.m[2,0] = m_Z.X; InitialMatrix.m[2,1] = m_Z.Y; InitialMatrix.m[2,2] = m_Z.Z;

					WMath.Matrix4x4	BranchAxes = InitialMatrix * NewBranchRotation;
					WMath.Matrix4x4	ThisAxes = InitialMatrix * ThisRotation;

					//////////////////////////////////////////////////////////////////////////
					// As nutrients come from the area surface of roots, we need to make
					//	the root's initial length sufficiently large to provide the required
					//	initial requested quantity...
					float	fTargetArea = T.m_NutrientUnitsPerNewRoot / T.m_NutrientUnitsPerRootSurfaceArea;

					// Invert area formula to retrieve the target length
					float	L = Length;
					float	Dr = 0.0f - SplitRadius;
					float	Ar = 0.0f + SplitRadius;
					float	fNewBranchLength = (float) Math.Sqrt( fTargetArea*fTargetArea / (Math.PI * Math.PI * Ar*Ar) - Dr*Dr );
					fNewBranchLength = Math.Min( T.m_MaxSegmentLength, fNewBranchLength );

// Should be equal to target area
double Check2 = Math.PI * Ar * Math.Sqrt( fNewBranchLength*fNewBranchLength + Dr*Dr );

					//////////////////////////////////////////////////////////////////////////
					// Create a new branch and transform this segment into a split segment
					Branch			B = new Branch( T, T.m_RootBranchesCount, m_Parent.m_Level+1 );
					B.Initialize(	SplitPosition,
									new Vector3( BranchAxes.m[0,0], BranchAxes.m[0,1], BranchAxes.m[0,2] ),
									new Vector3( BranchAxes.m[1,0], BranchAxes.m[1,1], BranchAxes.m[1,2] ),
									new Vector3( BranchAxes.m[2,0], BranchAxes.m[2,1], BranchAxes.m[2,2] ),
									SplitRadius,
									fNewBranchLength
								);
					SplitSegment	S = new SplitSegment( m_Parent, B );
					S.Initialize(	m_Position,
									new Vector3( ThisAxes.m[0,0], ThisAxes.m[0,1], ThisAxes.m[0,2] ),
									new Vector3( ThisAxes.m[1,0], ThisAxes.m[1,1], ThisAxes.m[1,2] ),
									new Vector3( ThisAxes.m[2,0], ThisAxes.m[2,1], ThisAxes.m[2,2] ),
									m_Radius
								);

					S.m_Previous = m_Previous;
					S.m_Next = m_Next;
					if ( m_Next != null )
						m_Next.m_Previous = S;
					if ( m_Previous != null )
						m_Previous.m_Next = S;
					else
						m_Parent.m_StartSegment = S;	// We're the new start segment !

					// Add one more root to the tree
					T.m_RootBranchesCount++;

					return S;
				}

				/// <summary>
				/// Evaluates a new prefered growth direction based on current direction and needs
				/// TODO: Apply some noise
				/// </summary>
				/// <returns></returns>
				protected Vector3	EvaluateNewBranchGrowthDirection( Vector3 _LocalDirection, float _TargetSegmentLength, float _MaxAngle )
				{
					Tree	T = m_Parent.m_Owner;

					// Evaluate current world growth direction
					Vector3	WorldDirection = Vector3.TransformNormal( _LocalDirection, m_Local2World );

					// Evaluate direction to light and required rotation
					Vector3	LightMainDirection = LightHemisphere.Hemisphere2World( T.m_LightHemisphere.ComputePreferedDirection( LightHemisphere.World2Hemisphere( WorldDirection ) ) );
					Vector3	LightAxis = Vector3.Cross( WorldDirection, LightMainDirection );
					float	AxisLength = LightAxis.Length();
					if ( AxisLength > 1e-6f )
						LightAxis /= AxisLength;
					float	LightAngle = (float) Math.Asin( AxisLength );
					WMath.AngleAxis	AALight = new WMath.AngleAxis( LightAngle, LightAxis.X, LightAxis.Y, LightAxis.Z );
					WMath.Quat		QLight = (WMath.Quat) AALight;

					// Evaluate torque balancing
					float	TorqueAmplitude = m_Torque.Length();
					Vector3	TorqueAxis = m_Torque / (TorqueAmplitude > 1e-6f ? TorqueAmplitude : 1.0f);
					float	TorqueAngle = -T.m_TorqueAmplitudeToCompensationAngle * TorqueAmplitude;
					TorqueAngle = Math.Max( -0.5f * (float) Math.PI, Math.Min( +0.5f * (float) Math.PI, TorqueAngle ) );
					WMath.AngleAxis	AATorque = new WMath.AngleAxis( TorqueAngle, TorqueAxis.X, TorqueAxis.Y, TorqueAxis.Z );
					WMath.Quat		QTorque = (WMath.Quat) AATorque;

					// The actual quaternion is an interpolation between serach for light and torque balancing based on importance of both parameters
					WMath.Quat	Q = new WMath.Quat();
					Q.MakeSLERP( QTorque, QLight, m_LengthVolumeGrowthImportance );
					WMath.AngleAxis	AA = new WMath.AngleAxis( Q );

					// Limit the rotation angle
					AA.Angle = Math.Max( -_MaxAngle, Math.Min( _MaxAngle, AA.Angle ) );

					// Apply rotation in WORLD space
					Vector3		TargetWorldDirection = RotateAxisByAngle( WorldDirection, AA );

					// Convert back into LOCAL space
					Vector3		TargetLocalDirection = Vector3.TransformNormal( TargetWorldDirection, m_World2Local );

					return TargetLocalDirection;
				}

				/// <summary>
				/// Evaluates a new prefered growth direction based on current direction and needs
				/// TODO: Apply some noise
				/// </summary>
				/// <returns></returns>
				protected Vector3	EvaluateNewRootGrowthDirection( Vector3 _LocalDirection, float _TargetSegmentLength, float _MaxAngle )
				{
					Tree	T = m_Parent.m_Owner;

					// Evaluate current world growth direction
					Vector3	WorldDirection = Vector3.TransformNormal( _LocalDirection, m_Local2World );

					// Evaluate direction to nutrients and required rotation
//					Vector3	NutrientsMainDirection = LightHemisphere.Hemisphere2World( T.m_LightHemisphere.ComputePreferedDirection( LightHemisphere.World2Hemisphere( WorldDirection ) ) );
					// TODO: For the moment we use our original direction although we should use a nutrients search strategy
					Vector3	NutrientsMainDirection = WorldDirection;

					Vector3	NutrientsAxis = Vector3.Cross( WorldDirection, NutrientsMainDirection );
					float	AxisLength = NutrientsAxis.Length();
					if ( AxisLength > 1e-6f )
						NutrientsAxis /= AxisLength;
					float	NutrientsAngle = (float) Math.Asin( AxisLength );
					WMath.AngleAxis	AANutrients = new WMath.AngleAxis( NutrientsAngle, NutrientsAxis.X, NutrientsAxis.Y, NutrientsAxis.Z );
					WMath.Quat		QNutrients = (WMath.Quat) AANutrients;

					// Evaluate torque balancing
					Vector3	Torque = T.m_Trunk.Torque;	// We use the entire tree's torque here
					
					float	TorqueAmplitude = Torque.Length();
					Vector3	TorqueAxis = Torque / (TorqueAmplitude > 1e-6f ? TorqueAmplitude : 1.0f);

					TorqueAmplitude /= T.m_RootBranchesCount;	// We assume all roots will balance the torque so we equally split the work

					float	TorqueAngle = -T.m_TorqueAmplitudeToCompensationAngle * TorqueAmplitude;
					TorqueAngle = Math.Max( -0.5f * (float) Math.PI, Math.Min( +0.5f * (float) Math.PI, TorqueAngle ) );
					WMath.AngleAxis	AATorque = new WMath.AngleAxis( TorqueAngle, TorqueAxis.X, TorqueAxis.Y, TorqueAxis.Z );
					WMath.Quat		QTorque = (WMath.Quat) AATorque;

					// The actual quaternion is an interpolation between serach for nutrients and torque balancing based on importance of both parameters
					WMath.Quat	Q = new WMath.Quat();
					Q.MakeSLERP( QTorque, QNutrients, 0.5f );
					WMath.AngleAxis	AA = new WMath.AngleAxis( Q );

					// Limit the rotation angle
					AA.Angle = Math.Max( -_MaxAngle, Math.Min( _MaxAngle, AA.Angle ) );

					// Apply rotation in WORLD space
					Vector3	TargetWorldDirection = RotateAxisByAngle( WorldDirection, AA );

					// Convert back into LOCAL space
					Vector3	TargetLocalDirection = Vector3.TransformNormal( TargetWorldDirection, m_World2Local );

					return TargetLocalDirection;
				}

				/// <summary>
				/// Solves the new radius & length to reach the designated target volume given current values and importance of growth in length and radius
				/// </summary>
				/// <param name="_CurrentLength">The current branch length</param>
				/// <param name="_CurrentRadius">The current branch radius</param>
				/// <param name="_TargetLength">The target branch length that we should reach if _LengthGrowthImportance == 1</param>
				/// <param name="_TargetRadius">The target branch radius that we should reach if _LengthGrowthImportance == 0</param>
				/// <param name="_LengthGrowthImportance">The balance in [0,1] between growing entirely in length (1) and growing entirely in radius (0)</param>
				/// <param name="_TargetVolume">The volume to reach by growing either in radius or in length</param>
				/// <param name="_NewLength">The new length we should grow</param>
				/// <param name="_NewRadius">The new radius we should grow</param>
				protected void		SolveRadiusAndLengthGrowth( float _CurrentLength, float _CurrentRadius, float _TargetLength, float _TargetRadius, float _TargetVolume, float _LengthGrowthImportance, out float _NewLength, out float _NewRadius )
				{
					if ( !m_bActiveLengthGrowth )
					{	// We're an old segment and can only grow our radius now... (My my ! Where are the times when we could grow length ?)

						// Compute the new radius by inverting the volume equation
						// V = PI * (r0² + r1² + r0*r1)/3 * Length;
						// so:
						// r0² + r1.r0 - (3 V / (PI*Length) - r1²) = 0
						// and solve for r0...
						//
						float	b = m_Next.m_Radius;
						float	c = m_Next.m_Radius*m_Next.m_Radius - 3.0f * _TargetVolume / ((float) Math.PI * _CurrentLength);
						float	Delta = (float) Math.Sqrt( b*b-4*c );
						_NewRadius = 0.5f * (-b + Delta);
						_NewLength = _CurrentLength;	// No growth
						return;
					}

					if ( _CurrentLength < 1e-3f || _CurrentRadius < 1e-3f || (_TargetVolume-Volume) < 1e-6f )
					{	// Below these sizes, the root solver is too unstable and returns dangerous roots
						//	so we simply grow as indicated...
						_NewRadius = _TargetRadius;
						_NewLength = _TargetLength;
						return;
					}

					// Based on the next segment's radius and our current radius and length, determine how we should
					//	split the growing process between radius increase and length increase
					//
					// We pose :
					//	V' = PI * L' * (r0'² + r1² + r0'.r1)/3
					// V' is our target volume (known)
					// L' is our target length (unknown)
					// r0' is our target radius (unknown)
					//
					// Reformulating for L' and r0' :
					//	L'.r0'² + r1.L'.r0' + r1².L' - 3V'/PI = 0   (1)
					//	
					//	We normalize the equation (1) using :
					//	x = (L' - L) / (TL - L) = (L' - L) / Dl  so x € [0,1]
					//	y = (r0' - r0) / (Tr - r0) = (r0' - r0) / Dr  so y € [0,1]
					//	
					//	with TL the Target Length and TR the Target Radius
					//	So L' = L + x.Dl and r0' = r0 + y.Dr
					//	
					//	Rewriting (1) :
					//	(L+x.Dl)*(r0+y.Dr)² + r1.(L+x.Dl)*(r0+y.Dr) + r1².(L+x.Dl) - 3V'/PI = 0   (2)
					//	
					//	And re-ordering (2) in terms of x and y :
					//	[Dr².Dl.x + Dr².L].y² + [Dr.Dl.(2r0+r1)].x.y + Dl[r0²+r0.r1+r1²].x + [2.r0.L.Dr + r1.L.Dr].y + (r0²+r0.r1.r1²).L - 3V'/PI = 0  (3)
					//
					// This defines a 2nd order curve of iso-volumes which we need intersect to find
					//	 the proper values for r0' and L'
					//
					// We also know the importance of each growth factor for both length and radius :
					//	
					//	  ^                
					//	1 |_____           o <= Length importance
					//	  |     ---      _ :
					//	  |         -- _   :
					//	  |         __ -   :
					//	  |     ___      - :
					//	  |_____           x <= Radius importance (always quadratically more important than length)
					//	0 ------------------->
					//	                   1
					//
					//	Their sum is always 1.
					//	
					//	 Radius
					//	   ^
					//	   |         / <= Bisector
					//	Dr o...     /
					//	   |    .../
					//	   |      / ... 
					//	   |     /      ..
					//	   |-.  /         .
					//	   |  \/            .
					//	   |A /              .
					//	   | /                .
					//	   |/                 .
					//	 0 |-------------------o---> Length
					//	   0                   Dl
					//	
					//	Dr is our additional radius if radius growth importance is 1 (i.e. fully growing in radius)
					//	Dl is our additional length if length growth importance is 1 (i.e. fully growing in length)
					//	A is our bisector angle which we define as :
					//		A = PI/2 * LengthGrowthImportance
					//	
					//	Now we can define the parametric equation of the bisector :
					//		y = cos( A ) t = c.t  (for concision)
					//		x = sin( A ) t = s.t
					//
					//  Substituting for x and y in (3) we obtain :
					//
					//	  [Dr².Dl.s.c²].t^3
					//	+ [(Dr².L.c²) + (Dr.Dl.(2r0+r1).c.s)].t²
					//	+ [Dl(r0²+r0.r1+r1²).s + (2.r0.L.Dr + r1.L.Dr).c].t
					//	+ (r0²+r0.r1.r1²).L - 3V'/PI = 0  (4)
					//
					//	Which is a cubic we solve for t.
					//
					double		Theta = 0.5 * Math.PI * _LengthGrowthImportance;
					double		Cos = Math.Cos( Theta );
					double		Sin = Math.Sin( Theta );
					double		L = _CurrentLength;
					double		r0 = m_Radius;
					double		r1 = m_Next.m_Radius;
					double		Dr = _TargetRadius - _CurrentRadius;
					double		Dl = _TargetLength - _CurrentLength;

					double		A = L*(r0*r0+r0*r1+r1*r1) - 3.0f * _TargetVolume / Math.PI;
					double		B = Dl*(r0*r0+r0*r1+r1*r1)*Sin + (2.0*r0*L*Dr + r1*L*Dr)*Cos;
					double		C = Dr*Dr*L*Cos*Cos + Dr*Dl*(2.0*r0+r1)*Cos*Sin;
					double		D = Dr*Dr*Dl*Sin*Cos*Cos;
					double[]	Roots = SolveCubic( A, B, C, D );

					double	BestT = double.NaN;
					if ( Roots[0] >= 0.0f && Roots[0] <= Math.Sqrt( 2.0 ) && (double.IsNaN( BestT ) || Roots[0] > BestT) )
						BestT = Roots[0];
					if ( Roots[1] >= 0.0f && Roots[1] <= Math.Sqrt( 2.0 ) && (double.IsNaN( BestT ) || Roots[1] > BestT) )
						BestT = Roots[1];
					if ( Roots[2] >= 0.0f && Roots[2] <= Math.Sqrt( 2.0 ) && (double.IsNaN( BestT ) || Roots[2] > BestT) )
						BestT = Roots[2];
					if ( double.IsNaN( BestT ) )
					{
// 						_NewRadius = _TargetRadius;
// 						_NewLength = _TargetLength;
// 						return;
						throw new Exception( "Failed to find a valid root for cubic volume solver ! Can't grow !" );
					}

					// We got our new radius & length !
					_NewRadius = (float) (r0 + Cos * BestT * Dr);
					_NewLength = (float) (L + Sin * BestT * Dl);
				}

				/// <summary>
				/// Rotates the provided axis by a given angle about the provided rotation axis
				/// </summary>
				/// <param name="_Axis">The axis to rotate</param>
				/// <param name="_Angle">The angle to rotate</param>
				/// <param name="_RotationAxis">The axis to rotate about</param>
				/// <returns></returns>
				protected Vector3	RotateAxisByAngle( Vector3 _Axis, float _Angle, Vector3 _RotationAxis )
				{
					return RotateAxisByAngle( _Axis, new WMath.AngleAxis( _Angle, _RotationAxis.X, _RotationAxis.Y, _RotationAxis.Z ) );
				}

				/// <summary>
				/// Rotates the provided axis by a given angle about the provided rotation axis
				/// </summary>
				/// <param name="_Axis">The axis to rotate</param>
				/// <param name="_AngleAxis">The angle axis to rotate about</param>
				/// <returns></returns>
				protected Vector3	RotateAxisByAngle( Vector3 _Axis, WMath.AngleAxis _AngleAxis )
				{
					WMath.Quat		Q = (WMath.Quat) _AngleAxis;
					WMath.Matrix4x4	Rot = (WMath.Matrix4x4) Q;
					WMath.Vector	Result = new WMath.Vector( _Axis.X, _Axis.Y, _Axis.Z ) * Rot;

					return new Vector3( Result.x, Result.y, Result.z );
				}

				#endregion

				#region Root Solvers

				/// <summary>
				/// Returns the single root of a linear polynomial a + b x = 0
				/// </summary>
				/// <param name="a"></param>
				/// <param name="b"></param>
				/// <returns></returns>
				protected double	SolveLinear( double a, double b )
				{
					return Math.Abs( b ) > 1e-12 ? -a / b : 0.0;
				}

				/// <summary>
				/// Returns the array of 2 real roots of a quadratic polynomial  a + b x + c x^2 = 0
				/// </summary>
				/// <param name="a"></param>
				/// <param name="b"></param>
				/// <param name="c"></param>
				/// <returns></returns>
				protected double[]	SolveQuadratic( double a, double b, double c )
				{
					double[]	Result = new double[2];
					if ( Math.Abs( c ) < 1e-12 )
					{	// Solve as linear instead
						Result[0] = SolveLinear( a, b );
						Result[1] = double.NaN;
						return	Result;
					}

					double		Delta = b * b - 4 * a * c;
					if ( Delta >= 0.0 )
					{
						Delta = Math.Sqrt( Delta );
						double	OneOver2a = 0.5 / a;

						Result[0] = OneOver2a * (-b - Delta);
						Result[1] = OneOver2a * (-b + Delta);
					}
					else
						Result[0] = Result[1] = double.NaN;

					return Result;
				}

				/// <summary>
				/// Returns the array of 3 real roots of a cubic polynomial  a + b x + c x^2 + d x^3 = 0
				/// NOTE: If roots are imaginary, the returned value in the array will be "undefined"
				/// Code from http://www.codeguru.com/forum/archive/index.php/t-265551.html (pretty much the same as http://mathworld.wolfram.com/CubicFormula.html)
				/// </summary>
				/// <param name="_Coefficients"></param>
				/// <returns></returns>
				protected double[]	SolveCubic( double a, double b, double c, double d )
				{
					double[]	Result = new double[3];
					if ( Math.Abs( d ) < 1e-12 )
					{	// Solve as quadratic instead
						double[]	Temp = SolveQuadratic( a, b, c );
						Result[0] = Temp[0];
						Result[1] = Temp[1];
						Result[2] = double.NaN;
						return	Result;
					}

					// Adjust coefficients
					double a1 = c / d;
					double a2 = b / d;
					double a3 = a / d;

					double Q = (a1 * a1 - 3 * a2) / 9;
					double R = (2 * a1 * a1 * a1 - 9 * a1 * a2 + 27 * a3) / 54;
					double Qcubed = Q * Q * Q;
					double Delta = Qcubed - R * R;

					if ( Delta >= 0 )
					{	// Three real roots
						if ( Q >= 0.0 )
						{
							double theta = (double) Math.Acos( R / Math.Sqrt(Qcubed) );
							double sqrtQ = (double) Math.Sqrt( Q );

							Result[0] = -2 * sqrtQ * (double) Math.Cos( theta / 3) - a1 / 3;
							Result[1] = -2 * sqrtQ * (double) Math.Cos( (theta + 2 * Math.PI) / 3 ) - a1 / 3;
							Result[2] = -2 * sqrtQ * (double) Math.Cos( (theta + 4 * Math.PI) / 3 ) - a1 / 3;
						}
						else
						{
							Result[0] = Result[1] = Result[2] = double.NaN;
						}
					}
					else
					{	// One real root
						var e = (double) Math.Pow( Math.Sqrt( -Delta ) + Math.Abs( R ), 1.0 / 3.0 );
						if ( R > 0 )
							e = -e;

						Result[0] = Result[1] = Result[2] = (e + Q / e) - a1 / 3.0f;
					}

					return	Result;
				}

				#endregion

				/// <summary>
				/// Computes a hit given a ray in WORLD space
				/// </summary>
				/// <param name="_WorldPosition"></param>
				/// <param name="_WorldDirection"></param>
				/// <param name="_Hits"></param>
				public virtual void	Hit( Vector3 _WorldPosition, Vector3 _WorldDirection, List<RayHit> _Hits )
				{
					if ( m_Next == null )
						return;

					// Transform the ray in LOCAL space
					Vector3	P = Vector3.TransformCoordinate( _WorldPosition, m_World2Local );
					Vector3	V = Vector3.TransformNormal( _WorldDirection, m_World2Local );

					// Compute the intersection with our clipped cone
					// Cone's radius varies with y: r(y) = r0 + (r1-r0)/L.y
					// and y = P.y + V.y * t
					// in terms of t : r(t) = r0 + (r1-r0)/L * (P.y + V.y * t)
					// or : r(t) = Pr + Vr * t
					// with:
					// Pr = r0 + (r1-r0)*P.y/L
					// Vr = (r1-r0)*V.y/L
					//
					// We also have [P.xz + V.xz * t]² = r(y)²
					// So :
					// P.xz² + 2*P.xz*V.xz * t + V.xz² * t² = Pr² + 2*Pr*Vr * t + Vr² * t²
					// Finally :
					// (P.xz² - Pr²) + 2*(P.xz*V.xz - Pr*Vr) * t + (V.xz² - Vr²) * t²= 0
					//
					float	L = Length;
					float	Dr = m_Next.m_Radius - m_Radius;
					float	Pr = m_Radius + Dr * P.Y / L;
					float	Vr = Dr * V.Y/L;
					float	a = V.X*V.X+V.Z*V.Z - Vr*Vr;
					float	b = P.X*V.X+P.Z*V.Z - Pr*Vr;
					float	c = P.X*P.X+P.Z*P.Z - Pr*Pr;
					float	Delta = b*b-a*c;
					if ( Delta < 0.0f )
						return;	// No hit

					float	HitDistance = (-b + (float) Math.Sqrt( Delta )) / a;

					// Check hit height
					float	HitHeight = P.Y + V.Y * HitDistance;
					if ( HitHeight < 0.0f || HitHeight > L )
						return;	// Out of range...

					RayHit	NewHit = new RayHit();
					NewHit.m_HitDistance = HitDistance;
					NewHit.m_HitSegment = this;
					_Hits.Add( NewHit );
				}

				#endregion
			}

			/// <summary>
			/// This is a segment that hosts a new branch splitting away
			/// </summary>
			public class	SplitSegment : Segment
			{
				#region FIELDS

				protected Branch	m_SplittingBranch = null;

				#endregion

				#region PROPERTIES

				/// <summary>
				/// Gets the branch splitting off that segment
				/// </summary>
				public Branch	SplittingBranch		{ get { return m_SplittingBranch; } }

				public override float AreaWithChildren
				{
					get { return base.AreaWithChildren + m_SplittingBranch.AreaWithChildren; }
				}

				public override int BranchesCount
				{
					get { return m_SplittingBranch.BranchesCount; }
				}

				#endregion

				#region METHODS

				public SplitSegment( Branch _Parent, Branch _SplittingBranch ) : base( _Parent )
				{
					m_SplittingBranch = _SplittingBranch;
				}

				#region Growth Algorithm

				public override void PropagateEvalFrame( Matrix _Parent2World )
				{
					base.PropagateEvalFrame( _Parent2World );
 					m_SplittingBranch.PropagateEvalFrame( m_Local2World );
				}

				public override void PropagateComputeTorque()
				{
					// Propagate through the splitting branch
					m_SplittingBranch.PropagateComputeTorque();

					// Add the mass of the splitting branch
					m_AccumulatedMass += m_SplittingBranch.AccumulatedMass;

					// And its torque...
					m_Torque += m_SplittingBranch.Torque;

					base.PropagateComputeTorque();
				}

				public override void PropagateComputeGrowthAndNeeds()
				{
					// Propagate through the splitting branch
					m_SplittingBranch.PropagateComputeGrowthAndNeeds();

					base.PropagateComputeGrowthAndNeeds();
				}

				public override void PropagateAccumulateLightAndNutrientNeeds( float _AccumulatedParentNutrientNeeds, ref float _AccumulatedLightNeeds, ref float _AccumulatedNutrientNeeds )
				{
					// Propagate through the splitting branch
					m_SplittingBranch.PropagateAccumulateLightAndNutrientNeeds( _AccumulatedParentNutrientNeeds, ref _AccumulatedLightNeeds, ref _AccumulatedNutrientNeeds );

					// Add the light needs of the splitting branch
					m_AccumulatedLightNeeds += m_SplittingBranch.AccumulatedLightNeeds;

					base.PropagateAccumulateLightAndNutrientNeeds( _AccumulatedParentNutrientNeeds, ref _AccumulatedLightNeeds, ref _AccumulatedNutrientNeeds );
				}

				public override float TransportLight()
				{
					// The remaining light is our own remaining light + the one from the splitting branch
					float	OurRemainingLight = base.TransportLight();
					float	SplittingBranchRemainingLight = m_SplittingBranch.TransportLight();

					// Collect some light from the splitting branch if we didn't meet our needs
					m_CollectedLight = Math.Min( Math.Max( 0.0f, SplittingBranchRemainingLight ), m_LightNeeds-m_CollectedLight );
					SplittingBranchRemainingLight -= m_LightNeeds-m_CollectedLight;	// The remaining light can be negative and that's important as a final remaining negative quantity decides for branch splitting !

					return OurRemainingLight + SplittingBranchRemainingLight;
				}

				public override void TransportNutrients( float _RemainingNutrients, ref float _GlobalRemainingNutrients )
				{
					// Collect some nutrients based on own need (deduced from the amount of volume we need to grow)
					m_CollectedNutrients = Math.Min( Math.Max( 0.0f, _RemainingNutrients ), m_NutrientsNeeds );
					_RemainingNutrients -= m_NutrientsNeeds;	// The remaining nutrients can be negative and that's important as a final remaining negative quantity decides for root splitting !
					_GlobalRemainingNutrients -= m_NutrientsNeeds;

					// Here, I'm using the volumes of both this segment and the splitting branch's start segment
					//	as factors for distributing the nutrients.
					//
					// One could also think of using the total need of the branch and child segments but that would
					//	imply some sort of foreknowledge of the required consumption and is more like "Growth Strategy #2" explained in Tree.GrowOneStep()
					// Or one could also use the needs of both the segment and splitting branch's start segment
					//	instead of volumes. Or the radii, or the amount of segments, or the mass ?
// 					float	v0 = Volume;
// 					float	v1 = m_SplittingBranch.m_StartSegment.Volume;
// 					float	f0 = v0 / (v0+v1);
// 					float	f1 = v1 / (v0+v1);

// 					float	m0 = AccumulatedMass;
// 					float	m1 = m_SplittingBranch.AccumulatedMass;
// 					float	f0 = m0 / (m0+m1);
// 					float	f1 = m1 / (m0+m1);

					float	r0 = Radius;
					float	r1 = m_SplittingBranch.m_StartSegment.Radius;
					float	f0 = r0 / (r0+r1);
					float	f1 = r1 / (r0+r1);

					// Continue transport toward leaves
					if ( m_Next != null )
						m_Next.TransportNutrients( f0 * _RemainingNutrients, ref _GlobalRemainingNutrients );

					m_SplittingBranch.TransportNutrients( f1 * _RemainingNutrients, ref _GlobalRemainingNutrients );
				}

				public override void GrowAsBranch()
				{
					base.GrowAsBranch();
					m_SplittingBranch.m_StartSegment.GrowAsBranch();
				}

				public override void GrowAsRoot()
				{
					base.GrowAsRoot();
					m_SplittingBranch.m_StartSegment.GrowAsRoot();
				}

				#endregion

				public override void Hit( Vector3 _WorldPosition, Vector3 _WorldDirection, List<RayHit> _Hits )
				{
					base.Hit( _WorldPosition, _WorldDirection, _Hits );
					m_SplittingBranch.Hit( _WorldPosition, _WorldDirection, _Hits );
				}

				#endregion
			}

			#endregion

			#region FIELDS

			// Our owner tree
			protected Tree		m_Owner = null;
			protected int		m_ID = -1;

			// Our hierarchical level
			protected int		m_Level = 0;

			// The initial segment for this branch
			protected Segment	m_StartSegment = null;

			// Cached parent's Local=>World transform (cached by the PropagateEvalFrame() method)
			protected Matrix	m_Parent2World = Matrix.Identity;

			#endregion

			#region PROPERTIES
			
			/// <summary>
			/// Gets the branch ID
			/// </summary>
			public int		ID				{ get { return m_ID; } }

			/// <summary>
			/// Gets the start segment for that branch
			/// </summary>
			public Segment	StartSegment	{ get { return m_StartSegment; } }

			/// <summary>
			/// Gets the hierarchical level of that branch
			/// </summary>
			public int		Level			{ get { return m_Level; } }

			/// <summary>
			/// Gets the mass of the branch
			/// </summary>
			public float	Mass
			{
				get
				{
					float	Result = 0.0f;
					Segment	Current = m_StartSegment;
					while ( Current != null )
					{
						Result += Current.Mass;
						Current = Current.Next;
					}

					return Result;
				}
			}

			/// <summary>
			/// Gets the length of that branch as the cumulated length of all of its segments
			/// </summary>
			public float	Length
			{
				get
				{
					float	Result = 0.0f;
					Segment	Current = m_StartSegment;
					while ( Current != null )
					{
						Result += Current.Length;
						Current = Current.Next;
					}

					return Result;
				}
			}

			/// <summary>
			/// Gets the volume of the branch
			/// </summary>
			public float	Volume
			{
				get
				{
					float	Result = 0.0f;
					Segment	Current = m_StartSegment;
					while ( Current != null )
					{
						Result += Current.Volume;
						Current = Current.Next;
					}

					return Result;
				}
			}

			/// <summary>
			/// Gets the area of the branch
			/// </summary>
			public float	Area
			{
				get
				{
					float	Result = 0.0f;
					Segment	Current = m_StartSegment;
					while ( Current != null )
					{
						Result += Current.Area;
						Current = Current.Next;
					}

					return Result;
				}
			}

			/// <summary>
			/// Gets the area of the branch and its children
			/// </summary>
			public float	AreaWithChildren
			{
				get { return m_StartSegment.AreaWithChildren; }
			}

			/// <summary>
			/// Gets the leaf helper at the end of that branch
			/// </summary>
			public Leaf		EndLeaf
			{
				get { return new Leaf( m_StartSegment.End ); }
			}

			/// <summary>
			/// Gets the amount of branches including that branch
			/// </summary>
			public int		BranchesCount
			{
				get { return 1 + m_StartSegment.BranchesCount; }
			}

			/// <summary>
			/// Gets the amount of segments in that branch
			/// </summary>
			public int		BranchSegmentsCount
			{
				get { return 1 + m_StartSegment.NextSegmentsCount; }
			}

			/// <summary>
			/// Gets the torque applied to this branch
			/// NOTE: You must call PropagateComputeTorque() before that property is valid
			/// </summary>
			public Vector3	Torque
			{
				get { return m_StartSegment.Torque; }
			}

			/// <summary>
			/// Gets the mass of the branch and its children
			/// NOTE: You must call PropagateComputeTorque() before that property is valid
			/// </summary>
			public float	AccumulatedMass
			{
				get { return m_StartSegment.AccumulatedMass; }
			}

			/// <summary>
			/// Gets the light needs of the branch and its children
			/// NOTE: You must call PropagateAccumulateLightAndNutrientNeeds() before that property is valid
			/// </summary>
			public float	AccumulatedLightNeeds
			{
				get { return m_StartSegment.AccumulatedLightNeeds; }
			}

			#endregion

			#region METHODS

			public Branch( Tree _Owner, int _ID, int _Level )
			{
				m_Owner = _Owner;
				m_ID = _ID;
				m_Level = _Level;
			}

			/// <summary>
			/// Initializes the branch's reference frame, radius and length
			/// </summary>
			/// <param name="_Length"></param>
			public void	Initialize( Vector3 _Position, Vector3 _X, Vector3 _Y, Vector3 _Z, float _Radius, float _Length )
			{
				// Create the start segment
				m_StartSegment = new Segment( this );
				m_StartSegment.Initialize( _Position, _X, _Y, _Z, _Radius );

				// Create the tip segment
				Segment	Tip = new Segment( this );
				Tip.Initialize( _Position + _Length * Vector3.UnitY, Vector3.UnitX, Vector3.UnitY, Vector3.UnitZ, 0.0f );
				m_StartSegment.AddSegment( Tip );
			}

			/// <summary>
			/// Computes torque amplitude bore by the branch given the parametrized value t € [0,Length]
			/// NOTE: PropagateComputeTorque() must have been called prior calling this method
			/// </summary>
			/// <param name="t"></param>
			/// <returns></returns>
			public Vector3	GetTorque( float t )
			{
				Segment	S;
				float	r;
				GetSegmentAt( t, out S, out r );

				return S.Torque;
			}

			/// <summary>
			/// Lists all the child branches of this branch (including this branch itself)
			/// </summary>
			/// <param name="_Branches"></param>
			public void	ListBranches( List<Branch> _Branches )
			{
				_Branches.Add( this );	 // Start by adding that branch

				// Browse through segments
				Segment	Current = m_StartSegment;
				while ( Current != null )
				{
					if ( Current is SplitSegment )
						(Current as SplitSegment).SplittingBranch.ListBranches( _Branches );	// Recurse through splitting branch

					Current = Current.Next;
				}
			}

			/// <summary>
			/// Gets the segment and segment ratio given the parametrized value t € [0,Length]
			/// </summary>
			/// <param name="t"></param>
			/// <param name="_Segment"></param>
			/// <param name="_SegmentRatio"></param>
			public void	GetSegmentAt( float t, out Segment _Segment, out float _SegmentRatio )
			{
				_Segment = null;
				_SegmentRatio = float.NaN;

				float	SumLength = 0.0f;
				Segment	Current = m_StartSegment;
				while ( Current != null )
				{
					float	SegmentLength = Current.Length;
					if ( t >= SumLength && t <= SumLength+SegmentLength )
					{	// We isolated the segment
						_Segment = Current;
						_SegmentRatio = (t - SumLength) / SegmentLength;
						return;
					}
					SumLength += SegmentLength;
					Current = Current.Next;
				}
			}

			/// <summary>
			/// Gets the reference frame in WORLD space given the parametrized value t € [0,Length]
			/// (PropagateEvalFrame must have been called prior calling this method !)
			/// </summary>
			/// <param name="t"></param>
			/// <param name="_Position"></param>
			/// <param name="_X"></param>
			/// <param name="_Y"></param>
			/// <param name="_Z"></param>
			public void	GetFrameAt( float t, out Matrix _Local2World )
			{
				Segment	S;
				float	r;
				GetSegmentAt( t, out S, out r );

				// Displace position along Y axis
				Matrix	L2W = S.Local2World;
				L2W.Row4 =  L2W.Row4 + t * S.Length * L2W.Row2;

				_Local2World = L2W;
			}

			/// <summary>
			/// Computes the list of hits given a ray in WORLD space
			/// </summary>
			/// <param name="_WorldPosition"></param>
			/// <param name="_WorldDirection"></param>
			/// <param name="_Hits"></param>
			public void	Hit( Vector3 _WorldPosition, Vector3 _WorldDirection, List<RayHit> _Hits )
			{
				Segment	Current = m_StartSegment;
				while ( Current != null )
				{
					Current.Hit( _WorldPosition, _WorldDirection, _Hits );
					Current = Current.Next;
				}
			}

			#region Growth Algorithm

			/// <summary>
			/// Evaluates the LOCAL2WORLD frame by propagating from the root to the leaves
			/// </summary>
			public void	PropagateEvalFrame( Matrix _Parent2World )
			{
				m_Parent2World = _Parent2World;
				m_StartSegment.PropagateEvalFrame( _Parent2World );
			}

			/// <summary>
			/// Computes the torque applied to the branch and propagates the computation from leaves to branch
			/// NOTE: The computation is performed in WORLD space so PropagateEvalFrame() must have been called prior calling this method
			/// </summary>
			public void	PropagateComputeTorque()
			{
				m_StartSegment.ClearGrowthParameters();
				m_StartSegment.PropagateComputeTorque();
			}

			/// <summary>
			/// Computes the growth and needs for that growth based on current segment parameters
			/// </summary>
			public void PropagateComputeGrowthAndNeeds()
			{
				m_StartSegment.PropagateComputeGrowthAndNeeds();
			}

			/// <summary>
			/// Propagate the sum of light & nutrient needs
			/// </summary>
			/// <param name="_AccumulatedParentNutrientNeeds">The needs in nutrients by the entire parent hierarchy</param>
			public void PropagateAccumulateLightAndNutrientNeeds( float _AccumulatedParentNutrientNeeds, ref float _AccumulatedLightNeeds, ref float _AccumulatedNutrientNeeds )
			{
				m_StartSegment.ClearAccumulatedLightNeeds();
				m_StartSegment.PropagateAccumulateLightAndNutrientNeeds( _AccumulatedParentNutrientNeeds, ref _AccumulatedLightNeeds, ref _AccumulatedNutrientNeeds );
			}

			/// <summary>
			/// Transports light from leaves to roots (basopetal)
			/// </summary>
			public float TransportLight()
			{
				return m_StartSegment.TransportLight();
			}

			/// <summary>
			/// Transports nutrients from roots to leaves (acropetal)
			/// </summary>
			public void TransportNutrients( float _RemainingNutrients, ref float _GlobalRemainingNutrients )
			{
				m_StartSegment.TransportNutrients( _RemainingNutrients, ref _GlobalRemainingNutrients );
			}

			/// <summary>
			/// Attempts to find best child branches for splitting, including this one
			/// </summary>
			/// <param name="_SplitsCount"></param>
			private Dictionary<Branch,float>	m_BranchScores = null;
			public void	SplitBest( int _SplitsCount )
			{
				if ( _SplitsCount == 0 )
					return;	// Nothing to split...

				// Get existing branches
				List<Branch>	Branches = new List<Branch>();
				ListBranches( Branches );

				// Build a list of scores for each branch
				m_BranchScores = new Dictionary<Branch,float>( Branches.Count );
				for ( int BranchIndex=0; BranchIndex < Branches.Count; BranchIndex++ )
				{
					Branch	B = Branches[BranchIndex];
					float	Score = GetBranchSplittingScore( B );
					m_BranchScores.Add( B, Score );
				}

				// Sort by score
				Branches.Sort( this );

				// Split the first branches
				_SplitsCount = Math.Min( _SplitsCount, Branches.Count );
				for ( int BranchIndex=0; BranchIndex < _SplitsCount; BranchIndex++ )
				{
					Branch	B = Branches[BranchIndex];
					SplitBranch( B );
				}
			}

			/// <summary>
			/// Returns the score to assign to the branch to determine its propention to be the best candidate for splitting
			/// </summary>
			/// <param name="_Branch"></param>
			/// <returns></returns>
			protected virtual float	GetBranchSplittingScore( Branch _Branch )	{ return 0.0f; }

			/// <summary>
			/// Splits the segment either as branch or as root depending on branch type
			/// </summary>
			/// <param name="_S"></param>
			protected virtual void	SplitBranch( Branch _B )					{}

			#region IComparer<Branch> Members

			public int Compare( Branch x, Branch y )
			{
				float	Sx = m_BranchScores[x];
				float	Sy = m_BranchScores[y];
				return Sx < Sy ? -1 : (Sx > Sy ? +1 : 0);
			}

			#endregion

			#endregion

			#endregion
		}

		/// <summary>
		/// This is the class for branches above the ground (i.e. trunk and others)
		/// </summary>
		public class BranchLight : Branch
		{
			#region FIELDS
			#endregion

			#region PROPERTIES

			#endregion

			#region METHODS

			public BranchLight( Tree _Owner, int _ID, int _Level ) : base( _Owner, _ID, _Level )
			{
			}

			/// <summary>
			/// Geometry growth for branches
			/// </summary>
			public void	Grow()
			{
				m_StartSegment.GrowAsBranch();
			}

			protected override float  GetBranchSplittingScore( Branch _Branch )
			{
				// At the moment, we use the hierarchical level as the score
				// TODO: Use a combination of hierarchical level, best lighting conditions, least mass/torque, etc.
				float	Score = _Branch.Level + m_Owner.NewRandom();
				return Score;
			}

			protected override void SplitBranch( Branch _B )
			{
				// Draw a random segment within that branch

				// At the moment, we use the last segment before the tip of branch
				// TODO: Use a combination of best light availability conditions, most mass/torque balancing, etc.
				Segment	S = _B.StartSegment.End.Previous;

				Segment	NewS = S.SplitAsBranch();

				// Update Local=>World
				NewS.PropagateEvalFrame( NewS.Previous != null ? NewS.Previous.Local2World : m_Parent2World );
			}

			#endregion
		}

		/// <summary>
		/// This is the class for branches below the ground (i.e. roots)
		/// </summary>
		public class BranchNutrient : Branch, IComparer<Branch>
		{
			#region FIELDS
			#endregion

			#region PROPERTIES
			#endregion

			#region METHODS

			public BranchNutrient( Tree _Owner, int _ID, int _Level ) : base( _Owner, _ID, _Level )
			{

			}

			/// <summary>
			/// Geometry growth for roots
			/// </summary>
			public void	Grow()
			{
				m_StartSegment.GrowAsRoot();
			}

			protected override float  GetBranchSplittingScore( Branch _Branch )
			{
				// At the moment, we use the hierarchical level as the score
				// TODO: Use a combination of hierarchical level, best nutrients availability conditions, most mass/torque balancing, etc.
				float	Score = _Branch.Level + m_Owner.NewRandom();
				return Score;
			}

			protected override void SplitBranch( Branch _B )
			{
				// Draw a random segment within that branch

				// At the moment, we use the last segment before the tip of branch
				// TODO: Use a combination of best nutrients availability conditions, most mass/torque balancing, etc.
				Segment	S = _B.StartSegment.End.Previous;

				Segment	NewS = S.SplitAsRoot();

				// Update Local=>World
				NewS.PropagateEvalFrame( NewS.Previous != null ? NewS.Previous.Local2World : m_Parent2World );
			}

			#endregion
		}

		/// <summary>
		/// This is the parameters access class
		/// </summary>
		public class	Parameters
		{
			#region FIELDS

			protected Tree	m_Owner = null;

			#endregion

			#region PROPERTIES

			// Weight & Resistance
			[System.ComponentModel.Category( "Weight & Resistance" )]
			[System.ComponentModel.Description( "Wood density in kg/m^3 (500kg/m^3 is quite average)" )]
			public float				Density								{ get { return m_Owner.m_Density; } set { m_Owner.m_Density = value; } }
			[System.ComponentModel.Category( "Weight & Resistance" )]
			[System.ComponentModel.Description( "Resistance to torque per unit volume (e.g. if it's equal to density then the branch can support its own weight distributed as a torque)" )]
			public float				TorqueResistancePerVolume			{ get { return m_Owner.m_TorqueResistancePerVolume; } set { m_Owner.m_TorqueResistancePerVolume = value; } }
			[System.ComponentModel.Category( "Weight & Resistance" )]
			[System.ComponentModel.Description( "Resistance to weight per unit volume (e.g. if it's equal to density then the branch can support its own weight, if it's twice the density then it can support another identical branch on top of itself)" )]
			public float				WeightResistancePerVolume			{ get { return m_Owner.m_WeightResistancePerVolume; } set { m_Owner.m_WeightResistancePerVolume = value; } }

			// Light & Nutrient needs and inputs
			[System.ComponentModel.Category( "Light & Nutrients" )]
			[System.ComponentModel.Description( "The amount of input light units provided by a single leaf" )]
			public float				LightUnitsPerLeaf					{ get { return m_Owner.m_LightUnitsPerLeaf; } set { m_Owner.m_LightUnitsPerLeaf = value; } }
			[System.ComponentModel.Category( "Light & Nutrients" )]
			[System.ComponentModel.Description( "The amount of light units needed to grow a cubic meter in volume" )]
			public float				LightNeedUnitPerVolumeGrowth		{ get { return m_Owner.m_LightNeedUnitPerVolumeGrowth; } set { m_Owner.m_LightNeedUnitPerVolumeGrowth = value; } }
			[System.ComponentModel.Category( "Light & Nutrients" )]
			[System.ComponentModel.Description( "The amount of input nutrient units that must be immediately provided by a new splitting root" )]
			public float				NutrientUnitsPerNewRoot				{ get { return m_Owner.m_NutrientUnitsPerNewRoot; } set { m_Owner.m_NutrientUnitsPerNewRoot = value; } }
			[System.ComponentModel.Category( "Light & Nutrients" )]
			[System.ComponentModel.Description( "The amount of nutrients units needed to grow a cubic meter in volume" )]
			public float				NutrientsNeedUnitPerVolumeGrowth	{ get { return m_Owner.m_NutrientsNeedUnitPerVolumeGrowth; } set { m_Owner.m_NutrientsNeedUnitPerVolumeGrowth = value; } }
			[System.ComponentModel.Category( "Light & Nutrients" )]
			[System.ComponentModel.Description( "The amount of input nutrients units provided by a single square meter of root surface area" )]
			public float				NutrientUnitsPerRootSurfaceArea		{ get { return m_Owner.m_NutrientUnitsPerRootSurfaceArea; } set { m_Owner.m_NutrientUnitsPerRootSurfaceArea = value; } }

			// Growth
			[System.ComponentModel.Category( "Growth" )]
			[System.ComponentModel.Description( "The propention to grow the radius rather than the length" )]
			public float				RadiusToLengthVolumeGrowthRatio		{ get { return m_Owner.m_RadiusToLengthVolumeGrowthRatio; } set { m_Owner.m_RadiusToLengthVolumeGrowthRatio = value; } }
			[System.ComponentModel.Category( "Growth" )]
			[System.ComponentModel.Description( "Maximum volume we can grow per simulation step" )]
			public float				MaxVolumeGrowthPerSimulationStep	{ get { return m_Owner.m_MaxVolumeGrowthPerSimulationStep; } set { m_Owner.m_MaxVolumeGrowthPerSimulationStep = value; } }
			[System.ComponentModel.Category( "Growth" )]
// 			[System.ComponentModel.Description( "Length growth factor per simulation step (a value of 1.05 would mean to grow by 5% each step)" )]
// 			public float				LengthGrowthFactor					{ get { return m_Owner.m_LengthGrowthFactor; } set { m_Owner.m_LengthGrowthFactor = value; } }
			[System.ComponentModel.Description( "Length growth per simulation step" )]
			public float				LengthGrowth						{ get { return m_Owner.m_LengthGrowth; } set { m_Owner.m_LengthGrowth = value; } }
			[System.ComponentModel.Category( "Growth" )]
			[System.ComponentModel.Description( "The maximum length a segment can reach before creating a new active segment" )]
			public float				MaxSegmentLength					{ get { return m_Owner.m_MaxSegmentLength; } set { m_Owner.m_MaxSegmentLength = value; } }
			[System.ComponentModel.Category( "Growth" )]
			[System.ComponentModel.Description( "The conversion factor from torque amplitude to radians (this determines the propension of growing branch to fight the torque)" )]
			public float				TorqueAmplitudeToCompensationAngle	{ get { return m_Owner.m_TorqueAmplitudeToCompensationAngle; } set { m_Owner.m_TorqueAmplitudeToCompensationAngle = value; } }
			[System.ComponentModel.Category( "Growth" )]
			[System.ComponentModel.Description( "The maximum distance a branch segment can move at each simulation step (this will ultimately determine the branch grow curvature)" )]
			public float				MaxBranchSegmentMotion				{ get { return m_Owner.m_MaxBranchSegmentMotion; } set { m_Owner.m_MaxBranchSegmentMotion = value; } }
			[System.ComponentModel.Category( "Growth" )]
			[System.ComponentModel.Description( "The maximum distance a root segment can move at each simulation step (this will ultimately determine the root grow curvature)" )]
			public float				MaxRootSegmentMotion				{ get { return m_Owner.m_MaxRootSegmentMotion; } set { m_Owner.m_MaxRootSegmentMotion = value; } }

			// Splitting parameters
			[System.ComponentModel.Category( "Splitting" )]
			[System.ComponentModel.Description( "The prefered branching position of a segment in [0,1] (0 is the segment start, 1 is the segment end)" )]
			public float				BaseBranchingPosition				{ get { return m_Owner.m_BaseBranchingPosition; } set { m_Owner.m_BaseBranchingPosition = value; } }
			[System.ComponentModel.Category( "Splitting" )]
			[System.ComponentModel.Description( "Branching position normalized variance" )]
			public float				BaseBranchingPositionVariance		{ get { return m_Owner.m_BaseBranchingPositionVariance; } set { m_Owner.m_BaseBranchingPositionVariance = value; } }
			[System.ComponentModel.Category( "Splitting" )]
			[System.ComponentModel.Description( "The prefered branching angle off of a segment" )]
			public float				BaseBranchingAngle					{ get { return m_Owner.m_BaseBranchingAngle; } set { m_Owner.m_BaseBranchingAngle = value; } }
			[System.ComponentModel.Category( "Splitting" )]
			[System.ComponentModel.Description( "Branching off normalized variance" )]
			public float				BaseBranchingAngleVariance			{ get { return m_Owner.m_BaseBranchingAngleVariance; } set { m_Owner.m_BaseBranchingAngleVariance = value; } }
			[System.ComponentModel.Category( "Splitting" )]
			[System.ComponentModel.Description( "Branching balancing factor in [0,1] (0 => the new branch takes all the bending 1 => the existing branch takes all the bending)" )]
			public float				BranchingBalancing					{ get { return m_Owner.m_BranchingBalancing; } set { m_Owner.m_BranchingBalancing = value; } }
			[System.ComponentModel.Category( "Splitting" )]
			[System.ComponentModel.Description( "Branching balancing factor normalized variance" )]
			public float				BranchingBalancingVariance			{ get { return m_Owner.m_BranchingBalancingVariance; } set { m_Owner.m_BranchingBalancingVariance = value; } }
// 			[System.ComponentModel.Category( "Splitting" )]
// 			[System.ComponentModel.Description( "Start radius for a new branch splitting off" )]
//			public float				BranchingStartRadius				{ get { return m_Owner.m_BranchingStartRadius; } set { m_Owner.m_BranchingStartRadius = value; } }

			// Last growth parameters
			[System.ComponentModel.Browsable( false )]
			public float			AvailableLight		{ get { return m_Owner.m_AvailableLight; } }
			[System.ComponentModel.Browsable( false )]
			public float			AvailableNutrients	{ get { return m_Owner.m_AvailableNutrients; } }
			[System.ComponentModel.Browsable( false )]
			public float			TotalLightNeeds		{ get { return m_Owner.m_AccumulatedLightNeeds; } }
			[System.ComponentModel.Browsable( false )]
			public float			TotalNutrientNeeds	{ get { return m_Owner.m_AccumulatedNutrientNeeds; } }

			#endregion

			#region METHODS

			public  Parameters( Tree _Owner )
			{
				m_Owner = _Owner;
			}

			#endregion
		}

		#endregion

		#region FIELDS

		// The original transform
		protected Matrix			m_TrunkLocal2World = Matrix.Identity;
		protected Matrix			m_RootLocal2World = Matrix.Identity;

		// Start branches for trunk & roots
		protected BranchLight		m_Trunk = null;	// Initial trunk branch
		protected BranchNutrient	m_Root = null;	// Initial root branch
		protected LightHemisphere	m_LightHemisphere = null;

		// Global tree parameters
		protected float				m_WeightResistancePerVolume = 400.0f;		// Resistance to weight per unit volume (e.g. if it's equal to density then the branch can support its own weight, if it's twice the density then it can support another identical branch on top of itself)
		protected float				m_TorqueResistancePerVolume = 400.0f;		// Resistance to torque per unit volume (e.g. if it's equal to density then the branch can support its own weight distributed as a torque)
		protected float				m_Density = 500.0f;							// Wood density in kg/m^3 (500kg/m^3 is quite average)
		protected float				m_RadiusToLengthVolumeGrowthRatio = 1.0f;	// The propention to grow the radius rather than the length
		protected float				m_MaxVolumeGrowthPerSimulationStep = 0.1f;	// Maximum volume we can grow per simulation step
//		protected float				m_LengthGrowthFactor = 1.05f;				// Length growth factor per simulation step (a value of 1.05 would mean to grow by 5% each step)
		protected float				m_LengthGrowth = 0.025f;					// Length growth factor per simulation step
		protected float				m_RadiusGrowth = 0.00125f;					// Radius growth factor per simulation step
		protected float				m_MaxSegmentLength = 1.0f;					// The maximum length a segment can reach before creating a new active segment
		protected float				m_TorqueAmplitudeToCompensationAngle = 1.0f;// The conversion factor from torque amplitude to radians (this determines the propension of growing branch to fight the torque)
		protected float				m_MaxBranchSegmentMotion = 0.01f;			// The maximum distance a branch segment can move at each simulation step (this will ultimately determine the branch grow curvature)
		protected float				m_MaxRootSegmentMotion = 0.01f;				// The maximum distance a root segment can move at each simulation step (this will ultimately determine the root grow curvature)

			// Light & Nutrient needs and inputs
		protected float				m_LightUnitsPerLeaf = 0.1f;					// The amount of input light units provided by a single leaf
		protected float				m_LightNeedUnitPerVolumeGrowth = 100.0f;	// The amount of light units needed to grow a cubic meter in volume
		protected float				m_NutrientUnitsPerRootSurfaceArea = 10.0f;	// The amount of input nutrients units provided by a single square meter of root surface area
		protected float				m_NutrientsNeedUnitPerVolumeGrowth = 100.0f;// The amount of nutrients units needed to grow a cubic meter in volume
		protected float				m_NutrientUnitsPerNewRoot = 0.1f;			// The amount of input nutrient units that must be immediately provided by a new splitting root

			// Splitting parameters
		protected float				m_BaseBranchingPosition = 0.5f;				// The prefered branching position of a segment in [0,1] (0 is the segment start, 1 is the segment end)
		protected float				m_BaseBranchingPositionVariance = 0.02f;	// Branching position normalized variance
		protected float				m_BaseBranchingAngle = 0.25f * (float) Math.PI;	// The prefered branching angle off of a segment
		protected float				m_BaseBranchingAngleVariance = 0.02f;		// Branching off normalized variance
		protected float				m_BranchingBalancing = 0.0f;				// Branching balancing factor in [0,1] (0 => the new branch takes all the bending 1 => the existing branch takes all the bending)
		protected float				m_BranchingBalancingVariance = 0.02f;		// Branching balancing factor normalized variance
//		protected float				m_BranchingStartRadius = 0.01f;				// Start radius for a new branch splitting off

		// Growth cached parameters
		protected Random			m_RNG = null;
		protected int				m_TrunkBranchesCount = 0;					// Total amount of branches from the trunk to the leaves
		protected int				m_RootBranchesCount = 0;					// Total amount of branches from the roots to the "leaf roots"
		protected float				m_AvailableLight = 0.0f;					// Start amount of available light
		protected float				m_AvailableNutrients = 0.0f;				// Start amount of available nutrients
		protected float				m_AccumulatedLightNeeds = 0.0f;				// The accumulated light needs for the entire tree (this variable is valid after a call to "PropagateAccumulateLightAndNutrientNeeds()" and is accumulated by each segment along the way)
		protected float				m_AccumulatedNutrientNeeds = 0.0f;			// The accumulated nutrient needs for the entire tree (this variable is valid after a call to "PropagateAccumulateLightAndNutrientNeeds()" and is accumulated by each segment along the way)

		protected Parameters		m_Parameters = null;

		#endregion

		#region PROPERTIES

		public BranchLight		Trunk				{ get { return m_Trunk; } }
		public BranchNutrient	Root				{ get { return m_Root; } }
		public LightHemisphere	LightHemisphere		{ get { return m_LightHemisphere; } set { m_LightHemisphere = value; } }
		public Parameters		Params				{ get { return m_Parameters; } }

		#endregion

		#region METHODS

		public Tree()
		{
			m_Parameters = new Parameters( this );
		}

		/// <summary>
		/// Create the initial tree
		/// </summary>
		/// <param name="_Direction"></param>
		/// <param name="_InitialRadius"></param>
		/// <param name="_InitialLength"></param>
		public void		Initialize( Vector3 _Direction, float _InitialRadius, float _InitialLength, int _RandomSeed )
		{
			// Initialize the Local=>World transform
			_Direction.Normalize();
			Vector3	At = Vector3.Cross( Vector3.UnitX, _Direction );
					At.Normalize();
			Vector3	Right = Vector3.Cross( _Direction, At );

			m_TrunkLocal2World.Row1 = new Vector4( Right, 0.0f );
			m_TrunkLocal2World.Row2 = new Vector4( _Direction, 0.0f );
			m_TrunkLocal2World.Row3 = new Vector4( At, 0.0f );
			m_TrunkLocal2World.Row4 = new Vector4( Vector3.Zero, 1.0f );

			Matrix	RootFlip = Matrix.RotationZ( (float) Math.PI );
			m_RootLocal2World = Matrix.Multiply( m_TrunkLocal2World, RootFlip );

			// Create the initial trunk branch
			m_Trunk = new BranchLight( this, m_TrunkBranchesCount, 0 );
			m_Trunk.Initialize( Vector3.Zero, Right, _Direction, At, _InitialRadius, _InitialLength );
			m_TrunkBranchesCount = 1;

			// Create the initial root branch
			m_Root = new BranchNutrient( this, m_RootBranchesCount, 0 );
			m_Root.Initialize( Vector3.Zero, Right, _Direction, At, _InitialRadius, 0.1f * _InitialLength );
			m_RootBranchesCount = 1;

			// Create the RNG
			m_RNG = new Random( _RandomSeed );
		}

		/// <summary>
		/// The main growing algorithm
		/// </summary>
		public void	GrowOneStep()
		{
			PropagateEvalFrame();

			//////////////////////////////////////////////////////////////////////////
			// 1] Gather global growth parameters at current stage

			// 1.1] The total amount of nutrients is the area covered by the roots multiplied by a factor
			float	fCurrentRootsArea = m_Root.AreaWithChildren;
			m_AvailableNutrients = fCurrentRootsArea * m_NutrientUnitsPerRootSurfaceArea;

			// 1.2] The total amount of light is the total amount of leaves (or branches, as each branch has a leaf) multiplied by a factor
			m_AvailableLight = m_TrunkBranchesCount * m_LightUnitsPerLeaf;

			//////////////////////////////////////////////////////////////////////////
			// 2] Compute segment-wise parameters

			// 2.1] Compute torque for every segment & branches
			m_Trunk.PropagateComputeTorque();

			// 2.2] Compute target volumes & length for every segment, nutrient+light needs
			m_Trunk.PropagateComputeGrowthAndNeeds();

			// 2.3] Accumulate light & nutrients needs for the entire tree
			m_AccumulatedLightNeeds = 0.0f;				// The accumulated light needs for the entire tree
			m_AccumulatedNutrientNeeds = 0.0f;			// The accumulated nutrient needs for the entire tree
			m_Trunk.PropagateAccumulateLightAndNutrientNeeds( 0.0f, ref m_AccumulatedLightNeeds, ref m_AccumulatedNutrientNeeds );

			//////////////////////////////////////////////////////////////////////////
			// 3] Start transport
			//
			// There are 2 transport strategies to think of :
			// 1) the light & nutrients are eaten by each segment along the transport
			// 2) the light & nutrients are equitably distributed within the entire tree based on needs percentiles
			//
			// Strategy 1) will have the inconvenient of end branches possibily lacking nutrients as parent branches will
			//	have eaten them before they reach the leaves, and start branches possibly lacking light as child branches
			//	will have eaten it before they reach the roots. There are clear "starving" patterns with that strategy.
			//
			// Strategy 2) would theorize the existence of a global consciousness of the tree that knows how to split
			//	available resources equally within its branches and segments. Not a consciousness per-se but rather some
			//	sort of notification mechanism flowing both ways telling each segment to leave some for the others...
			// Intuitively, nature is greedy and brutal so I would believe it's somewhat more of an "each segment for itself!" policy ^^
			//
			// Possibly, a third solution would be an hybrid of the 2 with, perhaps :
			//	_ light transport using strategy 2 so it always reaches the roots no matter what, that would make the trunk
			//		to always be able to grow and "starve" the leaves
			//
			// or :
			//	_ some "alert mechanism" that would force segments to diet so others that are really starving can get some
			//		of the light/nutrients nonetheless (some ratio between "eat all" and "leave some for the others")
			//

			// STRATEGY #1
			// 3.1] Basopetal light transport (from leaves to roots)
			float	RemainingLight = m_Trunk.TransportLight();

			// 3.2] Acropetal nutrients transport (from roots to leaves)
			float	RemainingNutrients = m_AvailableNutrients;	// The remaining nutrients for the entire tree
			m_Trunk.TransportNutrients( m_AvailableNutrients, ref RemainingNutrients );

			//////////////////////////////////////////////////////////////////////////
			// 4] Perform growth process

			// 4.1] Grow branches by nutrients & light input
			// This is the actual geometry growth which is both guided by light (heliotropism) and torque reduction.
			m_Trunk.Grow();

			// 4.2] Grow roots linearly (TODO: also use nutrients & light once everything works)
			m_Root.Grow();

			// 4.3] Split necessary branches based on light deficiency
			float	fNewBranchesCount = Math.Max( 0.0f, -RemainingLight ) / m_LightUnitsPerLeaf;
					fNewBranchesCount *= 1.5f;	// Arbitrary
			int		NewBranchesCount = (int) Math.Ceiling( fNewBranchesCount );

			m_Trunk.SplitBest( NewBranchesCount );

			// 4.4] Split necessary roots based on nutrients deficiency
// 			float	fTargetAdditionalArea = Math.Max( 0.0f, -RemainingNutrients ) / m_NutrientUnitsPerRootSurfaceArea;
// 					fTargetAdditionalArea *= 1.5f;	// Arbitrary
// 			float	fAreaRatio = (fCurrentRootsArea + fTargetAdditionalArea) / fCurrentRootsArea;
// 					fAreaRatio = Math.Min( 2.0f, fAreaRatio );	// Can't grow more than double
// 			int		CurrentRootsCount = m_Root.BranchesCount;
// 			int		TargetRootsCount = (int) Math.Ceiling( fAreaRatio * CurrentRootsCount );
// 			int		NewRootsCount = TargetRootsCount - CurrentRootsCount;

			float	fNewRootsCount = Math.Max( 0.0f, -RemainingNutrients ) / m_NutrientUnitsPerNewRoot;
					fNewRootsCount *= 1.5f;	// Arbitrary
			int		NewRootsCount = (int) Math.Ceiling( fNewRootsCount );
			
			m_Root.SplitBest( NewRootsCount );
		}

		/// <summary>
		/// Propagates the evaluation of the entire tree's WORLD space transform matrices
		/// </summary>
		public void	PropagateEvalFrame()
		{
			m_Trunk.PropagateEvalFrame( m_TrunkLocal2World );
			m_Root.PropagateEvalFrame( m_RootLocal2World );
		}

		/// <summary>
		/// Lists all the branches existing in the tree (also yielding the leaves as branches' tip segments have leaves)
		/// </summary>
		/// <returns></returns>
		public Branch[]	ListTrunkBranches()
		{
			List<Branch>	Result = new List<Branch>();
			m_Trunk.ListBranches( Result );

			return Result.ToArray();
		}

		/// <summary>
		/// Returns all the branch segments hit by the WORLD ray, in front to back order
		/// </summary>
		/// <param name="_WorldPosition"></param>
		/// <param name="_WorldDirection"></param>
		/// <returns></returns>
		public Branch.RayHit[]	Hit( Vector3 _WorldPosition, Vector3 _WorldDirection )
		{
			List<Branch.RayHit>	Hits = new List<Branch.RayHit>();
			m_Trunk.Hit( _WorldPosition, _WorldDirection, Hits );
			m_Root.Hit( _WorldPosition, _WorldDirection, Hits );
			Hits.Sort();

			return Hits.ToArray();
		}

		#region Helpers

		/// <summary>
		/// Draws a new random number in [0,_MaxValue]
		/// </summary>
		/// <returns></returns>
		protected int NewRandomInt( int _MaxValue )
		{
			return m_RNG.Next( _MaxValue );
		}

		/// <summary>
		/// Draws a new random number in [0,1]
		/// </summary>
		/// <returns></returns>
		protected float NewRandom()
		{
			return (float) m_RNG.NextDouble();
		}

		/// <summary>
		/// Draws a new random number in [-1,1]
		/// </summary>
		/// <returns></returns>
		protected float NewSignedRandom()
		{
			return (float) (2.0 * m_RNG.NextDouble() - 1.0);
		}

		#endregion

		#endregion
	}
}
