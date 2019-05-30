//========= Copyright 2019, HTC Corporation. All rights reserved. ===========

#if __cplusplus
#define public
#else
namespace HTC.UnityPlugin.FoveatedRendering
{
#endif
    
	public enum EventID {
		ENABLE_FOVEATED_RENDERING, DISABLE_FOVEATED_RENDERING, UPDATE_GAZE
	};

	public enum TargetArea {
		INNER, MIDDLE, PERIPHERAL
	};
   
    public enum RenderMode
    {
        RENDER_MODE_INVALID = 0,
        RENDER_MODE_MONO = 1,           
        RENDER_MODE_LEFT_EYE = 2,       
        RENDER_MODE_RIGHT_EYE = 3,      
        RENDER_MODE_STEREO = 4,             
        RENDER_MODE_MAX = RENDER_MODE_STEREO
    };

    public enum ShadingRatePreset
    {
        SHADING_RATE_HIGHEST_PERFORMANCE = 1, // 1x1  |  2x2  |  4x4
        SHADING_RATE_HIGH_PERFORMANCE = 2,    // 1x1  |  2x2  |  2x2
        SHADING_RATE_BALANCED = 3,            // 4xSS |  1x1  |  2x2
        SHADING_RATE_HIGH_QUALITY = 4,        // 4xSS |  2xSS |  1x1
        SHADING_RATE_HIGHEST_QUALITY = 5,     // 8xSS |  4xSS |  2xSS
        SHADING_RATE_CUSTOM = 6,              // default the same as HIGHEST_PERFORMANCE
        SHADING_RATE_MAX = SHADING_RATE_CUSTOM
    };

    public enum ShadingPatternPreset
    {
        SHADING_PATTERN_WIDE = 1,
        SHADING_PATTERN_BALANCED = 2,
        SHADING_PATTERN_NARROW = 3,
        SHADING_PATTERN_CUSTOM = 4,
        SHADING_PATTERN_MAX = SHADING_PATTERN_CUSTOM
    };

    public enum ShadingRate
    {
        CULL,               // no shading
        X16_PER_PIXEL,      // 16x supersampling
        X8_PER_PIXEL,       // 8x supersampling
        X4_PER_PIXEL,       // 4x supersampling
        X2_PER_PIXEL,       // 2x supersampling
        X1_PER_PIXEL,       // 1 shading pass / 1 pixel (normal shading)
        X1_PER_2X1_PIXELS,  // 1 shading pass / 2 pixels
        X1_PER_1X2_PIXELS,  // 1 shading pass / 2 pixels
        X1_PER_2X2_PIXELS,  // 1 shading pass / 4  pixels
        X1_PER_4X2_PIXELS,  // 1 shading pass / 8  pixels
        X1_PER_2X4_PIXELS,  // 1 shading pass / 8  pixels
        X1_PER_4X4_PIXELS   // 1 shading pass / 16 pixels
    };

#if __cplusplus
#undef public
#else
}
#endif