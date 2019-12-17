//========= Copyright 2020, HTC Corporation. All rights reserved. ===========

using UnityEngine;
#if USE_SRANIPAL
using ViveSR.anipal.Eye;
#endif

namespace HTC.UnityPlugin.FoveatedRendering
{
    public class ViveFoveatedGazeUpdater : MonoBehaviour
    {
        public static bool AttachGazeUpdater(GameObject obj)
        {
#if USE_SRANIPAL
            if (obj != null)
            {
                var gazeUpdater = obj.GetComponent<ViveFoveatedGazeUpdater>();
                if(gazeUpdater == null)
                {
                    gazeUpdater = obj.AddComponent<ViveFoveatedGazeUpdater>();
                }

                gazeUpdater.enabled = true;

                return true;
            }
#endif
            return false;
        }
        
        void OnEnable()
        {
#if USE_SRANIPAL
            if (SRanipal_Eye_Framework.Status != SRanipal_Eye_Framework.FrameworkStatus.WORKING)
            {
                var sranipal = SRanipal_Eye_Framework.Instance;
                if (sranipal == null)
                {
                    sranipal = gameObject.AddComponent<SRanipal_Eye_Framework>();
                }

                sranipal.StartFramework();
            }
#endif
        }
        
        void Update()
        {
#if USE_SRANIPAL
            if (SRanipal_Eye_Framework.Status == SRanipal_Eye_Framework.FrameworkStatus.WORKING)
            {
                VerboseData data;
                if (SRanipal_Eye.GetVerboseData(out data) &&
                    data.left.GetValidity(SingleEyeDataValidity.SINGLE_EYE_DATA_GAZE_DIRECTION_VALIDITY) &&
                    data.right.GetValidity(SingleEyeDataValidity.SINGLE_EYE_DATA_GAZE_DIRECTION_VALIDITY)
                    )
                {
                    ViveFoveatedRenderingAPI.SetNormalizedGazeDirection(data.left.gaze_direction_normalized, data.right.gaze_direction_normalized);
                    GL.IssuePluginEvent(ViveFoveatedRenderingAPI.GetRenderEventFunc(), (int)EventID.UPDATE_GAZE);
                }
            }
#endif
        }

    }
}