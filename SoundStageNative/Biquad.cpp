#include "Biquad.h"
#include <math.h>

#define BIQUAD_UNDEFINED 0 //unlike the other const types, this one should not be accessible from the public API

void Biquad_calculateCoeffs(Biquad *x)
{
    x->A = sqrtf(powf(10, x->gain/20));
    x->w0 = 2*M_PI*x->frequency/x->sampleRate;
    x->cosW0 = cosf(x->w0);
    x->sinW0 = sinf(x->w0);
    x->alpha = x->sinW0/(2*x->Q);
    
    switch (x->type)
    {
        case BIQUAD_LOWPASS:
            x->a0 = 1 + x->alpha;
            x->a1 = -2*x->cosW0;
            x->a2 = 1 - x->alpha;
            x->b0 = (1 - x->cosW0)/2;
            x->b1 = 1 - x->cosW0;
            x->b2 = (1 - x->cosW0)/2;
            break;

        case BIQUAD_HIGHPASS:
            x->a0 = 1 + x->alpha;
            x->a1 = -2*x->cosW0;
            x->a2 = 1 - x->alpha;
            x->b0 = (1 + x->cosW0)/2;
            x->b1 = -(1 + x->cosW0);
            x->b2 = (1 + x->cosW0)/2;
            break;

        case BIQUAD_LOWSHELF:
            x->b0 = x->A*((x->A+1) - (x->A-1)*x->cosW0 + 2*sqrtf(x->A)*x->alpha);
            x->b1 = 2*x->A* ((x->A-1) - (x->A+1)*x->cosW0);
            x->b2 = x->A*((x->A+1) - (x->A-1)*x->cosW0 - 2*sqrtf(x->A)*x->alpha);
            x->a0 = (x->A+1) + (x->A-1)*x->cosW0 + 2*sqrtf(x->A)*x->alpha;
            x->a1 = -2*((x->A-1) + (x->A+1)*x->cosW0);
            x->a2 = (x->A+1) + (x->A-1)*x->cosW0 - 2*sqrtf(x->A)*x->alpha;
            break;

        case BIQUAD_HIGHSHELF:
            x->b0 = x->A*((x->A+1) + (x->A-1)*x->cosW0 + 2*sqrtf(x->A)*x->alpha);
            x->b1 = -2*x->A*((x->A-1) + (x->A+1)*x->cosW0);
            x->b2 = x->A*((x->A+1) + (x->A-1)*x->cosW0 - 2*sqrtf(x->A)*x->alpha);
            x->a0 = (x->A+1) - (x->A-1)*x->cosW0 + 2*sqrtf(x->A)*x->alpha;
            x->a1 = 2*((x->A-1) - (x->A+1)*x->cosW0);
            x->a2 = (x->A+1) - (x->A-1)*x->cosW0 - 2*sqrtf(x->A)*x->alpha;
            break;

        case BIQUAD_PEAK:
            x->a0 = 1 + x->alpha/x->A;
            x->a1 = -2 * x->cosW0;
            x->a2 = 1 - x->alpha/x->A;
            x->b0 = 1 + x->alpha*x->A;
            x->b1 = -2 * x->cosW0;
            x->b2 = 1 - x->alpha*x->A;
            break;
    
        default:
            x->a0 = 1;
            x->a1 = 0;
            x->a2 = 0;
            x->b0 = 1;
            x->b1 = 0;
            x->b2 = 0;
                break;
        }
        
        x->b0_over_a0 = x->b0/x->a0;
        x->a1_over_a0 = x->a1/x->a0;
        x->a2_over_a0 = x->a2/x->a0;
        x->b1_over_a0 = x->b1/x->a0;
        x->b2_over_a0 = x->b2/x->a0;
}

void Biquad_setParameters(Biquad *x, int type, float frequency, float Q, float gain, float sampleRate)
{
    int recalculateCoeffs = 0;
    if(gain != x->gain)
    {
        recalculateCoeffs = 1;
        x->gain = gain;
    }
    if(type != x->type)
    {
        recalculateCoeffs = 1;
        x->type = type;
    }
    if(frequency != x->frequency)
    {
        recalculateCoeffs = 1;
        x->frequency = frequency;
    }
    if(Q != x->Q)
    {
        recalculateCoeffs = 1;
        x->Q = Q;
    }
    if(sampleRate != x->sampleRate)
    {
        recalculateCoeffs = 1;
        x->sampleRate = sampleRate;
    }

    if(recalculateCoeffs)
        Biquad_calculateCoeffs(x);
}

Biquad *Biquad_new(int type, float frequency, float Q, float gain, float sampleRate, int channels)
{
    Biquad *x = (Biquad *) _malloc(sizeof(Biquad));
    x->type = BIQUAD_UNDEFINED; //just to make sure coefficients get updated
    Biquad_setParameters(x, type, frequency, Q, gain, sampleRate);
    x->channels = channels;
    //need to remember z^-1 and z^-2 for both input and output and for each channel:
    x->xMem = (float*)_malloc(channels * 2 * sizeof(float));
    x->yMem = (float*)_malloc(channels * 2 * sizeof(float));
    return x;
}

void Biquad_free(Biquad *x)
{
    _free(x->xMem);
    _free(x->yMem);
    _free(x);
}

void Biquad_processChannel(Biquad *x, int type, float frequency, float Q, float gain, float sampleRate, float *in, float *out, int n, int channel)
{
    int offset = 2 * channel; //bc each channel has a memory of size 2
    
    float x0; //current input sample
    float y0; //current output sample
    float x1 = x->xMem[offset]; //x^-1, previous input sample
    float x2 = x->xMem[offset + 1]; //x^-2
    float y1 = x->yMem[offset]; //previous output sample
    float y2 = x->yMem[offset + 1];
    
    while(n--)
    {
        //advance input sample
        x0 = *in++;
        
        //apply filter
        *out++ = y0 = x->b0_over_a0 * x0 + x->b1_over_a0 * x1 + x->b2_over_a0 * x2
               - x->a1_over_a0 * y1 - x->a2_over_a0 * y2;
                
        //temp memory
        y2 = y1;
        y1 = y0;
        x2 = x1;
        x1 = x0;
    }
    
    //permanent memory
    x->yMem[offset + 1] = y1;
    x->yMem[offset] = y0;
    x->xMem[offset + 1] = x1;
    x->xMem[offset] = x0;
}

void Biquad_process(Biquad *x, int type, float frequency, float Q, float gain, float sampleRate, float *in, float *out, int n)
{
    //Set params & re-calculate coefficients if necessary
    Biquad_setParameters(x, type, frequency, Q, gain, sampleRate);
    
    //De-interlave audio data and calculate some intermediate values
    int channels = x->channels;
    int nPerChannel = n / channels;
    float *ptr = out;
    _fDeinterleave(in, out, n, channels);
    
    //process each channel
    for(int i = 0; i < channels; i++)
    {
        Biquad_processChannel(x, type, frequency, Q, gain, sampleRate, ptr, ptr, nPerChannel, i); //left channel
        ptr += nPerChannel;
    }
    
    //interleave audio data
    _fInterleave(out, out, n, channels);
}



