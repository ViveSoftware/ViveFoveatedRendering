//========= Copyright 2020, HTC Corporation. All rights reserved. ===========

using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.XR;
using System;

namespace HTC.UnityPlugin.FoveatedRendering
{
    public static class FoveatedRenderingExtensions
    {
        public static T Clamp<T>(this T input, T min, T max) where T : IComparable
        {
            if (min.CompareTo(input) > 0)
            { return min; }
            else if (max.CompareTo(input) < 0)
            { return max; }

            return input;
        }
    }

    [RequireComponent(typeof(Camera))]
    public class ViveFoveatedRendering : MonoBehaviour
    {
        Camera thisCamera = null;
        CommandBufferManager commandBufferMgr = new CommandBufferManager();

        bool foveatedRenderingInited = false;
        bool foveatedRenderingActivated = false;
        RenderMode renderMode = RenderMode.RENDER_MODE_MONO;

        [SerializeField]
        bool manualAdjustment = false;

        [SerializeField]
        ShadingRatePreset shadingRatePreset = ShadingRatePreset.SHADING_RATE_HIGHEST_PERFORMANCE;
        [SerializeField]
        ShadingPatternPreset patternPreset = ShadingPatternPreset.SHADING_PATTERN_NARROW;

        [SerializeField]
        Vector2 innerRegionRadii = new Vector2(0.25f, 0.25f);
        [SerializeField]
        Vector2 middleRegionRadii = new Vector2(0.33f, 0.33f);
        [SerializeField]
        Vector2 peripheralRegionRadii = new Vector2(1.0f, 1.0f);

        [SerializeField]
        ShadingRate innerShadingRate = ShadingRate.X1_PER_PIXEL;
        [SerializeField]
        ShadingRate middleShadingRate = ShadingRate.X1_PER_2X2_PIXELS;
        [SerializeField]
        ShadingRate peripheralShadingRate = ShadingRate.X1_PER_4X4_PIXELS;
        
        public void EnableFoveatedRendering(bool activate)
        {
            if (foveatedRenderingInited && activate != foveatedRenderingActivated)
            {
                foveatedRenderingActivated = activate;
                if (activate)
                {
                    commandBufferMgr.EnableCommands(thisCamera);
                }
                else
                {
                    commandBufferMgr.DisableCommands(thisCamera);
                }
            }
        }

        public void SetShadingRatePreset(ShadingRatePreset inputPreset)
        {
            if (foveatedRenderingInited)
            {
                shadingRatePreset = inputPreset.Clamp(ShadingRatePreset.SHADING_RATE_HIGHEST_PERFORMANCE, ShadingRatePreset.SHADING_RATE_MAX);
                ViveFoveatedRenderingAPI.SetFoveatedRenderingShadingRatePreset(shadingRatePreset);

                if (shadingRatePreset == ShadingRatePreset.SHADING_RATE_CUSTOM)
                {
                    SetShadingRate(TargetArea.INNER, innerShadingRate);
                    SetShadingRate(TargetArea.MIDDLE, middleShadingRate);
                    SetShadingRate(TargetArea.PERIPHERAL, peripheralShadingRate);
                }

                GL.IssuePluginEvent(ViveFoveatedRenderingAPI.GetRenderEventFunc(), (int)EventID.UPDATE_GAZE);
            }
        }

        public ShadingRatePreset GetShadingRatePreset()
        {
            return shadingRatePreset;
        }

        public void SetPatternPreset(ShadingPatternPreset inputPreset)
        {
            if (foveatedRenderingInited)
            {
                patternPreset = inputPreset.Clamp(ShadingPatternPreset.SHADING_PATTERN_WIDE, ShadingPatternPreset.SHADING_PATTERN_MAX);
                ViveFoveatedRenderingAPI.SetFoveatedRenderingPatternPreset(patternPreset);

                if (patternPreset == ShadingPatternPreset.SHADING_PATTERN_CUSTOM)
                {
                    SetRegionRadii(TargetArea.INNER, innerRegionRadii);
                    SetRegionRadii(TargetArea.MIDDLE, middleRegionRadii);
                    SetRegionRadii(TargetArea.PERIPHERAL, peripheralRegionRadii);
                }

                GL.IssuePluginEvent(ViveFoveatedRenderingAPI.GetRenderEventFunc(), (int)EventID.UPDATE_GAZE);
            }
        }

        public ShadingPatternPreset GetPatternPreset()
        {
            return patternPreset;
        }

        public void SetShadingRate(TargetArea targetArea, ShadingRate rate)
        {
            if (foveatedRenderingInited)
            {
                var clampedRate = rate.Clamp(ShadingRate.CULL, ShadingRate.X1_PER_4X4_PIXELS);
                switch (targetArea)
                {
                    case TargetArea.INNER:
                        innerShadingRate = clampedRate;
                        break;
                    case TargetArea.MIDDLE:
                        middleShadingRate = clampedRate;
                        break;
                    case TargetArea.PERIPHERAL:
                        peripheralShadingRate = clampedRate;
                        break;
                }

                ViveFoveatedRenderingAPI.SetShadingRate(targetArea, clampedRate);
                GL.IssuePluginEvent(ViveFoveatedRenderingAPI.GetRenderEventFunc(), (int)EventID.UPDATE_GAZE);
            }
        }

        public ShadingRate GetShadingRate(TargetArea targetArea)
        {
            switch (targetArea)
            {
                case TargetArea.INNER:
                    return innerShadingRate;
                case TargetArea.MIDDLE:
                    return middleShadingRate;
                case TargetArea.PERIPHERAL:
                    return peripheralShadingRate;
            }

            return ShadingRate.CULL;
        }

        public void SetRegionRadii(TargetArea targetArea, Vector2 radii)
        {
            if (foveatedRenderingInited)
            {
                var clampedRadii = new Vector2(radii.x.Clamp(0.01f, 10.0f), radii.y.Clamp(0.01f, 10.0f));
                switch (targetArea)
                {
                    case TargetArea.INNER:
                        innerRegionRadii = clampedRadii;
                        break;
                    case TargetArea.MIDDLE:
                        middleRegionRadii = clampedRadii;
                        break;
                    case TargetArea.PERIPHERAL:
                        peripheralRegionRadii = clampedRadii;
                        break;
                }

                ViveFoveatedRenderingAPI.SetRegionRadii(targetArea, clampedRadii);
                GL.IssuePluginEvent(ViveFoveatedRenderingAPI.GetRenderEventFunc(), (int)EventID.UPDATE_GAZE);
            }
        }

        public Vector2 GetRegionRadii(TargetArea targetArea)
        {
            switch (targetArea)
            {
                case TargetArea.INNER:
                    return innerRegionRadii;
                case TargetArea.MIDDLE:
                    return middleRegionRadii;
                case TargetArea.PERIPHERAL:
                    return peripheralRegionRadii;
            }

            return Vector2.zero;
        }

        void OnEnable()
        {
			ViveFoveatedRenderingAPI.InitializeNativeLogger(str => Debug.Log(str));

            thisCamera = GetComponent<Camera>();
            foveatedRenderingInited = ViveFoveatedRenderingAPI.InitializeFoveatedRendering(thisCamera.fieldOfView, thisCamera.aspect);
            if (foveatedRenderingInited)
            {
                var currentRenderingPath = thisCamera.actualRenderingPath;
                if (currentRenderingPath == RenderingPath.Forward)
                {
                    commandBufferMgr.AppendCommands("Enable Foveated Rendering", CameraEvent.BeforeForwardOpaque,
                        buf => buf.IssuePluginEvent(ViveFoveatedRenderingAPI.GetRenderEventFunc(), (int)EventID.ENABLE_FOVEATED_RENDERING),
                        buf => buf.ClearRenderTarget(false, true, Color.black));

                    commandBufferMgr.AppendCommands("Disable Foveated Rendering", CameraEvent.AfterForwardAlpha,
                        buf => buf.IssuePluginEvent(ViveFoveatedRenderingAPI.GetRenderEventFunc(), (int)EventID.DISABLE_FOVEATED_RENDERING));
                }
                else if (currentRenderingPath == RenderingPath.DeferredShading)
                {
                    commandBufferMgr.AppendCommands("Enable Foveated Rendering - GBuffer", CameraEvent.BeforeGBuffer,
                        buf => buf.IssuePluginEvent(ViveFoveatedRenderingAPI.GetRenderEventFunc(), (int)EventID.ENABLE_FOVEATED_RENDERING),
                        buf => buf.ClearRenderTarget(false, true, Color.black));

                    commandBufferMgr.AppendCommands("Disable Foveated Rendering - GBuffer", CameraEvent.AfterGBuffer,
                        buf => buf.IssuePluginEvent(ViveFoveatedRenderingAPI.GetRenderEventFunc(), (int)EventID.DISABLE_FOVEATED_RENDERING));

                    commandBufferMgr.AppendCommands("Enable Foveated Rendering - Alpha", CameraEvent.BeforeForwardAlpha,
                        buf => buf.IssuePluginEvent(ViveFoveatedRenderingAPI.GetRenderEventFunc(), (int)EventID.ENABLE_FOVEATED_RENDERING));

                    commandBufferMgr.AppendCommands("Disable Foveated Rendering - Alpha", CameraEvent.AfterForwardAlpha,
                        buf => buf.IssuePluginEvent(ViveFoveatedRenderingAPI.GetRenderEventFunc(), (int)EventID.DISABLE_FOVEATED_RENDERING));
                }
                
                EnableFoveatedRendering(true);
                bool isEyeTracked = ViveFoveatedGazeUpdater.AttachGazeUpdater(gameObject);
                if (manualAdjustment || (!ViveFoveatedInitParam.SetParamByHMD(this, isEyeTracked)))
                {
                    SetShadingRatePreset(shadingRatePreset);
                    SetPatternPreset(patternPreset);
                }

                ViveFoveatedRenderingAPI.SetNormalizedGazeDirection(new Vector3(0.0f, 0.0f, 1.0f), new Vector3(0.0f, 0.0f, 1.0f));
                GL.IssuePluginEvent(ViveFoveatedRenderingAPI.GetRenderEventFunc(), (int)EventID.UPDATE_GAZE);
            }
        }

        void OnDisable()
        {
            EnableFoveatedRendering(false);
            commandBufferMgr.ClearCommands();
			
			ViveFoveatedRenderingAPI.ReleaseFoveatedRendering();
            ViveFoveatedRenderingAPI.ReleaseNativeLogger();

            foveatedRenderingInited = false;

            var gazeUpdater = GetComponent<ViveFoveatedGazeUpdater>();
            if (gazeUpdater != null)
            {
                gazeUpdater.enabled = false;
            }
        }

        void OnPreRender()
        {
            if (XRSettings.enabled)
            {
                switch (XRSettings.stereoRenderingMode)
                {
                    case XRSettings.StereoRenderingMode.MultiPass:
                        renderMode = thisCamera.stereoActiveEye == Camera.MonoOrStereoscopicEye.Left ?
                        RenderMode.RENDER_MODE_LEFT_EYE : RenderMode.RENDER_MODE_RIGHT_EYE;
                        break;
                    case XRSettings.StereoRenderingMode.SinglePass:
                        renderMode = RenderMode.RENDER_MODE_STEREO;
                        break;
                    default:
                        renderMode = RenderMode.RENDER_MODE_MONO;
                        break;
                }
            }
            else
            {
                renderMode = RenderMode.RENDER_MODE_MONO;
            }

            ViveFoveatedRenderingAPI.SetRenderMode(renderMode);
        }
    }
}