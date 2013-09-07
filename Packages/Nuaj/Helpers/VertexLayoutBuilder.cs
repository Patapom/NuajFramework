using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using SharpDX;
using SharpDX.Direct3D10;
using SharpDX.DXGI;

namespace Nuaj.Helpers
{
	/// <summary>
	/// This helps building input vertex layouts from a template type
	/// </summary>
	public class VertexLayoutBuilder
	{
		#region METHODS

		/// <summary>
		/// Builds the vertex input layout from the template vertex structure type
		/// </summary>
		/// <param name="_ValidationTechnique">An effect technique used to validate the layout against</param>
		/// <typeparam name="T"></typeparam>
		/// <returns></returns>
		public static InputLayout	BuildVertexLayout<T>( EffectTechnique _ValidationTechnique ) where T:struct
		{
			// Let's use C# reflection to analyze the vertex structure used as template for this material
			Type	VSType = typeof(T);
			return BuildVertexLayout( VSType, _ValidationTechnique );
		}

		public static InputLayout	BuildVertexLayout( Type _VSType, EffectTechnique _ValidationTechnique )
		{
			List<InputElement>	InputElements = new List<InputElement>();
			BuildVertexInputElements( _VSType, InputElements, 0 );

			return new InputLayout(	Device.Instance.DirectXDevice, 
									// We need the shader so that its input signature is validated against the array...
									_ValidationTechnique.GetPassByIndex( 0 ).Description.Signature,
									InputElements.ToArray() );
		}

		protected static void	BuildVertexInputElements( Type _BufferType, List<InputElement> _InputElements, int _SlotIndex )
		{
			System.Reflection.FieldInfo[]	Fields = _BufferType.GetFields();

			int	Offset = 0;
			foreach ( System.Reflection.FieldInfo Field in Fields )
			{
				// First, check for a new buffer start
				VertexBufferStartAttribute[]	BufferStarts = Field.GetCustomAttributes( typeof(VertexBufferStartAttribute), false ) as VertexBufferStartAttribute[];
				if ( BufferStarts.Length > 0 )
				{
					if ( BufferStarts.Length > 1 )
						throw new Exception( "Field \"" + Field.Name + "\" has more than one VertexBufferStart attribute !" );

					// A new buffer start means analyzing the field as sub-vertex structure
					BuildVertexInputElements( Field.FieldType, _InputElements, BufferStarts[0].SlotIndex );
					continue;
				}

				// Retrieve semantic
				SemanticAttribute[]	Semantics = Field.GetCustomAttributes( typeof(SemanticAttribute), false ) as SemanticAttribute[];
				if ( Semantics.Length == 0 )
					throw new Exception( "Field \"" + Field.Name + "\" is missing its Semantic attribute !" );
				if ( Semantics.Length > 1 )
					throw new Exception( "Field \"" + Field.Name + "\" has more than one Semantic attribute !" );
				string				Semantic = Semantics[0].Semantic;
				int					Index = Semantics[0].Index;
				InputClassification	Classification = InputClassification.PerVertexData;
				int					StepRate = 0;

				InstanceSemanticAttribute	InstanceSemantic = Semantics[0] as InstanceSemanticAttribute;
				if ( InstanceSemantic != null )
				{	// This is an instance semantic
					Classification = InputClassification.PerInstanceData;
					StepRate = InstanceSemantic.StepRate;
				}

				// Build input elements for the current field
				Type	FieldType = Field.FieldType;
				if ( FieldType == typeof(Vector3) || FieldType == typeof(Color3) )
				{
					_InputElements.Add( new InputElement( Semantic, Index, Format.R32G32B32_Float, Offset, _SlotIndex, Classification, StepRate ) );
					Offset += 3*sizeof(float);
				}
				else if ( FieldType == typeof(Vector4) || FieldType == typeof(Color4) )
				{
					_InputElements.Add( new InputElement( Semantic, Index, Format.R32G32B32A32_Float, Offset, _SlotIndex, Classification, StepRate ) );
					Offset += 4*sizeof(float);
				}
				else if ( FieldType == typeof(Vector2) )
				{
					_InputElements.Add( new InputElement( Semantic, Index, Format.R32G32_Float, Offset, _SlotIndex, Classification, StepRate ) );
					Offset += 2*sizeof(float);
				}
				else if ( FieldType == typeof(float) )
				{
					_InputElements.Add( new InputElement( Semantic, Index, Format.R32_Float, Offset, _SlotIndex, Classification, StepRate ) );
					Offset += sizeof(float);
				}
				// Half types
// 				else if ( FieldType == typeof(Half3) )	<== NOT SUPPORTED !
// 				{
// 					_InputElements.Add( new InputElement( Semantic, Index, Format.R, Offset, _SlotIndex, Classification, StepRate ) );
// 					Offset += 3*sizeof(UInt16);
// 				}
				else if ( FieldType == typeof(Half4) )
				{
					_InputElements.Add( new InputElement( Semantic, Index, Format.R16G16B16A16_Float, Offset, _SlotIndex, Classification, StepRate ) );
					Offset += 4*sizeof(UInt16);
				}
				else if ( FieldType == typeof(Half2) )
				{
					_InputElements.Add( new InputElement( Semantic, Index, Format.R16G16_Float, Offset, _SlotIndex, Classification, StepRate ) );
					Offset += 2*sizeof(UInt16);
				}
				else if ( FieldType == typeof(Half) )
				{
					_InputElements.Add( new InputElement( Semantic, Index, Format.R16_Float, Offset, _SlotIndex, Classification, StepRate ) );
					Offset += sizeof(UInt16);
				}
				// Matrix types
				else if ( FieldType == typeof(Matrix) )
				{
					_InputElements.Add( new InputElement( Semantic, Index, Format.R32G32B32A32_Float, Offset, _SlotIndex, Classification, StepRate ) );
					Offset += 4*sizeof(float);
					_InputElements.Add( new InputElement( Semantic, Index+1, Format.R32G32B32A32_Float, Offset, _SlotIndex, Classification, StepRate ) );
					Offset += 4*sizeof(float);
					_InputElements.Add( new InputElement( Semantic, Index+2, Format.R32G32B32A32_Float, Offset, _SlotIndex, Classification, StepRate ) );
					Offset += 4*sizeof(float);
					_InputElements.Add( new InputElement( Semantic, Index+3, Format.R32G32B32A32_Float, Offset, _SlotIndex, Classification, StepRate ) );
					Offset += 4*sizeof(float);
				}
				// Integer types
				else if ( FieldType == typeof(Int32) )
				{
					_InputElements.Add( new InputElement( Semantic, Index, Format.R32_SInt, Offset, _SlotIndex, Classification, StepRate ) );
					Offset += sizeof(Int32);
				}
				else if ( FieldType == typeof(UInt32) )
				{
					_InputElements.Add( new InputElement( Semantic, Index, Format.R32_UInt, Offset, _SlotIndex, Classification, StepRate ) );
					Offset += sizeof(UInt32);
				}
				// Short integer types
				else if ( FieldType == typeof(Int16) )
				{
					_InputElements.Add( new InputElement( Semantic, Index, Format.R16_SInt, Offset, _SlotIndex, Classification, StepRate ) );
					Offset += sizeof(Int16);
				}
				else if ( FieldType == typeof(UInt16) )
				{
					_InputElements.Add( new InputElement( Semantic, Index, Format.R16_UInt, Offset, _SlotIndex, Classification, StepRate ) );
					Offset += sizeof(UInt16);
				}
				else
					throw new Exception( "Field \"" + Field.Name + "\" is of unsupported type \"" + FieldType.FullName + "\" !" );
			}
		}

		#endregion
	}
}
