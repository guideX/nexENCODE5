using nexENCODE_Studio.Services;
using nexENCODE_Studio.Visualization;

namespace nexENCODE_Studio.Examples
{
    /// <summary>
    /// Examples demonstrating audio visualization capabilities
    /// </summary>
    public static class VisualizationExamples
    {
        /// <summary>
        /// Example 1: Real-time spectrum analyzer during playback
        /// </summary>
        public static void RealtimeSpectrumExample()
        {
            var player = new AudioPlayerService
            {
                VisualizationEnabled = true
            };

            // Configure visualization
            var vizOptions = new VisualizationOptions
            {
                FftSize = 4096,
                SpectrumBands = 128,
                MinFrequency = 20f,
                MaxFrequency = 20000f,
                UseLogScale = true
            };

            // Load audio file
            player.Load(@"C:\Music\song.mp3", vizOptions);

            // Subscribe to spectrum updates
            var vizEngine = player.GetVisualizationEngine();
            if (vizEngine != null)
            {
                vizEngine.SpectrumUpdated += (sender, spectrum) =>
                {
                    Console.WriteLine($"Spectrum update: {spectrum.FrequencySpectrum.Length} bands");
                    
                    // Display frequency bands (simplified)
                    for (int i = 0; i < Math.Min(10, spectrum.FrequencySpectrum.Length); i++)
                    {
                        float magnitude = spectrum.FrequencySpectrum[i];
                        float frequency = spectrum.FrequencyLabels[i];
                        Console.WriteLine($"  {frequency:F0} Hz: {magnitude:F2} dB");
                    }
                };

                vizEngine.PeakLevelsUpdated += (sender, peaks) =>
                {
                    Console.WriteLine($"L: {peaks.LeftPeak:F3} | R: {peaks.RightPeak:F3}");
                    if (peaks.LeftClipping || peaks.RightClipping)
                        Console.WriteLine("?? CLIPPING DETECTED!");
                };
            }

            // Play audio
            player.Play();

            // Keep playing for 10 seconds
            Thread.Sleep(10000);

            player.Stop();
            player.Dispose();
        }

        /// <summary>
        /// Example 2: Generate static waveform for display
        /// </summary>
        public static void StaticWaveformExample()
        {
            Console.WriteLine("=== Generating Waveform ===\n");

            string audioFile = @"C:\Music\song.mp3";
            int displayWidth = 800; // pixels

            var waveform = WaveformGenerator.GenerateWaveform(audioFile, displayWidth);

            Console.WriteLine($"Waveform generated:");
            Console.WriteLine($"  Width: {waveform.Width} points");
            Console.WriteLine($"  Samples per pixel: {waveform.SamplesPerPixel}");
            Console.WriteLine($"  Duration: {waveform.EndTime}");
            Console.WriteLine();

            // Display ASCII waveform (simplified)
            Console.WriteLine("Waveform preview (first 80 points):");
            for (int i = 0; i < Math.Min(80, waveform.WaveformPoints.Length); i++)
            {
                float amplitude = waveform.WaveformPoints[i];
                int barHeight = (int)(amplitude * 20);
                Console.Write(new string('|', barHeight) + " ");
            }
            Console.WriteLine();
        }

        /// <summary>
        /// Example 3: Stereo waveform generation
        /// </summary>
        public static void StereoWaveformExample()
        {
            Console.WriteLine("=== Stereo Waveform ===\n");

            string audioFile = @"C:\Music\stereo_song.mp3";
            int width = 500;

            var (left, right) = WaveformGenerator.GenerateStereoWaveform(audioFile, width);

            Console.WriteLine($"Generated stereo waveform:");
            Console.WriteLine($"  Left channel: {left.Length} points");
            Console.WriteLine($"  Right channel: {right.Length} points");
            Console.WriteLine();

            // Compare channels
            float leftAvg = left.Average();
            float rightAvg = right.Average();
            
            Console.WriteLine($"Average levels:");
            Console.WriteLine($"  Left: {leftAvg:F4}");
            Console.WriteLine($"  Right: {rightAvg:F4}");
            
            if (Math.Abs(leftAvg - rightAvg) < 0.01f)
                Console.WriteLine("  Channels are balanced ?");
            else
                Console.WriteLine($"  Balance difference: {Math.Abs(leftAvg - rightAvg):F4}");
        }

        /// <summary>
        /// Example 4: VU meter simulation
        /// </summary>
        public static void VuMeterExample()
        {
            var player = new AudioPlayerService
            {
                VisualizationEnabled = true
            };

            player.Load(@"C:\Music\song.mp3");

            var vizEngine = player.GetVisualizationEngine();
            if (vizEngine != null)
            {
                vizEngine.PeakLevelsUpdated += (sender, peaks) =>
                {
                    // Clear console line
                    Console.Write("\r");

                    // Draw VU meters
                    DrawVuMeter("L", peaks.LeftPeak, peaks.LeftClipping);
                    Console.Write(" | ");
                    DrawVuMeter("R", peaks.RightPeak, peaks.RightClipping);

                    // Show peak hold
                    Console.Write($" | Peak L:{peaks.LeftDecayingPeak:F2} R:{peaks.RightDecayingPeak:F2}");
                };
            }

            player.Play();
            Thread.Sleep(30000); // Run for 30 seconds
            player.Stop();
            player.Dispose();
        }

        private static void DrawVuMeter(string label, float level, bool clipping)
        {
            Console.Write($"{label}[");
            
            int barLength = 20;
            int filled = (int)(level * barLength);
            
            for (int i = 0; i < barLength; i++)
            {
                if (i < filled)
                    Console.Write(clipping ? "!" : "?");
                else
                    Console.Write(" ");
            }
            
            Console.Write($"] {level * 100:F0}%");
        }

        /// <summary>
        /// Example 5: Audio analysis
        /// </summary>
        public static void AudioAnalysisExample()
        {
            var analyzer = new AudioAnalysisService();

            Console.WriteLine("=== Audio Analysis ===\n");

            string audioFile = @"C:\Music\song.mp3";
            var analysis = analyzer.AnalyzeAudioFile(audioFile);

            Console.WriteLine($"File: {Path.GetFileName(audioFile)}");
            Console.WriteLine();
            Console.WriteLine($"Peak Amplitude: {analysis.PeakAmplitude:F4} ({20 * Math.Log10(analysis.PeakAmplitude):F2} dB)");
            Console.WriteLine($"RMS Level: {analysis.RMSLevel:F4} ({20 * Math.Log10(analysis.RMSLevel):F2} dB)");
            Console.WriteLine($"Dynamic Range: {analysis.DynamicRange:F2} dB");
            Console.WriteLine($"Crest Factor: {analysis.CrestFactor:F2}");
            Console.WriteLine($"Clipped Samples: {analysis.ClippedSamples}");
            Console.WriteLine($"Total Silence: {analysis.TotalSilence}");
            Console.WriteLine($"Silence Regions: {analysis.SilenceRegions.Count}");

            if (analysis.ClippedSamples > 0)
                Console.WriteLine("\n?? WARNING: Clipping detected!");

            if (analysis.DynamicRange < 6)
                Console.WriteLine("\n?? WARNING: Low dynamic range (possibly over-compressed)");
        }

        /// <summary>
        /// Example 6: ReplayGain calculation
        /// </summary>
        public static void ReplayGainExample()
        {
            var analyzer = new AudioAnalysisService();

            Console.WriteLine("=== ReplayGain Calculation ===\n");

            string audioFile = @"C:\Music\song.mp3";
            var (gain, peak) = analyzer.CalculateReplayGain(audioFile);

            Console.WriteLine($"Track Gain: {gain:F2} dB");
            Console.WriteLine($"Track Peak: {peak:F4} ({20 * Math.Log10(peak):F2} dB)");
            Console.WriteLine();

            if (gain > 0)
                Console.WriteLine($"Suggested increase: +{gain:F2} dB");
            else
                Console.WriteLine($"Suggested decrease: {gain:F2} dB");

            if (peak > 0.95f)
                Console.WriteLine("?? WARNING: High peak level, may clip if gain is applied");
        }

        /// <summary>
        /// Example 7: True peak detection
        /// </summary>
        public static void TruePeakExample()
        {
            var analyzer = new AudioAnalysisService();

            Console.WriteLine("=== True Peak Detection ===\n");

            string audioFile = @"C:\Music\song.mp3";
            float truePeak = analyzer.CalculateTruePeak(audioFile);

            Console.WriteLine($"True Peak: {truePeak:F4}");
            Console.WriteLine($"True Peak dB: {20 * Math.Log10(truePeak):F2} dB");
            Console.WriteLine();

            if (truePeak > 1.0f)
                Console.WriteLine("?? WARNING: True peak exceeds 0 dBFS!");
            else if (truePeak > 0.99f)
                Console.WriteLine("?? CAUTION: Very close to 0 dBFS");
            else
                Console.WriteLine("? True peak is safe");
        }

        /// <summary>
        /// Example 8: Effective bit depth detection
        /// </summary>
        public static void BitDepthDetectionExample()
        {
            var analyzer = new AudioAnalysisService();

            Console.WriteLine("=== Bit Depth Detection ===\n");

            string audioFile = @"C:\Music\song.flac";
            int effectiveBits = analyzer.DetectEffectiveBitDepth(audioFile);

            Console.WriteLine($"Effective Bit Depth: ~{effectiveBits} bits");
            Console.WriteLine();

            if (effectiveBits < 16)
                Console.WriteLine("?? WARNING: Low bit depth, may be upsampled from lower quality");
            else if (effectiveBits == 16)
                Console.WriteLine("Standard CD quality (16-bit)");
            else if (effectiveBits > 16)
                Console.WriteLine("High resolution audio (>16-bit)");
        }

        /// <summary>
        /// Example 9: Custom visualization with configuration
        /// </summary>
        public static void CustomVisualizationExample()
        {
            var player = new AudioPlayerService
            {
                VisualizationEnabled = true
            };

            // Custom visualization settings
            var vizOptions = new VisualizationOptions
            {
                FftSize = 8192,              // Higher resolution
                SpectrumBands = 64,          // Fewer bands
                MinFrequency = 30f,          // Focus on 30 Hz - 16 kHz
                MaxFrequency = 16000f,
                UseLogScale = true,          // Logarithmic frequency scale
                SmoothingFactor = 5,         // More smoothing
                PeakDecayRate = 0.98f,       // Slower peak decay
                ClippingThreshold = 0.95f    // Lower clipping threshold
            };

            player.Load(@"C:\Music\song.mp3", vizOptions);

            var vizEngine = player.GetVisualizationEngine();
            if (vizEngine != null)
            {
                vizEngine.SpectrumUpdated += (sender, spectrum) =>
                {
                    Console.WriteLine($"Custom spectrum: {spectrum.FrequencySpectrum.Length} bands");
                };
            }

            player.Play();
            Thread.Sleep(10000);
            player.Stop();
            player.Dispose();
        }

        /// <summary>
        /// Example 10: Waveform with time range
        /// </summary>
        public static void WaveformRangeExample()
        {
            Console.WriteLine("=== Waveform Time Range ===\n");

            string audioFile = @"C:\Music\song.mp3";
            
            // Generate waveform for middle 30 seconds
            var startTime = TimeSpan.FromSeconds(30);
            var endTime = TimeSpan.FromSeconds(60);
            int width = 600;

            var waveform = WaveformGenerator.GenerateWaveformRange(
                audioFile, 
                startTime, 
                endTime, 
                width
            );

            Console.WriteLine($"Waveform for time range {startTime} - {endTime}:");
            Console.WriteLine($"  Width: {waveform.Width} points");
            Console.WriteLine($"  Samples per pixel: {waveform.SamplesPerPixel}");
            Console.WriteLine();

            // Find peak in this range
            float peak = waveform.WaveformPoints.Max();
            int peakIndex = Array.IndexOf(waveform.WaveformPoints, peak);
            var peakTime = startTime + TimeSpan.FromSeconds(
                (endTime - startTime).TotalSeconds * peakIndex / width
            );

            Console.WriteLine($"Peak in range: {peak:F4} at {peakTime}");
        }

        /// <summary>
        /// Example 11: Complete audio file visualization report
        /// </summary>
        public static void CompleteVisualizationReport()
        {
            var analyzer = new AudioAnalysisService();
            var player = new AudioPlayerService();

            Console.WriteLine("=== Complete Audio Visualization Report ===\n");

            string audioFile = @"C:\Music\song.mp3";
            Console.WriteLine($"Analyzing: {Path.GetFileName(audioFile)}\n");

            // 1. Basic analysis
            Console.WriteLine("--- Audio Analysis ---");
            var analysis = analyzer.AnalyzeAudioFile(audioFile);
            Console.WriteLine($"Peak: {analysis.PeakAmplitude:F4} ({20 * Math.Log10(analysis.PeakAmplitude):F2} dB)");
            Console.WriteLine($"RMS: {analysis.RMSLevel:F4} ({20 * Math.Log10(analysis.RMSLevel):F2} dB)");
            Console.WriteLine($"Dynamic Range: {analysis.DynamicRange:F2} dB");
            Console.WriteLine($"Crest Factor: {analysis.CrestFactor:F2}");
            Console.WriteLine();

            // 2. ReplayGain
            Console.WriteLine("--- ReplayGain ---");
            var (gain, peak) = analyzer.CalculateReplayGain(audioFile);
            Console.WriteLine($"Track Gain: {gain:F2} dB");
            Console.WriteLine($"Track Peak: {peak:F4}");
            Console.WriteLine();

            // 3. True Peak
            Console.WriteLine("--- True Peak ---");
            float truePeak = analyzer.CalculateTruePeak(audioFile);
            Console.WriteLine($"True Peak: {truePeak:F4} ({20 * Math.Log10(truePeak):F2} dBTP)");
            Console.WriteLine();

            // 4. Bit Depth
            Console.WriteLine("--- Bit Depth ---");
            int bitDepth = analyzer.DetectEffectiveBitDepth(audioFile);
            Console.WriteLine($"Effective: ~{bitDepth} bits");
            Console.WriteLine();

            // 5. Waveform
            Console.WriteLine("--- Waveform ---");
            var waveform = WaveformGenerator.GenerateWaveform(audioFile, 100);
            Console.WriteLine($"Generated: {waveform.Width} points");
            Console.WriteLine($"Duration: {waveform.EndTime}");
            Console.WriteLine();

            // 6. Warnings
            Console.WriteLine("--- Warnings ---");
            if (analysis.ClippedSamples > 0)
                Console.WriteLine($"?? {analysis.ClippedSamples} clipped samples");
            if (analysis.DynamicRange < 6)
                Console.WriteLine("?? Low dynamic range (over-compressed)");
            if (truePeak > 1.0f)
                Console.WriteLine("?? True peak exceeds 0 dBFS");
            if (analysis.TotalSilence.TotalSeconds > 5)
                Console.WriteLine($"?? {analysis.TotalSilence.TotalSeconds:F1}s of silence detected");
            Console.WriteLine();

            Console.WriteLine("Report complete.");
        }
    }
}
