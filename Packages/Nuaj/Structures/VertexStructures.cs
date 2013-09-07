using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;

using SharpDX;

namespace Nuaj
{
	/// <summary>
	/// This attribute must be used for every field inside a VertexStructure so the field can be mapped to a valid shader input semantic
	/// </summary>
	public class	SemanticAttribute : Attribute
	{
		#region CODE

		protected string	m_Semantic;
		protected int		m_Index = 0;
		protected bool		m_bPerVertexData = true;

		/// <summary>
		/// Gets the field's semantic
		/// </summary>
		public string		Semantic		{ get { return m_Semantic; } }

		/// <summary>
		/// Gets the field's semantic index
		/// (like 0 for TEXCOORD0, 1 for TEXCOORD1, etc.)
		/// (for a 4x4 matrix, each row would have the same semantic but with indices from 0 to 3)
		/// </summary>
		public int			Index			{ get { return m_Index; } }

		public SemanticAttribute( string _Semantic )					{ m_Semantic = _Semantic; }
		public SemanticAttribute( string _Semantic, int _Index )		{ m_Semantic = _Semantic; m_Index = _Index; }

		#endregion

		// Here are defined the standard semantics used across Nuaj
		public const string	POSITION = "POSITION";
		public const string	POSITION_TRANSFORMED = "SV_Position";
		public const string	NORMAL = "NORMAL";
		public const string	TANGENT = "TANGENT";
		public const string	BITANGENT = "BITANGENT";
		public const string	COLOR = "COLOR";
		public const string	VIEW = "VIEW";
		public const string	CURVATURE = "CURVATURE";
		public const string	TEXCOORD = "TEXCOORD";	// In the shader, this semantic is written as TEXCOORD0, TEXCOORD1, etc.
													// To reflect this, use that constant and increment the index in the constructor with index
	}

	/// <summary>
	/// Use this attribute to define semantic attributes for instance buffers
	/// </summary>
	public class	InstanceSemanticAttribute : SemanticAttribute
	{
		#region CODE

		protected int		m_StepRate = 1;

		/// <summary>
		/// Gets the rate at which this instance field should be used
		/// (e.g. use 1 if this field is changed at each instance, 2 if it is changed every 2 instances, etc.)
		/// </summary>
		public int			StepRate		{ get { return m_StepRate; } }

		public InstanceSemanticAttribute( string _Semantic, int _StepRate ) : base( _Semantic )													{ m_StepRate = _StepRate; }
		public InstanceSemanticAttribute( string _Semantic, int _Index, int _StepRate ) : base( _Semantic, _Index )								{ m_StepRate = _StepRate; }

		#endregion
	}

	/// <summary>
	/// Use this attribute to start a new buffer description
	/// </summary>
	/// <example>
	/// struct VS_COMPOSITE
	/// {
	///		[VertexBufferStart( 0 )]
	///		VS_P3	Position;		// This position will be stored in VB 0
	///		
	///		[VertexBufferStart( 1 )]
	///		VS_T2	UV;				// These UV coordinates will be stored in VB 1
	/// }
	/// </example>
	public class	VertexBufferStartAttribute : Attribute
	{
		#region CODE

		protected int	m_SlotIndex = 0;
		public int		SlotIndex	{ get { return m_SlotIndex; } }

		public VertexBufferStartAttribute( int _SlotIndex )	{ m_SlotIndex = _SlotIndex; }

		#endregion
	}

	/// <summary>
	/// This is an example of a basic vertex structure to be used as a model for your own structures
	/// Materials cannot be created if not provided with a valid vertex structure
	/// Also, only the following field types are supported by the materials :
	///		_ float / Half
	///		_ Vector2 / Half2
	///		_ Vector3 / Color3 (WARNING: Half3 is NOT supported !)
	///		_ Vector4 / Half4 / Color4
	///		_ Matrix
	///	
	/// You MUST use the SemanticAttribute for every field in the vertex structure
	///  otherwise the field won't be mapped to an input semantic against the vertex shader and the material will fail to initialize
	///  
	/// Naming convention goes as follow:
	///	
	///		Pt	= Transformed Position (XYZW)
	///		P	= Position
	///		T	= Texture2D Coordinates
	///		N	= Normal
	///		G	= Tangent
	///		B	= Bi-Tangent
	///		C	= Color (HDR) (I haven't found the use of 8-bits colors yet)
	///		V	= View
	///		S	= Size
	///		I	= Index
	///		D	= Displacement
	///		O	= Offset
	///		Cu	= Curvature
	///	
	/// Each letter is then followed by the amount of FLOAT registers used to store the info
	///	(typically, 3 for 3D positions, 2 for UV texture coordinates and so on)
	/// </summary>
	[StructLayout( LayoutKind.Sequential )]
	public struct	VS_P3
	{
		[Semantic( SemanticAttribute.POSITION )]
		public Vector3	Position;
	}

	[StructLayout( LayoutKind.Sequential )]
	public struct	VS_P3C4
	{
		[Semantic( SemanticAttribute.POSITION )]
		public Vector3	Position;
		[Semantic( SemanticAttribute.COLOR )]
		public Vector4	Color;
	}

	[StructLayout( LayoutKind.Sequential )]
	public struct	VS_P3T2
	{
		[Semantic( SemanticAttribute.POSITION )]
		public Vector3	Position;
		[Semantic( SemanticAttribute.TEXCOORD, 0 )]
		public Vector2	UV;
	}

	[StructLayout( LayoutKind.Sequential )]
	public struct	VS_P3N3
	{
		[Semantic( SemanticAttribute.POSITION )]
		public Vector3	Position;
		[Semantic( SemanticAttribute.NORMAL )]
		public Vector3	Normal;
	}

	[StructLayout( LayoutKind.Sequential )]
	public struct	VS_P3N3T2
	{
		[Semantic( SemanticAttribute.POSITION )]
		public Vector3	Position;
		[Semantic( SemanticAttribute.NORMAL )]
		public Vector3	Normal;
		[Semantic( SemanticAttribute.TEXCOORD, 0 )]
		public Vector2	UV;
	}

	[StructLayout( LayoutKind.Sequential )]
	public struct	VS_P3N3G3
	{
		[Semantic( SemanticAttribute.POSITION )]
		public Vector3	Position;
		[Semantic( SemanticAttribute.NORMAL )]
		public Vector3	Normal;
		[Semantic( SemanticAttribute.TANGENT )]
		public Vector3	Tangent;
	}

	[StructLayout( LayoutKind.Sequential )]
	public struct	VS_P3N3G3T2
	{
		[Semantic( SemanticAttribute.POSITION )]
		public Vector3	Position;
		[Semantic( SemanticAttribute.NORMAL )]
		public Vector3	Normal;
		[Semantic( SemanticAttribute.TANGENT )]
		public Vector3	Tangent;
		[Semantic( SemanticAttribute.TEXCOORD, 0 )]
		public Vector2	UV;
	}

	[StructLayout( LayoutKind.Sequential )]
	public struct	VS_P3N3G3B3T2
	{
		[Semantic( SemanticAttribute.POSITION )]
		public Vector3	Position;
		[Semantic( SemanticAttribute.NORMAL )]
		public Vector3	Normal;
		[Semantic( SemanticAttribute.TANGENT )]
		public Vector3	Tangent;
		[Semantic( SemanticAttribute.BITANGENT )]
		public Vector3	BiTangent;
		[Semantic( SemanticAttribute.TEXCOORD, 0 )]
		public Vector2	UV;
	}

	[StructLayout( LayoutKind.Sequential )]
	public struct	VS_P3N3G3B3T2Cu2
	{
		[Semantic( SemanticAttribute.POSITION )]
		public Vector3	Position;
		[Semantic( SemanticAttribute.NORMAL )]
		public Vector3	Normal;
		[Semantic( SemanticAttribute.TANGENT )]
		public Vector3	Tangent;
		[Semantic( SemanticAttribute.BITANGENT )]
		public Vector3	BiTangent;
		[Semantic( SemanticAttribute.TEXCOORD, 0 )]
		public Vector2	UV;
		[Semantic( SemanticAttribute.CURVATURE )]
		public Vector2	Curvature;
	}

	/// <summary>
	/// This structure should be used to render post-process passes
	/// </summary>
	[StructLayout( LayoutKind.Sequential )]
	public struct	VS_Pt4V3T2
	{
		[Semantic( SemanticAttribute.POSITION_TRANSFORMED )]
		public Vector4	Position;	// This is the position in clip space
		[Semantic( SemanticAttribute.VIEW )]
		public Vector3	View;		// This is the view vector in camera space (should be renormalized in the pixel shader)
		[Semantic( SemanticAttribute.TEXCOORD, 0 )]
		public Vector2	UV;			// This is the mapping coord in [0,1] where (0,0) is the top left corner and (1,1) the bottom right corner of the quad
	}

	[StructLayout( LayoutKind.Sequential )]
	public struct	VS_Pt4
	{
		[Semantic( SemanticAttribute.POSITION_TRANSFORMED )]
		public Vector4	Position;	// This is the position in clip space
	}

	[StructLayout( LayoutKind.Sequential )]
	public struct	VS_Pt4C4T2
	{
		[Semantic( SemanticAttribute.POSITION_TRANSFORMED )]
		public Vector4		Position;
		[Semantic( SemanticAttribute.COLOR )]
		public Vector4		Color;
		[Semantic( SemanticAttribute.TEXCOORD, 0 )]
		public Vector2		UV;
	}

	[StructLayout( LayoutKind.Sequential )]
	public struct	VS_Pt4T2
	{
		[Semantic( SemanticAttribute.POSITION_TRANSFORMED )]
		public Vector4		Position;
		[Semantic( SemanticAttribute.TEXCOORD, 0 )]
		public Vector2		UV;
	}

	[StructLayout( LayoutKind.Sequential )]
	public struct	VS_P3C4T2
	{
		[Semantic( SemanticAttribute.POSITION )]
		public Vector3		Position;
		[Semantic( SemanticAttribute.COLOR )]
		public Vector4		Color;
		[Semantic( SemanticAttribute.TEXCOORD, 0 )]
		public Vector2		UV;
	}

	[StructLayout( LayoutKind.Sequential )]
	public struct	VS_T2
	{
		[Semantic( SemanticAttribute.TEXCOORD, 0 )]
		public Vector2		UV;
	}

	[StructLayout( LayoutKind.Sequential )]
	public struct	VS_T4
	{
		[Semantic( SemanticAttribute.TEXCOORD, 0 )]
		public Vector4		UV;
	}
}
