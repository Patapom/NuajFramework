// This is the main DLL file.

#include "stdafx.h"
#include "Scene.h"

using namespace FBXImporter;

// Read the relevant scene data
//
void	Scene::ReadSceneData()
{
	// ======================================
	// 1] Read scene global settings
	KFbxGlobalSettings&	Settings = m_pScene->GetGlobalSettings();

	KFbxAxisSystem	AxisSystem = Settings.GetAxisSystem();
	int				Sign = 0;
	switch ( AxisSystem.GetUpVector( Sign ) )
	{
	case KFbxAxisSystem::XAxis:
		m_UpAxis = UP_AXIS::X;
		break;
	case KFbxAxisSystem::YAxis:
		m_UpAxis = UP_AXIS::Y;
		break;
	case KFbxAxisSystem::ZAxis:
		m_UpAxis = UP_AXIS::Z;
		break;
	}

	if ( m_UpAxis != UP_AXIS::Z )
		throw gcnew Exception( "Only Z-Up is supported for now !" );
 

	// ======================================
	// 2] Read back the nodes' hierarchy
	KFbxNode*	pRootNode = m_pScene->GetRootNode();

	m_Nodes->Clear();
	m_RootNode = CreateNodesHierarchy( nullptr, pRootNode );

	// ======================================


	// TODO:
	// _ Support markers as simple transforms
}

// Recursively creates the hierarchy of nodes
Node^	Scene::CreateNodesHierarchy( Node^ _Parent, KFbxNode* _pNode )
{
	Node^	Result = nullptr;

	// Determine what kind of node we're dealing with
	KFbxNodeAttribute::EAttributeType	AttributeType = KFbxNodeAttribute::eNULL;
	if ( _pNode->GetNodeAttribute() != NULL )
		AttributeType = _pNode->GetNodeAttribute()->GetAttributeType();

	switch ( AttributeType )
	{
	case KFbxNodeAttribute::eMESH:		// MESH NODE
		Result = gcnew NodeMesh( this, _Parent, _pNode );
		break;

	case KFbxNodeAttribute::eCAMERA:	// CAMERA NODE
		Result = gcnew NodeCamera( this, _Parent, _pNode );
		break;

	case KFbxNodeAttribute::eLIGHT:		// LIGHT NODE
		Result = gcnew NodeLight( this, _Parent, _pNode );
		break;

	case KFbxNodeAttribute::eSKELETON:	// SKELETON NODE
		Result = gcnew NodeSkeleton( this, _Parent, _pNode );
		break;

	default:							// ANONYMOUS NODE
		if ( _Parent == nullptr )
			Result = gcnew NodeRoot( this, _pNode );
		else
			Result = gcnew NodeGeneric( this, _Parent, _pNode );
	}

	if ( Result == nullptr )
		return	nullptr;

	// Add that node
	m_Nodes->Add( Result );

	// Create child nodes
	for ( int ChildIndex=0; ChildIndex < _pNode->GetChildCount(); ChildIndex++ )
	{
		Node^	Child = CreateNodesHierarchy( Result, _pNode->GetChild( ChildIndex ) );
		if ( Child != nullptr )
			Result->AddChild( Child );
	}

	return	Result;
}
