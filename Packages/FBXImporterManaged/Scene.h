// FBXImporter.h
#pragma managed
#pragma once

using namespace System;
using namespace System::Collections::Generic;

#include "Helpers.h"
#include "Nodes.h"
#include "NodeMesh.h"
#include "NodeSkeleton.h"
#include "Layers.h"
#include "Materials.h"
#include "HardwareMaterials.h"

namespace FBXImporter
{
	//////////////////////////////////////////////////////////////////////////
	// Contains informations about the available FBX "takes"
	// In FBX, a scene can have one or more "takes". A take is a container for animation data.
	// You can access a file's take information without the overhead of loading the entire file into the scene.
	//
	public ref class		Take
	{
	protected:	// FIELDS

		int					m_Index;
		String^				m_Name;
		String^				m_Description;
		String^				m_ImportName;
		FBXTimeSpan^		m_LocalTimeSpan;
		FBXTimeSpan^		m_ReferenceTimeSpan;


	public:		// PROPERTIES

		property String^			Name
		{
			String^		get()		{ return m_Name; }
		}

		// Gets the take's duration in seconds
		property float				Duration
		{
			float		get()
			{
				float	D0 = (float) (m_ReferenceTimeSpan->Stop.TotalSeconds - m_ReferenceTimeSpan->Start.TotalSeconds);
				float	D1 = (float) (m_LocalTimeSpan->Stop.TotalSeconds - m_LocalTimeSpan->Start.TotalSeconds);
				return	D0;
			}
		}

	public:		// METHODS

		Take( int _TakeIndex, KFbxTakeInfo* _pTakeInfo )
		{
			m_Index = _TakeIndex;
			m_Name = Helpers::GetString( _pTakeInfo->mName );
			m_Description = Helpers::GetString( _pTakeInfo->mDescription );
			m_ImportName = Helpers::GetString( _pTakeInfo->mImportName );
			m_LocalTimeSpan = Helpers::GetTimeSpan( _pTakeInfo->mLocalTimeSpan );
			m_ReferenceTimeSpan = Helpers::GetTimeSpan( _pTakeInfo->mReferenceTimeSpan );
		}
	};

	public ref class	Scene
	{
	public:		// NESTED TYPES

		enum class	UP_AXIS
		{
			X,
			Y,
			Z
		};

	protected:	// FIELDS

		KFbxSdkManager*		m_pSDKManager;			// SdkManager pointer
		KFbxScene*			m_pScene;				// Scene pointer
		KFbxIOSettings*		m_pIOSettings;

		// Scene infos
		Take^				m_CurrentTake;
		List<Take^>^		m_Takes;

		UP_AXIS				m_UpAxis;

		// Materials list
		List<Material^>^	m_Materials;
		Dictionary<String^,Material^>^	m_Name2Material;

		// Nodes hierarchy
		List<Node^>^		m_Nodes;
		Node^				m_RootNode;


	public:		// PROPERTIES

		property cli::array<Take^>^			Takes
		{
			cli::array<Take^>^		get()	{ return m_Takes->ToArray(); }
		}

		property Take^						CurrentTake
		{
			Take^					get()	{ return m_CurrentTake; }
		}

		property cli::array<Material^>^		Materials
		{
			cli::array<Material^>^	get()	{ return m_Materials->ToArray(); }
		}

		property cli::array<Node^>^			Nodes
		{
			cli::array<Node^>^		get()	{ return m_Nodes->ToArray(); }
		}

		property Node^						RootNode
		{
			Node^					get()	{ return m_RootNode; }
		}

		property UP_AXIS					UpAxis
		{
			UP_AXIS					get()	{ return m_UpAxis; }
		}


	public:		// METHODS

		Scene()
		{
			// The first thing to do is to create the FBX SDK manager which is the object allocator for almost all the classes in the SDK.
			m_pSDKManager = KFbxSdkManager::Create();
			if ( !m_pSDKManager )
				throw gcnew Exception( "Unable to create the FBX SDK manager!" );


			// Create an IOSettings object
			m_pIOSettings = KFbxIOSettings::Create( m_pSDKManager, IOSROOT );
			m_pSDKManager->SetIOSettings( m_pIOSettings );

			// Load plugins from the executable directory
			KString lPath = KFbxGetApplicationDirectory();
			KString lExtension = "dll";
			m_pSDKManager->LoadPluginsDirectory( lPath.Buffer(), lExtension.Buffer() );


			// Initialize lists
			m_Takes = gcnew List<Take^>();
			m_Materials = gcnew List<Material^>();
			m_Nodes = gcnew List<Node^>();
			m_Name2Material = gcnew Dictionary<String^,Material^>();
		}

		~Scene()
		{
			// Destroy any existing scene
			if ( m_pScene != nullptr )
				m_pScene->Destroy( true, true );

			// Delete the FBX SDK manager. All the objects that have been allocated 
			// using the FBX SDK manager and that haven't been explicitly destroyed 
			// are automatically destroyed at the same time.
			if ( m_pSDKManager )
				m_pSDKManager->Destroy();
			m_pSDKManager = NULL;
		}

		//////////////////////////////////////////////////////////////////////////
		// Loads a scene from disk
		//
		void		Load( System::String^ _FileName )
		{
			// Clear lists & pointers
			m_CurrentTake = nullptr;
			m_Takes->Clear();

			m_RootNode = nullptr;
			m_Nodes->Clear();

			m_Materials->Clear();
			m_Name2Material->Clear();

			// Get the file version number generate by the FBX SDK.
			int lSDKMajor,  lSDKMinor,  lSDKRevision;
			KFbxSdkManager::GetFileFormatVersion( lSDKMajor, lSDKMinor, lSDKRevision );

			// Create an importer.
			KFbxImporter* pImporter = KFbxImporter::Create( m_pSDKManager,"" );
			
			try
			{
				// Destroy any existing scene
				if ( m_pScene != nullptr )
					m_pScene->Destroy( true, true );

				// Create the entity that will hold the scene.
				m_pScene = KFbxScene::Create( m_pSDKManager, "" );

				// Initialize the importer by providing a filename.
				const char*	pFileName = Helpers::FromString( _FileName );
				const bool	bImportStatus = pImporter->Initialize( pFileName, -1, m_pIOSettings );

				int lFileMajor, lFileMinor, lFileRevision;
				pImporter->GetFileVersion( lFileMajor, lFileMinor, lFileRevision );

				if ( !bImportStatus )
				{
					System::String^	Report = "Call to KFbxImporter::Initialize() failed.\n" +
											 "Error returned: " + Helpers::GetString( pImporter->GetLastErrorString() ) + "\n\n";

					if ( pImporter->GetLastErrorID() == KFbxIO::eFILE_VERSION_NOT_SUPPORTED_YET ||
						 pImporter->GetLastErrorID() == KFbxIO::eFILE_VERSION_NOT_SUPPORTED_ANYMORE )
					{
						Report += "FBX version number for this FBX SDK is " + lSDKMajor + "." + lSDKMinor + "." + lSDKRevision + "\n";
						Report += "FBX version number for file \"" + _FileName + "\" is " + lFileMajor + "." + lFileMinor + "." + lFileRevision + "\n\n";
					}

					throw gcnew Exception( Report );
				}

				if ( pImporter->IsFBX() )
				{	// Build animation takes
					int				AnimTakesCount = pImporter->GetAnimStackCount();
					System::String^	pCurrentTakeName = Helpers::GetString( pImporter->GetActiveAnimStackName().Buffer() );
					for ( int AnimTakeIndex=0; AnimTakeIndex < AnimTakesCount; AnimTakeIndex++ )
					{
						KFbxTakeInfo*	pTakeInfo = pImporter->GetTakeInfo( AnimTakeIndex );
						Take^			NewTake = gcnew Take( AnimTakeIndex, pTakeInfo );
						m_Takes->Add( NewTake );

						// Is this current take ??
						if ( NewTake->Name == pCurrentTakeName )
							m_CurrentTake = NewTake;	// This is our current take...
					}

					// Set the import states. By default, the import states are always set to true. The code below shows how to change these states.
					m_pIOSettings->SetBoolProp(IMP_FBX_MATERIAL,        true);
					m_pIOSettings->SetBoolProp(IMP_FBX_TEXTURE,         true);
					m_pIOSettings->SetBoolProp(IMP_FBX_LINK,            true);
					m_pIOSettings->SetBoolProp(IMP_FBX_SHAPE,           true);
					m_pIOSettings->SetBoolProp(IMP_FBX_GOBO,            true);
					m_pIOSettings->SetBoolProp(IMP_FBX_ANIMATION,       true);
					m_pIOSettings->SetBoolProp(IMP_FBX_GLOBAL_SETTINGS, true);
				}

				// Import the scene.
				bool	bStatus = pImporter->Import( m_pScene );
				if ( !bStatus )
					throw gcnew Exception( "Failed to import \"" + _FileName + "\" ! Last Error : " + Helpers::GetString( pImporter->GetLastErrorString() ) );
			}
			catch ( Exception^ )
			{
				m_pScene->Destroy( true, true );
				throw;
			}
			finally
			{
				// Destroy the importer.
				pImporter->Destroy();
			}

			try
			{
				ReadSceneData();
			}
			catch ( Exception^ _e )
			{
				m_pScene->Destroy( true, true );
				throw gcnew Exception( "An error occurred while importing scene data !", _e );
			}
		}

		// Finds a node by name
		//	_bThrowOnMultipleNodes, will throw an exception if multiple nodes are found with the same name
		//
		Node^			FindNode( String^ _NodeName )
		{
			return	FindNode( _NodeName, true );
		}
		Node^			FindNode( String^ _NodeName, bool _bThrowOnMultipleNodes )
		{
			if ( _NodeName == nullptr )
				return	nullptr;

			Node^	Result = nullptr;
			for ( int NodeIndex=0; NodeIndex < m_Nodes->Count; NodeIndex++ )
				if ( m_Nodes[NodeIndex]->Name == _NodeName )
				{
					if ( !_bThrowOnMultipleNodes )
						return	m_Nodes[NodeIndex];	// No use to look any further if we don't check duplicate names !

					if ( Result != nullptr )
						throw gcnew Exception( "There are more than one object with the name \"" + _NodeName + "\" !" );

					Result = m_Nodes[NodeIndex];
				}

			return	Result;
		}

	protected:

		void	ReadSceneData();
		Node^	CreateNodesHierarchy( Node^ _Parent, KFbxNode* _pNode );

	internal:

		// Resolves a FBX material into one of our materials
		//
		Material^		ResolveMaterial( KFbxSurfaceMaterial* _pMaterial )
		{
			if ( _pMaterial == NULL )
				return	nullptr;

			String^	MaterialName = Helpers::GetString( _pMaterial->GetName() );
			if ( m_Name2Material->ContainsKey( MaterialName ) )
				return	m_Name2Material[MaterialName];

			//////////////////////////////////////////////////////////////////////////
			// Build a brand new material
			Material^	NewMaterial = nullptr;

			// Check for hardward shader materials
            const KFbxImplementation*	pImplementation = GetImplementation( _pMaterial, ImplementationHLSL );
            if ( pImplementation != NULL )
				NewMaterial = gcnew MaterialHLSL( this, _pMaterial, pImplementation );
			else
			{
				pImplementation = GetImplementation( _pMaterial, ImplementationCGFX );
				if ( pImplementation != NULL )
					NewMaterial = gcnew MaterialCGFX( this, _pMaterial, pImplementation );
				else
				{	// Standard materials
					kFbxClassId	ClassID = _pMaterial->GetClassId();
					if ( ClassID.Is( KFbxSurfaceLambert::ClassId ) )
						NewMaterial = gcnew MaterialLambert( this, dynamic_cast<KFbxSurfaceLambert*>( _pMaterial ) );
					else if ( ClassID.Is( KFbxSurfacePhong::ClassId ) )
						NewMaterial = gcnew MaterialPhong( this, dynamic_cast<KFbxSurfacePhong*>( _pMaterial ) );
					else
						NewMaterial = gcnew Material( this, dynamic_cast<KFbxSurfaceMaterial*>( _pMaterial ) );
// 					else
// 						throw gcnew Exception( "Unsupported material class ID: " + Helpers::GetString( _pMaterial->GetClassId().GetName() ) + " for material \"" + Helpers::GetString( _pMaterial->GetName() ) + "\" !" );
				}
            }

			// Register it for later
			m_Materials->Add( NewMaterial );
			m_Name2Material->Add( MaterialName, NewMaterial );

			return	NewMaterial;
		}
	};
}
