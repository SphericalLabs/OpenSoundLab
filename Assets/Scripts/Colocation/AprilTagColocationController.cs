using System.Collections;
using System.Collections.Generic;
using IAS.CoLocationMUVR;
using Meta.XR;
using Unity.Collections;
using UnityEngine;

[DefaultExecutionOrder(-50)]
public class AprilTagColocationController : MonoBehaviour
{
    [Tooltip("Meta passthrough camera component. Use one instance, usually the right camera.")]
    public PassthroughCameraAccess cameraAccess;
    public OVRCameraRig cameraRig;
    public CalibrationManager manualCalibrationManager;

    public OVRInput.Button contextButton = OVRInput.Button.Start;
    public bool ownManualContextButton = true;
    public float longPressSeconds = 0.200f;
    public float permissionRequestTimeoutSeconds = 12f;
    public float cameraWarmupSeconds = 2f;
    public float sampleSeconds = 0.6f;
    public int minSamples = 1;
    public int maxDetections = 8;
    public int expectedTagId = -1;
    public int maxHamming = 1;
    public float minDecisionMargin = 20f;
    public float maxPoseError = 10f;
    public float tagSizeMeters = 0.24f;
    public bool flipCameraImageY = true;
    public bool preferHalfCameraResolution = true;
    public Vector2Int preferredCameraResolution = new Vector2Int(640, 480);
    public bool nativeDownsampleWhenAbovePreferred = true;
    public float workerResultTimeoutSeconds = 0.5f;
    public float flatMarkerNormalThreshold = 0.7f;
    public bool logCalibrationDetails = true;

    readonly List<Pose> markerPoseSamples = new List<Pose>(16);
    AprilTagDetectionWorker detectionWorker;
    byte[] cameraPixelBuffer;
    bool contextWasPressed;
    bool longPressTriggered;
    bool ignoreContextUntilReleased;
    bool calibrationRunning;
    bool manualWasActiveOnContextPress;
    float contextPressStartedAt;
    int sampleUpdatedFrames;
    int sampleRawDetections;
    int sampleRejectedById;
    int sampleRejectedByHamming;
    int sampleRejectedByMargin;
    int sampleRejectedByPoseError;
    int sampleScheduledFrames;
    int sampleCompletedFrames;
    int sampleSkippedBusy;
    int activeNativeDownsampleFactor = 1;
    float sampleDetectionMillisecondsTotal;
    float sampleDetectionMillisecondsMax;
    void OnDestroy()
    {
        if (detectionWorker != null)
        {
            detectionWorker.Dispose();
            detectionWorker = null;
        }
    }

    void Update()
    {
        bool contextPressed = OVRInput.Get(contextButton);
        bool manualCalibrationActive = isManualCalibrationActive();
        refreshManualManagerState(contextPressed, manualCalibrationActive);
        if (ignoreContextUntilReleased)
        {
            if (!contextPressed)
            {
                ignoreContextUntilReleased = false;
                contextWasPressed = false;
                longPressTriggered = false;
            }
            return;
        }
        if (calibrationRunning) return;

        if (contextPressed && !contextWasPressed)
        {
            contextWasPressed = true;
            longPressTriggered = false;
            manualWasActiveOnContextPress = manualCalibrationActive;
            contextPressStartedAt = Time.time;
        }

        if (contextWasPressed && contextPressed && !longPressTriggered && Time.time - contextPressStartedAt >= longPressSeconds)
        {
            longPressTriggered = true;
            contextWasPressed = false;
            ignoreContextUntilReleased = true;
            if (!manualWasActiveOnContextPress) StartCoroutine(calibrateFromMarker());
            return;
        }

        if (contextWasPressed && !contextPressed)
        {
            bool shortPress = !longPressTriggered && Time.time - contextPressStartedAt < longPressSeconds;
            contextWasPressed = false;
            if (shortPress) setManualCalibrationActive(!manualWasActiveOnContextPress);
        }
    }

    IEnumerator calibrateFromMarker()
    {
        calibrationRunning = true;

        if (!resolveReferences())
        {
            calibrationRunning = false;
            yield break;
        }

        if (Application.isEditor)
        {
            Debug.LogWarning("AprilTag colocation needs the headset passthrough camera and is not available in the editor.", this);
            calibrationRunning = false;
            yield break;
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        if (!PassthroughCameraAccess.IsSupported)
        {
            Debug.LogWarning("AprilTag colocation requires Quest 3 or Quest 3S with Horizon OS v74 or newer.", this);
            calibrationRunning = false;
            yield break;
        }
#endif

#if UNITY_ANDROID && !UNITY_EDITOR
        yield return requestPassthroughCameraPermission();
        if (!hasPassthroughCameraPermission())
        {
            Debug.LogWarning("AprilTag colocation needs headset camera permission before passthrough camera access can start.", this);
            calibrationRunning = false;
            yield break;
        }
#endif

        configurePreferredCameraResolution();
        if (!cameraAccess.enabled) cameraAccess.enabled = true;

        float warmupEnd = Time.time + cameraWarmupSeconds;
        while (!cameraAccess.IsPlaying && Time.time < warmupEnd) yield return null;

        if (!cameraAccess.IsPlaying)
        {
            Debug.LogWarning("AprilTag colocation could not start because passthrough camera access is not playing.", this);
            calibrationRunning = false;
            yield break;
        }
        ensureDetectionWorker();
        activeNativeDownsampleFactor = getNativeDownsampleFactor();
        if (logCalibrationDetails)
        {
            Debug.Log("AprilTag camera resolution=" + cameraAccess.CurrentResolution.x + "x" + cameraAccess.CurrentResolution.y +
                ", nativeDownsampleFactor=" + activeNativeDownsampleFactor + ".", this);
        }

        markerPoseSamples.Clear();
        resetSampleDiagnostics();
        float sampleEnd = Time.time + sampleSeconds;
        while (Time.time < sampleEnd)
        {
            yield return null;
            collectDetectionWorkerResult();
            if (markerPoseSamples.Count >= minSamples) break;
            if (!cameraAccess.IsUpdatedThisFrame) continue;
            sampleUpdatedFrames++;
            scheduleMarkerDetection();
        }

        float workerWaitEnd = Time.time + workerResultTimeoutSeconds;
        while (markerPoseSamples.Count < minSamples && detectionWorker != null && detectionWorker.IsBusy && Time.time < workerWaitEnd)
        {
            yield return null;
            collectDetectionWorkerResult();
        }
        collectDetectionWorkerResult();

        if (markerPoseSamples.Count < minSamples)
        {
            Debug.LogWarning(getSampleFailureMessage(), this);
            calibrationRunning = false;
            yield break;
        }

        applyAverageCalibration();
        calibrationRunning = false;
    }

#if UNITY_ANDROID && !UNITY_EDITOR
    IEnumerator requestPassthroughCameraPermission()
    {
        if (hasPassthroughCameraPermission()) yield break;

        string permission = OVRPermissionsRequester.PassthroughCameraAccessPermission;
        bool requestFinished = false;
        UnityEngine.Android.PermissionCallbacks callbacks = new UnityEngine.Android.PermissionCallbacks();
        callbacks.PermissionGranted += grantedPermission =>
        {
            if (grantedPermission == permission) requestFinished = true;
        };
        callbacks.PermissionDenied += deniedPermission =>
        {
            if (deniedPermission == permission) requestFinished = true;
        };
        callbacks.PermissionDeniedAndDontAskAgain += deniedPermission =>
        {
            if (deniedPermission == permission) requestFinished = true;
        };

        UnityEngine.Android.Permission.RequestUserPermission(permission, callbacks);

        float timeoutAt = Time.time + permissionRequestTimeoutSeconds;
        while (!requestFinished && !hasPassthroughCameraPermission() && Time.time < timeoutAt)
        {
            yield return null;
        }
    }

    bool hasPassthroughCameraPermission()
    {
        return UnityEngine.Android.Permission.HasUserAuthorizedPermission(OVRPermissionsRequester.PassthroughCameraAccessPermission);
    }
#endif

    void scheduleMarkerDetection()
    {
        if (detectionWorker == null) return;
        if (detectionWorker.IsBusy)
        {
            sampleSkippedBusy++;
            return;
        }

        Vector2Int resolution = cameraAccess.CurrentResolution;
        NativeArray<Color32> pixels = cameraAccess.GetColors();
        if (!pixels.IsCreated || resolution.x <= 0 || resolution.y <= 0) return;

        int byteCount = resolution.x * resolution.y * 4;
        if (cameraPixelBuffer == null || cameraPixelBuffer.Length < byteCount) cameraPixelBuffer = new byte[byteCount];
        if (!AprilTagNative.CopyRgba32ToByteArray(pixels, resolution.x, resolution.y, cameraPixelBuffer)) return;

        Pose cameraTrackingPose = cameraAccess.GetCameraPose();
        AprilTagNative.CameraIntrinsics intrinsics = getCurrentIntrinsics();
        if (detectionWorker.TrySchedule(
            cameraPixelBuffer,
            resolution.x,
            resolution.y,
            intrinsics,
            tagSizeMeters,
            flipCameraImageY,
            activeNativeDownsampleFactor,
            cameraTrackingPose))
        {
            sampleScheduledFrames++;
        }
    }

    void collectDetectionWorkerResult()
    {
        if (detectionWorker == null) return;
        while (detectionWorker.TryGetResult(out AprilTagDetectionWorker.Result result))
        {
            sampleCompletedFrames++;
            sampleRawDetections += result.count;
            sampleDetectionMillisecondsTotal += result.elapsedMilliseconds;
            sampleDetectionMillisecondsMax = Mathf.Max(sampleDetectionMillisecondsMax, result.elapsedMilliseconds);

            if (trySelectDetection(result.detections, result.count, out AprilTagNative.OslAprilTagDetection detection))
            {
                markerPoseSamples.Add(multiply(result.cameraTrackingPose, getCameraFromMarkerPose(detection)));
            }
        }
    }

    void applyAverageCalibration()
    {
        Pose markerTrackingPose = averagePoses(markerPoseSamples);
        Pose targetMarkerPose = getTargetMarkerPose();

        Transform trackingSpace = cameraRig.trackingSpace;
        Pose trackingPose = new Pose(trackingSpace.position, trackingSpace.rotation);
        Pose detectedMarkerPose = transformPose(trackingSpace, markerTrackingPose);
        string markerOrientationMode;
        Quaternion markerYaw = getMarkerYawRotation(markerTrackingPose.rotation, out markerOrientationMode);
        Quaternion nextTrackingRotation = getMarkerYawRotation(targetMarkerPose.rotation, out _) * Quaternion.Inverse(markerYaw);
        Pose nextTrackingPose = new Pose(
            targetMarkerPose.position - nextTrackingRotation * markerTrackingPose.position,
            nextTrackingRotation);
        Pose delta = multiply(nextTrackingPose, inverse(trackingPose));
        trackingSpace.SetPositionAndRotation(nextTrackingPose.position, nextTrackingPose.rotation);

        float averageDetectionMs = sampleCompletedFrames > 0 ? sampleDetectionMillisecondsTotal / sampleCompletedFrames : 0f;
        Debug.Log("AprilTag colocation applied from " + markerPoseSamples.Count + " marker frames. completedFrames=" +
            sampleCompletedFrames + ", averageDetectionMs=" + averageDetectionMs.ToString("F2") +
            ", maxDetectionMs=" + sampleDetectionMillisecondsMax.ToString("F2") + ".", this);
        if (logCalibrationDetails)
        {
            Debug.Log("AprilTag colocation details: detectedMarkerPosition=" + formatVector(detectedMarkerPose.position) +
                ", detectedMarkerEuler=" + formatVector(detectedMarkerPose.rotation.eulerAngles) +
                ", targetMarkerPosition=" + formatVector(targetMarkerPose.position) +
                ", targetMarkerEuler=" + formatVector(targetMarkerPose.rotation.eulerAngles) +
                ", markerTrackingPosition=" + formatVector(markerTrackingPose.position) +
                ", markerTrackingEuler=" + formatVector(markerTrackingPose.rotation.eulerAngles) +
                ", markerOrientationMode=" + markerOrientationMode +
                ", deltaPosition=" + formatVector(delta.position) +
                ", deltaEuler=" + formatVector(delta.rotation.eulerAngles) + ".", this);
        }
    }

    Pose getTargetMarkerPose()
    {
        return new Pose(Vector3.zero, Quaternion.identity);
    }

    bool trySelectDetection(AprilTagNative.OslAprilTagDetection[] detections, int count, out AprilTagNative.OslAprilTagDetection bestDetection)
    {
        bestDetection = default;
        float bestScore = float.MinValue;
        if (detections == null) return false;

        int detectionCount = Mathf.Min(count, detections.Length);
        for (int i = 0; i < detectionCount; i++)
        {
            AprilTagNative.OslAprilTagDetection detection = detections[i];
            if (expectedTagId >= 0 && detection.id != expectedTagId)
            {
                sampleRejectedById++;
                continue;
            }
            if (detection.hamming > maxHamming)
            {
                sampleRejectedByHamming++;
                continue;
            }
            if (detection.decisionMargin < minDecisionMargin)
            {
                sampleRejectedByMargin++;
                continue;
            }
            if (maxPoseError > 0f && detection.poseError > maxPoseError)
            {
                sampleRejectedByPoseError++;
                continue;
            }

            float score = detection.decisionMargin - detection.poseError;
            if (score <= bestScore) continue;
            bestScore = score;
            bestDetection = detection;
        }

        return bestScore > float.MinValue;
    }

    void resetSampleDiagnostics()
    {
        sampleUpdatedFrames = 0;
        sampleRawDetections = 0;
        sampleRejectedById = 0;
        sampleRejectedByHamming = 0;
        sampleRejectedByMargin = 0;
        sampleRejectedByPoseError = 0;
        sampleScheduledFrames = 0;
        sampleCompletedFrames = 0;
        sampleSkippedBusy = 0;
        sampleDetectionMillisecondsTotal = 0f;
        sampleDetectionMillisecondsMax = 0f;
    }

    string getSampleFailureMessage()
    {
        if (!logCalibrationDetails)
        {
            return $"AprilTag colocation found {markerPoseSamples.Count} usable marker frames, expected at least {minSamples}.";
        }

        Vector2Int resolution = cameraAccess != null ? cameraAccess.CurrentResolution : Vector2Int.zero;
        float averageDetectionMs = sampleCompletedFrames > 0 ? sampleDetectionMillisecondsTotal / sampleCompletedFrames : 0f;
        return "AprilTag colocation found " + markerPoseSamples.Count + " usable marker frames, expected at least " + minSamples +
            ". updatedFrames=" + sampleUpdatedFrames +
            ", scheduledFrames=" + sampleScheduledFrames +
            ", completedFrames=" + sampleCompletedFrames +
            ", skippedBusy=" + sampleSkippedBusy +
            ", rawDetections=" + sampleRawDetections +
            ", rejectedId=" + sampleRejectedById +
            ", rejectedHamming=" + sampleRejectedByHamming +
            ", rejectedMargin=" + sampleRejectedByMargin +
            ", rejectedPoseError=" + sampleRejectedByPoseError +
            ", resolution=" + resolution.x + "x" + resolution.y +
            ", nativeDownsampleFactor=" + activeNativeDownsampleFactor +
            ", averageDetectionMs=" + averageDetectionMs.ToString("F2") +
            ", maxDetectionMs=" + sampleDetectionMillisecondsMax.ToString("F2") +
            ", expectedTagId=" + expectedTagId +
            ", tagSizeMeters=" + tagSizeMeters +
            ", flipCameraImageY=" + flipCameraImageY +
            ", minDecisionMargin=" + minDecisionMargin +
            ", maxPoseError=" + maxPoseError + ".";
    }

    AprilTagNative.CameraIntrinsics getCurrentIntrinsics()
    {
        PassthroughCameraAccess.CameraIntrinsics intrinsics = cameraAccess.Intrinsics;
        Vector2 sensorResolution = intrinsics.SensorResolution;
        Vector2 currentResolution = cameraAccess.CurrentResolution;
        if (sensorResolution.x <= 0f || sensorResolution.y <= 0f) sensorResolution = currentResolution;

        Vector2 scaleFactor = new Vector2(currentResolution.x / sensorResolution.x, currentResolution.y / sensorResolution.y);
        scaleFactor /= Mathf.Max(scaleFactor.x, scaleFactor.y);
        Rect crop = new Rect(
            sensorResolution.x * (1f - scaleFactor.x) * 0.5f,
            sensorResolution.y * (1f - scaleFactor.y) * 0.5f,
            sensorResolution.x * scaleFactor.x,
            sensorResolution.y * scaleFactor.y);

        return new AprilTagNative.CameraIntrinsics
        {
            fx = intrinsics.FocalLength.x * currentResolution.x / crop.width,
            fy = intrinsics.FocalLength.y * currentResolution.y / crop.height,
            cx = (intrinsics.PrincipalPoint.x - crop.x) * currentResolution.x / crop.width,
            cy = (intrinsics.PrincipalPoint.y - crop.y) * currentResolution.y / crop.height
        };
    }

    Pose getCameraFromMarkerPose(AprilTagNative.OslAprilTagDetection detection)
    {
        Vector3 position = new Vector3(detection.positionX, -detection.positionY, detection.positionZ);
        Matrix4x4 rotation = Matrix4x4.identity;
        rotation.m00 = detection.rotation00;
        rotation.m01 = detection.rotation01;
        rotation.m02 = -detection.rotation02;
        rotation.m10 = -detection.rotation10;
        rotation.m11 = -detection.rotation11;
        rotation.m12 = detection.rotation12;
        rotation.m20 = detection.rotation20;
        rotation.m21 = detection.rotation21;
        rotation.m22 = -detection.rotation22;

        Vector3 forward = new Vector3(rotation.m02, rotation.m12, rotation.m22);
        Vector3 up = new Vector3(rotation.m01, rotation.m11, rotation.m21);
        return new Pose(position, Quaternion.LookRotation(forward, up));
    }

    Pose averagePoses(List<Pose> poses)
    {
        Vector3 position = Vector3.zero;
        Vector4 rotation = Vector4.zero;
        Quaternion referenceRotation = poses[0].rotation;

        for (int i = 0; i < poses.Count; i++)
        {
            position += poses[i].position;
            Quaternion q = poses[i].rotation;
            if (Quaternion.Dot(referenceRotation, q) < 0f)
            {
                q = new Quaternion(-q.x, -q.y, -q.z, -q.w);
            }
            rotation += new Vector4(q.x, q.y, q.z, q.w);
        }

        position /= poses.Count;
        rotation /= poses.Count;
        float rotationMagnitude = Mathf.Sqrt(Vector4.Dot(rotation, rotation));
        Quaternion averageRotation = rotationMagnitude > 0.0001f
            ? new Quaternion(rotation.x / rotationMagnitude, rotation.y / rotationMagnitude, rotation.z / rotationMagnitude, rotation.w / rotationMagnitude)
            : Quaternion.identity;
        return new Pose(position, averageRotation);
    }

    Pose transformPose(Transform parent, Pose pose)
    {
        return new Pose(parent.TransformPoint(pose.position), parent.rotation * pose.rotation);
    }

    Pose multiply(Pose a, Pose b)
    {
        return new Pose(a.position + a.rotation * b.position, a.rotation * b.rotation);
    }

    Pose inverse(Pose pose)
    {
        Quaternion rotation = Quaternion.Inverse(pose.rotation);
        return new Pose(rotation * -pose.position, rotation);
    }

    Quaternion getMarkerYawRotation(Quaternion rotation, out string orientationMode)
    {
        Vector3 normal = rotation * Vector3.forward;
        bool flatMarker = Mathf.Abs(Vector3.Dot(normal.normalized, Vector3.up)) > flatMarkerNormalThreshold;
        orientationMode = flatMarker ? "floor" : "wall";

        Vector3 sourceAxis = flatMarker ? rotation * Vector3.up : normal;
        Vector3 forward = Vector3.ProjectOnPlane(sourceAxis, Vector3.up);
        if (forward.sqrMagnitude < 0.0001f && flatMarker)
        {
            forward = Vector3.ProjectOnPlane(rotation * Vector3.right, Vector3.up);
        }
        if (forward.sqrMagnitude < 0.0001f) return Quaternion.identity;
        return Quaternion.LookRotation(forward.normalized, Vector3.up);
    }

    string formatVector(Vector3 value)
    {
        return "(" + value.x.ToString("F3") + ", " + value.y.ToString("F3") + ", " + value.z.ToString("F3") + ")";
    }

    bool resolveReferences()
    {
        if (cameraAccess == null) cameraAccess = FindObjectOfType<PassthroughCameraAccess>();
        if (cameraRig == null) cameraRig = FindObjectOfType<OVRCameraRig>();
        if (manualCalibrationManager == null) manualCalibrationManager = FindObjectOfType<CalibrationManager>();

        if (cameraAccess == null)
        {
            Debug.LogWarning("AprilTag colocation needs a PassthroughCameraAccess component assigned.", this);
            return false;
        }

        if (cameraRig == null || cameraRig.trackingSpace == null)
        {
            Debug.LogWarning("AprilTag colocation needs an OVRCameraRig with a trackingSpace assigned.", this);
            return false;
        }

        return true;
    }

    void ensureDetectionWorker()
    {
        if (detectionWorker != null) return;
        detectionWorker = new AprilTagDetectionWorker(maxDetections);
    }

    void configurePreferredCameraResolution()
    {
        if (!preferHalfCameraResolution || cameraAccess == null) return;
        if (cameraAccess.IsPlaying) return;

        Vector2Int selectedResolution = getPreferredSupportedCameraResolution();
        if (selectedResolution.x <= 0 || selectedResolution.y <= 0) return;
        cameraAccess.RequestedResolution = selectedResolution;
    }

    Vector2Int getPreferredSupportedCameraResolution()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        Vector2Int[] supportedResolutions = getSupportedCameraResolutions();
#else
        Vector2Int[] supportedResolutions = { cameraAccess.RequestedResolution };
#endif
        if (supportedResolutions == null || supportedResolutions.Length == 0) return preferredCameraResolution;

        Vector2Int bestBelowPreferred = Vector2Int.zero;
        Vector2Int smallestSameAspect = Vector2Int.zero;
        float targetAspect = preferredCameraResolution.y > 0
            ? (float)preferredCameraResolution.x / preferredCameraResolution.y
            : 4f / 3f;

        for (int i = 0; i < supportedResolutions.Length; i++)
        {
            Vector2Int resolution = supportedResolutions[i];
            if (resolution == preferredCameraResolution) return resolution;

            float aspect = resolution.y > 0 ? (float)resolution.x / resolution.y : 0f;
            if (Mathf.Abs(aspect - targetAspect) > 0.02f) continue;

            if (resolution.x <= preferredCameraResolution.x && resolution.y <= preferredCameraResolution.y &&
                resolution.x * resolution.y > bestBelowPreferred.x * bestBelowPreferred.y)
            {
                bestBelowPreferred = resolution;
            }

            if (smallestSameAspect == Vector2Int.zero ||
                resolution.x * resolution.y < smallestSameAspect.x * smallestSameAspect.y)
            {
                smallestSameAspect = resolution;
            }
        }

        if (bestBelowPreferred != Vector2Int.zero) return bestBelowPreferred;
        if (smallestSameAspect != Vector2Int.zero) return smallestSameAspect;
        return supportedResolutions[0];
    }

#if UNITY_ANDROID && !UNITY_EDITOR
    unsafe Vector2Int[] getSupportedCameraResolutions()
    {
        return PassthroughCameraAccess.GetSupportedResolutions(cameraAccess.CameraPosition);
    }
#endif

    int getNativeDownsampleFactor()
    {
        if (!nativeDownsampleWhenAbovePreferred) return 1;
        Vector2Int resolution = cameraAccess.CurrentResolution;
        if (preferredCameraResolution.x <= 0 || preferredCameraResolution.y <= 0) return 1;
        if (resolution.x > preferredCameraResolution.x || resolution.y > preferredCameraResolution.y) return 2;
        return 1;
    }

    void setManualCalibrationActive(bool active)
    {
        if (manualCalibrationManager == null) manualCalibrationManager = FindObjectOfType<CalibrationManager>();
        if (manualCalibrationManager == null)
        {
            Debug.LogWarning("Manual colocation fallback is not available because no CalibrationManager was found.", this);
            return;
        }

        manualCalibrationManager.enabled = true;
        manualCalibrationManager.ToggleCalibrationUI(active);
    }

    void refreshManualManagerState(bool contextPressed, bool manualCalibrationActive)
    {
        if (!ownManualContextButton) return;
        if (contextButton != OVRInput.Button.Start) return;
        if (manualCalibrationManager == null) manualCalibrationManager = FindObjectOfType<CalibrationManager>();
        if (manualCalibrationManager == null) return;

        if (manualCalibrationActive && contextPressed)
        {
            if (manualCalibrationManager.enabled) manualCalibrationManager.enabled = false;
        }
        else if (manualCalibrationActive)
        {
            if (!manualCalibrationManager.enabled) manualCalibrationManager.enabled = true;
        }
        else if (manualCalibrationManager.enabled)
        {
            manualCalibrationManager.enabled = false;
        }
    }

    bool isManualCalibrationActive()
    {
        if (manualCalibrationManager == null) return false;
        if (manualCalibrationManager.uiParentObject != null && manualCalibrationManager.uiParentObject.activeInHierarchy) return true;
        if (manualCalibrationManager.calibrateCourser != null && manualCalibrationManager.calibrateCourser.activeInHierarchy) return true;
        return false;
    }
}
