using System;
using System.Diagnostics;
using System.Threading;
using UnityEngine;

public sealed class AprilTagDetectionWorker : IDisposable
{
    public struct Result
    {
        public int count;
        public float elapsedMilliseconds;
        public Pose cameraTrackingPose;
        public AprilTagNative.OslAprilTagDetection[] detections;
    }

    readonly object sync = new object();
    readonly Thread thread;
    readonly AprilTagNative.OslAprilTagDetection[] workerDetections;
    byte[] pendingPixels;
    bool hasRequest;
    bool hasResult;
    bool isBusy;
    bool stopRequested;
    int pendingWidth;
    int pendingHeight;
    int pendingDownsampleFactor;
    AprilTagNative.CameraIntrinsics pendingIntrinsics;
    float pendingTagSizeMeters;
    bool pendingFlipY;
    Pose pendingCameraTrackingPose;
    Result latestResult;
    IntPtr detectorContext;

    public AprilTagDetectionWorker(int maxDetections)
    {
        workerDetections = new AprilTagNative.OslAprilTagDetection[Mathf.Max(1, maxDetections)];
        thread = new Thread(run);
        thread.IsBackground = true;
        thread.Name = "AprilTagDetectionWorker";
        thread.Start();
    }

    public bool IsBusy
    {
        get
        {
            lock (sync) return isBusy || hasRequest;
        }
    }

    public bool TrySchedule(
        byte[] rgbaPixels,
        int width,
        int height,
        AprilTagNative.CameraIntrinsics intrinsics,
        float tagSizeMeters,
        bool flipY,
        int downsampleFactor,
        Pose cameraTrackingPose)
    {
        if (rgbaPixels == null || width <= 0 || height <= 0 || tagSizeMeters <= 0f) return false;
        int byteCount = width * height * 4;
        if (rgbaPixels.Length < byteCount) return false;

        lock (sync)
        {
            if (stopRequested || isBusy || hasRequest || hasResult) return false;
            if (pendingPixels == null || pendingPixels.Length < byteCount) pendingPixels = new byte[byteCount];
            Buffer.BlockCopy(rgbaPixels, 0, pendingPixels, 0, byteCount);
            pendingWidth = width;
            pendingHeight = height;
            pendingIntrinsics = intrinsics;
            pendingTagSizeMeters = tagSizeMeters;
            pendingFlipY = flipY;
            pendingDownsampleFactor = Mathf.Max(1, downsampleFactor);
            pendingCameraTrackingPose = cameraTrackingPose;
            hasRequest = true;
            isBusy = true;
            Monitor.Pulse(sync);
            return true;
        }
    }

    public bool TryGetResult(out Result result)
    {
        lock (sync)
        {
            if (!hasResult)
            {
                result = default;
                return false;
            }

            result = latestResult;
            latestResult = default;
            hasResult = false;
            return true;
        }
    }

    public void Dispose()
    {
        lock (sync)
        {
            stopRequested = true;
            Monitor.Pulse(sync);
        }
        if (thread.IsAlive) thread.Join(500);
    }

    void run()
    {
        try
        {
            detectorContext = AprilTagNative.CreateDetector();
            while (true)
            {
                int width;
                int height;
                int downsampleFactor;
                AprilTagNative.CameraIntrinsics intrinsics;
                float tagSizeMeters;
                bool flipY;
                Pose cameraTrackingPose;
                byte[] pixels;

                lock (sync)
                {
                    while (!stopRequested && !hasRequest) Monitor.Wait(sync);
                    if (stopRequested) break;

                    width = pendingWidth;
                    height = pendingHeight;
                    intrinsics = pendingIntrinsics;
                    tagSizeMeters = pendingTagSizeMeters;
                    flipY = pendingFlipY;
                    downsampleFactor = pendingDownsampleFactor;
                    cameraTrackingPose = pendingCameraTrackingPose;
                    pixels = pendingPixels;
                    hasRequest = false;
                }

                long start = Stopwatch.GetTimestamp();
                int count = AprilTagNative.DetectRgba32(
                    detectorContext,
                    pixels,
                    width,
                    height,
                    intrinsics,
                    tagSizeMeters,
                    flipY,
                    downsampleFactor,
                    workerDetections);
                float elapsedMilliseconds = (float)((Stopwatch.GetTimestamp() - start) * 1000.0 / Stopwatch.Frequency);

                AprilTagNative.OslAprilTagDetection[] detections = new AprilTagNative.OslAprilTagDetection[workerDetections.Length];
                Array.Copy(workerDetections, detections, workerDetections.Length);

                lock (sync)
                {
                    latestResult = new Result
                    {
                        count = count,
                        elapsedMilliseconds = elapsedMilliseconds,
                        cameraTrackingPose = cameraTrackingPose,
                        detections = detections
                    };
                    hasResult = true;
                    isBusy = false;
                }
            }
        }
        finally
        {
            AprilTagNative.DestroyDetector(detectorContext);
            detectorContext = IntPtr.Zero;
        }
    }
}
