// This is the main DLL file.

#include "stdafx.h"

#include "ObjectProperty.h"
#include "BaseObject.h"
#include "Textures.h"
#include "Scene.h"

#include "Nodes.h"

using namespace	FBXImporter;

ObjectProperty::ObjectProperty( BaseObject^ _Owner, KFbxProperty& _Property ) : m_Owner( _Owner )
{
	//////////////////////////////////////////////////////////////////////////
	// Store static data
	m_Name = Helpers::GetString( _Property.GetLabel() );
	m_InternalName = Helpers::GetString( _Property.GetName() );
	m_TypeName = Helpers::GetString( _Property.GetPropertyDataType().GetName() );

	//////////////////////////////////////////////////////////////////////////
	// Retrieve the property value
	m_Value = nullptr;
	switch ( _Property.GetPropertyDataType().GetType() )
	{
	case	eBOOL1:
		m_Value = KFbxGet<bool>( _Property );
		break;

	case	eDOUBLE1:
	case	eFLOAT1:
		m_Value = KFbxGet<double>( _Property );
		break;

	case	eINTEGER1:
		m_Value = KFbxGet<int>( _Property );
		break;

	case	eDOUBLE3:
		m_Value = Helpers::ToVector( KFbxGet<fbxDouble3>( _Property ) );
		break;

	case	eDOUBLE4:
		m_Value = Helpers::ToVector( KFbxGet<fbxDouble4>( _Property ) );
		break;

	case	eSTRING:
		m_Value = Helpers::GetString( KFbxGet<KString>( _Property ) );
		break;

	case	eENUM:
		m_Value = KFbxGet<int>( _Property );
		break;

	case	eREFERENCE:
		{
			fbxReference*	pReference = NULL;
//			_Property.Get( &pReference, eREFERENCE );
			break;
		}

//	default:
//		throw gcnew Exception( "Property type \"" + TypeName + "\" is unsupported! Can't get a value..." );
	}

	//////////////////////////////////////////////////////////////////////////
	// Retrieve textures
	if ( _Property.GetSrcObjectCount( KFbxLayeredTexture::ClassId ) > 0 )
		throw gcnew Exception( "Found unsupported layer textures on property \"" + _Owner->Name + "." + Name + "\" !\r\nOnly single textures are supported in this version." );

	List<Texture^>^	Textures = gcnew List<Texture^>();
	for ( int TextureIndex=0; TextureIndex < _Property.GetSrcObjectCount( KFbxTexture::ClassId ); TextureIndex++ )
	{
		KFbxTexture*	pTexture = KFbxCast<KFbxTexture>( _Property.GetSrcObject( KFbxTexture::ClassId, TextureIndex ) );
		Textures->Add( gcnew Texture( _Owner->ParentScene, pTexture ) );
	}

	m_Textures = Textures->ToArray();

	//////////////////////////////////////////////////////////////////////////
	// Retrieve animations
	m_AnimTrack = nullptr;

	Node^	OwnerNode = dynamic_cast<Node^>( m_Owner );
	if ( OwnerNode == nullptr || OwnerNode->GetCurrentTake() == nullptr )
		return;	// Anim tracks are only supported on node objects

	// TODO !
// 	const char*		pCurrentTakeName = Helpers::FromString( OwnerNode->GetCurrentTake()->Name );
// 	KFCurveNode*	pCurveNode = _Property.GetKFCurveNode( false, pCurrentTakeName );
// 	if ( pCurveNode != NULL )
// 		m_AnimTrack = gcnew AnimationTrack( nullptr, this, OwnerNode, pCurveNode, _Property.GetPropertyDataType().GetType() );	// Create the single anim track
}
