using System;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;

namespace Nuaj.Cirrus.Utility
{
	/// <summary>
	/// This class represents a lens-flare objet that can be displayed by the LensFlare render technique
	/// The lens-flare can be loaded from an Optical Flares (*.OFP) file (cf. http://www.videocopilot.net/products/opticalflares/)
	///  so you can create a lens-flare in After Effects and import it into Nuaj'.
	/// </summary>
	public class LensFlare
	{
		#region NESTED TYPES

		// Some values have a Vector2 and a boolean with the boolean telling the Y value is driven by the X value (e.g. uniform scaling)
		[System.Diagnostics.DebuggerDisplay( "X={X} Y={Y} Bool={Bool}" )]
		public struct	Vector2Bool
		{
			public float	X, Y;
			public bool		Bool;

			static public Vector2Bool	ZERO = new Vector2Bool() { X=0.0f, Y=0.0f };
			static public Vector2Bool	ONE = new Vector2Bool() { X=100.0f, Y=100.0f };
		}

		// Some values have a color and a boolean to enable or disable the color
		[System.Diagnostics.DebuggerDisplay( "Color={Color} Bool={Bool}" )]
		public struct	ColorBool
		{
			public Color	Color;
			public bool		Bool;
		}

		// Some values have an enum + the name of the enum value
		[System.Diagnostics.DebuggerDisplay( "Value={Value} Name={Name}" )]
		public struct	EnumString<T> where T:struct
		{
			public T		Value;
			public string	Name;
		}

		/// <summary>
		/// An atom is a unit element in the file stream : the file is only composed of a series of atoms.
		/// An atom is :
		///  _ 1 WORD => the atom type
		///  _ 1 WORD => the atom run length
		///  _ N bytes with N = run length => the atom variant value (i.e. bool, int, float, string, color, etc.)
		/// </summary>
		[System.Diagnostics.DebuggerDisplay( "Type={Type} AsInt={AsInt}" )]
		protected class	Atom
		{
			#region NESTED TYPES

			public enum ATOM_TYPE
			{
				// Field atoms
				FIELD_ID = 0x32,
				FIELD_TYPE = 0x33,
				FIELD_VALUE = 0x34,

				// Lens object atoms
				LENS_OBJECT_NAME = 0x02,
				LENS_OBJECT_HIDE = 0x03,
				LENS_OBJECT_SOLO = 0x04,

				// Block headers
				GENERAL_PARAMETERS = 0xBB8,
				LENS_OBJECT_DESCRIPTOR = 0x01,

				// Block end markers
				FIELD_END = 0x7D0,
				GLOBAL_PARAMS_END = 0xFA0,
				LENS_OBJECT_END = 0x3E8,
			}

			#endregion

			#region FIELDS

			protected ATOM_TYPE			m_Type;

			protected bool				m_ValueBool;
			protected int				m_ValueInt;
			protected float				m_ValueFloat;
			protected string			m_ValueString;
			protected Color				m_ValueColor;
			protected Vector2Bool		m_ValueVector2Bool = new Vector2Bool();
			protected ColorBool			m_ValueColorBool = new ColorBool();
			protected EnumString<int>	m_ValueEnumString = new EnumString<int>();

			protected static byte[]		ms_TempContent = new byte[64];

			#endregion

			#region PROPERTIES

			public ATOM_TYPE		Type			{ get { return m_Type; } }

			public bool				AsBool			{ get { return m_ValueBool; } }
			public int				AsInt			{ get { return m_ValueInt; } }
			public Color			AsColor			{ get { return Int2Color( m_ValueInt ); } }
			public float			AsFloat			{ get { return m_ValueFloat; } }
			public string			AsString		{ get { return m_ValueString; } }
			public Vector2Bool		AsVector2Bool	{ get { return m_ValueVector2Bool; } }
			public ColorBool		AsColorBool		{ get { return m_ValueColorBool; } }
			public EnumString<int>	AsEnumString	{ get { return m_ValueEnumString; } }

			#endregion

			#region METHODS

			public Atom() { }
			public Atom( System.IO.BinaryReader _Reader ) { Read( _Reader ); }
			
			public unsafe void	Read( System.IO.BinaryReader _Reader )
			{
				m_Type = (ATOM_TYPE) _Reader.ReadInt16();
				short	RunLength = _Reader.ReadInt16();
				_Reader.Read( ms_TempContent, 0, RunLength );

				fixed ( byte* pTemp = &ms_TempContent[0] )
					switch ( m_Type )
					{
						case ATOM_TYPE.FIELD_ID:
						case ATOM_TYPE.FIELD_TYPE:
							m_ValueInt = *((int*) pTemp);
							break;

						case ATOM_TYPE.FIELD_VALUE:
							m_ValueBool = *pTemp != 0;
							m_ValueInt = *((int*) pTemp);
							m_ValueFloat = *((float*) pTemp);
							m_ValueString = System.Runtime.InteropServices.Marshal.PtrToStringAnsi( new IntPtr( &pTemp[4] ) );
							// V2Bool
							m_ValueVector2Bool.X = *((float*) &pTemp[0]);
							m_ValueVector2Bool.Y = *((float*) &pTemp[4]);
							m_ValueVector2Bool.Bool = pTemp[8] != 0;
							// ColorBool
							m_ValueColorBool.Color = Int2Color( m_ValueInt );
							m_ValueColorBool.Bool = pTemp[4] != 0;
							// EnumString
							m_ValueEnumString.Value = m_ValueInt;
							m_ValueEnumString.Name = m_ValueString;
							break;

						case ATOM_TYPE.LENS_OBJECT_DESCRIPTOR:
							m_ValueInt = *((int*) pTemp);
							// TODO: Decode that shit further...
							break;

						case ATOM_TYPE.LENS_OBJECT_NAME:
							m_ValueString = System.Runtime.InteropServices.Marshal.PtrToStringAnsi( new IntPtr( pTemp ) );
							break;

						case ATOM_TYPE.LENS_OBJECT_HIDE:
						case ATOM_TYPE.LENS_OBJECT_SOLO:
							m_ValueBool = *pTemp != 0;
							break;
					}
			}

			#endregion
		}

		/// <summary>
		/// A field is a series of atoms describing an object field (i.e. all the values you can see in the Optical Flares plug-in's lens objects).
		/// The field is composed of :
		///	 _ 1 INT atom => the field ID
		///	 _ 1 INT atom => the field type
		///	 _ 1 VALUE atom => the field value, which is extracted from the atom's variant value based on the field type
		///	 _ 1 atom that marks the end of the field
		/// </summary>
		[System.Diagnostics.DebuggerDisplay( "ID={ID} Type={Type} AsInt={AsInt}" )]
		protected class Field
		{
			#region NESTED TYPES

			public enum TYPE
			{
				FLOAT = 0,
				VECTOR2BOOL = 1,
				COLOR = 2,
				INTEGER = 3,
				BOOL = 4,
				ENUM_STRING = 5,
				SEPARATOR = 6,
			}

			#endregion

			#region FIELDS

			protected int	m_ID;
			protected TYPE	m_Type;
			protected Atom	m_Value;

			protected bool	m_bEndOfObject = false;

			#endregion

			#region PROPERTIES

			public int				ID				{ get { return m_ID; } }
			public TYPE				Type			{ get { return m_Type; } }
			public Atom				Value			{ get { return m_Value; } }

			public bool				AsBool			{ get { return Value.AsBool; } }
			public int				AsInt			{ get { return Value.AsInt; } }
			public float			AsFloat			{ get { return Value.AsFloat; } }
			public Color			AsColor			{ get { return Value.AsColor; } }
			public string			AsString		{ get { return Value.AsString; } }
			public Vector2Bool		AsVector2Bool	{ get { return Value.AsVector2Bool; } }
			public ColorBool		AsColorBool		{ get { return Value.AsColorBool; } }
			public EnumString<int>	AsEnumString	{ get { return Value.AsEnumString; } }

			public bool				IsEndOfObject	{ get { return m_bEndOfObject; } }

			#endregion

			#region METHODS

			public	Field()	{}
			public	Field( System.IO.BinaryReader _Reader ) { Read( _Reader ); }

			public Field	Read( System.IO.BinaryReader _Reader )
			{
				// Read field ID
				Atom	FieldID = new Atom( _Reader );
				m_ID = FieldID.AsInt;
				if ( FieldID.Type != Atom.ATOM_TYPE.FIELD_ID )
				{
//					throw new Exception( "Expected field head atom code 0x32" );
					m_bEndOfObject = true;
					return this;	// This is not a field but an end marker !
				}

				// Read field type
				Atom	FieldType = new Atom( _Reader );
				if ( FieldType.Type != Atom.ATOM_TYPE.FIELD_TYPE )
					throw new Exception( "Expected field type atom code 0x33" );
				m_Type = (TYPE) FieldType.AsInt;

				// Read value
				m_Value = new Atom( _Reader );

				// Should be the field tail...
				Atom	Tail = new Atom( _Reader );
				if ( Tail.Type != Atom.ATOM_TYPE.FIELD_END )
					throw new Exception( "Expected field end atom code 0x7D0" );

				return this;
			}

			#endregion
		}

		/// <summary>
		/// Represents an abstract lens-flare element
		/// </summary>
		public abstract class	BaseElement
		{
			#region NESTED TYPES

			protected class		IDAttribute : Attribute
			{
				public int ID;
				public IDAttribute( int _ID ) { ID = _ID; }
			}

			#endregion

			#region FIELDS

			protected LensFlare					m_Owner = null;
			protected System.IO.BinaryReader	m_Reader = null;

			#endregion

			#region PROPERTIES
			#endregion

			#region METHODS

			public BaseElement( LensFlare _Owner, System.IO.BinaryReader _Reader )
			{
				m_Owner = _Owner;
				m_Reader = _Reader;
				Load();
			}

			protected virtual void	Load()
			{
				// Register fields and their ID
				Dictionary<int,System.Reflection.FieldInfo>	ID2Field = new Dictionary<int,System.Reflection.FieldInfo>();
				System.Reflection.FieldInfo[]	Fields = GetType().GetFields();
				foreach ( System.Reflection.FieldInfo F in Fields )
				{
					IDAttribute[]	IDs = F.GetCustomAttributes( typeof(IDAttribute), true ) as IDAttribute[];
					if ( IDs.Length == 1 )
						ID2Field.Add( IDs[0].ID, F );	// Register the field...
				}

				// Assign field values
 				Field	Field = new Field();
 				while ( true )
				{
					Field.Read( m_Reader );
					if ( Field.IsEndOfObject )
						return;		// Done !
					if ( Field.Type == Field.TYPE.SEPARATOR )
						continue;	// Don't care...

					System.Reflection.FieldInfo	ThisField = ID2Field[Field.ID];
					switch ( Field.Type )
					{
						case Field.TYPE.BOOL:
							ThisField.SetValue( this, Field.AsBool );
							break;
						case Field.TYPE.FLOAT:
							ThisField.SetValue( this, Field.AsFloat );
							break; 
						case Field.TYPE.INTEGER:
							ThisField.SetValue( this, Field.AsInt );
							break;
						case Field.TYPE.COLOR:
							if ( ThisField.FieldType == typeof(Color) )
								ThisField.SetValue( this, Field.AsColor );
							else if ( ThisField.FieldType == typeof(ColorBool) )
								ThisField.SetValue( this, Field.AsColorBool );
							break;
						case Field.TYPE.ENUM_STRING:
							if ( ThisField.FieldType == typeof(int) || ThisField.FieldType.IsEnum )
								ThisField.SetValue( this, Field.AsInt );
							else if ( ThisField.FieldType == typeof(string) )
								ThisField.SetValue( this, Field.AsString );
							else if ( ThisField.FieldType.Name.StartsWith( "EnumString" ) )
							{	// This is a bit tricky as we can't assign our EnumString<int> to a more specific EnumString<T>
								// So we create an instance of the actual EnumString<T> assuming T is an enum then we assign
								//	this instance's fields one by one (i.e. T and string) and assign the instance to the field...

								// Create the EnumString<T> instance
								object	EnumStringInstance = System.Reflection.Assembly.GetExecutingAssembly().CreateInstance( ThisField.FieldType.FullName );

								// Assign instance fields
								System.Reflection.FieldInfo	ValueField = ThisField.FieldType.GetField( "Value" );
								System.Reflection.FieldInfo	NameField = ThisField.FieldType.GetField( "Name" );
								
								ValueField.SetValue( EnumStringInstance, Field.AsEnumString.Value );
								NameField.SetValue( EnumStringInstance, Field.AsEnumString.Name );

								// Assign actual field with instance (crazy ain't it ? ^^)
								ThisField.SetValue( this, EnumStringInstance );
							}
							break;
						case Field.TYPE.VECTOR2BOOL:
							ThisField.SetValue( this, Field.AsVector2Bool );
							break;
					}
				}
			}

// 			protected void	EnumerateFields()
// 			{
// 				Field	F = new Field();
// 				while ( !F.IsEndOfObject )
// 				{
// 					F.Read( m_Reader );
// 				}
// 			}

			#endregion
		}

		/// <summary>
		/// General parameters for the lens flare, not a lens object
		/// </summary>
		public class	GlobalParameters : BaseElement
		{
			#region NESTED TYPES

			public enum BLEND_MODE
			{
				ADD,
				SCREEN,
			}

			public enum MATTE_BOX_SHAPE
			{
				NONE,
				BOX,
				ELLIPSE,
			}

			public enum TEXTURE_IMAGE_TYPE
			{
				NONE,
				COARSE,
				DAG,
				DIRTY,
				DODGY,
				GRAINY,
				GRIME,
				MESSY,
				RANDOM_GRIME,
				SMALL_GRIME,
				SMUDGY,
				SPAZTIC,
				SPECKS,
				CUSTOM1,
				CUSTOM2,
				CUSTOM3,
				CUSTOM4,
				CUSTOM5,
			}

			public enum CHROMATIC_ABERRATION
			{
				NONE,
				PURPLE_FRINGE,
				RED_BLUE_SHIFT,
			}

			#endregion

			#region FIELDS

			// Common settings
			[ID( 0x70 )] public float				GlobalScale;
			[ID( 0x08 )] public float				AspectRatio;
			[ID( 0x09 )] public BLEND_MODE			BlendMode;
			[ID( 0x04 )] public Color				GlobalColor;
			[ID( 0x12 )] public float				GlobalSeed;

			// Matte box controls
			[ID( 0x24 )] public MATTE_BOX_SHAPE		MatteBoxShape;
			[ID( 0x25 )] public float				MatteBoxStartRange;
			[ID( 0x26 )] public float				MatteBoxFadeAmount;

			// Lens texture
			[ID( 0x13 )] public EnumString<TEXTURE_IMAGE_TYPE>	TextureImage;		// Texture index + name
			[ID( 0x3F )] public float				TextureIlluminationRadius;
			[ID( 0x65 )] public float				TextureFallOff;
			[ID( 0x64 )] public float				TextureBrightness;
			[ID( 0x01 )] public Vector2Bool			TextureScale = Vector2Bool.ONE;
			[ID( 0x03 )] public Vector2Bool			TextureOffset = Vector2Bool.ZERO;

			// Chromatic aberration
			[ID( 0x37 )] public CHROMATIC_ABERRATION	AberrationType;
			[ID( 0x33 )] public float				AberrationIntensity;
			[ID( 0x54 )] public float				AberrationSpread;

			// Color correction
			[ID( 0x00 )] public float				CorrectionBrightness;
			[ID( 0x42 )] public float				CorrectionContrast;
			[ID( 0x36 )] public float				CorrectionSaturation;

			#endregion

			#region METHODS

			public GlobalParameters( LensFlare _Owner, System.IO.BinaryReader _Reader ) : base( _Owner, _Reader )
			{
			}

			#endregion
		}

		/// <summary>
		/// Base lens object with generic parameters
		/// </summary>
		public abstract class	LensObject : BaseElement
		{
			#region NESTED TYPES

			public enum AUTO_ROTATE_MODE
			{
				NONE,
				TO_LIGHT,			// Rotates towards light
				TO_CENTER,			// Rotates towards center
			}

			public enum AUTO_ROTATE_COMPLETION_MODE
			{
				NONE,
				OBJECT_ROTATION,
				TO_LIGHT,			// Rotates towards light
				TO_CENTER,			// Rotates towards center
			}

			public enum TRANSLATION_MODE
			{
				FREE,
				HORIZONTAL,			// Only moves horizontally
				VERTICAL,			// Only moves vertically
				NONE,				// Fixed
				CUSTOM,				// Fixed, custom translation
			}

			public enum COLOR_SOURCE
			{
				GLOBAL,
				CUSTOM,				// Only color 1 is used, overriding global color
				SPECTRUM,			// Hue Spectrum
				GRADIENT,			// Color 1 and 2 create a gradient
			}

			public enum TRIGGER_TYPE
			{
				FROM_BORDER,		// Trigger zone is the screen border
				FROM_CENTER,		// Trigger zone is the screen center
				FROM_LIGHT,			// Trigger zone is the light's position
			}

			public enum TRIGGER_MODE
			{
				OBJECT_POSITION,	// Trigger works based on the position of the lens object on screen
				LIGHT_POSITION,		// Trigger works based on the position of the light on screen
			}

			public enum FALLOFF_TYPE
			{
				LINEAR,
				SMOOTH,
				EXPONENTIAL,
			}

			public enum SHAPE_TYPE
			{
				POLYGON,
				CIRCLE,
				TEXTURE
			}

			#endregion

			#region FIELDS

			public string			Name;
			public bool				Hide;
			public bool				Solo;

			// Common settings
			[ID( 0x00 )] public float			Brightness;
			[ID( 0x01 )] public float			Scale;
			[ID( 0x55 )] public Vector2Bool		Stretch = Vector2Bool.ONE;
			[ID( 0x02 )] public Vector2Bool		Distance = Vector2Bool.ONE;
			[ID( 0x05 )] public float			Rotation;	// In degrees !
			[ID( 0x29 )] public AUTO_ROTATE_MODE	AutoRotate;
			[ID( 0x03 )] public Vector2Bool		Offset = Vector2Bool.ZERO;
			[ID( 0x06 )] public TRANSLATION_MODE	Translation;
			[ID( 0x17 )] public Vector2Bool		CustomTranslation = Vector2Bool.ZERO;
			[ID( 0x08 )] public float			AspectRatio;

			// Colorize
			[ID( 0x07 )] public COLOR_SOURCE	ColorSource;
			[ID( 0x04 )] public Color			Color1;
			[ID( 0x66 )] public Color			Color2;
			[ID( 0x5C )] public float			GradientLoops;
			[ID( 0x61 )] public float			GradientOffset;
			[ID( 0x62 )] public bool			ReverseGradient;

			// Dynamic triggering
			[ID( 0x6A )] public bool			EnableTrigger;
			[ID( 0x4B )] public float			BrightnessOffset;
			[ID( 0x4C )] public float			ScaleOffset;
			[ID( 0x7B )] public Vector2Bool		StretchOffset = Vector2Bool.ZERO;
			[ID( 0x6B )] public float			RotationOffset;
			[ID( 0x53 )] public ColorBool		ColorShift;	// Enable color shift is the bool associated to the color
			[ID( 0x47 )] public TRIGGER_TYPE	TriggerType;
			[ID( 0x51 )] public TRIGGER_MODE	TriggerMode;
			[ID( 0x63 )] public bool			InvertTrigger;
			[ID( 0x4D )] public float			BorderWidth;
			[ID( 0x48 )] public float			Expansion;
			[ID( 0x50 )] public float			InnerFalloffRange;
			[ID( 0x49 )] public float			OuterFalloffRange;
			[ID( 0x4A )] public FALLOFF_TYPE	FalloffType;
			[ID( 0x7A )] public Vector2Bool		TriggerStretch = Vector2Bool.ONE;
			[ID( 0x4F )] public bool			PreviewTrigger;
			[ID( 0x52 )] public Vector2Bool		OffsetTriggerLocation = Vector2Bool.ZERO;
			[ID( 0x58 )] public bool			OneWayBrightness;
			[ID( 0x59 )] public bool			OneWayScale;
			[ID( 0x5A )] public bool			OneWayColor;

			// Matte box controls
			[ID( 0x27 )] public float			StartRangeOffset;
			[ID( 0x28 )] public bool			ExcludeFromMatteBox;

			// Advanced settings
			[ID( 0x0A )] public bool			Ignore3DPerspective;
			[ID( 0x1A )] public bool			IgnoreGlobalBrightness;
			[ID( 0x1B )] public bool			IgnoreGlobalScale;
			[ID( 0x16 )] public bool			IgnoreGlobalRotation;

			#endregion

			#region PROPERTIES
			#endregion

			#region METHODS

			public LensObject( LensFlare _Owner, System.IO.BinaryReader _Reader, string _Name, bool _bHide, bool _bSolo ) : base( _Owner, _Reader )
			{
				Name = _Name;
				Hide = _bHide;
				Solo = _bSolo;
			}

			protected override void Load()
			{
				base.Load();

				// Check we really have to enable the trigger (i.e. check it's not default values)
				EnableTrigger &= IsTriggerEnabled();
			}

			/// <summary>
			/// This is a check method to verify we really have to enable the dynamic triggering
			/// </summary>
			/// <returns>True if the trigger values are not default</returns>
			protected virtual bool	IsTriggerEnabled()
			{
				if ( BrightnessOffset != 0.0f ) return true;
				if ( ScaleOffset != 0.0f ) return true;
				if ( StretchOffset.X != 0.0f || StretchOffset.Y != 0.0f ) return true;
				if ( RotationOffset != 0.0f ) return true;
				if ( ColorShift.Bool && ColorShift.Color.ToArgb() != 0x00FFFFFF ) return true;

				return false;
			}

			#endregion
		}

		/// <summary>
		/// Lens Orbs
		/// </summary>
		public class	LensObjectLensOrbs : LensObject
		{
			#region FIELDS

			// Colorize
			[ID( 0x23 )] public float			ColorRandom;

			// Orbs controls
			[ID( 0x0E )] public float			NumberOfObjects;
			[ID( 0x3F )] public float			IlluminationRadius;
			[ID( 0x0F )] public float			ScaleRandom;
			[ID( 0x10 )] public float			BrightnessRandom;
			[ID( 0x2F )] public float			RotationRandom;
			[ID( 0x12 )] public float			RandomSeed;

			// Object shape
			[ID( 0x0B )] public SHAPE_TYPE		ShapeType;
			[ID( 0x13 )] public EnumString<int>	TextureName;		// Texture index + name
			[ID( 0x57 )] public float			ShapeOrientation;
			[ID( 0x0C )] public float			PolygonSides;
			[ID( 0x2A )] public float			PolygonRoundness;
			[ID( 0x2B )] public float			BladeNotching;
			[ID( 0x11 )] public float			Smoothness;
			[ID( 0x19 )] public float			SmoothnessRandom;
			[ID( 0x2E )] public float			OutlineIntensity;
			[ID( 0x2C )] public float			OutlineThickness;
			[ID( 0x2D )] public float			OutlineFeathering;

			// Circular completion
			[ID( 0x31 )] public float			Completion;
			[ID( 0x32 )] public float			CompletionFeathering;
			[ID( 0x3C )] public float			CompletionRotation;
			[ID( 0x3E )] public float			CompletionRotationRandom;
			[ID( 0x3B )] public AUTO_ROTATE_COMPLETION_MODE	AutoRotateCompletion;

			// Dynamic triggering
			[ID( 0x4E )] public float			CompletionOffset;
			[ID( 0x56 )] public float			OutlineIntensityOffset;

			// Advanced
			[ID( 0x40 )] public bool			RenderOnce;

			#endregion

			#region METHODS

			public LensObjectLensOrbs( LensFlare _Owner, System.IO.BinaryReader _Reader, string _Name, bool _bHide, bool _bSolo ) : base( _Owner, _Reader, _Name, _bHide, _bSolo )
			{
			}

			protected override bool IsTriggerEnabled()
			{
				if ( base.IsTriggerEnabled() )
					return true;

				if ( CompletionOffset != 0.0f ) return true;
				if ( OutlineIntensityOffset != 0.0f ) return true;

				return false;
			}

			#endregion
		}

		/// <summary>
		/// Caustic
		/// </summary>
		public class	LensObjectCaustic : LensObject
		{
			#region FIELDS

			// Caustic controls
			[ID( 0x76 )] public float			RingBrightness;
			[ID( 0x77 )] public float			RingOutline;
			[ID( 0x72 )] public float			CoreBrightness;
			[ID( 0x73 )] public Vector2Bool		CoreScale = Vector2Bool.ONE;
			[ID( 0x74 )] public float			CoreRoundness;
			[ID( 0x75 )] public float			CoreDistortion;

			#endregion

			#region METHODS

			public LensObjectCaustic( LensFlare _Owner, System.IO.BinaryReader _Reader, string _Name, bool _bHide, bool _bSolo ) : base( _Owner, _Reader, _Name, _bHide, _bSolo )
			{
			}

			#endregion
		}

		/// <summary>
		/// Hoop
		/// </summary>
		public class	LensObjectHoop : LensObject
		{
			#region FIELDS

			// The hoop overrides the distance field which is not a Vector2 anymore
			[ID( 0x67 )] public new float		Distance;

			// Animation controls
			[ID( 0x6E )] public bool			EnableAnimation;
			[ID( 0x6C )] public float			AnimationSpeed;
			[ID( 0x6D )] public float			AnimationAmount;

			// Hoop controls
			[ID( 0x34 )] public float			Complexity;
			[ID( 0x68 )] public bool			Continuous;
			[ID( 0x35 )] public float			Detail;
			[ID( 0x43 )] public float			Length;
			[ID( 0x69 )] public float			LineThickness;
			[ID( 0x78 )] public float			LineSpacing;
			[ID( 0x12 )] public float			RandomSeed;

			// Circular completion
			[ID( 0x31 )] public float			Completion;
			[ID( 0x32 )] public float			CompletionFeathering;
			[ID( 0x3C )] public float			CompletionRotation;

			// Dynamic triggering
			[ID( 0x4E )] public float			CompletionOffset;

			#endregion

			#region METHODS

			public LensObjectHoop( LensFlare _Owner, System.IO.BinaryReader _Reader, string _Name, bool _bHide, bool _bSolo ) : base( _Owner, _Reader, _Name, _bHide, _bSolo )
			{
			}

			protected override bool IsTriggerEnabled()
			{
				if ( base.IsTriggerEnabled() )
					return true;

				if ( CompletionOffset != 0.0f ) return true;

				return false;
			}

			#endregion
		}

		/// <summary>
		/// Ring
		/// </summary>
		public class	LensObjectRing : LensObject
		{
			#region FIELDS

			// Animation controls
			[ID( 0x6E )] public bool			EnableAnimation;
			[ID( 0x6C )] public float			AnimationSpeed;
			[ID( 0x6D )] public float			AnimationAmount;

			// Ring controls
			[ID( 0x38 )] public float			Thickness;
			[ID( 0x39 )] public float			InsideFeathering;
			[ID( 0x3A )] public float			OutsideFeathering;

			// Circular completion
			[ID( 0x31 )] public float			Completion;
			[ID( 0x32 )] public float			CompletionFeathering;
			[ID( 0x3C )] public float			CompletionRotation;
			[ID( 0x3B )] public AUTO_ROTATE_COMPLETION_MODE	AutoRotateCompletion;

			// Dynamic triggering
			[ID( 0x4E )] public float			CompletionOffset;
			[ID( 0x79 )] public float			ThicknessOffset;

			// Advanced
			[ID( 0x5D )] public float			ConvexRoundness;

			#endregion

			#region METHODS

			public LensObjectRing( LensFlare _Owner, System.IO.BinaryReader _Reader, string _Name, bool _bHide, bool _bSolo ) : base( _Owner, _Reader, _Name, _bHide, _bSolo )
			{
			}

			protected override bool IsTriggerEnabled()
			{
				if ( base.IsTriggerEnabled() )
					return true;

				if ( CompletionOffset != 0.0f ) return true;
				if ( ThicknessOffset != 0.0f ) return true;

				return false;
			}

			#endregion
		}

		/// <summary>
		/// Sparkle
		/// </summary>
		public class	LensObjectSparkle : LensObject
		{
			#region FIELDS

			// Animation controls
			[ID( 0x6E )] public bool			EnableAnimation;
			[ID( 0x6C )] public float			AnimationSpeed;
			[ID( 0x6D )] public float			AnimationAmount;

			// Shimmer controls
			[ID( 0x34 )] public float			Complexity;
			[ID( 0x45 )] public float			Length;
			[ID( 0x5E )] public float			LengthRandom;
			[ID( 0x43 )] public float			Thickness;
			[ID( 0x6F )] public float			ThicknessRandom;
			[ID( 0x10 )] public float			BrightnessRandom;
			[ID( 0x0D )] public float			Spread;
			[ID( 0x5F )] public float			SpreadRandom;
			[ID( 0x41 )] public float			SpacingRandom;
			[ID( 0x57 )] public float			ShapeOrientation;
			[ID( 0x12 )] public float			RandomSeed;

			// Circular completion
			[ID( 0x31 )] public float			Completion;
			[ID( 0x32 )] public float			CompletionFeathering;
			[ID( 0x3C )] public float			CompletionRotation;
			[ID( 0x3B )] public AUTO_ROTATE_COMPLETION_MODE	AutoRotateCompletion;

			// Dynamic triggering
			[ID( 0x4E )] public float			CompletionOffset;
			[ID( 0x60 )] public float			SpreadOffset;

			#endregion

			#region METHODS

			public LensObjectSparkle( LensFlare _Owner, System.IO.BinaryReader _Reader, string _Name, bool _bHide, bool _bSolo ) : base( _Owner, _Reader, _Name, _bHide, _bSolo )
			{
			}

			protected override bool IsTriggerEnabled()
			{
				if ( base.IsTriggerEnabled() )
					return true;

				if ( CompletionOffset != 0.0f ) return true;
				if ( SpreadOffset != 0.0f ) return true;

				return false;
			}

			#endregion
		}

		/// <summary>
		/// Spike Ball
		/// </summary>
		public class	LensObjectSpikeBall : LensObject
		{
			#region FIELDS

			// Animation controls
			[ID( 0x6E )] public bool			EnableAnimation;
			[ID( 0x6C )] public float			AnimationSpeed;
			[ID( 0x6D )] public float			AnimationAmount;

			// Shimmer controls
			[ID( 0x34 )] public float			Complexity;
			[ID( 0x45 )] public float			Length;
			[ID( 0x5E )] public float			LengthRandom;
			[ID( 0x43 )] public float			Thickness;
			[ID( 0x6F )] public float			ThicknessRandom;
			[ID( 0x10 )] public float			BrightnessRandom;
			[ID( 0x41 )] public float			SpacingRandom;
			[ID( 0x57 )] public float			ShapeOrientation;
			[ID( 0x12 )] public float			RandomSeed;

			// Circular completion
			[ID( 0x31 )] public float			Completion;
			[ID( 0x32 )] public float			CompletionFeathering;
			[ID( 0x3C )] public float			CompletionRotation;
			[ID( 0x3B )] public AUTO_ROTATE_COMPLETION_MODE	AutoRotateCompletion;

			// Dynamic triggering
			[ID( 0x4E )] public float			CompletionOffset;

			#endregion

			#region METHODS

			public LensObjectSpikeBall( LensFlare _Owner, System.IO.BinaryReader _Reader, string _Name, bool _bHide, bool _bSolo ) : base( _Owner, _Reader, _Name, _bHide, _bSolo )
			{
			}

			protected override bool IsTriggerEnabled()
			{
				if ( base.IsTriggerEnabled() )
					return true;

				if ( CompletionOffset != 0.0f ) return true;

				return false;
			}

			#endregion
		}

		/// <summary>
		/// Glint
		/// </summary>
		public class	LensObjectGlint : LensObject
		{
			#region FIELDS

			// Animation controls
			[ID( 0x6E )] public bool			EnableAnimation;
			[ID( 0x6C )] public float			AnimationSpeed;
			[ID( 0x6D )] public float			AnimationAmount;

			// Shimmer controls
			[ID( 0x34 )] public float			Complexity;
			[ID( 0x45 )] public float			Length;
			[ID( 0x5E )] public float			LengthRandom;
			[ID( 0x43 )] public float			Thickness;
			[ID( 0x41 )] public float			SpacingRandom;
			[ID( 0x57 )] public float			ShapeOrientation;
			[ID( 0x12 )] public float			RandomSeed;

			// Circular completion
			[ID( 0x31 )] public float			Completion;
			[ID( 0x32 )] public float			CompletionFeathering;
			[ID( 0x3C )] public float			CompletionRotation;
			[ID( 0x3B )] public AUTO_ROTATE_COMPLETION_MODE	AutoRotateCompletion;

			// Dynamic triggering
			[ID( 0x4E )] public float			CompletionOffset;

			#endregion

			#region METHODS

			public LensObjectGlint( LensFlare _Owner, System.IO.BinaryReader _Reader, string _Name, bool _bHide, bool _bSolo ) : base( _Owner, _Reader, _Name, _bHide, _bSolo )
			{
			}

			protected override bool IsTriggerEnabled()
			{
				if ( base.IsTriggerEnabled() )
					return true;

				if ( CompletionOffset != 0.0f ) return true;

				return false;
			}

			#endregion
		}

		/// <summary>
		/// Shimmer
		/// </summary>
		public class	LensObjectShimmer : LensObject
		{
			#region FIELDS

			// Animation controls
			[ID( 0x6E )] public bool			EnableAnimation;
			[ID( 0x6C )] public float			AnimationSpeed;
			[ID( 0x6D )] public float			AnimationAmount;

			// Shimmer controls
			[ID( 0x34 )] public float			Complexity;
			[ID( 0x35 )] public float			Detail;
			[ID( 0x57 )] public float			ShapeOrientation;
			[ID( 0x12 )] public float			RandomSeed;

			// Circular completion
			[ID( 0x31 )] public float			Completion;
			[ID( 0x32 )] public float			CompletionFeathering;
			[ID( 0x3C )] public float			CompletionRotation;
			[ID( 0x3B )] public AUTO_ROTATE_COMPLETION_MODE	AutoRotateCompletion;

			// Dynamic triggering
			[ID( 0x4E )] public float			CompletionOffset;

			#endregion

			#region METHODS

			public LensObjectShimmer( LensFlare _Owner, System.IO.BinaryReader _Reader, string _Name, bool _bHide, bool _bSolo ) : base( _Owner, _Reader, _Name, _bHide, _bSolo )
			{
			}

			protected override bool IsTriggerEnabled()
			{
				if ( base.IsTriggerEnabled() )
					return true;

				if ( CompletionOffset != 0.0f ) return true;

				return false;
			}

			#endregion
		}

		/// <summary>
		/// Streak
		/// </summary>
		public class	LensObjectStreak : LensObject
		{
			#region FIELDS

			// Streak control
			[ID( 0x45 )] public float			Length;
			[ID( 0x43 )] public float			Thickness;
			[ID( 0x44 )] public float			CoreIntensity;
			[ID( 0x1E )] public float			Symmetry;
			[ID( 0x1F )] public float			FanEnds;
			[ID( 0x46 )] public float			FanFeathering;
			[ID( 0x20 )] public float			ReplicatorCopies;
			[ID( 0x21 )] public float			ReplicatorAngle;
			[ID( 0x22 )] public float			ScaleRandom;
			[ID( 0x41 )] public float			SpacingRandom;
			[ID( 0x12 )] public float			RandomSeed;

			#endregion

			#region METHODS

			public LensObjectStreak( LensFlare _Owner, System.IO.BinaryReader _Reader, string _Name, bool _bHide, bool _bSolo ) : base( _Owner, _Reader, _Name, _bHide, _bSolo )
			{
			}

			#endregion
		}

		/// <summary>
		/// Simple glow object that sticks to the light
		/// </summary>
		public class	LensObjectGlow : LensObject
		{
			#region FIELDS

			// Glow control
			[ID( 0x1D )] public float			Gamma;

			#endregion

			#region METHODS

			public LensObjectGlow( LensFlare _Owner, System.IO.BinaryReader _Reader, string _Name, bool _bHide, bool _bSolo ) : base( _Owner, _Reader, _Name, _bHide, _bSolo )
			{
			}

			#endregion
		}

		/// <summary>
		/// Simple iris object showing a basic polygon by default
		/// NOTE: The multi-iris and iris objects are merged into that class as a single iris is just a particular case (i.e. no random) of a multi-iris
		/// </summary>
		public class	LensObjectIris : LensObject
		{
			#region FIELDS

			protected bool		m_bIsMultiIris = false;

			// Colorize
			[ID( 0x23 )] public float			ColorRandom = 0.0f;

			// Multi-iris control
			[ID( 0x0D )] public float			Spread = 0.0f;
			[ID( 0x3D )] public float			SpreadRandom = 0.0f;
			[ID( 0x0E )] public float			NumberOfObjects = 1;
			[ID( 0x0F )] public float			ScaleRandom = 0.0f;
			[ID( 0x10 )] public float			BrightnessRandom = 0.0f;
			[ID( 0x2F )] public float			RotationRandom = 0.0f;
			[ID( 0x30 )] public float			OffsetRandom = 0.0f;
			[ID( 0x12 )] public float			RandomSeed = 5000;

			// Object shape
			[ID( 0x0B )] public SHAPE_TYPE		ShapeType;
			[ID( 0x13 )] public EnumString<int>	TextureName;		// Texture index + name
			[ID( 0x57 )] public float			ShapeOrientation;
			[ID( 0x71 )] public float			OrientationRandom = 0.0f;
			[ID( 0x0C )] public float			PolygonSides;
			[ID( 0x2A )] public float			PolygonRoundness;
			[ID( 0x2B )] public float			BladeNotching;
			[ID( 0x11 )] public float			Smoothness;
			[ID( 0x19 )] public float			SmoothnessRandom = 0.0f;
			[ID( 0x2E )] public float			OutlineIntensity;
			[ID( 0x2C )] public float			OutlineThickness;
			[ID( 0x2D )] public float			OutlineFeathering;

			// Circular completion
			[ID( 0x31 )] public float			Completion;
			[ID( 0x32 )] public float			CompletionFeathering;
			[ID( 0x3C )] public float			CompletionRotation;
			[ID( 0x3E )] public float			CompletionRotationRandom = 0.0f;
			[ID( 0x3B )] public AUTO_ROTATE_COMPLETION_MODE	AutoRotateCompletion;

			// Dynamic triggering
			[ID( 0x4E )] public float			CompletionOffset;
			[ID( 0x56 )] public float			OutlineIntensityOffset;

			// Advanced
			[ID( 0x5D )] public float			ConvexRoundness;

			#endregion

			#region PROPERTIES

			/// <summary>
			/// Tells if this object is a single iris (false) or a multi-iris (true)
			/// This is important as the Distance parameter is not treated the same way
			/// </summary>
			public bool		IsMultiIris { get { return m_bIsMultiIris; } }

			#endregion

			#region METHODS

			public LensObjectIris( LensFlare _Owner, System.IO.BinaryReader _Reader, string _Name, bool _bHide, bool _bSolo, bool _bIsMultiIris ) : base( _Owner, _Reader, _Name, _bHide, _bSolo )
			{
				m_bIsMultiIris = _bIsMultiIris;
			}

			protected override bool IsTriggerEnabled()
			{
				if ( base.IsTriggerEnabled() )
					return true;

				if ( CompletionOffset != 0.0f ) return true;
				if ( OutlineIntensityOffset != 0.0f ) return true;

				return false;
			}

			#endregion
		}

// 		/// <summary>
// 		/// Multiple iris objects
// 		/// </summary>
// 		public class	LensObjectMultiIris : LensObject
// 		{
// 			#region FIELDS
// 
// 			// Colorize
// 			[ID( 0x23 )] public float			ColorRandom;
// 
// 			// Multi iris control
// 			[ID( 0x0D )] public float			Spread;
// 			[ID( 0x3D )] public float			SpreadRandom;
// 			[ID( 0x0E )] public float			NumberOfObjects;
// 			[ID( 0x0F )] public float			ScaleRandom;
// 			[ID( 0x10 )] public float			BrightnessRandom;
// 			[ID( 0x2F )] public float			RotationRandom;
// 			[ID( 0x30 )] public float			OffsetRandom;
// 			[ID( 0x12 )] public float			RandomSeed;
// 
// 			// Object shape
// 			[ID( 0x0B )] public SHAPE_TYPE		ShapeType;
// 			[ID( 0x13 )] public EnumString<int>	TextureName;		// Texture index + name
// 			[ID( 0x57 )] public float			ShapeOrientation;
// 			[ID( 0x71 )] public float			OrientationRandom;
// 			[ID( 0x0C )] public float			PolygonSides;
// 			[ID( 0x2A )] public float			PolygonRoundness;
// 			[ID( 0x2B )] public float			BladeNotching;
// 			[ID( 0x11 )] public float			Smoothness;
// 			[ID( 0x19 )] public float			SmoothnessRandom;
// 			[ID( 0x2E )] public float			OutlineIntensity;
// 			[ID( 0x2C )] public float			OutlineThickness;
// 			[ID( 0x2D )] public float			OutlineFeathering;
// 
// 			// Circular completion
// 			[ID( 0x31 )] public float			Completion;
// 			[ID( 0x32 )] public float			CompletionFeathering;
// 			[ID( 0x3C )] public float			CompletionRotation;
// 			[ID( 0x3E )] public float			CompletionRotationRandom;
// 			[ID( 0x3B )] public AUTO_ROTATE_COMPLETION_MODE	AutoRotateCompletion;
// 
// 			// Dynamic triggering
// 			[ID( 0x4E )] public float			CompletionOffset;
// 			[ID( 0x56 )] public float			OutlineIntensityOffset;
// 
// 			// Advanced
// 			[ID( 0x5D )] public float			ConvexRoundness;
// 
// 			#endregion
// 
// 			#region METHODS
// 
// 			public LensObjectMultiIris( LensFlare _Owner, System.IO.BinaryReader _Reader, string _Name, bool _bHide, bool _bSolo ) : base( _Owner, _Reader, _Name, _bHide, _bSolo )
// 			{
// 			}
// 
// 			protected override bool IsTriggerEnabled()
// 			{
// 				if ( base.IsTriggerEnabled() )
// 					return true;
// 
// 				if ( CompletionOffset != 0.0f ) return true;
// 				if ( OutlineIntensityOffset != 0.0f ) return true;
// 
// 				return false;
// 			}
// 
// 			#endregion
// 		}

		#endregion

		#region FIELDS

		protected GlobalParameters	m_Parameters = null;
		protected List<LensObject>	m_LensObjects = new List<LensObject>();

		#endregion

		#region PROPERTIES

		public GlobalParameters	Parameters	{ get { return m_Parameters; } }
		public LensObject[]		LensObjects	{ get { return m_LensObjects.ToArray(); } }

		#endregion

		#region METHODS

		public LensFlare()
		{
		}

		/// <summary>
		/// Loads an .OFP file from a stream
		/// </summary>
		/// <param name="_Stream"></param>
		public void		Load( System.IO.Stream _Stream )
		{
			System.IO.BinaryReader	Reader = new System.IO.BinaryReader( _Stream, System.Text.Encoding.ASCII );

			byte[]	Header = new byte[4];
			Reader.Read( Header, 0, 4 );

			//////////////////////////////////////////////////////////////////////////
			// Check mandatory header signature & version
			if ( Header[0] != 'O' || Header[1] != 'F' || Header[2] != 'P' )
				throw new Exception( "This is not an OFP file !" );

			if ( Header[3] != 3 )
				throw new Exception( "OFP File is version " + Header[3] + " but only version 3 is currently supported !" );

			//////////////////////////////////////////////////////////////////////////
			// Decode file
			while ( _Stream.Position < _Stream.Length )
			{
				Atom	A = new Atom( Reader );

				switch ( A.Type )
				{
					case Atom.ATOM_TYPE.GENERAL_PARAMETERS:
						m_Parameters = new GlobalParameters( this, Reader );
						break;

					case Atom.ATOM_TYPE.LENS_OBJECT_DESCRIPTOR:
						{
							int	LensObjectType = A.AsInt;

							// The next atom should be the object name
							A.Read( Reader );
							if ( A.Type != Atom.ATOM_TYPE.LENS_OBJECT_NAME )
								throw new Exception( "Expected lens object name !" );

							string LensObjectName = A.AsString;

							// Next one should be the HIDE flag
							A.Read( Reader );
							if ( A.Type != Atom.ATOM_TYPE.LENS_OBJECT_HIDE )
								throw new Exception( "Expected lens object hide flag !" );

							bool	bHideObject = A.AsBool;

							// Next one should be the SOLO flag
							A.Read( Reader );
							if ( A.Type != Atom.ATOM_TYPE.LENS_OBJECT_SOLO )
								throw new Exception( "Expected lens object solo flag !" );

							bool	bSoloObject = A.AsBool;

							// Create the lens object
							LensObject	LO = null;
							switch ( LensObjectType )
							{
		 						case 0x1: LO = new LensObjectGlow( this, Reader, LensObjectName, bHideObject, bSoloObject ); break;
		 						case 0x2: LO = new LensObjectStreak( this, Reader, LensObjectName, bHideObject, bSoloObject ); break;
		 						case 0x4: LO = new LensObjectIris( this, Reader, LensObjectName, bHideObject, bSoloObject, true ); break;
		 						case 0x5: LO = new LensObjectShimmer( this, Reader, LensObjectName, bHideObject, bSoloObject ); break;
		 						case 0x6: LO = new LensObjectRing( this, Reader, LensObjectName, bHideObject, bSoloObject ); break;
		 						case 0x7: LO = new LensObjectHoop( this, Reader, LensObjectName, bHideObject, bSoloObject ); break;
		 						case 0x8: LO = new LensObjectGlint( this, Reader, LensObjectName, bHideObject, bSoloObject ); break;
		 						case 0x9: LO = new LensObjectSparkle( this, Reader, LensObjectName, bHideObject, bSoloObject ); break;
		 						case 0xB: LO = new LensObjectSpikeBall( this, Reader, LensObjectName, bHideObject, bSoloObject ); break;
		 						case 0xC: LO = new LensObjectCaustic( this, Reader, LensObjectName, bHideObject, bSoloObject ); break;
		 						case 0xE: LO = new LensObjectLensOrbs( this, Reader, LensObjectName, bHideObject, bSoloObject ); break;
		 						case 0xF: LO = new LensObjectIris( this, Reader, LensObjectName, bHideObject, bSoloObject, false ); break;
							}

							if ( LO != null )
			 					m_LensObjects.Add( LO );
						}
						break;

					default:
						throw new Exception( "Unrecognized atom type 0x" + ((int) A.Type).ToString( "X" ) + " !" );
				}
			}
		}

		/// <summary>
		/// Helper to convert an ABGR color (i.e. their format) to an ARGB color (i.e. standard System.Drawing.Color type format)
		/// </summary>
		/// <param name="_ABGR"></param>
		/// <returns></returns>
		protected static Color Int2Color( int _ABGR )
		{
			int	ARGB = (int) (
						  ((uint) (_ABGR & 0xFF00FF00))
						| ((uint) (_ABGR & 0x00FF0000) >> 16)
						| ((uint) (_ABGR & 0x000000FF) << 16)
						);
			return Color.FromArgb( ARGB );
		}

		#endregion
	}
}
