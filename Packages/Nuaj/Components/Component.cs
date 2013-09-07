using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using SharpDX;
using SharpDX.Direct3D10;
using SharpDX.DXGI;
using SharpDX.D3DCompiler;

namespace Nuaj
{
	/// <summary>
	/// Base component class for all components in the library
	/// NOTE: All components must be disposed of when not used any more
	/// 
	/// When you inherit a component and create IDisposable objects, use the ToDispose()
	/// template to mark the object as "to be disposed of" using the following syntax :
	/// 
	///		var MyObject = ToDispose( CreateTheObject() );
	/// 
	/// MyObject will thus be stacked for disposal on the component's own disposal.
	/// </summary>
	public class Component : IDisposable
	{
		#region FIELDS

		protected Device				m_Device = null;
		protected string				m_Name = null;
		protected object				m_Tag = null;

		protected Stack<IDisposable>	m_Disposables = new Stack<IDisposable>();

		#endregion

		#region PROPERTIES

		[System.ComponentModel.Browsable( false )]
		public Device		Device	{ get { return m_Device; } }
		[System.ComponentModel.Browsable( false )]
		public string		Name	{ get { return m_Name; } }
		[System.ComponentModel.Browsable( false )]
		public object		Tag		{ get { return m_Tag; } set { m_Tag = value; } }

		public event EventHandler	Disposing;

		#endregion

		#region METHODS

		/// <summary>
		/// All components must have a valid device and a name
		/// </summary>
		/// <param name="_Device"></param>
		/// <param name="_Name"></param>
		public	Component( Device _Device, string _Name )
		{
			m_Device = _Device;
			if ( this is Device )
				m_Device = this as Device;	// Allow null device if we're the Device ourselves... Special singleton case.
			else if ( m_Device == null )
				throw new NException( this, "Invalid device !" );
			m_Name = _Name;
		}

		public override string ToString()
		{
			return m_Name;
		}

		/// <summary>
		/// Use this helper to stack objects that will need disposal on this component's own disposal
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="_Item"></param>
		/// <returns></returns>
		protected T	ToDispose<T>( T _Item ) where T : IDisposable
		{
			IDisposable	I = _Item as IDisposable;
			if ( I != null )
				m_Disposables.Push( I );

			return _Item;
		}

		/// <summary>
		/// Use this helper to stack objects that will need disposal on this component's own disposal
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="_Items"></param>
		protected void	ToDispose<T>( T[] _Items ) where T : IDisposable
		{
			foreach ( IDisposable Item in _Items )
				if ( Item != null )
					m_Disposables.Push( Item );
		}

		/// <summary>
		/// Removes an item from the stack of disposable items
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="_Item"></param>
		/// <remarks>This method must be used with care!</remarks>
		protected void	RemoveFromDisposeStack<T>( T _Item ) where T : IDisposable
		{
			// Get current stack as an array
			IDisposable[]	Disposables = m_Disposables.ToArray();

			// Then re-push all items in order except the one we don't want
			m_Disposables.Clear();
			for ( int DisposableIndex=Disposables.Length-1; DisposableIndex >= 0; DisposableIndex-- )
			{
				IDisposable D = Disposables[DisposableIndex];
				if ( (D as object) != (_Item as object) )
					m_Disposables.Push( D );
			}
		}

		#region IDisposable Members

		public virtual void	Dispose()
		{
			// Notify
			if ( Disposing != null )
				Disposing( this, EventArgs.Empty );

			// Dispose of our stuff...
			while( m_Disposables.Count > 0 )
				m_Disposables.Pop().Dispose();
		}

		#endregion

		#endregion
	}
}
