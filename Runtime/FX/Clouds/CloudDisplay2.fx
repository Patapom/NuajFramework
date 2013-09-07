// This shader displays clouds and sky
//
#include "../Camera.fx"
#include "../Samplers.fx"
#include "../ReadableZBufferSupport.fx"
#include "CloudSupport.fx"

static const int	STEPS_COUNT_CLOUDS = 96;
static const int	STEPS_COUNT_AIR = 8;
static const int	STEPS_COUNT_AIR_GOD_RAYS = 64;

static const float	PI = 3.14159265358979;

float2		BufferInvSize;

float		CloudTime;

float		NoiseOffset;
float		NoiseSize;
float		NoiseFrequencyFactor;
float		CloudSpeed;
float		CloudEvolutionSpeed;

float		DensityCloud;

float3		SunColor;

// Phase function is a mix of 4 henyey-greenstein lobes : strong forward, forward, backward and medium side
float		ScatteringAnisotropyStrongForward = 0.95;
float		PhaseWeightStrongForward = 0.08;
float		ScatteringAnisotropyForward = 0.8;
float		PhaseWeightForward = 0.2;
float		ScatteringAnisotropyBackward = -0.7;
float		PhaseWeightBackward = 0.1;
float		ScatteringAnisotropySide = -0.4;
float		PhaseWeightSide = 0.8;
float		PhaseWeightSide2 = 0.2;

float		DirectionalFactor = 1.0;					// Factor to apply to directional energy
float		IsotropicFactor = 1.0;						// Factor to apply to isotropic energy
float		AerosolsFactor = 1.0;						// Factor to apply to energy scattered by aerosols

// Deep shadow map
float3		ShadowVector;		// A vector in the direction of the shadow, pointing from the Sun to the ground, whose size is 1/9 the depth encoded by the deep shadow map


struct VS_IN
{
	float4	Position		: SV_POSITION;
};

struct PS_OUT
{
	float3	InScattering	: SV_TARGET0;
	float4	ExtinctionZ		: SV_TARGET1;
};


VS_IN	VS( VS_IN _In ) { return _In; }

// ===================================================================================
// Compute the intersection with ground and cloud planes
struct	Hit
{
	bool	bTraceAirBefore;		// True to trace air -> cloud-in
	bool	bTraceAirBeforeBelow;	// True if camera is below clouds
	bool	bTraceAirAfter;			// True to trace cloud-out -> air
	bool	bTraceAirAfterBelow;	// True if camera is above clouds
	bool	bTraceCloud;			// True to trace cloud-in -> cloud-out
	float	HitDistanceCloudIn;
	float	HitDistanceCloudOut;
	float	HitDistanceAirOut;
};

// Computes the intersection of the camera ray with the sphere of given altitude
//	_Altitude, altitude of the sphere in kilometers (relative to sea level)
//
float	ComputeSphereIntersection( float3 _CameraPosition, float3 _CameraView, float _Altitude )
{
	float	Radius = EARTH_RADIUS + _Altitude;
	float	Height = EARTH_RADIUS + WorldUnit2Kilometer * _CameraPosition.y;

	float	b = Height*_CameraView.y;
	float	c = Height*Height - Radius*Radius;
	float	Delta = b*b-c;
	if ( Delta < 0.0 )
		return -1.0;

	return (Height > Radius ? -b - sqrt(Delta) : -b + sqrt(Delta)) / WorldUnit2Kilometer;
}

// Computes the intersections of the camera ray with the cloud planes
//
Hit		ComputeLayerIntersection( float3 _CameraPosition, float3 _CameraView, float _HitGround )
{
	// Compute distances to bottom & top cloud planes
	float	HitCloudTop = (CloudPlaneHeightTop - _CameraPosition.y) / _CameraView.y;
	float	HitCloudBottom = (CloudPlaneHeightBottom - _CameraPosition.y) / _CameraView.y;

	float	HitCloudIn = min( HitCloudTop, HitCloudBottom );
	float	HitCloudOut = max( HitCloudTop, HitCloudBottom );

	// Light path is decomposed into 3 parts : Air => Cloud => Air
	Hit	Result;
	Result.bTraceAirBefore = HitCloudIn > 0.0;
	Result.bTraceAirBeforeBelow = _CameraPosition.y <= CloudPlaneHeightBottom;
	Result.HitDistanceCloudIn = max( 0.0, min( _HitGround, HitCloudIn ) );
	Result.bTraceCloud = HitCloudOut > 0.0 && _HitGround > HitCloudIn;
	Result.HitDistanceCloudOut = max( 0.0, min( _HitGround, HitCloudOut ) );
	Result.bTraceAirAfter = HitCloudOut < _HitGround;
	Result.bTraceAirAfterBelow = _CameraView.y < 0.0;
	Result.HitDistanceAirOut = _HitGround;

	return Result;
}

// ===================================================================================
// Converts a WORLD position into a 3D volume cloud position
float3	World2Volume( float3 _WorldPosition )
{
	float	DeltaHeight = CloudPlaneHeightTop - CloudPlaneHeightBottom;

	// Offset so cloud top is 0
	_WorldPosition.y = min( DeltaHeight, CloudPlaneHeightTop - _WorldPosition.y );

	// Scale into [0,1]
	_WorldPosition *= NoiseSize / DeltaHeight;

	// Scroll...
	_WorldPosition.x += CloudSpeed * CloudTime;

	return _WorldPosition;
}

// ===================================================================================
// Noise sampling

// Static version (1 tap) with 4 precomputed octaves
float	GetNoise2( float3 _WorldPosition, float _MipLevel )
{
	return LargeNoiseTexture.SampleLevel( VolumeSampler, World2Volume( _WorldPosition ), _MipLevel ).x;
}

// Dynamic version (4 octaves)
float	GetNoise( float3 _WorldPosition, float _MipLevel )
{
	float3	UVW = World2Volume( _WorldPosition );

	float	Value  = NoiseTexture0.SampleLevel( VolumeSampler, UVW, _MipLevel ).x;
	UVW *= NoiseFrequencyFactor;
	UVW.x += CloudEvolutionSpeed * CloudTime;

	Value += 0.5   * NoiseTexture1.SampleLevel( VolumeSampler, UVW, _MipLevel ).x;
	UVW *= NoiseFrequencyFactor;
	UVW.x += CloudEvolutionSpeed * CloudTime;

	Value += 0.25  * NoiseTexture2.SampleLevel( VolumeSampler, UVW, _MipLevel ).x;
	UVW *= NoiseFrequencyFactor;
	UVW.x += CloudEvolutionSpeed * CloudTime;

	Value += 0.125 * NoiseTexture3.SampleLevel( VolumeSampler, UVW, _MipLevel ).x;

	return Value;
}

// "Analytic Perlin" noise
// Stolen from http://www.pouet.net/topic.php?which=7931
float	AnalyticPerlin( float3 _Position )
{
	float3	Ip = floor( _Position );
	float4	a = dot( Ip, float3( 1.0, 57.0, 21.0 ) ) + float4( 0.0, 57.0, 21.0, 78.0 );
	float3	f = 0.5 + 0.5 * cos( (_Position-Ip) * PI );

	a = lerp( sin( cos(a) * a ), sin( cos(1.0 + a)*(1.0+a) ), f.x );
	a.xy = lerp( a.xz, a.yw, f.y );

	return lerp( a.x, a.y, f.z );
}

// Dynamic analytic version (4 octaves)
float	GetNoise1( float3 _WorldPosition, float _MipLevel )
{
	float3	UVW = 10.0 * World2Volume( _WorldPosition );

	float	Value  = AnalyticPerlin( UVW );
	UVW *= 2.0;
	UVW.x += CloudEvolutionSpeed * CloudTime;

	Value += 0.5   * AnalyticPerlin( UVW );
	UVW *= 2.0;
	UVW.x += CloudEvolutionSpeed * CloudTime;

	Value += 0.25  * AnalyticPerlin( UVW );
	UVW *= 2.0;
	UVW.x += CloudEvolutionSpeed * CloudTime;

	Value += 0.125 * AnalyticPerlin( UVW );

	return Value;
}

/*	// That one is working pretty fast but it looks a bit "blocky"

// Array Perlin noise
// Noise is fetched from an array of 256 float4 [0,1] random values
float4	NoiseTable[257] = { float4( 0.248668584157093, 0.110743977181029, 0.467010679872246, 0.771604122021982 ), float4( 0.657518893786482, 0.432782601300991, 0.354083763600366, 0.943862276125635 ), float4( 0.101266453555444, 0.642455555332105, 0.0286368127114311, 0.248029145527645 ), float4( 0.320110131669841, 0.989767147689018, 0.682123118863498, 0.654887887022871 ), float4( 0.282729223967869, 0.615346408269995, 0.704251535564778, 0.701796675427722 ), float4( 0.94977844690428, 0.093388480643457, 0.160907185711389, 0.38197107770572 ), float4( 0.797947697247354, 0.169467153106568, 0.793783622697826, 0.307228242190195 ), float4( 0.823028430260265, 0.886469540599021, 0.556302315814561, 0.716734007334678 ), float4( 0.699295832169846, 0.0136144026245989, 0.991876096460911, 0.806301604866191 ), float4( 0.858743599084552, 0.0625033248506968, 0.514393753611666, 0.525258244259869 ), float4( 0.272825158328202, 0.993796349965872, 0.6911044277675, 0.386615484201636 ), float4( 0.434333927665993, 0.675405530107862, 0.677381673677537, 0.47287468261685 ), float4( 0.244518749529737, 0.353484110605663, 0.526252646244249, 0.16942728691242 ), float4( 0.346587812223745, 0.691694964511178, 0.395143099778864, 0.155280103513636 ), float4( 0.94983679146964, 0.0850396021665258, 0.973656424774628, 0.488051740679914 ), float4( 0.638998978603165, 0.0468555214101707, 0.12083384586537, 0.214796912956423 ), float4( 0.086153239517544, 0.311902805376753, 0.548733313357799, 0.306495729045242 ), float4( 0.997891051228107, 0.875821513997308, 0.796144287938319, 0.220225899117173 ), float4( 0.100952654658329, 0.178993291304909, 0.42897151709952, 0.955982096938408 ), float4( 0.402284052875957, 0.774291701509753, 0.947637150039727, 0.122542167139492 ), float4( 0.492085479429031, 0.320908940080977, 0.062709492660458, 0.469544319654603 ), float4( 0.360216894354772, 0.386875028902141, 0.370146195110933, 0.00760086765866767 ), float4( 0.618471302845735, 0.836595992947275, 0.856464813396551, 0.773703996918026 ), float4( 0.0888469000760684, 0.026342012931752, 0.886259265656704, 0.225969636918031 ), float4( 0.872962504100503, 0.476307514811078, 0.300462244684092, 0.12243112228924 ), float4( 0.126672216750063, 0.370885944632295, 0.474983631388742, 0.36869723553243 ), float4( 0.557339822667344, 0.306026747127076, 0.0684746322540914, 0.167594520918836 ), float4( 0.262723447411658, 0.439161002840456, 0.752996050637679, 0.175545089959886 ), float4( 0.137402452126798, 0.851114257635136, 0.995966261250883, 0.318090038522189 ), float4( 0.984146028749713, 0.651289526210767, 0.85458001860165, 0.699278210615403 ), float4( 0.94175661026582, 0.541132445699131, 0.688024426199507, 0.161295058280833 ), float4( 0.0193567006007567, 0.0224402910202929, 0.131378999041104, 0.074610641726577 ), float4( 0.292734025648206, 0.203001880181489, 0.0830195928379053, 0.925976538064879 ), float4( 0.473829456825661, 0.825206027750487, 0.995869950389429, 0.121199534796737 ), float4( 0.845925308692234, 0.694012257128028, 0.912204496987259, 0.0541901472276962 ), float4( 0.31840039664805, 0.202551674192097, 0.74487742024701, 0.179310300005279 ), float4( 0.0835999423095956, 0.680919723436665, 0.636301544791228, 0.237732642440932 ), float4( 0.0303757516808695, 0.568169227134515, 0.241823608168319, 0.221672977889736 ), float4( 0.621727496209427, 0.601184034068689, 0.18067451202342, 0.585539771050932 ), float4( 0.682861518432787, 0.31368857310791, 0.349340534931673, 0.534899531647051 ), float4( 0.174647748085972, 0.993863990527514, 0.874860495270631, 0.0597215672301695 ), float4( 0.356141410002551, 0.8270195125728, 0.701715633134225, 0.312196424376311 ), float4( 0.855244307245707, 0.874766726454146, 0.472164729829954, 0.290133771621684 ), float4( 0.739085029223508, 0.800389871373954, 0.380877813967354, 0.739204936073723 ), float4( 0.796255025452122, 0.508714126194229, 0.077695115971237, 0.338436977164092 ), float4( 0.386138746229065, 0.893646356600172, 0.0442348900457075, 0.72456479851369 ), float4( 0.96117827201317, 0.86134661494817, 0.304249041855451, 0.872645422756972 ), float4( 0.644531515727067, 0.410330179338497, 0.43833801636395, 0.532236735584325 ), float4( 0.344671722196355, 0.377304965340209, 0.879542399141724, 0.324536406120535 ), float4( 0.327691178921466, 0.68515585301684, 0.823168890002728, 0.256580429736795 ), float4( 0.979204090302439, 0.324105120414917, 0.382488335195225, 0.155609025226724 ), float4( 0.0960044973045608, 0.951689836546634, 0.482587948666228, 0.821337624835473 ), float4( 0.220306220101335, 0.441469575949698, 0.78928474559881, 0.174147392238559 ), float4( 0.235993457136673, 0.010903557767581, 0.148760785417986, 0.2810013914858 ), float4( 0.949629100481807, 0.15029569675694, 0.0985432952169996, 0.494794795054381 ), float4( 0.522770470717349, 0.829070210377253, 0.667664908649244, 0.44491412790721 ), float4( 0.436428710090196, 0.93992799424563, 0.945462049425329, 0.361780063883299 ), float4( 0.92084747223223, 0.0563414078468184, 0.411513757152256, 0.111099172435281 ), float4( 0.685545236191501, 0.821114686234442, 0.359232886861653, 0.0620336258141481 ), float4( 0.511158021404947, 0.888625864818984, 0.62856030120913, 0.00948843546653559 ), float4( 0.378758666281942, 0.482911417019978, 0.652339202655637, 0.20306193977737 ), float4( 0.621045433739687, 0.264190624125391, 0.296243278447652, 0.333768164428774 ), float4( 0.228544179922223, 0.598541007655925, 0.374907305638728, 0.177395482164526 ), float4( 0.586612557799841, 0.328374094948347, 0.733809959019446, 0.150133879925187 ), float4( 0.656440211765673, 0.937574207288015, 0.719180315136528, 0.156076503058931 ), float4( 0.00622778712130514, 0.120807884782929, 0.900490152603244, 0.163964812254517 ), float4( 0.0299558187974411, 0.678185573163529, 0.488602156047058, 0.414878770902231 ), float4( 0.651670670905928, 0.0867271596038375, 0.769843370080853, 0.0610032356628232 ), float4( 0.521735395547811, 0.089054859750464, 0.116036128772439, 0.0398590536973714 ), float4( 0.176731007721615, 0.464602968871874, 0.823868694167523, 0.172238085964805 ), float4( 0.643684715797978, 0.611693884996555, 0.133235883961076, 0.322306464576305 ), float4( 0.68143410220809, 0.23411827498773, 0.524486614635441, 0.357171141243154 ), float4( 0.0873047272149961, 0.209099006936466, 0.405593414048475, 0.573583814116932 ), float4( 0.169445549682456, 0.472483798150198, 0.00326064834523045, 0.257950781499013 ), float4( 0.582421264416734, 0.48837439040112, 0.173106120979928, 0.942859860576158 ), float4( 0.775588468078332, 0.881364507545421, 0.682097493522846, 0.141817020318386 ), float4( 0.828697637575072, 0.313904069975905, 0.655660086616715, 0.497557698049377 ), float4( 0.212337966175907, 0.693950905322074, 0.973402872203571, 0.191837242893799 ), float4( 0.113705513120492, 0.546942229171722, 0.512391787260953, 0.39453390212475 ), float4( 0.987572000821853, 0.578183688026938, 0.482530710046427, 0.795837543809711 ), float4( 0.153698958528088, 0.131431014803905, 0.327574043687235, 0.442571663969463 ), float4( 0.681133745555363, 0.19625955596392, 0.891557685980367, 0.0492515973976122 ), float4( 0.0857942114052336, 0.858085347273427, 0.457437789280637, 0.688356617320495 ), float4( 0.291496847891946, 0.881008833591365, 0.396649617886473, 0.762320208252557 ), float4( 0.929596391473709, 0.99141886364269, 0.493608827001233, 0.367530032232185 ), float4( 0.578458188371015, 0.0269289165860642, 0.144833175067246, 0.393353821892922 ), float4( 0.235696134732895, 0.213756171154676, 0.459878300996441, 0.622503320510734 ), float4( 0.960092010889245, 0.60872674622048, 0.27037878067716, 0.0042375763897959 ), float4( 0.00584368035469376, 0.377268577170218, 0.78916090204807, 0.644157453274428 ), float4( 0.553790463858187, 0.239525829553383, 0.460683274763023, 0.632438081611152 ), float4( 0.422346383995538, 0.606408489219103, 0.411763486644143, 0.354252618902481 ), float4( 0.236513116041437, 0.285046254883076, 0.900340395001853, 0.232696679529127 ), float4( 0.150292611285249, 0.750071579008396, 0.464937510651042, 0.996153137179163 ), float4( 0.0845748610257054, 0.115000677814242, 0.217379355438696, 0.126770041942024 ), float4( 0.986597839736658, 0.934220221794313, 0.206875529236568, 0.467377574400686 ), float4( 0.73638125496748, 0.269054365469634, 0.089159586508367, 0.477067465184754 ), float4( 0.587706566596267, 0.453200212890841, 0.682512936965801, 0.914228270721728 ), float4( 0.0918479315432943, 0.752492164612046, 0.20852974439437, 0.690070561920326 ), float4( 0.530735588879667, 0.861170745390081, 0.945183648236647, 0.972049699151912 ), float4( 0.615165429941921, 0.790580556164766, 0.156840705851484, 0.950649879849819 ), float4( 0.313415776152823, 0.227181621467314, 0.472210709225485, 0.210020431880849 ), float4( 0.143789235569439, 0.274225643497997, 0.919662715364091, 0.890843002540452 ), float4( 0.159889221731522, 0.662390860106047, 0.65755961353777, 0.619570242063874 ), float4( 0.0326503003168154, 0.993305700362337, 0.896056826643672, 0.153292018525904 ), float4( 0.517248902710736, 0.934696021459389, 0.766546052306213, 0.783312903150596 ), float4( 0.602533317917275, 0.986112124280125, 0.140848747985833, 0.397800446673203 ), float4( 0.541541834614026, 0.774866948730716, 0.465417548299496, 0.223404115635624 ), float4( 0.169817029577595, 0.245329656286784, 0.511604612000102, 0.196017283571892 ), float4( 0.777379515942829, 0.256225649386749, 0.153961798247863, 0.509199633500166 ), float4( 0.796843656244149, 0.879139154627518, 0.333278229615315, 0.31348092309827 ), float4( 0.533537497526751, 0.791669934425349, 0.754339048990206, 0.429457071437248 ), float4( 0.094932551074276, 0.588959502330497, 0.65742026160351, 0.53742988851733 ), float4( 0.965113918746409, 0.791891629710743, 0.454800796441175, 0.680469408482532 ), float4( 0.0240345038585525, 0.373527802700888, 0.348116561932544, 0.327303651872698 ), float4( 0.0863328734814808, 0.074410262552281, 0.668478597266822, 0.368922286838723 ), float4( 0.808808095198501, 0.696258599728466, 0.721025972962857, 0.914559565444737 ), float4( 0.150786248105944, 0.461542329965878, 0.842190726121045, 0.776424650930066 ), float4( 0.839343902114473, 0.386857193143506, 0.356448362281755, 0.638109748083218 ), float4( 0.601417791844075, 0.453065129207943, 0.249775405623845, 0.810863383491926 ), float4( 0.231773075289919, 0.711391676548585, 0.302867895598927, 0.95258233228353 ), float4( 0.117446687127206, 0.927987659782165, 0.258290196889215, 0.377925399866852 ), float4( 0.790528859845609, 0.831135203517571, 0.17198277971334, 0.40385171324194 ), float4( 0.908109087454206, 0.826658146375165, 0.422866760018685, 0.722433393691868 ), float4( 0.210660557360696, 0.964355942776592, 0.504672827899769, 0.837278897798284 ), float4( 0.0706439614624921, 0.839779483545469, 0.278670823331303, 0.633390221108398 ), float4( 0.746768776209452, 0.880995610673444, 0.698085986402857, 0.578256725602903 ), float4( 0.435443267428988, 0.816691048357958, 0.0790516166384572, 0.570969374650609 ), float4( 0.123752397077043, 0.537253178440618, 0.0955305765827794, 0.374941196932896 ), float4( 0.771542366953354, 0.715896264983293, 0.251475599711517, 0.880820435416335 ), float4( 0.437968402839251, 0.343100573096006, 0.124030705599129, 0.319651044588374 ), float4( 0.289559550252538, 0.438339012879105, 0.86831556347586, 0.0126857557393078 ), float4( 0.963990433124821, 0.634014968589887, 0.427449190722522, 0.637061849067482 ), float4( 0.948392301308174, 0.412496507825561, 0.740219422029433, 0.39199359174445 ), float4( 0.432720853217282, 0.669477674490529, 0.205813556074078, 0.236451076453762 ), float4( 0.229901673379308, 0.680033471286312, 0.942482132437864, 0.973837811487651 ), float4( 0.752083586879113, 0.60101340506273, 0.280099316164897, 0.370855909013588 ), float4( 0.731127569792386, 0.0479255630857896, 0.950891026738515, 0.494764292377403 ), float4( 0.712880343065076, 0.623852392483434, 0.399310494959033, 0.727543388366487 ), float4( 0.71574877794634, 0.95901977874293, 0.343830670855861, 0.308429763330347 ), float4( 0.0126800471975841, 0.685400230663549, 0.614266292478082, 0.801428298839102 ), float4( 0.389241857635436, 0.441989767570975, 0.622577073342436, 0.711255889251482 ), float4( 0.797033756411184, 0.70353698483833, 0.942220343715614, 0.102064692462825 ), float4( 0.510082708909215, 0.015024523257755, 0.650918762037027, 0.757934931552939 ), float4( 0.400618440658142, 0.150192894111477, 0.56756745770926, 0.688546145189808 ), float4( 0.158239696714207, 0.497459654462272, 0.281558185946922, 0.916064870039031 ), float4( 0.683123941851372, 0.932684898345119, 0.924181506002406, 0.32453990882474 ), float4( 0.0131860128665278, 0.0126760336629469, 0.676244813798109, 0.473701074474352 ), float4( 0.325647003634668, 0.897383792743731, 0.223771029256178, 0.544501442715759 ), float4( 0.0657671788082305, 0.141053833598762, 0.584595953852216, 0.310093819308138 ), float4( 0.978436331720295, 0.568843426913416, 0.573822152602404, 0.0275905849540562 ), float4( 0.105705219370176, 0.84882633427569, 0.984681583468188, 0.697855819807321 ), float4( 0.972933630446407, 0.641375563406095, 0.326924947708345, 0.565555883834863 ), float4( 0.39145232103367, 0.655284525666053, 0.150190066616139, 0.515220392735312 ), float4( 0.403842044716627, 0.698201422439051, 0.11830435698773, 0.456556959290317 ), float4( 0.517808261568569, 0.298037164517696, 0.698069876384954, 0.784357722748238 ), float4( 0.0272921710402203, 0.468519269241262, 0.776417688828156, 0.612698916165484 ), float4( 0.791253494001577, 0.106417319321268, 0.692167752744708, 0.25956460705938 ), float4( 0.565596940259262, 0.257473638401122, 0.710109813469513, 0.589396269800792 ), float4( 0.923637501859869, 0.253967600992866, 0.810359650668856, 0.834297607575682 ), float4( 0.94800331487693, 0.226325686195086, 0.351606278378333, 0.371810449460433 ), float4( 0.685751085954602, 0.110688929963246, 0.0822487534406822, 0.670362477968616 ), float4( 0.747193726127592, 0.708550636520866, 0.140659397999132, 0.36756575636918 ), float4( 0.0227494766110319, 0.128038994561899, 0.79228555773957, 0.680399167202599 ), float4( 0.870773550528462, 0.789464429854166, 0.000298413913835964, 0.637185950128914 ), float4( 0.0724086454475339, 0.371982667302705, 0.906602325805743, 0.866516311125139 ), float4( 0.949207810661387, 0.0673603406489642, 0.999958943575602, 0.133978682632548 ), float4( 0.945174712196539, 0.560793796815348, 0.591582890875443, 0.149874443723762 ), float4( 0.887841771770195, 0.284006749412048, 0.508553644413386, 0.291482575373483 ), float4( 0.946430886139363, 0.326259426924521, 0.0986066367936352, 0.916603241076974 ), float4( 0.38627051580058, 0.10605521085954, 0.865505190037892, 0.0827028574807117 ), float4( 0.965757921322136, 0.324601996375528, 0.236815130448348, 0.437557945697363 ), float4( 0.465188080661552, 0.0297106462669143, 0.71862271927233, 0.134173072005703 ), float4( 0.25366918707903, 0.173173700539942, 0.761888962128148, 0.576020647574226 ), float4( 0.319723360389342, 0.485089967253194, 0.422602638799046, 0.618390745305638 ), float4( 0.110729986387645, 0.948270070808134, 0.725187765772076, 0.186399929312244 ), float4( 0.116967745645422, 0.99078495427537, 0.479723984598985, 0.738742727198984 ), float4( 0.619485350148513, 0.500802982366086, 0.733968281063236, 0.544514123603941 ), float4( 0.690857793060531, 0.0836951728368621, 0.250915434328334, 0.966353434587993 ), float4( 0.506477477264813, 0.823899468325032, 0.900758389803003, 0.624605814285858 ), float4( 0.830545210200616, 0.562400997878239, 0.668790601970996, 0.915464065929625 ), float4( 0.842171077543018, 0.457409818869741, 0.896205256644732, 0.714668071230253 ), float4( 0.5221177872839, 0.932532996839161, 0.971759214984141, 0.461340918886168 ), float4( 0.903656788125474, 0.480215891487997, 0.805873254689329, 0.438000444992446 ), float4( 0.380867445087464, 0.679105260725648, 0.965735111835289, 0.974972967046766 ), float4( 0.844878011776543, 0.498072403249365, 0.81807259554885, 0.964385098295466 ), float4( 0.295742365203678, 0.174108595668389, 0.443315278945172, 0.169974014242168 ), float4( 0.922258266211608, 0.795535527540155, 0.0695431703094128, 0.495823892064311 ), float4( 0.584331577450191, 0.797996824513188, 0.787845535105022, 0.548328988509406 ), float4( 0.279479468837138, 0.809723699842451, 0.344228851769226, 0.659557926775682 ), float4( 0.0945796976306381, 0.765055913368732, 0.216624939915084, 0.686952353309352 ), float4( 0.529043767381945, 0.272627362177068, 0.640857335478467, 0.210641901572534 ), float4( 0.277821918147533, 0.812914989335889, 0.585485989500529, 0.827372216539165 ), float4( 0.858164356489742, 0.925785422756237, 0.779727802509315, 0.332472806951251 ), float4( 0.744328402329389, 0.70440550367553, 0.619721700725947, 0.668062481874629 ), float4( 0.0140945399245687, 0.726231242402564, 0.792409805018646, 0.726582259743746 ), float4( 0.862989826529748, 0.47593532291983, 0.877009341435977, 0.105659963612286 ), float4( 0.692370356382975, 0.257544266179923, 0.158520976155308, 0.571143745245013 ), float4( 0.334876408956422, 0.306177185059608, 0.880393269416128, 0.0798220984078115 ), float4( 0.281447463334281, 0.131120242239498, 0.43534133091352, 0.0231150030266098 ), float4( 0.533251260189922, 0.232673377372638, 0.892152096094634, 0.109343276875719 ), float4( 0.210049538039625, 0.242170953770248, 0.637659535574568, 0.658546154693955 ), float4( 0.0182690220038728, 0.455372728153771, 0.804000586180017, 0.575073965161608 ), float4( 0.190001999116504, 0.676166369894597, 0.645463386851113, 0.368348455228074 ), float4( 0.972646108350086, 0.490042680171338, 0.823962526779604, 0.0531084444621151 ), float4( 0.395618020741091, 0.53519737186618, 0.518271545189559, 0.0202776519676101 ), float4( 0.654394013180581, 0.0143422442555159, 0.492495807582743, 0.551987171430135 ), float4( 0.0453921533401088, 0.699905704101504, 0.0510253436169705, 0.613208160089891 ), float4( 0.26906417276201, 0.596606697699338, 0.134811221684707, 0.781421162551931 ), float4( 0.83407914630793, 0.683066528142927, 0.51653272170412, 0.6208188727595 ), float4( 0.838275787345262, 0.218463186742022, 0.0873909416084136, 0.236997628229203 ), float4( 0.453543679999906, 0.5834470109937, 0.38114174612851, 0.658710039061825 ), float4( 0.660713798208495, 0.512044814188054, 0.107175990057725, 0.791404783162942 ), float4( 0.307157715459893, 0.382232886451405, 0.627496982285519, 0.998053888323742 ), float4( 0.714401832183079, 0.871874444127024, 0.454949263695138, 0.19570729378411 ), float4( 0.749675146187504, 0.0856723641444335, 0.613154001353846, 0.318363317902369 ), float4( 0.404347384536801, 0.190792426090125, 0.306009792399597, 0.593395301417166 ), float4( 0.54135514820989, 0.864042224299182, 0.534269308920144, 0.28957958020716 ), float4( 0.973509958467218, 0.203143654020104, 0.214832657116853, 0.177154833999069 ), float4( 0.447806430257767, 0.281273916960356, 0.566733971967704, 0.0709470021868809 ), float4( 0.633200498127006, 0.833785768520919, 0.89127337322164, 0.533347339152055 ), float4( 0.592729714043778, 0.259620560454028, 0.306050444629998, 0.886831286310605 ), float4( 0.969109715413819, 0.136757333360965, 0.0670193303688519, 0.962204702180906 ), float4( 0.228117264447788, 0.320825427920011, 0.871143726571996, 0.752603423200829 ), float4( 0.605309185388176, 0.769027623706044, 0.832650243692403, 0.262751253909781 ), float4( 0.277437218594103, 0.787746444711344, 0.117354890851935, 0.796671573909312 ), float4( 0.97777550526791, 0.817596409850566, 0.817894824695725, 0.104014061439789 ), float4( 0.167400229334552, 0.45034214828645, 0.550247458065975, 0.433127915222723 ), float4( 0.305140472159321, 0.384002261508257, 0.562506795657103, 0.915889377666586 ), float4( 0.194398990922793, 0.0798066622017914, 0.725633603858591, 0.144726824082773 ), float4( 0.884741981460127, 0.419178506088992, 0.624285586003347, 0.404597814848925 ), float4( 0.79702289393033, 0.572064606739238, 0.0614623157593712, 0.652684530547207 ), float4( 0.331999927448109, 0.462229233916024, 0.571845648610892, 0.678778806551722 ), float4( 0.448623673267953, 0.303982718057922, 0.793509783592778, 0.845454053415663 ), float4( 0.716430877668984, 0.0946017993123279, 0.555571833884144, 0.775133304193212 ), float4( 0.441725735758304, 0.202036383190209, 0.719431056976053, 0.518767567127369 ), float4( 0.58650987529499, 0.633891415146129, 0.657064230021585, 0.844115002939531 ), float4( 0.758318632262907, 0.95525434890541, 0.558204432278035, 0.525502523186385 ), float4( 0.0433940198474536, 0.68792341960963, 0.378009272449654, 0.858258712505111 ), float4( 0.163460858707996, 0.71275707600301, 0.999648679978982, 0.405710898528672 ), float4( 0.756134094091195, 0.165210294148517, 0.77201413399168, 0.705170995418528 ), float4( 0.878496499675557, 0.871468651514253, 0.98450424195477, 0.00115775410139829 ), float4( 0.590492477915479, 0.717052742241441, 0.199458499997602, 0.0997971916104654 ), float4( 0.524234828317647, 0.950500299665379, 0.703001088324469, 0.682705598269918 ), float4( 0.699747449112939, 0.105518018875978, 0.818087939553935, 0.163131478784201 ), float4( 0.915000376717653, 0.21734731281984, 0.8943658982843, 0.376745578542699 ), float4( 0.904024801637989, 0.0463431254245076, 0.635384786704269, 0.760700253658323 ), float4( 0.925973445608268, 0.935251071087667, 0.681993194707666, 0.00367380166597376 ), float4( 0.094953119333346, 0.149860935355472, 0.0189992101020176, 0.276515441609787 ), float4( 0.430022249198529, 0.0142600615575258, 0.640271067451812, 0.715041223780737 ), float4( 0.649387173191359, 0.655906475920187, 0.253622525024052, 0.0412658900214666 ), float4( 0.755795848907808, 0.45840724066757, 0.00126769486873769, 0.092893720182075 ), float4( 0.984922331285161, 0.695303674179736, 0.158511263392172, 0.0579428398320185 ), float4( 0.894669136449075, 0.83651720119478, 0.490710521811019, 0.538786781271355 ), float4( 0.270844395864217, 0.395268555448981, 0.801146193780539, 0.83215337425105 ), float4( 0.236083864809984, 0.223803988296447, 0.0751843084931301, 0.655241406827812 ), float4( 0.0350595475337745, 0.195784698331628, 0.00484407227711942, 0.374373892962175 ), float4( 0.248668584157093, 0.110743977181029, 0.467010679872246, 0.771604122021982 ) };
float4	GetArrayNoise( float3 _WorldPosition )
{
	_WorldPosition *= 10.0;

	int3	IPos = int3(floor(_WorldPosition));
	float3	t = _WorldPosition - IPos;
	IPos &= 0xFF;
	float4	R0 = lerp( NoiseTable[IPos.x], NoiseTable[IPos.x+1], smoothstep( 0.0, 1.0, t.x ) );
	float4	R1 = lerp( NoiseTable[IPos.y], NoiseTable[IPos.y+1], smoothstep( 0.0, 1.0, t.y ) );
	float4	R2 = lerp( NoiseTable[IPos.z], NoiseTable[IPos.z+1], smoothstep( 0.0, 1.0, t.z ) );
	return 0.33333 * (R0+R1+R2);
	
// 	float	Pos = _WorldPosition.x;
// 	int		I = int(floor(Pos));
// 	float	t = Pos - I;
// 	float4	Random0 = lerp( NoiseTable[I], NoiseTable[I+1], smoothstep( 0.0, 1.0, t ) );
// 
// 	Pos = _WorldPosition.y + 256.0 * Random0.x;
// 	I = int(floor(Pos));
// 	t = Pos - I;
// 	float4	Random1 = lerp( NoiseTable[I], NoiseTable[I+1], smoothstep( 0.0, 1.0, t ) );
// 
// 	Pos = _WorldPosition.z + 256.0 * Random1.y;
// 	I = int(floor(Pos));
// 	t = Pos - I;
// 	float4	Random2 = lerp( NoiseTable[I], NoiseTable[I+1], smoothstep( 0.0, 1.0, t ) );
// 
// 	return 0.333333 * (Random0 + Random1 + Random2);
}

// Dynamic array noise version (4 octaves)
float	GetNoise( float3 _WorldPosition, float _MipLevel )
{
	float3	UVW = World2Volume( _WorldPosition );

	float	Value  = GetArrayNoise( UVW ).x;
	UVW *= 2.0;
	UVW.x += CloudEvolutionSpeed * CloudTime;

	Value += 0.5   * GetArrayNoise( UVW ).y;
	UVW *= 2.0;
	UVW.x += CloudEvolutionSpeed * CloudTime;

	Value += 0.25  * GetArrayNoise( UVW ).z;
	UVW *= 2.0;
	UVW.x += CloudEvolutionSpeed * CloudTime;

	Value += 0.125 * GetArrayNoise( UVW ).w;

	return Value;
}
//*/



// ===================================================================================
// Bevels the noise values so the clouds completely disappear at top and bottom
float	Bevel( float3 _WorldPosition )
{
	// Compute a normalized height that is 0 at the center of the cloud layer, and 1 at the top/bottom
	float	NormalizedHeight = 1.0 - abs( 2.0 * (CloudPlaneHeightTop - _WorldPosition.y) / (CloudPlaneHeightTop - CloudPlaneHeightBottom) - 1.0 );
	float	Bevel = smoothstep( 0.0, 1.0, NormalizedHeight );
	return pow( Bevel, 0.1 );
}

// ===================================================================================
// Computes the density and traversed optical depth at current position within the cloud
//
float	ComputeCloudDensity( float3 _WorldPosition, float _MipLevel=0.0 )
{
	float	NoiseDensity = Bevel( _WorldPosition ) * GetNoise( _WorldPosition, _MipLevel );


	// Alter offset based on position so we alternate areas of high and low cloud coverage
	float3	AreaOffset = CloudSpeed * CloudTime * float3( 1.0, 0.0, 1.0 );
	float	Value  = NoiseTexture0.SampleLevel( VolumeSampler, World2Volume( 0.1 * (_WorldPosition + AreaOffset) ), _MipLevel ).x;
	float	AlteredOffset = NoiseOffset * lerp( 0.25, 1.75, Value);


	return DensityCloud * saturate( NoiseDensity + AlteredOffset );
}

float2	ComputeCloudDensityAndOpticalDepth( float3 _WorldPosition )
{
	return float2( ComputeCloudDensity( _WorldPosition ), ComputeCloudOpticalDepth( _WorldPosition ) );
}

// ===================================================================================
// Traces through air
float	FarClipAir = 1000.0;

void	TraceAir( float3 _View, float _DistanceIn, float _DistanceOut, inout float3 _Extinction, inout float3 _InScatteredEnergy, float2 _TerminatorDistance, bool _bBelowClouds, uniform int _StepsCount )
{
	float3	CameraPosition = Camera2World[3].xyz;
	float3	ViewPosition = CameraPosition + _DistanceIn * _View;
	float	Distance = _DistanceOut - _DistanceIn;

	float	OriginalDistance = Distance;
	if ( _bBelowClouds )
		Distance = min( FarClipAir, Distance );

	// Compute camera height & hit distance in kilometers
	float	AltitudeKm = WorldUnit2Kilometer * ViewPosition.y;
	float	HitDistance = WorldUnit2Kilometer * Distance;
	float3	CurrentPosition = float3( 0.0, AltitudeKm, 0.0 );

	// Compute phases
	float	CosTheta = dot( _View, SunDirection );
	float	PhaseRayleigh = 0.75 * (1.0 + CosTheta*CosTheta);
	float	PhaseMie = 1.0 / (1.0 + MiePhaseAnisotropy * CosTheta);
			PhaseMie = (1.0 - MiePhaseAnisotropy*MiePhaseAnisotropy) * PhaseMie * PhaseMie;

	// Ray-march the view ray
	float3	AccumulatedLightRayleigh = 0.0.xxx;
	float3	AccumulatedLightMie = 0.0.xxx;

	float3	SumExtinctionCoefficients = 0.0;
	float3	SumInScattering = 0.0;

	float	StepSize = HitDistance / _StepsCount;
	float3	Step = StepSize * _View;
	float3	StepCloud = Distance / _StepsCount * _View;

	CurrentPosition += 0.5 * Step;	// Start at half a step
	float3	CurrentPositionCloud = ViewPosition + 0.5 * StepCloud;

	for ( int StepIndex=0; StepIndex < _StepsCount; StepIndex++ )
	{
		// =============================================
		// Sample density at current altitude and optical depth in Sun direction
		float4	OpticalDepth = ComputeOpticalDepth( CurrentPosition.y, SunDirection, float3( 0, 1, 0 ) );

		// Retrieve densities at current position
		float	Rho_air = OpticalDepth.x;
		float	Rho_aerosols = OpticalDepth.y;

		// =============================================
		// Retrieve sun light attenuated when passing through the atmosphere
		float3	OpticalDepth_cloud = 0.0;
		if ( _bBelowClouds )
			OpticalDepth_cloud = ComputeCloudOpticalDepth( CurrentPositionCloud );
		float3	SunExtinction = exp( -Sigma_Rayleigh * OpticalDepth.z - Sigma_Mie * OpticalDepth.w - CloudSigma_t * OpticalDepth_cloud );
		float3	Light = _SunIntensity * SunExtinction;

		// =============================================
		// Compute in-scattered light
		float3	ScatteringRayleigh = Rho_air * DensitySeaLevel_Rayleigh * INV_WAVELENGTHS_POW4;
		float	ScatteringMie = Rho_aerosols * DensitySeaLevel_Mie;
		float3	InScatteringRayleigh = Light * ScatteringRayleigh * PhaseRayleigh;
		float3	InScatteringMie = Light * ScatteringMie * PhaseMie;

		SumInScattering += InScatteringRayleigh + InScatteringMie;

		// =============================================
		// Accumulate in-scattered light (only if not in Earth's shadow)
		float	Distance2Camera = _DistanceIn + StepIndex * Distance / _StepsCount;
		if ( Distance2Camera >= _TerminatorDistance.x && Distance2Camera <= _TerminatorDistance.y )
		{
			AccumulatedLightRayleigh += InScatteringRayleigh * _Extinction * StepSize;
			AccumulatedLightMie += InScatteringMie * _Extinction * StepSize;
		}

		// =============================================
		// Accumulate extinction along view
		float3	CurrentExtinctionCoefficient = exp( -(Sigma_Rayleigh * Rho_air + Sigma_Mie * Rho_aerosols) * StepSize );
		SumExtinctionCoefficients += CurrentExtinctionCoefficient;
		_Extinction *= CurrentExtinctionCoefficient;

		// March
		CurrentPosition += Step;
		CurrentPositionCloud += StepCloud;
	}

	_InScatteredEnergy += AccumulatedLightRayleigh + AccumulatedLightMie;

	// Finish by extrapolating extinction & in-scattering
	if ( OriginalDistance > Distance )
	{
		float3	AverageExtinctionCoefficient = SumExtinctionCoefficients / _StepsCount;
		float3	AverageInScattering = SumInScattering / _StepsCount;

		float	DeltaDistance = WorldUnit2Kilometer * (OriginalDistance - Distance);
		float3	ExtrapolatedExtinction = exp( -AverageExtinctionCoefficient * DeltaDistance );
		_Extinction *= ExtrapolatedExtinction;
		_InScatteredEnergy += AverageInScattering * ExtrapolatedExtinction * DeltaDistance;
	}
}

// ===================================================================================
// Traces through the cloud layer
float	FarClipClouds = 1000.0;

void	TraceCloud( float3 _View, float _DistanceIn, float _DistanceOut, inout float3 _Extinction, inout float3 _InScatteredEnergy, float2 _TerminatorDistance, uniform int _StepsCount )
{
	float3	CameraPosition = Camera2World[3].xyz;
	float3	ViewPosition = CameraPosition + _DistanceIn * _View;
	float	Distance = _DistanceOut - _DistanceIn;

	float	OriginalDistance = Distance;
	Distance = min( FarClipClouds, Distance );

	// Compute camera height in kilometers
	float3	StartSkyPosition = float3( 0.0, EARTH_RADIUS + WorldUnit2Kilometer * ViewPosition.y, 0.0 );

	// Compute light phases
	// Fr(θ) = (1-k²) / (1+kcos(θ))^2         <= Shlick's equivalent to Henyey-Greenstein
	float	CosTheta = -dot( _View, SunDirection );
	float	PhaseRayleigh = 0.75 * (1.0 + CosTheta*CosTheta);
	float	PhaseMie = 1.0 / (1.0 + MiePhaseAnisotropy * CosTheta);
			PhaseMie = (1.0 - MiePhaseAnisotropy*MiePhaseAnisotropy) * PhaseMie * PhaseMie;

		// Strong forward phase
	float	Den = 1.0 / (1.0 + ScatteringAnisotropyStrongForward * CosTheta);
	float	PhaseStrongForward = (1.0 - ScatteringAnisotropyStrongForward*ScatteringAnisotropyStrongForward) * Den * Den;
		// Forward phase
			Den = 1.0 / (1.0 + ScatteringAnisotropyForward * CosTheta);
	float	PhaseForward = (1.0 - ScatteringAnisotropyForward*ScatteringAnisotropyForward) * Den * Den;
		// Backward phase
			Den = 1.0 / (1.0 + ScatteringAnisotropyBackward * CosTheta);
	float	PhaseBackward = (1.0 - ScatteringAnisotropyBackward*ScatteringAnisotropyBackward) * Den * Den;
		// Side phase
//	CosTheta = 0.4 + 0.6 * CosTheta;	// Add bias
	float	PhaseSide = saturate( pow( sqrt(1.0 - 0.8 * CosTheta*CosTheta), ScatteringAnisotropySide ) );

	float	PhaseAmbient = PhaseWeightSide * PhaseSide;
	float	PhaseDirect = PhaseWeightSide2 * PhaseAmbient + PhaseWeightStrongForward * PhaseStrongForward + PhaseWeightForward * PhaseForward + PhaseWeightBackward * PhaseBackward;

	float3	AccumulatedLightRayleigh = 0.0.xxx;
	float3	AccumulatedLightMie = 0.0.xxx;
	float3	AccumulatedLightCloud = 0.0.xxx;

	float3	SumExtinctionCoefficients = 0.0;
	float3	SumInScattering = 0.0;

	float	k = exp( log( 1e-4 + Distance ) / _StepsCount );
	float	PreviousDistance = 0.0;
	float	CurrentDistance = 1.0;

	for ( int StepIndex=0; StepIndex < _StepsCount; StepIndex++ )
	{
//		CurrentDistance *= k;
		CurrentDistance = StepIndex * Distance / _StepsCount;

		float	StepSize = WorldUnit2Kilometer * (CurrentDistance - PreviousDistance);
		float3	CurrentPosition = ViewPosition + CurrentDistance * _View;

		// =============================================
		// Sample density at current altitude and optical depth in Sun direction
		float3	CurrentSkyPosition = StartSkyPosition + WorldUnit2Kilometer * CurrentDistance * _View;
		float4	OpticalDepth_air = ComputeOpticalDepth( CurrentSkyPosition.y, SunDirection, float3( 0, 1, 0 ) );

		// Retrieve densities at current position
		float	Rho_air = OpticalDepth_air.x;
		float	Rho_aerosols = OpticalDepth_air.y;

		// =============================================
		// Sample cloud density at current altitude and extrapolate optical depth in Sun direction
		float2	DensityOpticalDepth = ComputeCloudDensityAndOpticalDepth( CurrentPosition );
		float	Rho_cloud = DensityOpticalDepth.x;
		float	OpticalDepth_cloud = DensityOpticalDepth.y;

		// =============================================
		// Retrieve sun light attenuated when passing through the atmosphere & cloud
		float3	SunExtinction = exp( -Sigma_Rayleigh * OpticalDepth_air.z - Sigma_Mie * OpticalDepth_air.w - CloudSigma_t * OpticalDepth_cloud );
		float3	Light = DirectionalFactor * _SunIntensity * SunExtinction;

		// =============================================
		// Invent a scattered light based on traversed cloud depth
		// The more light goes through cloud matter, the more it's scattered within the cloud
//		float3	ScatteredLight = IsotropicFactor * SkyZenith * SunColor * CloudSigma_s * SumRho_cloud;
		float3	ScatteredLight = IsotropicFactor * 0.5 * (SkyZenith + SunColor) * CloudSigma_s * OpticalDepth_cloud;

		// =============================================
		// Compute in-scattered light
		float3	ScatteringCoeffRayleigh = Rho_air * DensitySeaLevel_Rayleigh * INV_WAVELENGTHS_POW4;
		float	ScatteringCoeffMie = Rho_aerosols * DensitySeaLevel_Mie;
		float	ScatteringCoeffCloud = Rho_cloud;

		float3	InScatteringRayleigh = Light * ScatteringCoeffRayleigh * PhaseRayleigh;
		float3	InScatteringMie = Light * ScatteringCoeffMie * PhaseMie;

		float3	InScatteringCloud  = Light * ScatteringCoeffCloud * PhaseDirect;
				InScatteringCloud += ScatteredLight * ScatteringCoeffCloud * PhaseAmbient;

		float3	InScatteringLightning = ComputeLightningLightingCloud( CurrentPosition, _View ).xxx;

		SumInScattering += InScatteringRayleigh + InScatteringMie + InScatteringCloud + InScatteringLightning;

		// =============================================
		// Accumulate in-scattered light (only if not in Earth's shadow)
		float	Distance2Camera = _DistanceIn + StepIndex * Distance / _StepsCount;
		if ( Distance2Camera >= _TerminatorDistance.x && Distance2Camera <= _TerminatorDistance.y )
		{
			AccumulatedLightRayleigh += InScatteringRayleigh * _Extinction * StepSize;
			AccumulatedLightMie += InScatteringMie * _Extinction * StepSize;
			AccumulatedLightCloud += InScatteringCloud * _Extinction * StepSize;
			AccumulatedLightCloud += InScatteringLightning * _Extinction * StepSize;
		}

		// =============================================
		// Accumulate extinction along view
		float3	CurrentExtinctionCoefficient = Sigma_Rayleigh * Rho_air + Sigma_Mie * Rho_aerosols + CloudSigma_t * Rho_cloud;
		SumExtinctionCoefficients += CurrentExtinctionCoefficient;
		_Extinction *= exp( -CurrentExtinctionCoefficient * StepSize );

		PreviousDistance = CurrentDistance;
	}

	_InScatteredEnergy += AccumulatedLightRayleigh + AccumulatedLightMie + AccumulatedLightCloud;

// 	// Finish by extrapolating extinction & in-scattering
// 	if ( OriginalDistance > Distance )
// 	{
// 		float3	AverageExtinctionCoefficient = SumExtinctionCoefficients / _StepsCount;
// 		float3	AverageInScattering = SumInScattering / _StepsCount;
// 
// 		float	DeltaDistance = WorldUnit2Kilometer * (OriginalDistance - Distance);
// 		float3	ExtrapolatedExtinction = exp( -AverageExtinctionCoefficient * DeltaDistance );
// 		_Extinction *= ExtrapolatedExtinction;
// //		_InScatteredEnergy += AverageInScattering * ExtrapolatedExtinction * DeltaDistance;
// 	}
}

// ===================================================================================
PS_OUT	PS_Compute( VS_IN _In )
{
	PS_OUT	Out;
	Out.InScattering = 0.0;
	Out.ExtinctionZ = float4( 1.0.xxx, 0.0 );

	// Compute intersections with ground & cloud planes
	float3	CameraView = float3( CameraData.y * CameraData.x * (2.0 * _In.Position.x * BufferInvSize.x - 1.0), CameraData.x * (1.0 - 2.0 * _In.Position.y * BufferInvSize.y), 1.0 );
	float	CameraViewLength = length( CameraView );
			CameraView /= CameraViewLength;
	float	Z = ReadDepth( _In.Position.xy * BufferInvSize / ZBufferInvSize );
	float	Distance2Pixel = CameraViewLength * Z;
	float3	View = mul( float4( CameraView, 0.0 ), Camera2World ).xyz;
	float3	CameraPosition = Camera2World[3].xyz;

	// If pixel is at "infinity" then compute intersection with upper atmosphere
	if ( Z > CameraData.w-5.0 )
		Distance2Pixel = ComputeSphereIntersection( CameraPosition, View, ATMOSPHERE_RADIUS - EARTH_RADIUS );

	// Compute hit with the cloud layer
	Hit		H = ComputeLayerIntersection( CameraPosition, View, Distance2Pixel );

	// Compute potential intersection with earth's shadow
	float	Height = EARTH_RADIUS + WorldUnit2Kilometer * CameraPosition.y;
	float	HitDistance = WorldUnit2Kilometer * Distance2Pixel;
	float3	CurrentPosition = float3( 0.0, Height, 0.0 );

	float2	TerminatorDistance = float2( 0.0, 1e6 );
	if ( SunDirection.y < 0.0 )
	{	// Project current position in the 2D plane orthogonal to the light to test the intersection with the shadow cylinder cast by the Earth
		float3	X = normalize( cross( SunDirection, float3( 0.0, 1.0, 0.0 ) ) );
		float3	Z = cross( X, SunDirection );

		float2	P = float2( Height*X.y, Height*Z.y );			// Position in plane
		float2	V = float2( dot( View, X ), dot( View, Z ) );	// View in plane
		float	a = dot( V, V );
		float	b = dot( P, V );
		float	c = dot( P, P ) - EARTH_RADIUS*EARTH_RADIUS;
		float	Delta = b*b - a*c;
		if ( Delta > 0.0 )
		{	// Distance til we hit the terminator, in WORLD space
			if ( c < 0 )
				TerminatorDistance = float2( 0.0, (-b+sqrt(Delta)) / max( 1e-4, WorldUnit2Kilometer * a) );	// From inside
			else
				TerminatorDistance = float2( (-b-sqrt(Delta)) / max( 1e-4, WorldUnit2Kilometer * a), 1e6 );	// From outside
		}
	}

	// Trace
	if ( H.bTraceAirBefore )
	{
		if ( H.bTraceAirBeforeBelow )
			TraceAir( View, 0.0, H.HitDistanceCloudIn, Out.ExtinctionZ.xyz, Out.InScattering, TerminatorDistance, true, STEPS_COUNT_AIR_GOD_RAYS );
		else
			TraceAir( View, 0.0, H.HitDistanceCloudIn, Out.ExtinctionZ.xyz, Out.InScattering, TerminatorDistance, false, STEPS_COUNT_AIR );
	}

	if ( H.bTraceCloud )
		TraceCloud( View, H.HitDistanceCloudIn, H.HitDistanceCloudOut, Out.ExtinctionZ.xyz, Out.InScattering, TerminatorDistance, STEPS_COUNT_CLOUDS );
	
	if ( H.bTraceAirAfter )
	{
		if ( H.bTraceAirAfterBelow )
			TraceAir( View, H.HitDistanceCloudOut, H.HitDistanceAirOut, Out.ExtinctionZ.xyz, Out.InScattering, TerminatorDistance, true, STEPS_COUNT_AIR_GOD_RAYS );
		else
			TraceAir( View, H.HitDistanceCloudOut, H.HitDistanceAirOut, Out.ExtinctionZ.xyz, Out.InScattering, TerminatorDistance, false, STEPS_COUNT_AIR );
	}

// 	if ( H.bTraceCloud )
// 	{
// Visualize mip level
// 		float	Distance2Camera = length( H.HitDistanceCloudIn );
// 		float	SizeVolumeCube = (CloudPlaneHeightTop - CloudPlaneHeightBottom) / (NoiseSize * 128.0);	// World size of a unit volume
// 		float	SizePixel = 2.0 * CameraData.x * Distance2Camera * BufferInvSize.y;						// Vertical size of a pixel at current distance
// 		float	MipLevel = max( 0.0, log( SizePixel / SizeVolumeCube ) * INV_LOG2 );
// 
// 		float3	MipColors[] = 
// 		{
// 			float3( 1, 0, 0 ),
// 			float3( 0, 1, 0 ),
// 			float3( 0, 0, 1 ),
// 			float3( 1, 1, 0 ),
// 			float3( 0, 1, 1 ),
// 			float3( 1, 0, 1 ),
// 			float3( 1, 1, 1 ),
// 			float3( 1, 1, 1 ),
// 			float3( 1, 1, 1 ),
// 			float3( 1, 1, 1 ),
// 			float3( 1, 1, 1 ),
// 		};
// 
// 		Out.Extinction = 0.0;
// 		Out.InScattering = MipColors[(int) floor( min( 6, MipLevel ) )];
// 		return Out;
//	}

//	Out.InScattering *= TerminatorDistance;

	Out.ExtinctionZ.w = Z;

	return Out;
}

// ===================================================================================
// Combination of in-scattering and extinction buffers with the source buffer
Texture2D	SourceBuffer;
float3		VolumeBufferInvSize;
Texture2D	VolumeTextureInScattering;
Texture2D	VolumeTextureExtinction;

float		BilateralThreshold = 10.0;

float4	PS_Combine( VS_IN _In ) : SV_TARGET0
{
	float2	UV = _In.Position.xy * BufferInvSize;
	float4	SourceColor = SourceBuffer.SampleLevel( LinearClamp, UV, 0 );
	float	Z = ReadDepth( _In.Position.xy );

	// Perform an average of extinction & in-scattering values based on differences in Z : samples that are closest to actual depth will be used
	float2	dUVs[] =
	{
		-VolumeBufferInvSize.xz,
		+VolumeBufferInvSize.xz,
		-VolumeBufferInvSize.zy,
		+VolumeBufferInvSize.zy,

		-VolumeBufferInvSize.xy,
		+VolumeBufferInvSize.xy,
		float2( VolumeBufferInvSize.x, -VolumeBufferInvSize.y ),
		float2( -VolumeBufferInvSize.x, VolumeBufferInvSize.y ),
	};

	float3	InScattering = 0.0;
	float4	ExtinctionZ = 0.0;
	float	SumWeights = 0.0;
	for ( int SampleIndex=0; SampleIndex < 8; SampleIndex++ )
	{
		float4	ExtinctionZSample = VolumeTextureExtinction.SampleLevel( LinearClamp, UV + dUVs[SampleIndex], 0 );
		if ( abs( ExtinctionZSample.w - Z ) > BilateralThreshold )
			continue;

		float3	InScatteringSample = VolumeTextureInScattering.SampleLevel( LinearClamp, UV + dUVs[SampleIndex], 0 ).xyz;

		InScattering += InScatteringSample;
		ExtinctionZ += ExtinctionZSample;
		SumWeights++;
	}

	// Sample center
	float3	CenterInScattering = VolumeTextureInScattering.SampleLevel( LinearClamp, UV, 0 ).xyz;
	float4	CenterExtinctionZ = VolumeTextureExtinction.SampleLevel( LinearClamp, UV, 0 );
	if ( SumWeights == 0.0 || abs( CenterExtinctionZ.w - Z ) < BilateralThreshold )
	{
		InScattering += CenterInScattering;
		ExtinctionZ += CenterExtinctionZ;
		SumWeights++;
	}

	InScattering /= SumWeights;
	ExtinctionZ /= SumWeights;

	// =============================================
	// Display the Sun
	float3	CameraView = normalize( float3( CameraData.y * CameraData.x * (2.0 * _In.Position.x * BufferInvSize.x - 1.0), CameraData.x * (1.0 - 2.0 * _In.Position.y * BufferInvSize.y), 1.0 ) );
	float3	View = mul( float4( CameraView, 0.0 ), Camera2World ).xyz;

	// 	149.5978875 = Distance to the Sun in millions of kilometers
	// 	0.695       = Radius of the Sun in millions of kilometers
	float	CosAngle = 0.998;	// Cos( SunCoverAngle ) but arbitrary instead of physical computation, otherwise the Sun is really too small
	float	DotSun = dot( View, SunDirection );

	float	Infinity = CameraData.w-5.0;
	float3	SunExtinction = ExtinctionZ.xyz * step( Infinity, ExtinctionZ.w ) * smoothstep( CosAngle, 1.0, DotSun );

	// =============================================
	// Small radial blur for glare effect
// 	float4	SunPositionProj = mul( float4( SunDirection, 0.0 ), World2Proj );
// 	SunPositionProj /= SunPositionProj.w;
//	float2	Center = float2( 0.5 * (1.0 + SunPositionProj.x), 0.5 * (1.0 - SunPositionProj.y) );
// 
// 	float2	Direction = UV - Center;
// 	float	Distance2Center = length( Direction );
// 			Direction /= Distance2Center;
// 
// 	// Accumulate samples
// 	float3	SumExtinction = 0.0;
// 	for ( int i=0; i < 16; i++ )
// 	{
// 		float	SampleDistance = 0.1 * (i+0.5) / 16.0;
// 		float4	ExtinctionZ = VolumeTextureExtinction.SampleLevel( LinearClamp, Center + Direction * SampleDistance, 0 );
// 		SumExtinction += ExtinctionZ.xyz * step( Infinity, ExtinctionZ.w );
// 	}
// 	SumExtinction *= 1.0 / max( 1.0, 16.0 * 8.0 * Distance2Center );

	return float4( SunExtinction * _SunIntensity + InScattering + ExtinctionZ.xyz * SourceColor.xyz, SourceColor.w );
}


// ===================================================================================
// Renders the deep shadow map
//
struct SHADOW_VS_IN
{
	float4	Position		: SV_POSITION;
};

struct SHADOW_PS_OUT
{
	float4	SumDensity0 : SV_TARGET0;
#if defined(DEEP_SHADOW_MAP_HI_RES)
	float4	SumDensity1 : SV_TARGET1;
#endif
};

SHADOW_VS_IN	VS_Shadow( SHADOW_VS_IN _In ) { return _In; }

SHADOW_PS_OUT	PS_Shadow( SHADOW_VS_IN _In )
{
	float2	UV = _In.Position.xy * BufferInvSize;
	float3	WorldPosition = float3( ShadowRectangle.x + UV.x * ShadowRectangle.z, CloudPlaneHeightTop, ShadowRectangle.y + UV.y * ShadowRectangle.w );

	// Compute mip level that will depend on the resolution of the deep shadow map
	float	World2NoiseSize = NoiseSize / (CloudPlaneHeightTop - CloudPlaneHeightBottom);	// 1 world unit equals this amount of noise units
	float2	ShadowTexelSize = ShadowRectangle.zw * World2NoiseSize * BufferInvSize / 128.0;	// Size of a shadow texel in noise volume size
	float	MaxTexelSize = max( ShadowTexelSize.x, ShadowTexelSize.y );
	float	MipLevel = max( 0.0, log( MaxTexelSize ) * INV_LOG2 );

	// Accumulate 8 density levels into 2 render targets
	SHADOW_PS_OUT	Out;

	WorldPosition += 0.5 * ShadowVector;	// Start half a step within the cloud layer

	// 1st render target
	Out.SumDensity0.x = ComputeCloudDensity( WorldPosition, MipLevel );	WorldPosition += ShadowVector;
	Out.SumDensity0.y = ComputeCloudDensity( WorldPosition, MipLevel );	WorldPosition += ShadowVector;
	Out.SumDensity0.z = ComputeCloudDensity( WorldPosition, MipLevel );	WorldPosition += ShadowVector;
	Out.SumDensity0.w = ComputeCloudDensity( WorldPosition, MipLevel );	WorldPosition += ShadowVector;

#if defined(DEEP_SHADOW_MAP_HI_RES)
	// 2nd render target
	Out.SumDensity1.x = ComputeCloudDensity( WorldPosition, MipLevel );	WorldPosition += ShadowVector;
	Out.SumDensity1.y = ComputeCloudDensity( WorldPosition, MipLevel );	WorldPosition += ShadowVector;
	Out.SumDensity1.z = ComputeCloudDensity( WorldPosition, MipLevel );	WorldPosition += ShadowVector;
	Out.SumDensity1.w = ComputeCloudDensity( WorldPosition, MipLevel );
#endif

	return Out;
}

// ===================================================================================
technique10 Display
{
	pass P0
	{
		SetVertexShader( CompileShader( vs_4_0, VS() ) );
		SetGeometryShader( 0 );
		SetPixelShader( CompileShader( ps_4_0, PS_Compute() ) );
	}
}

technique10 Combine
{
	pass P0
	{
		SetVertexShader( CompileShader( vs_4_0, VS() ) );
		SetGeometryShader( 0 );
		SetPixelShader( CompileShader( ps_4_0, PS_Combine() ) );
	}
}

technique10 RenderDeepShadowMap
{
	pass P0
	{
		SetVertexShader( CompileShader( vs_4_0, VS_Shadow() ) );
		SetGeometryShader( 0 );
		SetPixelShader( CompileShader( ps_4_0, PS_Shadow() ) );
	}
}
