using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;

public static class AprilTagNative
{
    public struct CameraIntrinsics
    {
        public float fx;
        public float fy;
        public float cx;
        public float cy;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct OslAprilTagDetection
    {
        public int id;
        public int hamming;
        public float decisionMargin;
        public float poseError;
        public float centerX;
        public float centerY;
        public float corner0X;
        public float corner0Y;
        public float corner1X;
        public float corner1Y;
        public float corner2X;
        public float corner2Y;
        public float corner3X;
        public float corner3Y;
        public float positionX;
        public float positionY;
        public float positionZ;
        public float rotation00;
        public float rotation01;
        public float rotation02;
        public float rotation10;
        public float rotation11;
        public float rotation12;
        public float rotation20;
        public float rotation21;
        public float rotation22;
    }

    const int rgba32PixelFormat = 1;

    public static System.IntPtr CreateDetector()
    {
        return OslCreateAprilTagDetector();
    }

    public static void DestroyDetector(System.IntPtr detectorContext)
    {
        if (detectorContext != System.IntPtr.Zero) OslDestroyAprilTagDetector(detectorContext);
    }

    public static unsafe bool CopyRgba32ToByteArray(NativeArray<Color32> pixels, int width, int height, byte[] target)
    {
        if (!pixels.IsCreated) return false;
        if (width <= 0 || height <= 0) return false;
        int byteCount = width * height * 4;
        if (pixels.Length < width * height) return false;
        if (target == null || target.Length < byteCount) return false;

        void* pixelPtr = NativeArrayUnsafeUtility.GetUnsafeReadOnlyPtr(pixels);
        Marshal.Copy(new System.IntPtr(pixelPtr), target, 0, byteCount);
        return true;
    }

    public static unsafe int DetectRgba32(
        System.IntPtr detectorContext,
        byte[] pixels,
        int width,
        int height,
        CameraIntrinsics intrinsics,
        float tagSizeMeters,
        bool flipY,
        int downsampleFactor,
        OslAprilTagDetection[] detections)
    {
        if (detectorContext == System.IntPtr.Zero) return 0;
        if (pixels == null) return 0;
        if (width <= 0 || height <= 0 || tagSizeMeters <= 0f) return 0;
        if (pixels.Length < width * height * 4) return 0;
        if (detections == null || detections.Length == 0) return 0;

        fixed (byte* pixelPtr = pixels)
        fixed (OslAprilTagDetection* detectionPtr = detections)
        {
            return OslDetectAprilTagsWithContext(
                detectorContext,
                pixelPtr,
                width,
                height,
                width * 4,
                rgba32PixelFormat,
                intrinsics.fx,
                intrinsics.fy,
                intrinsics.cx,
                intrinsics.cy,
                tagSizeMeters,
                flipY ? 1 : 0,
                Mathf.Max(1, downsampleFactor),
                detectionPtr,
                detections.Length);
        }
    }

    [DllImport("OSLNative", CallingConvention = CallingConvention.Cdecl)]
    static extern System.IntPtr OslCreateAprilTagDetector();

    [DllImport("OSLNative", CallingConvention = CallingConvention.Cdecl)]
    static extern void OslDestroyAprilTagDetector(System.IntPtr detectorContext);

    [DllImport("OSLNative", CallingConvention = CallingConvention.Cdecl)]
    static unsafe extern int OslDetectAprilTagsWithContext(
        System.IntPtr detectorContext,
        byte* pixels,
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
}
