#include "AprilTagApi.h"

#include <cstdlib>
#include <cstring>
#include <new>

#include "apriltag.h"
#include "apriltag_pose.h"
#include "common/image_u8.h"
#include "common/matd.h"
#include "common/zarray.h"
#include "tagStandard41h12.h"

namespace
{
    struct AprilTagDetectorContext
    {
        apriltag_family_t* family = nullptr;
        apriltag_detector_t* detector = nullptr;
        image_u8_t* image = nullptr;
        int imageWidth = 0;
        int imageHeight = 0;
    };

    int normalizeDownsampleFactor(int downsampleFactor)
    {
        return downsampleFactor > 1 ? downsampleFactor : 1;
    }

    bool isValidInput(
        const unsigned char* pixels,
        int width,
        int height,
        int stride,
        int pixelFormat,
        float tagSizeMeters,
        OslAprilTagDetection* detections,
        int maxDetections)
    {
        if (pixels == nullptr) return false;
        if (width <= 0 || height <= 0 || stride <= 0) return false;
        if (tagSizeMeters <= 0.0f) return false;
        if (detections == nullptr || maxDetections <= 0) return false;
        if (pixelFormat == OSL_APRILTAG_PIXEL_GRAY8) return stride >= width;
        if (pixelFormat == OSL_APRILTAG_PIXEL_RGBA32) return stride >= width * 4;
        return false;
    }

    void configureDetector(apriltag_detector_t* detector)
    {
        detector->nthreads = 2;
        detector->quad_decimate = 2.0f;
        detector->quad_sigma = 0.0f;
        detector->refine_edges = true;
        detector->decode_sharpening = 0.25;
        detector->debug = false;
    }

    bool initializeContext(AprilTagDetectorContext* context)
    {
        if (context == nullptr) return false;

        context->family = tagStandard41h12_create();
        context->detector = apriltag_detector_create();
        if (context->family == nullptr || context->detector == nullptr) return false;

        apriltag_detector_add_family_bits(context->detector, context->family, 1);
        configureDetector(context->detector);
        return true;
    }

    void clearContext(AprilTagDetectorContext* context)
    {
        if (context == nullptr) return;
        if (context->image != nullptr) image_u8_destroy(context->image);
        if (context->detector != nullptr) apriltag_detector_destroy(context->detector);
        if (context->family != nullptr) tagStandard41h12_destroy(context->family);

        context->image = nullptr;
        context->detector = nullptr;
        context->family = nullptr;
        context->imageWidth = 0;
        context->imageHeight = 0;
    }

    bool ensureImage(AprilTagDetectorContext* context, int width, int height)
    {
        if (context == nullptr || width <= 0 || height <= 0) return false;
        if (context->image != nullptr && context->imageWidth == width && context->imageHeight == height) return true;

        if (context->image != nullptr) image_u8_destroy(context->image);
        context->image = image_u8_create(width, height);
        context->imageWidth = context->image != nullptr ? width : 0;
        context->imageHeight = context->image != nullptr ? height : 0;
        return context->image != nullptr;
    }

    void copyGray8(
        const unsigned char* pixels,
        int width,
        int height,
        int stride,
        bool flipY,
        int downsampleFactor,
        image_u8_t* image)
    {
        if (downsampleFactor <= 1)
        {
            for (int y = 0; y < height; y++)
            {
                int sourceY = flipY ? height - 1 - y : y;
                std::memcpy(image->buf + y * image->stride, pixels + sourceY * stride, width);
            }
            return;
        }

        for (int y = 0; y < image->height; y++)
        {
            unsigned char* dst = image->buf + y * image->stride;
            for (int x = 0; x < image->width; x++)
            {
                int sum = 0;
                for (int yy = 0; yy < downsampleFactor; yy++)
                {
                    int sourceY = flipY
                        ? height - 1 - (y * downsampleFactor + yy)
                        : y * downsampleFactor + yy;
                    const unsigned char* src = pixels + sourceY * stride + x * downsampleFactor;
                    for (int xx = 0; xx < downsampleFactor; xx++)
                    {
                        sum += src[xx];
                    }
                }
                dst[x] = static_cast<unsigned char>(sum / (downsampleFactor * downsampleFactor));
            }
        }
    }

    void copyRgba32AsGray(
        const unsigned char* pixels,
        int width,
        int height,
        int stride,
        bool flipY,
        int downsampleFactor,
        image_u8_t* image)
    {
        if (downsampleFactor <= 1)
        {
            for (int y = 0; y < height; y++)
            {
                int sourceY = flipY ? height - 1 - y : y;
                const unsigned char* src = pixels + sourceY * stride;
                unsigned char* dst = image->buf + y * image->stride;
                for (int x = 0; x < width; x++)
                {
                    unsigned char r = src[x * 4 + 0];
                    unsigned char g = src[x * 4 + 1];
                    unsigned char b = src[x * 4 + 2];
                    dst[x] = static_cast<unsigned char>((77 * r + 150 * g + 29 * b) >> 8);
                }
            }
            return;
        }

        for (int y = 0; y < image->height; y++)
        {
            unsigned char* dst = image->buf + y * image->stride;
            for (int x = 0; x < image->width; x++)
            {
                int sum = 0;
                for (int yy = 0; yy < downsampleFactor; yy++)
                {
                    int sourceY = flipY
                        ? height - 1 - (y * downsampleFactor + yy)
                        : y * downsampleFactor + yy;
                    const unsigned char* src = pixels + sourceY * stride + x * downsampleFactor * 4;
                    for (int xx = 0; xx < downsampleFactor; xx++)
                    {
                        unsigned char r = src[xx * 4 + 0];
                        unsigned char g = src[xx * 4 + 1];
                        unsigned char b = src[xx * 4 + 2];
                        sum += (77 * r + 150 * g + 29 * b) >> 8;
                    }
                }
                dst[x] = static_cast<unsigned char>(sum / (downsampleFactor * downsampleFactor));
            }
        }
    }

    bool copyPixelsAsGray(
        const unsigned char* pixels,
        int width,
        int height,
        int stride,
        int pixelFormat,
        bool flipY,
        int downsampleFactor,
        image_u8_t* image)
    {
        if (image == nullptr) return false;

        if (pixelFormat == OSL_APRILTAG_PIXEL_GRAY8)
        {
            copyGray8(pixels, width, height, stride, flipY, downsampleFactor, image);
        }
        else
        {
            copyRgba32AsGray(pixels, width, height, stride, flipY, downsampleFactor, image);
        }

        return true;
    }

    void fillDetection(apriltag_detection_t* detection, apriltag_pose_t* pose, double poseError, OslAprilTagDetection* result)
    {
        result->id = detection->id;
        result->hamming = detection->hamming;
        result->decisionMargin = detection->decision_margin;
        result->poseError = static_cast<float>(poseError);
        result->centerX = static_cast<float>(detection->c[0]);
        result->centerY = static_cast<float>(detection->c[1]);
        result->corner0X = static_cast<float>(detection->p[0][0]);
        result->corner0Y = static_cast<float>(detection->p[0][1]);
        result->corner1X = static_cast<float>(detection->p[1][0]);
        result->corner1Y = static_cast<float>(detection->p[1][1]);
        result->corner2X = static_cast<float>(detection->p[2][0]);
        result->corner2Y = static_cast<float>(detection->p[2][1]);
        result->corner3X = static_cast<float>(detection->p[3][0]);
        result->corner3Y = static_cast<float>(detection->p[3][1]);

        result->positionX = static_cast<float>(matd_get(pose->t, 0, 0));
        result->positionY = static_cast<float>(matd_get(pose->t, 1, 0));
        result->positionZ = static_cast<float>(matd_get(pose->t, 2, 0));
        result->rotation00 = static_cast<float>(matd_get(pose->R, 0, 0));
        result->rotation01 = static_cast<float>(matd_get(pose->R, 0, 1));
        result->rotation02 = static_cast<float>(matd_get(pose->R, 0, 2));
        result->rotation10 = static_cast<float>(matd_get(pose->R, 1, 0));
        result->rotation11 = static_cast<float>(matd_get(pose->R, 1, 1));
        result->rotation12 = static_cast<float>(matd_get(pose->R, 1, 2));
        result->rotation20 = static_cast<float>(matd_get(pose->R, 2, 0));
        result->rotation21 = static_cast<float>(matd_get(pose->R, 2, 1));
        result->rotation22 = static_cast<float>(matd_get(pose->R, 2, 2));
    }

    void destroyPose(apriltag_pose_t* pose)
    {
        if (pose->R != nullptr) matd_destroy(pose->R);
        if (pose->t != nullptr) matd_destroy(pose->t);
    }

    int detectAprilTagsWithContext(
        AprilTagDetectorContext* context,
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
        bool flipY,
        int downsampleFactor,
        OslAprilTagDetection* detections,
        int maxDetections)
    {
        if (!isValidInput(pixels, width, height, stride, pixelFormat, tagSizeMeters, detections, maxDetections))
        {
            return 0;
        }
        if (context == nullptr || context->family == nullptr || context->detector == nullptr) return 0;

        downsampleFactor = normalizeDownsampleFactor(downsampleFactor);
        int imageWidth = width / downsampleFactor;
        int imageHeight = height / downsampleFactor;
        if (imageWidth <= 0 || imageHeight <= 0) return 0;
        if (!ensureImage(context, imageWidth, imageHeight)) return 0;
        if (!copyPixelsAsGray(pixels, width, height, stride, pixelFormat, flipY, downsampleFactor, context->image)) return 0;

        if (flipY) cy = static_cast<float>(height - 1) - cy;
        float scaledFx = fx / downsampleFactor;
        float scaledFy = fy / downsampleFactor;
        float scaledCx = (cx + 0.5f) / downsampleFactor - 0.5f;
        float scaledCy = (cy + 0.5f) / downsampleFactor - 0.5f;

        zarray_t* tagDetections = apriltag_detector_detect(context->detector, context->image);
        int count = 0;
        if (tagDetections != nullptr)
        {
            int sourceCount = zarray_size(tagDetections);
            int copyCount = sourceCount < maxDetections ? sourceCount : maxDetections;
            for (int i = 0; i < copyCount; i++)
            {
                apriltag_detection_t* detection = nullptr;
                zarray_get(tagDetections, i, &detection);
                if (detection == nullptr) continue;

                apriltag_detection_info_t info;
                info.det = detection;
                info.tagsize = tagSizeMeters;
                info.fx = scaledFx;
                info.fy = scaledFy;
                info.cx = scaledCx;
                info.cy = scaledCy;

                apriltag_pose_t pose = { nullptr, nullptr };
                double error = estimate_tag_pose(&info, &pose);
                fillDetection(detection, &pose, error, &detections[count]);
                destroyPose(&pose);
                count++;
            }

            apriltag_detections_destroy(tagDetections);
        }

        return count;
    }

}

extern "C" {

void* OslCreateAprilTagDetector()
{
    AprilTagDetectorContext* context = new (std::nothrow) AprilTagDetectorContext();
    if (context == nullptr) return nullptr;
    if (!initializeContext(context))
    {
        clearContext(context);
        delete context;
        return nullptr;
    }
    return context;
}

void OslDestroyAprilTagDetector(void* detectorContext)
{
    AprilTagDetectorContext* context = static_cast<AprilTagDetectorContext*>(detectorContext);
    if (context == nullptr) return;
    clearContext(context);
    delete context;
}

int OslDetectAprilTagsWithContext(
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
    int maxDetections)
{
    return detectAprilTagsWithContext(
        static_cast<AprilTagDetectorContext*>(detectorContext),
        pixels,
        width,
        height,
        stride,
        pixelFormat,
        fx,
        fy,
        cx,
        cy,
        tagSizeMeters,
        flipY != 0,
        downsampleFactor,
        detections,
        maxDetections);
}

}
