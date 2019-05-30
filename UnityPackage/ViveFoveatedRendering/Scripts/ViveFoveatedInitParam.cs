//========= Copyright 2019, HTC Corporation. All rights reserved. ===========

using UnityEngine;
using System.Collections.Generic;

namespace HTC.UnityPlugin.FoveatedRendering
{
    public class ViveFoveatedInitParam
    {
        const string VIVE_VID = "VID_0BB4";
        const string VIVE_PRO_PID = "PID_0309";
        const string VIVE_COSMOS_PID = "PID_0313";
        public static bool SetParamByHMD(ViveFoveatedRendering vfr, bool isEyeTracked = false)
        {
            if (vfr == null) { return false; }

            bool setParamSuccess = false;

            List<string> vids = new List<string>();
            List<string> pids = new List<string>();
            ViveFoveatedRenderingAPI.GetVidPid(str => vids.Add(str), str => pids.Add(str));

            for (int i = 0; i < vids.Count; i++)
            {
                if (vids[i] == VIVE_VID)
                {
                    if (pids[i] == VIVE_PRO_PID)
                    {
                        vfr.SetPatternPreset(ShadingPatternPreset.SHADING_PATTERN_CUSTOM);
                        vfr.SetRegionRadii(TargetArea.INNER, new Vector2(0.25f, 0.25f));
                        vfr.SetRegionRadii(TargetArea.MIDDLE, new Vector2(0.33f, 0.33f));
                        vfr.SetRegionRadii(TargetArea.PERIPHERAL, new Vector2(2.0f, 2.0f));

                        vfr.SetShadingRatePreset(ShadingRatePreset.SHADING_RATE_CUSTOM);
                        vfr.SetShadingRate(TargetArea.INNER, ShadingRate.X1_PER_PIXEL);
                        vfr.SetShadingRate(TargetArea.MIDDLE, ShadingRate.X1_PER_1X2_PIXELS);
                        vfr.SetShadingRate(TargetArea.PERIPHERAL, ShadingRate.X1_PER_2X2_PIXELS);

                        setParamSuccess = true;
                    }
                    else if (pids[i] == VIVE_COSMOS_PID)
                    {
                        vfr.SetPatternPreset(ShadingPatternPreset.SHADING_PATTERN_CUSTOM);
                        vfr.SetRegionRadii(TargetArea.INNER, new Vector2(0.33f, 0.33f));
                        vfr.SetRegionRadii(TargetArea.MIDDLE, new Vector2(0.6f, 0.6f));
                        vfr.SetRegionRadii(TargetArea.PERIPHERAL, new Vector2(2.0f, 2.0f));

                        vfr.SetShadingRatePreset(ShadingRatePreset.SHADING_RATE_CUSTOM);
                        vfr.SetShadingRate(TargetArea.INNER, ShadingRate.X1_PER_PIXEL);
                        vfr.SetShadingRate(TargetArea.MIDDLE, ShadingRate.X1_PER_1X2_PIXELS);
                        vfr.SetShadingRate(TargetArea.PERIPHERAL, ShadingRate.X1_PER_2X2_PIXELS);

                        setParamSuccess = true;
                    }
                }
            }

            return setParamSuccess;
        }
    }
}