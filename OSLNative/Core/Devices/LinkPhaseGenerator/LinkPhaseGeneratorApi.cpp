// This file is part of OpenSoundLab, which is based on SoundStage VR.
//
// Copyright © 2020-2026 OSLLv1 Sphericals OpenSoundLab
//
// OpenSoundLab is licensed under the OpenSoundLab License Agreement (OSLLv1).
// You may obtain a copy of the License at
// https://github.com/SphericalLabs/OpenSoundLab/LICENSE-OSLLv1.md
//
// By using, modifying, or distributing this software, you agree to be bound by the terms of the license.

#include "OSLNativeExport.h"

#if defined(_WIN32) && !defined(LINK_PLATFORM_WINDOWS)
#define LINK_PLATFORM_WINDOWS 1
#elif defined(__APPLE__) && !defined(LINK_PLATFORM_MACOSX)
#define LINK_PLATFORM_UNIX 1
#define LINK_PLATFORM_MACOSX 1
#elif (defined(__linux__) || defined(__ANDROID__)) && !defined(LINK_PLATFORM_LINUX)
#define LINK_PLATFORM_UNIX 1
#define LINK_PLATFORM_LINUX 1
#endif

#include <ableton/Link.hpp>

#include <algorithm>
#include <chrono>
#include <cstdint>

struct LinkPhaseGeneratorHandle {
    explicit LinkPhaseGeneratorHandle(double initialBpm)
        : link(std::max(1.0, initialBpm)) {
        link.enableStartStopSync(true);
    }

    ableton::Link link;
};

namespace {
std::chrono::microseconds linkNow(LinkPhaseGeneratorHandle* x) {
    return x->link.clock().micros();
}

double safeQuantum(double quantum) {
    return quantum > 0.0 ? quantum : 4.0;
}

double safeBpm(double bpm) {
    return bpm > 0.0 ? bpm : 120.0;
}
}

extern "C" {

OSL_API LinkPhaseGeneratorHandle* LinkPhaseGenerator_Create(double bpm) {
    return new LinkPhaseGeneratorHandle(safeBpm(bpm));
}

OSL_API void LinkPhaseGenerator_Destroy(LinkPhaseGeneratorHandle* x) {
    delete x;
}

OSL_API void LinkPhaseGenerator_SetEnabled(LinkPhaseGeneratorHandle* x, int enabled) {
    if (x == nullptr)
        return;

    x->link.enableStartStopSync(true);
    x->link.enable(enabled != 0);
}

OSL_API int LinkPhaseGenerator_IsEnabled(LinkPhaseGeneratorHandle* x) {
    if (x == nullptr)
        return 0;

    return x->link.isEnabled() ? 1 : 0;
}

OSL_API int LinkPhaseGenerator_NumPeers(LinkPhaseGeneratorHandle* x) {
    if (x == nullptr)
        return 0;

    return static_cast<int>(x->link.numPeers());
}

OSL_API double LinkPhaseGenerator_GetTempo(LinkPhaseGeneratorHandle* x) {
    if (x == nullptr)
        return 120.0;

    auto state = x->link.captureAppSessionState();
    return state.tempo();
}

OSL_API void LinkPhaseGenerator_SetTempo(LinkPhaseGeneratorHandle* x, double bpm) {
    if (x == nullptr)
        return;

    auto state = x->link.captureAppSessionState();
    state.setTempo(safeBpm(bpm), linkNow(x));
    x->link.commitAppSessionState(state);
}

OSL_API int LinkPhaseGenerator_IsPlaying(LinkPhaseGeneratorHandle* x) {
    if (x == nullptr)
        return 0;

    auto state = x->link.captureAppSessionState();
    return state.isPlaying() ? 1 : 0;
}

OSL_API void LinkPhaseGenerator_SetIsPlaying(LinkPhaseGeneratorHandle* x, int isPlaying) {
    if (x == nullptr)
        return;

    auto state = x->link.captureAppSessionState();
    state.setIsPlaying(isPlaying != 0, linkNow(x));
    x->link.commitAppSessionState(state);
}

OSL_API void LinkPhaseGenerator_SetIsPlayingQuantized(LinkPhaseGeneratorHandle* x, int isPlaying, double quantum) {
    if (x == nullptr)
        return;

    auto state = x->link.captureAppSessionState();
    auto now = linkNow(x);
    if (isPlaying != 0)
        state.setIsPlayingAndRequestBeatAtTime(true, now, 0.0, safeQuantum(quantum));
    else
        state.setIsPlaying(false, now);
    x->link.commitAppSessionState(state);
}

OSL_API void LinkPhaseGenerator_RequestBeatZero(LinkPhaseGeneratorHandle* x, double quantum) {
    if (x == nullptr)
        return;

    auto state = x->link.captureAppSessionState();
    state.forceBeatAtTime(0.0, linkNow(x), safeQuantum(quantum));
    x->link.commitAppSessionState(state);
}

OSL_API void LinkPhaseGenerator_GetAudioState(LinkPhaseGeneratorHandle* x, double quantum, double* phase, double* bpm,
                                              int* isPlaying, int* peers) {
    if (phase != nullptr) *phase = 0.0;
    if (bpm != nullptr) *bpm = 120.0;
    if (isPlaying != nullptr) *isPlaying = 0;
    if (peers != nullptr) *peers = 0;
    if (x == nullptr)
        return;

    const double q = safeQuantum(quantum);
    auto state = x->link.captureAudioSessionState();
    auto now = linkNow(x);

    if (phase != nullptr) *phase = state.phaseAtTime(now, q) / q;
    if (bpm != nullptr) *bpm = state.tempo();
    if (isPlaying != nullptr) *isPlaying = state.isPlaying() ? 1 : 0;
    if (peers != nullptr) *peers = static_cast<int>(x->link.numPeers());
}
}
