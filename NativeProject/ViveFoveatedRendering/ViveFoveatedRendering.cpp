//========= Copyright 2020, HTC Corporation. All rights reserved. ===========

#include <d3d11.h>
#include ".\Unity\IUnityInterface.h"
#include ".\Unity\IUnityGraphics.h"
#include ".\Unity\IUnityGraphicsD3D11.h"
#include <nvapi.h>
#include <vector>
#include <setupapi.h>
#include <cfgmgr32.h>
#include <tchar.h>
#include "..\..\UnityPackage\ViveFoveatedRendering\Plugins\ViveFoveatedRenderingEnum.cs"

#define LOG nLog

using namespace std;

static const string PLUGIN_NAME = "ViveFoveatedRendering";
static const string VERSION = "1.0.1.0";

typedef void(__stdcall *UNITY_STR_CALLBACK)(const char*);
static UNITY_STR_CALLBACK s_logger = nullptr;

void nLog(string str) {
	if (s_logger != nullptr) {
		string log = PLUGIN_NAME + ": " + str;
		s_logger(log.c_str());
	} 
}

struct Vector2 {
	float x, y;
};
struct Vector3 {
	float x, y, z;
};

extern "C" {
	//	Unity events
	void UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API UnityPluginLoad(IUnityInterfaces* unityInterfaces);
	void UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API UnityPluginUnload();
	UnityRenderingEvent UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API GetRenderEventFunc();
	//	VRS APIs
	bool UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API InitializeFoveatedRendering(float verticalFov, float aspectRatio);
	void UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API ReleaseFoveatedRendering();
	void UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API SetRenderMode(RenderMode mode);
	void UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API SetFoveatedRenderingShadingRatePreset(ShadingRatePreset preset);
	void UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API SetFoveatedRenderingPatternPreset(ShadingPatternPreset preset);
	void UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API SetRegionRadii(TargetArea targetArea, Vector2 radii);
	void UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API SetShadingRate(TargetArea targetArea, ShadingRate rate);
	void UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API SetNormalizedGazeDirection(Vector3 leftEyeDir, Vector3 rightEyeDir);
	//	Other APIs
	void UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API InitializeNativeLogger(UNITY_STR_CALLBACK logCallback) { s_logger = logCallback; }
	void UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API ReleaseNativeLogger() { s_logger = nullptr; }
	void UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API GetVidPid(UNITY_STR_CALLBACK addVidCallback, UNITY_STR_CALLBACK addPidCallback);
};

static void UNITY_INTERFACE_API OnGraphicsDeviceEvent(UnityGfxDeviceEventType eventType);
static void UNITY_INTERFACE_API OnRenderEvent(int eventID);

//	Functions
static bool NVInit(ID3D11Device* device);
static bool InitializeFoveatedRendering(ID3D11Device* device, float hFov, float vFov);
static Vector2 CalcNormalizedGazeLocation(const Vector3& gazeDirNormalized, float offset);
static bool UpdateIfChanged(Vector2* inout, const Vector2& compared, float threshold);
static bool UpdateGazeData(ID3D11DeviceContext* deviceContext);
static void LatchGazeData(ID3D11DeviceContext* deviceContext);
static void EnableShadingRatePattern(ID3D11DeviceContext* deviceContext, NV_VRS_RENDER_MODE renderMode);
static void DisableShadingRatePattern(ID3D11DeviceContext* deviceContext);

//	System variables
static IUnityInterfaces* s_unityInterfaces = nullptr;
static IUnityGraphics* s_graphics = nullptr;
static ID3D11Device* s_device = nullptr;
static float s_tanHalfHorizontalFov = tanf(111.5129f / 2.0f);
static float s_tanHalfVerticalFov = tanf(111.5129f / 2.0f);

//	VRS related variables
static ID3DNvVRSHelper* g_vrsHelper = nullptr;
static ID3DNvGazeHandler* g_gazeHandler = nullptr;
static bool g_foveatedRenderingInitialized = false;
static bool g_gazeDataValidToUpdate = false;
static NV_VRS_RENDER_MODE g_vrsRenderMode = NV_VRS_RENDER_MODE_MONO;

static NV_FOVEATED_RENDERING_FOVEATION_PATTERN_PRESET g_foveatedRenderingPatternPreset = NV_FOVEATED_RENDERING_FOVEATION_PATTERN_PRESET_NARROW;
static float g_innerMostRegionRadii[2] = { 0.25f, 0.25f };
static float g_middleRegionRadii[2] = { 0.33f, 0.33f };
static float g_peripheralRegionRadii[2] = { 1.0f, 1.0f };

static NV_FOVEATED_RENDERING_SHADING_RATE_PRESET g_foveatedRenderingShadingRatePreset = NV_FOVEATED_RENDERING_SHADING_RATE_PRESET_HIGHEST_PERFORMANCE;
static NV_PIXEL_SHADING_RATE g_innerMostRegionShadingRate = NV_PIXEL_X1_PER_RASTER_PIXEL;
static NV_PIXEL_SHADING_RATE g_middleRegionShadingRate = NV_PIXEL_X1_PER_1X2_RASTER_PIXELS;
static NV_PIXEL_SHADING_RATE g_peripheralRegionShadingRate = NV_PIXEL_X1_PER_2X2_RASTER_PIXELS;

static Vector2 g_normalizedGazePosLeft = { 0.0f, 0.0f };	//	Normalized position, range from -0.5 to 0.5 with origin (0, 0).
static Vector2 g_normalizedGazePosRight = { 0.0f, 0.0f };	//	Normalized position, range from -0.5 to 0.5 with origin (0, 0).
static float g_gazeStability = 0.01f;						//	Threshold of gaze pose distance to update VRS gaze data.

void UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API UnityPluginLoad(IUnityInterfaces* unityInterfaces) {
	s_unityInterfaces = unityInterfaces;
	s_graphics = s_unityInterfaces->Get<IUnityGraphics>();
	s_graphics->RegisterDeviceEventCallback(OnGraphicsDeviceEvent);

	OnGraphicsDeviceEvent(kUnityGfxDeviceEventInitialize);
}

void UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API UnityPluginUnload() {
	if (s_device) {
		s_device->Release();
		s_device = nullptr;
	}
	
	s_graphics->UnregisterDeviceEventCallback(OnGraphicsDeviceEvent);
	s_graphics = nullptr;

	s_unityInterfaces = nullptr;
}

UnityRenderingEvent UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API GetRenderEventFunc() {
	return OnRenderEvent;
}

bool UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API InitializeFoveatedRendering(float verticalFov, float aspectRatio) {
	if (s_device && !g_foveatedRenderingInitialized) {
		LOG(VERSION);
		LOG("Initialize foveated rendering.");

		const float DEG2RAD = 0.01745329f;

		s_tanHalfVerticalFov = tanf(DEG2RAD * verticalFov / 2.0f);
		s_tanHalfHorizontalFov = s_tanHalfVerticalFov * aspectRatio;

		float horizontalFov = 2.0f * atan(s_tanHalfHorizontalFov) / DEG2RAD;
		g_foveatedRenderingInitialized = InitializeFoveatedRendering(s_device, horizontalFov, verticalFov);
	}

	return g_foveatedRenderingInitialized;
}

void UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API ReleaseFoveatedRendering() {
	LOG("Release foveated rendering.");
	if (g_foveatedRenderingInitialized) {
		g_vrsHelper = nullptr;
		g_gazeHandler = nullptr;
		g_foveatedRenderingInitialized = false;
	}
}

template<typename T>
T Clamp(const T& input, const T& lower, const T& upper) {
	return max(min(input, upper), lower);
}

void UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API SetRenderMode(RenderMode mode) {
	g_vrsRenderMode = Clamp(
		(NV_VRS_RENDER_MODE)mode,
		NV_VRS_RENDER_MODE_MONO,
		NV_VRS_RENDER_MODE_MAX);
}

void UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API SetFoveatedRenderingShadingRatePreset(ShadingRatePreset preset) {
	g_foveatedRenderingShadingRatePreset = Clamp(
		(NV_FOVEATED_RENDERING_SHADING_RATE_PRESET)preset,
		NV_FOVEATED_RENDERING_SHADING_RATE_PRESET_HIGHEST_PERFORMANCE,
		NV_FOVEATED_RENDERING_SHADING_RATE_PRESET_CUSTOM);

	g_gazeDataValidToUpdate = true;
}

void UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API SetFoveatedRenderingPatternPreset(ShadingPatternPreset preset) {
	g_foveatedRenderingPatternPreset = Clamp(
		(NV_FOVEATED_RENDERING_FOVEATION_PATTERN_PRESET)preset,
		NV_FOVEATED_RENDERING_FOVEATION_PATTERN_PRESET_WIDE,
		NV_FOVEATED_RENDERING_FOVEATION_PATTERN_PRESET_CUSTOM);

	g_gazeDataValidToUpdate = true;
}

void UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API SetRegionRadii(TargetArea targetArea, Vector2 radii) {
	float clampedX = Clamp(radii.x, 0.01f, 10.0f);
	float clampedY = Clamp(radii.y, 0.01f, 10.0f);

	switch (targetArea) {
	case INNER:
		g_innerMostRegionRadii[0] = clampedX;
		g_innerMostRegionRadii[1] = clampedY;
		break;
	case MIDDLE:
		g_middleRegionRadii[0] = clampedX;
		g_middleRegionRadii[1] = clampedY;
		break;
	case PERIPHERAL:
	default:
		g_peripheralRegionRadii[0] = clampedX;
		g_peripheralRegionRadii[1] = clampedY;
		break;
	}

	g_gazeDataValidToUpdate = true;
}

void UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API SetShadingRate(TargetArea targetArea, ShadingRate rate) {
	NV_PIXEL_SHADING_RATE clampedRate = Clamp(
		(NV_PIXEL_SHADING_RATE)rate,
		NV_PIXEL_X0_CULL_RASTER_PIXELS,
		NV_PIXEL_X1_PER_4X4_RASTER_PIXELS);

	switch (targetArea) {
	case INNER:
		g_innerMostRegionShadingRate = clampedRate;
		break;
	case MIDDLE:
		g_middleRegionShadingRate = clampedRate;
		break;
	case PERIPHERAL:
	default:
		g_peripheralRegionShadingRate = clampedRate;
		break;
	}

	g_gazeDataValidToUpdate = true;
}

void UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API SetNormalizedGazeDirection(Vector3 leftGazeDirNormalized, Vector3 rightGazeDirNormalized) {
	float offset = g_vrsRenderMode == NV_VRS_RENDER_MODE::NV_VRS_RENDER_MODE_MONO ? 0.0f : 0.05f;
	Vector2 leftPos = CalcNormalizedGazeLocation(leftGazeDirNormalized, -offset);
	Vector2 rightPos = CalcNormalizedGazeLocation(rightGazeDirNormalized, offset);

	bool isLeftChanged = UpdateIfChanged(&g_normalizedGazePosLeft, leftPos, g_gazeStability);
	bool isRightChanged = UpdateIfChanged(&g_normalizedGazePosRight, rightPos, g_gazeStability);

	g_gazeDataValidToUpdate = isLeftChanged || isRightChanged;
}

void UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API GetVidPid(UNITY_STR_CALLBACK addVidCallback, UNITY_STR_CALLBACK addPidCallback) {
	if (addVidCallback == nullptr || addPidCallback == nullptr) {
		return;
	}

	const LPCTSTR USB = TEXT("USB");
	const LPCTSTR VID_PREFIX = TEXT("VID_");
	const LPCTSTR PID_PREFIX = TEXT("PID_");

	HDEVINFO devInfo = SetupDiGetClassDevs(NULL, USB, NULL, DIGCF_ALLCLASSES | DIGCF_PRESENT);
	if (devInfo == INVALID_HANDLE_VALUE) {
		return;
	}

	SP_DEVINFO_DATA devInfoData = {};
	devInfoData.cbSize = sizeof(devInfoData);
	for (int i = 0; SetupDiEnumDeviceInfo(devInfo, i, &devInfoData); i++) {
		TCHAR deviceInstanceID[MAX_DEVICE_ID_LEN];
		if (CR_SUCCESS != CM_Get_Device_ID(devInfoData.DevInst, deviceInstanceID, MAX_PATH, 0)) {
			continue;
		}

		LPTSTR nextToken = NULL;
		LPTSTR token = _tcstok_s(deviceInstanceID, TEXT("\\#&"), &nextToken);
		if (_tcsncmp(token, USB, lstrlen(USB)) == 0) {
			TCHAR vid[MAX_DEVICE_ID_LEN] = {};
			TCHAR pid[MAX_DEVICE_ID_LEN] = {};
			while (NULL != (token = _tcstok_s(NULL, TEXT("\\#&"), &nextToken))) {
				if (_tcsncmp(token, VID_PREFIX, lstrlen(VID_PREFIX)) == 0) {
					_tcscpy_s(vid, sizeof(vid) / sizeof(vid[0]), token);
				} else if (_tcsncmp(token, PID_PREFIX, lstrlen(PID_PREFIX)) == 0) {
					_tcscpy_s(pid, sizeof(pid) / sizeof(pid[0]), token);
				}
			}

			basic_string<TCHAR> vidString(vid);
			basic_string<TCHAR> pidString(pid);
			addVidCallback(string(vidString.begin(), vidString.end()).c_str());
			addPidCallback(string(pidString.begin(), pidString.end()).c_str());
		}
	}
}

void UNITY_INTERFACE_API OnGraphicsDeviceEvent(UnityGfxDeviceEventType eventType) {
	switch (eventType) {
	case kUnityGfxDeviceEventInitialize:
		if (s_unityInterfaces) {
			s_device = s_unityInterfaces->Get<IUnityGraphicsD3D11>()->GetDevice();
			NVInit(s_device);
		}
		break;
	case kUnityGfxDeviceEventShutdown:
		NvAPI_Unload();
		break;
	}
}

void UNITY_INTERFACE_API OnRenderEvent(int eventID) {
	if (s_device && g_foveatedRenderingInitialized) {
		ID3D11DeviceContext* deviceContext = nullptr;
		s_device->GetImmediateContext(&deviceContext);

		switch (eventID) {
		case ENABLE_FOVEATED_RENDERING:
			EnableShadingRatePattern(deviceContext, g_vrsRenderMode);
			break;
		case DISABLE_FOVEATED_RENDERING:
			DisableShadingRatePattern(deviceContext);
			break;
		case UPDATE_GAZE:
			if (UpdateGazeData(deviceContext)) {
				LatchGazeData(deviceContext);
			}
			
			break;
		}

		deviceContext->Release();
	}
}

static bool NVInit(ID3D11Device* device) {

	NvAPI_Status NvStatus = NvAPI_Initialize();
	if (NvStatus != NvAPI_Status::NVAPI_OK) {
		LOG("NvAPI init fail.");
		return false;
	}

	NvStatus = NvAPI_D3D_RegisterDevice(device);
	if (NvStatus != NvAPI_Status::NVAPI_OK) {
		LOG("NvAPI register device fail.");
		return false;		
	}

	return true;
}

static bool InitializeFoveatedRendering(ID3D11Device* device, float hFov = 110.0f, float vFov = 110.0f) {
	NV_VRS_HELPER_INIT_PARAMS vrsHelperInitParams = {};
	vrsHelperInitParams.version = NV_VRS_HELPER_INIT_PARAMS_VER;
	vrsHelperInitParams.ppVRSHelper = &g_vrsHelper;
	NvAPI_Status NvStatus = NvAPI_D3D_InitializeVRSHelper(device, &vrsHelperInitParams);
	if (NvStatus != NVAPI_OK) {
		LOG("VRS Helper is not supported on this setup.");
		return false;
	}

	NV_GAZE_HANDLER_INIT_PARAMS gazeHandlerInitParams = {};
	gazeHandlerInitParams.version = NV_GAZE_HANDLER_INIT_PARAMS_VER;

	gazeHandlerInitParams.GazeDataDeviceId = 0;
	gazeHandlerInitParams.GazeDataType = NV_GAZE_DATA_STEREO;
	gazeHandlerInitParams.fHorizontalFOV = hFov;
	gazeHandlerInitParams.fVericalFOV = vFov;

	gazeHandlerInitParams.ppNvGazeHandler = &g_gazeHandler;

	NvStatus = NvAPI_D3D_InitializeNvGazeHandler(device, &gazeHandlerInitParams);
	if (NvStatus != NVAPI_OK) {
		LOG("Foveated rendering gaze data handler is not supported on this setup.");
		return false;
	}

	const NvU32 NV_SHADER_EXTN_SLOT_NUMBER = 7;
	NvStatus = NvAPI_D3D11_SetNvShaderExtnSlot(device, NV_SHADER_EXTN_SLOT_NUMBER);
	if (NvStatus != NVAPI_OK) {
		LOG("Set Nvidia shader extension error.");
		return false;
	}

	g_gazeDataValidToUpdate = true;

	LOG("Init foveated rendering success.");

	return true;
}

static void LatchGazeData(ID3D11DeviceContext* deviceContext)
{
	if (g_vrsHelper && deviceContext) {
		NV_VRS_HELPER_LATCH_GAZE_PARAMS latchGazeParams = {};
		latchGazeParams.version = NV_VRS_HELPER_LATCH_GAZE_PARAMS_VER;
		NvAPI_Status NvStatus = g_vrsHelper->LatchGaze(deviceContext, &latchGazeParams);
		if (NvStatus != NVAPI_OK) {
			LOG("Latch gaze error.");
		}
	}
}

//	gaze_direction_normalized is a vector which cast toward to +z direction under right-hand coordinate system.
//	Since Unity is uses Left-hand system in Windows, the X should be inversed.
static Vector2 CalcNormalizedGazeLocation(const Vector3& gazeDirNormalized, float offset) {
	float tanNormalized[2] = {
		(gazeDirNormalized.x / gazeDirNormalized.z) / s_tanHalfHorizontalFov,
		(gazeDirNormalized.y / gazeDirNormalized.z) / s_tanHalfVerticalFov
	};
	
	tanNormalized[0] += offset;

	return{ -tanNormalized[0] / 2.0f, tanNormalized[1] / 2.0f };
}

static bool UpdateIfChanged(Vector2* inout, const Vector2& compared, float threshold) {
	if (abs(inout->x - compared.x) <= threshold &&
		abs(inout->y - compared.y) <= threshold) {
		return false;
	}

	inout->x = compared.x;
	inout->y = compared.y;
	return true;
}

static bool UpdateGazeData(ID3D11DeviceContext* deviceContext)
{
	if (g_gazeHandler) {
		if (g_gazeDataValidToUpdate) {
			static unsigned long long gazeTimestamp = 0;
			NV_FOVEATED_RENDERING_UPDATE_GAZE_DATA_PARAMS gazeDataParams = {};
			gazeDataParams.version = NV_FOVEATED_RENDERING_UPDATE_GAZE_DATA_PARAMS_VER;
			gazeDataParams.Timestamp = ++gazeTimestamp;

			auto leftEyePtr = &(gazeDataParams.sStereoData.sLeftEye);
			leftEyePtr->version = NV_FOVEATED_RENDERING_GAZE_DATA_PER_EYE_VER;
			leftEyePtr->fGazeNormalizedLocation[0] = g_normalizedGazePosLeft.x;
			leftEyePtr->fGazeNormalizedLocation[1] = g_normalizedGazePosLeft.y;
			leftEyePtr->GazeDataValidityFlags = NV_GAZE_LOCATION_VALID;

			auto rightEyePtr = &(gazeDataParams.sStereoData.sRightEye);
			rightEyePtr->version = NV_FOVEATED_RENDERING_GAZE_DATA_PER_EYE_VER;
			rightEyePtr->fGazeNormalizedLocation[0] = g_normalizedGazePosRight.x;
			rightEyePtr->fGazeNormalizedLocation[1] = g_normalizedGazePosRight.y;
			rightEyePtr->GazeDataValidityFlags = NV_GAZE_LOCATION_VALID;

			NvAPI_Status NvStatus = g_gazeHandler->UpdateGazeData(deviceContext, &gazeDataParams);
			if (NvStatus != NVAPI_OK) {
				LOG("Update gaze data error.");
			}

			g_gazeDataValidToUpdate = false;

			return true;
		}
	}

	return false;
}

static void EnableShadingRatePattern(ID3D11DeviceContext* deviceContext, NV_VRS_RENDER_MODE renderMode)
{
	if (g_vrsHelper && deviceContext) {
		NV_VRS_HELPER_ENABLE_PARAMS g_vrsHelperEnableParams = {};
		g_vrsHelperEnableParams.version = NV_VRS_HELPER_ENABLE_PARAMS_VER;
		g_vrsHelperEnableParams.RenderMode = renderMode;
		g_vrsHelperEnableParams.ContentType = NV_VRS_CONTENT_TYPE_FOVEATED_RENDERING;
		g_vrsHelperEnableParams.sFoveatedRenderingDesc.version = NV_FOVEATED_RENDERING_DESC_VER;

		g_vrsHelperEnableParams.sFoveatedRenderingDesc.ShadingRatePreset = g_foveatedRenderingShadingRatePreset;
		if (g_foveatedRenderingShadingRatePreset == NV_FOVEATED_RENDERING_SHADING_RATE_PRESET_CUSTOM) {
			auto customPresetDesc = &(g_vrsHelperEnableParams.sFoveatedRenderingDesc.ShadingRateCustomPresetDesc);
			customPresetDesc->version = NV_FOVEATED_RENDERING_CUSTOM_SHADING_RATE_PRESET_DESC_VER1;
			customPresetDesc->InnerMostRegionShadingRate = g_innerMostRegionShadingRate;
			customPresetDesc->MiddleRegionShadingRate = g_middleRegionShadingRate;
			customPresetDesc->PeripheralRegionShadingRate = g_peripheralRegionShadingRate;
		}

		g_vrsHelperEnableParams.sFoveatedRenderingDesc.FoveationPatternPreset = g_foveatedRenderingPatternPreset;
		if (g_foveatedRenderingPatternPreset == NV_FOVEATED_RENDERING_FOVEATION_PATTERN_PRESET_CUSTOM) {
			auto customPresetDesc = &(g_vrsHelperEnableParams.sFoveatedRenderingDesc.FoveationPatternCustomPresetDesc);
			customPresetDesc->version = NV_FOVEATED_RENDERING_CUSTOM_FOVEATION_PATTERN_PRESET_DESC_VER1;
			customPresetDesc->fInnermostRadii[0] = g_innerMostRegionRadii[0];
			customPresetDesc->fInnermostRadii[1] = g_innerMostRegionRadii[1];
			customPresetDesc->fMiddleRadii[0] = g_middleRegionRadii[0];
			customPresetDesc->fMiddleRadii[1] = g_middleRegionRadii[1];
			customPresetDesc->fPeripheralRadii[0] = g_peripheralRegionRadii[0];
			customPresetDesc->fPeripheralRadii[1] = g_peripheralRegionRadii[1];
		}

		NvAPI_Status NvStatus = g_vrsHelper->Enable(deviceContext, &g_vrsHelperEnableParams);
		if (NvStatus != NVAPI_OK) {
			LOG("Enable VRS error");
		}
	}
}

static void DisableShadingRatePattern(ID3D11DeviceContext* deviceContext)
{
	if (g_vrsHelper && deviceContext) {
		NV_VRS_HELPER_DISABLE_PARAMS disableParams = {};
		disableParams.version = NV_VRS_HELPER_DISABLE_PARAMS_VER;
		NvAPI_Status NvStatus = g_vrsHelper->Disable(deviceContext, &disableParams);
		if (NvStatus != NVAPI_OK) {
			LOG("Disable VRS error");
		}
	}
}