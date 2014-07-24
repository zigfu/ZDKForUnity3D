using System;

public static class AudioToEnergy {

    public const float MinEnergyValue = 0.0f;       // Note: The values assigned gere for MinEnergyValue and MaxEnergyValue should not be altered
    public const float MaxEnergyValue = 1.0f;

    const int AudioSamplesPerEnergySample = 80;     // Number of audio samples represented by each element in _energyBuffer
    const float EnergyNoiseFloor = 0.2f;            // Bottom portion of computed energy signal that will be discarded as noise.


    // Summary:
    //      Calculates the "energy" of an audio signal (audioBuffer) and stores it in energyBuffer
    // 
    // Returns:
    //      The number of energy samples that were added to energyBuffer
    //
    static public uint Convert(byte[] audioBuffer, int numSamplesToProcess, float[] energyBuffer, uint startIndex)
    {
        if (numSamplesToProcess >= audioBuffer.Length)
        {
            numSamplesToProcess = audioBuffer.Length;
        }

        uint energyCreated = 0;
        double accumulatedSquareSum = 0;           // Sum of squares of audio samples being accumulated to compute the next energy value.
        int accumulatedSampleCount = 0;            // Number of audio samples accumulated so far to compute the next energy value.

        for (int i = 0; i < numSamplesToProcess; i += 2)
        {
            // Compute the sum of squares of audio samples that will get accumulated into a single energy value.
            short audioSample = BitConverter.ToInt16(audioBuffer, i);
            accumulatedSquareSum += audioSample * audioSample;
            ++accumulatedSampleCount;

            if (accumulatedSampleCount < AudioSamplesPerEnergySample)
            {
                continue;
            }

            // Each energy value will represent the logarithm of the mean of the sum of squares of a group of audio samples.
            double meanSquare = accumulatedSquareSum / AudioSamplesPerEnergySample;
            double amplitude = Math.Log(meanSquare) / Math.Log(int.MaxValue);

            // Truncate portion of signal below noise floor
            float amplitudeAboveNoise = (float)Math.Max(0, amplitude - EnergyNoiseFloor);

            // Renormalize signal above noise floor to [0,1] range.
            int idx = (int)(startIndex + energyCreated) % energyBuffer.Length;
            energyBuffer[idx] = amplitudeAboveNoise / (1 - EnergyNoiseFloor);
            energyCreated++;

            accumulatedSquareSum = 0;
            accumulatedSampleCount = 0;
        }

        return energyCreated;
    }
}
