// This is the main DLL file.

#include "stdafx.h"

#include "AnimationTrack.h"
#include "Nodes.h"
#include "ObjectProperty.h"

using namespace	FBXImporter;

AnimationTrack::AnimationTrack( AnimationTrack^ _ParentTrack, ObjectProperty^ _Owner, Node^ _ParentNode, KFCurveNode* _pCurveNode, EFbxType _PropertyType ) :
m_ParentTrack( _ParentTrack ), m_Owner( _Owner ), m_ParentNode( _ParentNode ), m_pCurveNode( _pCurveNode )
{
	// Get the track's name
 	m_Name = Helpers::GetString( m_pCurveNode->GetName() );

	// and its time span
	KTime	Start, Stop;
	m_pCurveNode->GetAnimationInterval( Start, Stop );
	m_TimeSpan = Helpers::GetTimeSpan( KTimeSpan( Start, Stop ) );


	//////////////////////////////////////////////////////////////////////////
	// Build the set of animation keys
	List<AnimationKey^>^	Keys = gcnew List<AnimationKey^>();

	KFCurve*	pCurve = m_pCurveNode->FCurveGet();
	if ( pCurve != NULL )
	{
		m_Defaultvalue = (float) pCurve->GetValue();

//		kFCurveInterpolation		InterpolatioType = pCurve->KeyGetInterpolation( KeyIndex );
//		kFCurveTangeantMode			TangentMode = pCurve->KeyGetTangeantMode( KeyIndex );
//		kFCurveTangeantWeightMode	TangentWeightMode = pCurve->KeyGetTangeantWeightMode( KeyIndex );
// 		kFCurveTangeantVelocityMode	TangentVelocityMode = pCurve->KeyGetTangeantVelocityMode( KeyIndex );

		AnimationKey^	Previous = nullptr;
		for ( int KeyIndex=0; KeyIndex < pCurve->KeyGetCount(); KeyIndex++ )
		{
			KFCurveKey&		SourceKey = pCurve->KeyGet( KeyIndex );

			AnimationKey^	K = gcnew AnimationKey();
							K->Previous = Previous;
			if ( Previous != nullptr )
				Previous->Next = K;

			Previous = K;

			Keys->Add( K );

			// Build the key
			K->Time = (float) SourceKey.GetTime().GetSecondDouble();

				// Retrieve interpolation type
			switch ( SourceKey.GetInterpolation() )
			{
			case KFCURVE_INTERPOLATION_CONSTANT:
				K->Type = AnimationKey::KEY_TYPE::CONSTANT;
				break;

			case KFCURVE_INTERPOLATION_LINEAR:
				K->Type = AnimationKey::KEY_TYPE::LINEAR;
				break;

			case KFCURVE_INTERPOLATION_CUBIC:
				K->Type = AnimationKey::KEY_TYPE::CUBIC;
				break;

			default:
				throw gcnew Exception( "Unsupported Interpolation Mode !" );
			}

			// Retrieve the key's value
			K->Value = float( SourceKey.GetValue() );

			if ( K->Type != AnimationKey::KEY_TYPE::CUBIC )
				continue;	// No other data needed

				// Retrieve cubic interpolation data
			kFCurveTangeantMode			TangentMode = SourceKey.GetTangeantMode();
			kFCurveTangeantWeightMode	TangentWeightMode = SourceKey.GetTangeantWeightMode();
			kFCurveTangeantVelocityMode	TangentVelocityMode = SourceKey.GetTangeantVelocityMode();

// 			KFCurveTangeantInfo			LeftInfo = pCurve->KeyGetLeftDerivativeInfo( KeyIndex );
// 			KFCurveTangeantInfo			RightInfo = pCurve->KeyGetRightDerivativeInfo( KeyIndex );
 
			switch ( TangentMode )
			{
			case KFCURVE_TANGEANT_AUTO:	// Cardinal spline
				K->CubicType = AnimationKey::CUBIC_INTERPOLATION_TYPE::CARDINAL;
				break;

			case KFCURVE_TANGEANT_TCB:	// TCB spline
				K->CubicType = AnimationKey::CUBIC_INTERPOLATION_TYPE::TCB;
				K->Tension = SourceKey.GetDataFloat( KFCURVEKEY_TCB_TENSION );
				K->Continuity = SourceKey.GetDataFloat( KFCURVEKEY_TCB_CONTINUITY );
				K->Bias = SourceKey.GetDataFloat( KFCURVEKEY_TCB_BIAS );
				break;

			case KFCURVE_TANGEANT_USER:	// Left slope = Right slope
			case KFCURVE_GENERIC_BREAK:	// Indexpendent left slope & right slopes
				K->CubicType = AnimationKey::CUBIC_INTERPOLATION_TYPE::CUSTOM;
				K->RightSlope = SourceKey.GetDataDouble( KFCURVEKEY_RIGHT_SLOPE );
				K->NextLeftSlope = SourceKey.GetDataDouble( KFCURVEKEY_NEXT_LEFT_SLOPE );
				K->RightWeight = SourceKey.GetDataDouble( KFCURVEKEY_RIGHT_WEIGHT );
				K->NextLeftWeight = SourceKey.GetDataDouble( KFCURVEKEY_NEXT_LEFT_WEIGHT );
				break;

			default:
				throw gcnew Exception( "Unsupported Tangent Mode !" );
			}
		}
	}

	m_Keys = Keys->ToArray();

	//////////////////////////////////////////////////////////////////////////
	// Recurse through children
	//
	List<AnimationTrack^>^	ChildTracks = gcnew List<AnimationTrack^>();
	for ( int ChildIndex=0; ChildIndex < m_pCurveNode->GetCount(); ChildIndex++ )
	{
		AnimationTrack^	ChildTrack = gcnew AnimationTrack( this, _Owner, _ParentNode, m_pCurveNode->Get( ChildIndex ), _PropertyType );
		ChildTracks->Add( ChildTrack );
	}

	m_ChildTracks = ChildTracks->ToArray();
}

AnimationTrack::AnimationTrack( AnimationTrack^ _Source )
{
	m_Owner = _Source->m_Owner;
	m_ParentNode = _Source->m_ParentNode;

	m_ParentTrack = _Source->m_ParentTrack;
	m_ChildTracks = gcnew cli::array<AnimationTrack^>( _Source->m_ChildTracks->Length );
	for ( int ChildTrackIndex=0; ChildTrackIndex < _Source->m_ChildTracks->Length; ChildTrackIndex++ )
		m_ChildTracks[ChildTrackIndex] = gcnew AnimationTrack( _Source->m_ChildTracks[ChildTrackIndex] );

	m_Name = _Source->m_Name;
	m_TimeSpan = _Source->m_TimeSpan;
	m_pCurveNode = _Source->m_pCurveNode;
	m_Defaultvalue = _Source->m_Defaultvalue;

	m_Keys = gcnew cli::array<AnimationKey^>( _Source->m_Keys->Length );
	for ( int KeyIndex=0; KeyIndex < m_Keys->Length; KeyIndex++ )
	{
		AnimationKey^	SK = _Source->m_Keys[KeyIndex];
		AnimationKey^	K = m_Keys[KeyIndex] = gcnew AnimationKey();

		K->Previous = KeyIndex > 0 ? m_Keys[KeyIndex-1] : nullptr;
		K->Next = nullptr;
		if ( KeyIndex > 0 )
			m_Keys[KeyIndex-1]->Next = K;
		K->Type = SK->Type;
		K->Time = SK->Time;
		K->Value = SK->Value;
		K->CubicType = SK->CubicType;
		K->RightSlope = SK->RightSlope;
		K->NextLeftSlope = SK->NextLeftSlope;
		K->RightWeight = SK->RightWeight;
		K->NextLeftWeight = SK->NextLeftWeight;
// 		K->RightVelocity = SK->RightVelocity;
// 		K->NextLeftVelocity = SK->NextLeftVelocity;
		K->Tension = SK->Tension;
		K->Continuity = SK->Continuity;
		K->Bias = SK->Bias;
	}
}

float	AnimationTrack::Evaluate( float _Time )
{
	KFCurve*	pCurve = m_pCurveNode->FCurveGet();

	KTime	T;
			T.SetSecondDouble( _Time );

	return	pCurve->Evaluate( T );
}
