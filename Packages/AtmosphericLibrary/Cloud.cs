using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.IO;

using WMath;

namespace Atmospheric
{
	public class	Cloud
	{
		#region NESTED TYPES

		[System.Diagnostics.DebuggerDisplay( "Position={m_Position} Radius={m_Radius} Order={m_Order} Visible={m_bVisible}" )]
		public class	CloudElement
		{
			#region FIELDS

			protected int			m_Order = 0;
			protected bool			m_bVisible = true;
			protected Point			m_Position = null;
			protected float			m_Radius = 0.0f;

			#endregion

			#region PROPERTIES

			public int			Order		{ get { return m_Order; } }
			public bool			Visible		{ get { return m_bVisible; } }
			public Point		Position	{ get { return m_Position; } }
			public float		Radius		{ get { return m_Radius; } }

			#endregion

			#region METHODS

			public	CloudElement( int _Order, Point _Position, float _fRadius, bool _bVisible )
			{
				m_Order = _Order;
				m_Position = _Position;
				m_Radius = _fRadius;
				m_bVisible = _bVisible;
			}

			public static CloudElement	FromXml( XmlElement _Element )
			{
				return new CloudElement(	int.Parse( _Element.GetAttribute( "Order" ) ),
											new Point( float.Parse( _Element.GetAttribute( "x" ) ), float.Parse( _Element.GetAttribute( "y" ) ), float.Parse( _Element.GetAttribute( "z" ) ) ),
											float.Parse( _Element.GetAttribute( "Radius" ) ), 
											bool.Parse( _Element.GetAttribute( "Visible" ) )
										);
			}

			#endregion
		};

		#endregion

		#region FIELDS

		protected List<CloudElement>	m_CloudElements = new List<CloudElement>();

		#endregion

		#region PROPERTIES

		public int					ElementsCount
		{
			get { return m_CloudElements.Count; }
		}

		public CloudElement[]		Elements
		{
			get { return m_CloudElements.ToArray(); }
		}

		#endregion

		#region METHODS

		public		Cloud()
		{
		}

		public void		FromXml( string _FileName )
		{
			StreamReader	Reader = new StreamReader( _FileName );
			FromXml( Reader );
			Reader.Close();
		}

		public void		FromXml( TextReader _Reader )
		{
			XmlDocument	Doc = new XmlDocument();
						Doc.Load( _Reader );

			XmlElement	RootElement = Doc["CloudBuilder"]["Stage"]["Spheres"];

			BuildElements( RootElement );
		}

		protected void	BuildElements( XmlElement _ParentElement )
		{
			foreach ( XmlElement Child in _ParentElement.ChildNodes )
				if ( Child.Name == "Sphere" )
				{
					// Build a new cloud element
					CloudElement	Element = CloudElement.FromXml( Child );
					m_CloudElements.Add( Element );

					// Recurse through children
					BuildElements( Child );
				}
		}

		#endregion
	}
}
