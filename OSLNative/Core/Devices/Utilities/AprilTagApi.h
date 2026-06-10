#pragma once

#include "OSLNativeExport.h"

#ifdef __cplusplus
extern "C" {
#endif

enum OslAprilTagPixelFormat
{
    OSL_APRILTAG_PIXEL_GRAY8 = 0,
    OSL_APRILTAG_PIXEL_RGBA32 = 1
};

typedef struct OslAprilTagDetection
{
    int id;
    int hamming;
    float decisionMargin;
    float poseError;
    float centerX;
    float centerY;
    float corner0X;
    float corner0Y;
    float corner1X;
    float corner1Y;
    float corner2X;
    float corner2Y;
    float corner3X;
    float corner3Y;
    float positionX;
    float positionY;
    float positionZ;
    float rotation00;
    float rotation01;
    float rotation02;
    float rotation10;
    float rotation11;
    float rotation12;
    float rotation20;
    float rotation21;
    float rotation22;
} OslAprilTagDetection;

OSL_API void* OslCreateAprilTagDetector();

OSL_API void OslDestroyAprilTagDetector(void* detectorContext);

OSL_API int OslDetectAprilTagsWithContext(
    void* detectorContext,
    const unsigned char* pixels,
    int width,
    int height,
    int stride,
    int pixelFormat,
    float fx,
    float fy,
    float cx,
    float cy,
    float tagSizeMeters,
    int flipY,
    int downsampleFactor,
    OslAprilTagDetection* detections,
    int maxDetections);

#ifdef __cplusplus
}
#endif
