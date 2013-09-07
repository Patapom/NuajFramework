// Contains the Object Property class attached to each object
//
#pragma managed
#pragma once

#include "Helpers.h"
#include "AnimationTrack.h"

using namespace System;
using namespace System::Collections::Generic;
using namespace System::ComponentModel;


namespace FBXImporter
{
	ref class	BaseObject;
	ref class	Texture;

	//////////////////////////////////////////////////////////////////////////
	// Represents a property attached to an object
	//
	[System::Diagnostics::DebuggerDisplayAttribute( "Name={Name} Value={Value}" )]
	public ref class	ObjectProperty
	{
	protected:	// FIELDS

		BaseObject^		m_Owner;

		String^			m_Name;
		String^			m_InternalName;
		String^			m_TypeName;
		Object^			m_Value;

		cli::array<Texture^>^	m_Textures;

		AnimationTrack^	m_AnimTrack;


	public:		// PROPERTIES

		property String^	Name
		{
			String^		get()	{ return m_Name; }
		}

		property String^	InternalName
		{
			String^		get()	{ return m_InternalName; }
		}

		property String^	TypeName
		{
			String^		get()	{ return m_TypeName; }
		}

		property Object^	Value
		{
			Object^		get()	{ return m_Value; }
		}

		property cli::array<Texture^>^	Textures
		{
			cli::array<Texture^>^	get()	{ return m_Textures; }
		}

		property AnimationTrack^	AnimTrack
		{
			AnimationTrack^	get()	{ return m_AnimTrack; }
		}


	public:		// METHODS

		ObjectProperty( BaseObject^ _Owner, KFbxProperty& _Property );
	};
}
