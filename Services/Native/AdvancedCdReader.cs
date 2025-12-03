using NAudio.Wave;
using System.Runtime.InteropServices;

namespace nexENCODE_Studio.Services.Native
{
    /// <summary>
    /// Advanced CD-DA (Digital Audio) reader using Windows DeviceIoControl
    /// This provides actual digital audio extraction from physical CDs
    /// </summary>
    public class AdvancedCdReader : IDisposable
    {
        private const int CD_SECTOR_SIZE = 2352; // CD-DA raw sector size
        private const int CD_FRAMES_PER_SECOND = 75;
        private const int SAMPLE_RATE = 44100;
        
        private IntPtr _driveHandle = IntPtr.Zero;
        private readonly char _driveLetter;
        private bool _disposed;

        public AdvancedCdReader(char driveLetter)
        {
            _driveLetter = char.ToUpper(driveLetter);
        }

        /// <summary>
        /// Opens the CD drive for raw reading
        /// </summary>
        public void Open()
        {
            if (_driveHandle != IntPtr.Zero)
                return;

            string devicePath = $"\\\\.\\{_driveLetter}:";
            _driveHandle = NativeMethods.CreateFile(
                devicePath,
                NativeMethods.GENERIC_READ,
                NativeMethods.FILE_SHARE_READ | NativeMethods.FILE_SHARE_WRITE,
                IntPtr.Zero,
                NativeMethods.OPEN_EXISTING,
                0,
                IntPtr.Zero
            );

            if (_driveHandle == IntPtr.Zero || _driveHandle == new IntPtr(-1))
            {
                int error = Marshal.GetLastWin32Error();
                throw new InvalidOperationException($"Failed to open CD drive {_driveLetter}: Error code {error}");
            }
        }

        /// <summary>
        /// Reads raw CD-DA audio data from a specific sector range
        /// </summary>
        public int ReadRawSectors(int startSector, int sectorCount, byte[] buffer, int offset)
        {
            if (_driveHandle == IntPtr.Zero)
                Open();

            // Allocate input structure
            var rawReadInfo = new NativeMethods.RAW_READ_INFO
            {
                DiskOffset = (long)startSector * CD_SECTOR_SIZE,
                SectorCount = sectorCount,
                TrackMode = NativeMethods.TRACK_MODE_TYPE.CDDA
            };

            int inputSize = Marshal.SizeOf(rawReadInfo);
            IntPtr inputBuffer = Marshal.AllocHGlobal(inputSize);
            int outputSize = sectorCount * CD_SECTOR_SIZE;
            IntPtr outputBuffer = Marshal.AllocHGlobal(outputSize);

            try
            {
                Marshal.StructureToPtr(rawReadInfo, inputBuffer, false);

                bool success = NativeMethods.DeviceIoControl(
                    _driveHandle,
                    NativeMethods.IOCTL_CDROM_RAW_READ,
                    inputBuffer,
                    (uint)inputSize,
                    outputBuffer,
                    (uint)outputSize,
                    out uint bytesReturned,
                    IntPtr.Zero
                );

                if (success && bytesReturned > 0)
                {
                    Marshal.Copy(outputBuffer, buffer, offset, (int)bytesReturned);
                    return (int)bytesReturned;
                }
                else
                {
                    int error = Marshal.GetLastWin32Error();
                    // Return 0 for read errors - caller can retry or skip
                    return 0;
                }
            }
            finally
            {
                Marshal.FreeHGlobal(inputBuffer);
                Marshal.FreeHGlobal(outputBuffer);
            }
        }

        /// <summary>
        /// Reads an entire CD track to WAV file with error recovery
        /// </summary>
        public void ReadTrackToWavFile(CdTrackInfo trackInfo, string outputFile, IProgress<int>? progress, CancellationToken cancellationToken)
        {
            var waveFormat = new WaveFormat(SAMPLE_RATE, 16, 2);
            
            using (var writer = new WaveFileWriter(outputFile, waveFormat))
            {
                // Calculate sectors
                int startSector = (int)(trackInfo.StartPosition.TotalSeconds * CD_FRAMES_PER_SECOND);
                int totalSectors = (int)(trackInfo.Length.TotalSeconds * CD_FRAMES_PER_SECOND);
                
                const int sectorsPerRead = 26; // ~1/3 second chunks (industry standard)
                byte[] buffer = new byte[CD_SECTOR_SIZE * sectorsPerRead];
                
                int sectorsRead = 0;
                int consecutiveErrors = 0;
                const int maxConsecutiveErrors = 10;

                while (sectorsRead < totalSectors && !cancellationToken.IsCancellationRequested)
                {
                    int sectorsToRead = Math.Min(sectorsPerRead, totalSectors - sectorsRead);
                    int currentSector = startSector + sectorsRead;

                    int bytesRead = ReadRawSectors(currentSector, sectorsToRead, buffer, 0);

                    if (bytesRead > 0)
                    {
                        writer.Write(buffer, 0, bytesRead);
                        sectorsRead += sectorsToRead;
                        consecutiveErrors = 0;

                        // Report progress
                        int percentComplete = Math.Min(100, (sectorsRead * 100) / totalSectors);
                        progress?.Report(percentComplete);
                    }
                    else
                    {
                        // Read error - try to recover
                        consecutiveErrors++;
                        
                        if (consecutiveErrors >= maxConsecutiveErrors)
                        {
                            throw new InvalidOperationException($"Too many consecutive read errors at sector {currentSector}");
                        }

                        // Write silence for bad sectors and continue
                        Array.Clear(buffer, 0, sectorsToRead * CD_SECTOR_SIZE);
                        writer.Write(buffer, 0, sectorsToRead * CD_SECTOR_SIZE);
                        sectorsRead += sectorsToRead;
                    }
                }

                progress?.Report(100);
            }
        }

        /// <summary>
        /// Closes the CD drive
        /// </summary>
        public void Close()
        {
            if (_driveHandle != IntPtr.Zero && _driveHandle != new IntPtr(-1))
            {
                NativeMethods.CloseHandle(_driveHandle);
                _driveHandle = IntPtr.Zero;
            }
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                Close();
                _disposed = true;
            }
        }

        /// <summary>
        /// Native Windows API methods for CD reading
        /// </summary>
        private static class NativeMethods
        {
            public const uint GENERIC_READ = 0x80000000;
            public const uint FILE_SHARE_READ = 0x00000001;
            public const uint FILE_SHARE_WRITE = 0x00000002;
            public const uint OPEN_EXISTING = 3;
            public const uint IOCTL_CDROM_RAW_READ = 0x0002403E;

            [StructLayout(LayoutKind.Sequential)]
            public struct RAW_READ_INFO
            {
                public long DiskOffset;
                public int SectorCount;
                public TRACK_MODE_TYPE TrackMode;
            }

            public enum TRACK_MODE_TYPE
            {
                YellowMode2 = 0,
                XAForm2 = 1,
                CDDA = 2
            }

            [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
            public static extern IntPtr CreateFile(
                string lpFileName,
                uint dwDesiredAccess,
                uint dwShareMode,
                IntPtr lpSecurityAttributes,
                uint dwCreationDisposition,
                uint dwFlagsAndAttributes,
                IntPtr hTemplateFile);

            [DllImport("kernel32.dll", SetLastError = true)]
            public static extern bool DeviceIoControl(
                IntPtr hDevice,
                uint dwIoControlCode,
                IntPtr lpInBuffer,
                uint nInBufferSize,
                IntPtr lpOutBuffer,
                uint nOutBufferSize,
                out uint lpBytesReturned,
                IntPtr lpOverlapped);

            [DllImport("kernel32.dll", SetLastError = true)]
            public static extern bool CloseHandle(IntPtr hObject);
        }
    }
}
