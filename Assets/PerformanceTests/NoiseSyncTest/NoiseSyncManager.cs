using UnityEngine;

public class NoiseSyncManager : MonoBehaviour
{
    public int seed = 12345;
    public int steps = 1000000;

    void Update()
    {
        if (!Input.GetKeyDown(KeyCode.Space)) return;

        //if (Time.frameCount % 30 != 0) return; // do the global sync every n frames
        
        NoiseSignalGenerator[] generators = FindObjectsOfType<NoiseSignalGenerator>();

        foreach (var generator in generators)
        {
            generator.syncNoiseSignalGenerator(seed, steps);
        }
    }
}
