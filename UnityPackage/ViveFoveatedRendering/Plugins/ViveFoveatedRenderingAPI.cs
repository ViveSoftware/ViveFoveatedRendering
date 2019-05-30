//========= Copyright 2019, HTC Corporation. All rights reserved. ===========

using UnityEngine;
using System;
using System.Runtime.InteropServices;

namespace HTC.UnityPlugin.FoveatedRendering
{
    public class ViveFoveatedRenderingAPI
    {
        const string LIB_NAME = "ViveFoveatedRendering";
		
		//	Unity events
        [DllImport(LIB_NAME)]
        static extern public IntPtr GetRenderEventFunc();
		
		//	VRS APIs
        [DllImport(LIB_NAME)]
        static extern public bool InitializeFoveatedRendering(float verticalFov, float aspectRatio);
        [DllImport(LIB_NAME)]
        static extern public void ReleaseFoveatedRendering();
        [DllImport(LIB_NAME)]
        static extern public void SetRenderMode(RenderMode mode);
        [DllImport(LIB_NAME)]
        static extern public void SetFoveatedRenderingPatternPreset(ShadingPatternPreset preset);
        [DllImport(LIB_NAME)]
        static extern public void SetFoveatedRenderingShadingRatePreset(ShadingRatePreset preset);
        [DllImport(LIB_NAME)]
        static extern public void SetRegionRadii(TargetArea targetArea, Vector2 radii);
        [DllImport(LIB_NAME)]
        static extern public void SetShadingRate(TargetArea targetArea, ShadingRate rate);
		[DllImport(LIB_NAME)]
        static extern public void SetNormalizedGazeDirection(Vector3 leftEyeDir, Vector3 rightEyeDir);

        //	Log APIs
        public delegate void UnityStrCallback(string str);
        [DllImport(LIB_NAME)]
        static extern public void InitializeNativeLogger(UnityStrCallback log);
        [DllImport(LIB_NAME)]
        static extern public void ReleaseNativeLogger();
        
        [DllImport(LIB_NAME)]
        static extern public void GetVidPid(UnityStrCallback addVidCallback, UnityStrCallback addPidCallback);
    }
}