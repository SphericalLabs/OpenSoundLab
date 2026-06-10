#pragma once

#if defined(_MSC_VER)
#define OSL_API __declspec(dllexport)
#elif defined(__GNUC__) || defined(__clang__)
#define OSL_API __attribute__((visibility("default")))
#else
#define OSL_API
#endif
