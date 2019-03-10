using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API;
using Vintagestory.API.MathTools;

namespace VSRidged
{
    class RidgetNoise : NormalizedSimplexNoise
    {
        public RidgetNoise(double[] inputAmplitudes, double[] frequencies, long seed) : base(inputAmplitudes, frequencies, seed)
        {
        }

        public static new RidgetNoise FromDefaultOctaves(int quantityOctaves, double baseFrequency, double persistence, long seed)
        {
            double[] frequencies = new double[quantityOctaves];
            double[] amplitudes = new double[quantityOctaves];

            for (int i = 0; i < quantityOctaves; i++)
            {
                frequencies[i] = Math.Pow(2, i) * baseFrequency;
                amplitudes[i] = Math.Pow(persistence, i);
            }

            return new RidgetNoise(amplitudes, frequencies, seed);
        }



        public override double Noise(double x, double y)
        {
            double value = 0;

            for (int i = 0; i < scaledAmplitudes2D.Length; i++)
            {
                value -= Math.Abs(1.2 * octaves[i].Evaluate(x * frequencies[i], y * frequencies[i]) * scaledAmplitudes2D[i]);
            }

            return Math.Tanh(value) + 1.0;

        }

        public override double Noise(double x, double y, double z)
        {
            double value = 0;

            for (int i = 0; i < scaledAmplitudes3D.Length; i++)
            {
                value -= Math.Abs(1.2 * octaves[i].Evaluate(x * frequencies[i], y * frequencies[i], z * frequencies[i]) * scaledAmplitudes3D[i]);
            }

            return Math.Tanh(value) + 1.0;
        }

        public override double Noise(double x, double y, double z, double[] amplitudes)
        {
            double value = 0;

            for (int i = 0; i < scaledAmplitudes3D.Length; i++)
            {
                value -= Math.Abs(1.2 * octaves[i].Evaluate(x * frequencies[i], y * frequencies[i], z * frequencies[i])) * amplitudes[i];
            }
            return Math.Tanh(-Math.Abs(value)) + 1.0;
        }

        public new double Noise(double x, double y, double z, double[] amplitudes, double[] thresholds)
        {
            double value = 0;

            for (int i = 0; i < scaledAmplitudes3D.Length; i++)
            {
                double val = 2.0 * (Math.Abs(octaves[i].Evaluate(x * frequencies[i], y * frequencies[i], z * frequencies[i])) * - amplitudes[i]) - 1.0;        

                value +=  1.2 * (val > 0 ? Math.Max(0.0, val - thresholds[i]) : Math.Min(0.0, val + thresholds[i]));
            }
            return Math.Tanh(value) / 2 + 0.5;
        }

        public double Noise(double x, double y, double z, double[] simplexAmplitudes, double[] simplexThresholds, double[] ridgedAmplitudes, double[] ridgedThresholds)
        {
            //Method gets called in GenTerra, line 383

            double value = 0;
            double mountains = 0;
            double rivers = 0;

            for (int i = 0; i < scaledAmplitudes3D.Length; i++)
            {
                double val = octaves[i].Evaluate(x * frequencies[i], y * frequencies[i], z * frequencies[i]) * simplexAmplitudes[i];
                value += 1.2 * (val > 0 ? Math.Max(0, val - simplexThresholds[i]) : Math.Min(0, val + simplexThresholds[i]));

                double ridged = 1.2 * Math.Abs(octaves[i].Evaluate(x * frequencies[i], y * frequencies[i], z * frequencies[i])) * ridgedAmplitudes[i] + ridgedThresholds[i];

                if (ridged > 0)
                {
                    mountains -= ridged;
                }
                else
                {
                    rivers -= ridged; //Maybe: rivers = Math.Min(rivers, -ridged)
                }

        
            }
            //This can probably be optimized to only call Tanh once
            return Math.Min(Math.Max(Math.Tanh(mountains) + 1.0, Math.Tanh(value) / 2 + 0.5), Math.Tanh(rivers));
        }
    }
}
