using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using SharpDX;
using SharpDX.Direct3D10;
using SharpDX.DXGI;
using Buffer = SharpDX.Direct3D10.Buffer;

namespace Nuaj
{
	/// <summary>
	/// The StreamOutput Buffer class encompasses a DirectX stream output buffer
	/// </summary>
	/// <typeparam name="VS">The vertex structure that should be used with this StreamOutput buffer</typeparam>
	public class StreamOutputBuffer<VS> : Component where VS:struct
	{
		#region FIELDS

		protected Buffer				m_Buffer = null;
		protected int					m_MaxVerticesCount = 0;

		protected StreamOutputBufferBinding	m_OutputBinding;
		protected VertexBufferBinding		m_InputBinding;

		// Vertex layout cache
		protected EffectTechnique		m_LastTechniqueForVertexLayout = null;
		protected InputLayout			m_VertexLayout = null;

		// Query
		protected Query					m_Query = null;
		protected UInt64				m_WrittenPrimitivesCount = 0;
		protected UInt64				m_StorageNeeded = 0;

		#endregion

		#region PROPERTIES

		/// <summary>
		/// Gets the StreamOutput buffer
		/// </summary>
		public Buffer			Buffer	{ get { return m_Buffer; } }

		/// <summary>
		/// Gets the maximum vertices in the buffer
		/// </summary>
		public int				MaxVerticesCount	{ get { return m_MaxVerticesCount; } }

		/// <summary>
		/// Gets the last written amount of primitives (you must have used the BeginQuery() and EndQuery() to retrieve actual results)
		/// </summary>
		/// <remarks>A value of 0 means an invalid generation</remarks>
		public UInt64			WrittenPrimitivesCount	{ get { return m_WrittenPrimitivesCount; } }

		/// <summary>
		/// Gets the amount of storage needed to store the written primitives (you must have used the BeginQuery() and EndQuery() to retrieve actual results)
		/// </summary>
		/// <remarks>A value of 0 means an invalid generation</remarks>
		public UInt64			StorageNeeded			{ get { return m_StorageNeeded; } }

		#endregion

		#region METHODS

		/// <summary>
		/// Creates an empty StreamOutput buffer
		/// </summary>
		/// <param name="_Device"></param>
		/// <param name="_Name"></param>
		/// <param name="_EffectSource">The source code for the shader</param>
		public	StreamOutputBuffer( Device _Device, string _Name ) : base( _Device, _Name )
		{
		}

		/// <summary>
		/// Creates a StreamOutput buffer from an array of vertices
		/// </summary>
		/// <param name="_Device"></param>
		/// <param name="_Name"></param>
		/// <param name="_MaxVerticesCount">The maximum amount of vertices in the buffer</param>
		public	StreamOutputBuffer( Device _Device, string _Name, int _MaxVerticesCount ) : base( _Device, _Name )
		{
			Init( _MaxVerticesCount );
		}

		/// <summary>
		/// Initializes the StreamOutput buffer with a maximum size
		/// </summary>
		/// <param name="_MaxVerticesCount">The maximum amount of vertices in the buffer</param>
		public void		Init( int _MaxVerticesCount )
		{
			m_MaxVerticesCount = _MaxVerticesCount;

			int	StructureSize = System.Runtime.InteropServices.Marshal.SizeOf( typeof(VS) );

			// Build the buffer from the stream
			m_Buffer = ToDispose( new Buffer( m_Device.DirectXDevice,
				new BufferDescription()
				{
					BindFlags = BindFlags.VertexBuffer | BindFlags.StreamOutput,
					CpuAccessFlags = CpuAccessFlags.None,
					OptionFlags = ResourceOptionFlags.None,
					SizeInBytes = m_MaxVerticesCount * StructureSize,
					Usage = ResourceUsage.Default
				} ) );

			// Build the bindings
			m_OutputBinding = new StreamOutputBufferBinding( m_Buffer, 0 );
			m_InputBinding = new VertexBufferBinding( m_Buffer, StructureSize, 0 );
		}

		/// <summary>
		/// Uses the StreamOutput buffer for the Stream Output stage
		/// </summary>
		public void	UseAsOutput()
		{
			m_Device.StreamOutput.SetTargets( m_OutputBinding );
		}

		/// <summary>
		/// Uses the StreamOutput buffer as input for the input assembler
		/// </summary>
		/// <param name="_ValidationTechnique">The effect technique the vertex layout must be validated against</param>
		public void	UseAsInput( EffectTechnique _ValidationTechnique )
		{
			if ( _ValidationTechnique != m_LastTechniqueForVertexLayout )
			{	// Rebuild vertex layout...
				m_VertexLayout = ToDispose( Helpers.VertexLayoutBuilder.BuildVertexLayout<VS>( _ValidationTechnique ) );
				m_LastTechniqueForVertexLayout = _ValidationTechnique;
			}

			UseAsInput( m_VertexLayout );
		}

		/// <summary>
		/// Uses the StreamOutput buffer as input for the input assembler
		/// </summary>
		/// <param name="_VertexLayout">The vertex layout to use with this stream (NOTE: must obviously be compatible, like the one from the material)</param>
		public void	UseAsInput( InputLayout _VertexLayout )
		{
			m_Device.InputAssembler.InputLayout = _VertexLayout;
			UseAsInput();
		}

		/// <summary>
		/// Uses the StreamOutput buffer as input for the input assembler
		/// </summary>
		/// <remarks>Warning: this method is unchecked and assumes you setup the correct vertex layout prior calling it</remarks>
		public void	UseAsInput()
		{
			m_Device.InputAssembler.SetVertexBuffers( 0, m_InputBinding );
		}

		/// <summary>
		/// Draws the content of the stream output
		/// </summary>
		public void	Draw()
		{
			m_Device.DrawAuto();
		}

		/// <summary>
		/// You MUST call this to remove the stream output after it's been used as output,
		/// otherwise you will continue to render everything else to the stream
		/// </summary>
		public void UnUse()
		{
			m_Device.StreamOutput.SetTargets( null );
		}

		/// <summary>
		/// Begins a statistics query
		/// </summary>
		public void	BeginQuery()
		{
			if ( m_Query == null )
			{	// Create the query for the first time
				m_Query = ToDispose( new SharpDX.Direct3D10.Query( m_Device.DirectXDevice, new QueryDescription()
					{
						Type = QueryType.StreamOutputStatistics,
						Flags = QueryFlags.None
					} ) );
			}
			m_Query.Begin();
		}

		/// <summary>
		/// Ends the statistics query
		/// Results are available through the WrittenPrimitivesCount && StorageNeeded
		/// </summary>
		/// <param name="_bSynchronous">Tells if we should wait for results</param>
		public void	EndQuery( bool _bSynchronous )
		{
			m_Query.End();

			m_WrittenPrimitivesCount = m_StorageNeeded = 0;

			// Wait for data as posts are asynchronous
			TimeSpan	WaitTime;
			if ( _bSynchronous )
			{
				DateTime	StartWait = DateTime.Now;
				while ( !m_Query.IsDataAvailable )
				{
					System.Threading.Thread.Sleep( 10 );

					WaitTime = DateTime.Now - StartWait;
					if ( WaitTime.TotalSeconds > 4 )
						break;	// Time out...
				}
			}

			if ( !m_Query.IsDataAvailable )
				return;	// Not available...

			// Read back our statistics
			using ( DataStream QueryData = m_Query.GetData() )
			{
				m_WrittenPrimitivesCount = QueryData.Read<UInt64>();
				m_StorageNeeded = QueryData.Read<UInt64>();
			}
		}

/* Some code to use one day to read back the generated content...
{
	BufferDescription	Desc = new BufferDescription();
	Desc.BindFlags = BindFlags.None;
	Desc.CpuAccessFlags = CpuAccessFlags.Read;
	Desc.OptionFlags = ResourceOptionFlags.None;
	Desc.Usage = ResourceUsage.Staging;
	Desc.SizeInBytes = 3*5*64*64*64 * 24;
	SharpDX.Direct3D10.Buffer	B = new SharpDX.Direct3D10.Buffer( m_Device.DirectXDevice, Desc );

	m_Device.DirectXDevice.CopyResource( Current.Geometry.Buffer, B );

	DataStream	S = B.Map( MapMode.Read, MapFlags.None );

	for ( int i=0; i < 1000; i++ )
	{
		Vector3	TempPos = S.Read<Vector3>();
		Vector3	TempNormal = S.Read<Vector3>();
	}

	S.Dispose();
	B.Dispose();
}
*/
		#endregion
	}
}
