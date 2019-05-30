//========= Copyright 2019, HTC Corporation. All rights reserved. ===========

using UnityEngine;
using HTC.UnityPlugin.FoveatedRendering;
#if USE_SRANIPAL
using ViveSR.anipal.Eye;
#endif

[RequireComponent(typeof(ViveFoveatedRendering))]
public class ViveFoveatedVisualizer : MonoBehaviour
{
    ViveFoveatedRendering viveFoveatedRendering = null;
    Material visualizeMat = null;
    Camera thisCamera = null;
    Vector3 normalizedGazeDirection = new Vector3(0.0f, 0.0f, 1.0f);
    Vector2 eyeResolution = Vector2.one;

    void Start()
    {
        thisCamera = GetComponent<Camera>();
        viveFoveatedRendering = GetComponent<ViveFoveatedRendering>();
        visualizeMat = new Material(Shader.Find("Hidden/VisualizeViveFoveatedRendering"));
        if (UnityEngine.XR.XRSettings.enabled)
        {
            eyeResolution.x = UnityEngine.XR.XRSettings.eyeTextureWidth;
            eyeResolution.y = UnityEngine.XR.XRSettings.eyeTextureHeight;
        }
        else
        {
            eyeResolution.x = Screen.width;
            eyeResolution.y = Screen.height;
        }
    }

    void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        if (visualizeMat != null)
        {
#if USE_SRANIPAL
            if (SRanipal_Eye_Framework.Status == SRanipal_Eye_Framework.FrameworkStatus.WORKING)
            {
                VerboseData data;
                SRanipal_Eye.GetVerboseData(out data);
                SingleEyeData targetEyeData;
                if (UnityEditor.PlayerSettings.stereoRenderingPath == UnityEditor.StereoRenderingPath.MultiPass)
                {
                    targetEyeData = thisCamera.stereoActiveEye == Camera.MonoOrStereoscopicEye.Left ? data.left : data.right;
                }
                else
                {
                    targetEyeData = data.combined.eye_data;
                }

                if (targetEyeData.GetValidity(SingleEyeDataValidity.SINGLE_EYE_DATA_GAZE_DIRECTION_VALIDITY))
                {
                    normalizedGazeDirection = targetEyeData.gaze_direction_normalized;
                }
            }
#endif
            float tanHalfVerticalFov = Mathf.Tan(Mathf.Deg2Rad * thisCamera.fieldOfView / 2.0f);
            float tanHalfHorizontalFov = tanHalfVerticalFov * thisCamera.aspect;

            Vector2 gazeData = Vector2.zero;
            gazeData.x = (normalizedGazeDirection.x / normalizedGazeDirection.z) / tanHalfHorizontalFov;
            gazeData.y = (normalizedGazeDirection.y / normalizedGazeDirection.z) / tanHalfVerticalFov;
            gazeData.x = -gazeData.x;

            gazeData = (gazeData + Vector2.one) / 2.0f;
            visualizeMat.SetVector("_GazeData", new Vector4(gazeData.x, gazeData.y, 0, 0));

            var innerRadii = viveFoveatedRendering.GetRegionRadii(TargetArea.INNER) * 0.5f;
            var middleRadii = viveFoveatedRendering.GetRegionRadii(TargetArea.MIDDLE) * 0.5f;
            var peripheralRadii = viveFoveatedRendering.GetRegionRadii(TargetArea.PERIPHERAL) * 0.5f;
                        
            //  To keep the shape of given region, invert the aspect ratio.
            //  Align short side.
            if (eyeResolution.x > eyeResolution.y)
            {
                float ratio = eyeResolution.y / eyeResolution.x;
                innerRadii.x *= ratio;
                middleRadii.x *= ratio;
                peripheralRadii.x *= ratio;
            }
            else
            {
                float ratio = eyeResolution.x / eyeResolution.y;
                innerRadii.y *= ratio;
                middleRadii.y *= ratio;
                peripheralRadii.y *= ratio;
            }

            //  For calculation convenience with single pass stereo mode, the x range in UV space is divided by 2.
            Vector4 gazePointRadii = new Vector4(0.005f, 0.005f, 0.0f, 0.0f);
            if (UnityEditor.PlayerSettings.stereoRenderingPath == UnityEditor.StereoRenderingPath.SinglePass)
            {
                gazePointRadii.x *= 0.5f;
                innerRadii.x *= 0.5f;
                middleRadii.x *= 0.5f;
                peripheralRadii.x *= 0.5f;
            }

            visualizeMat.SetVector("_GazePointRadii", gazePointRadii);
            visualizeMat.SetVector("_InnerRadii", new Vector4(innerRadii.x, innerRadii.y, 0, 0));
            visualizeMat.SetVector("_MiddleRadii", new Vector4(middleRadii.x, middleRadii.y, 0, 0));
            visualizeMat.SetVector("_PeripheralRadii", new Vector4(peripheralRadii.x, peripheralRadii.y, 0, 0));

            Graphics.Blit(source, destination, visualizeMat);
        }
        else
        {
            Graphics.Blit(source, destination);
        }
    }
}
