using System.Runtime.InteropServices;
using System.Text;

namespace nexENCODE_Studio.Services.Native
{
    /// <summary>
    /// Native methods for Windows Media Control Interface (MCI)
    /// </summary>
    internal static class MciNativeMethods
    {
        [DllImport("winmm.dll")]
        public static extern int mciSendString(string command, StringBuilder? returnValue, int returnLength, IntPtr hwndCallback);

        [DllImport("winmm.dll")]
        public static extern int mciGetErrorString(int errorCode, StringBuilder errorText, int errorTextSize);

        /// <summary>
        /// Gets a user-friendly error message from MCI error code
        /// </summary>
        public static string GetErrorMessage(int errorCode)
        {
            if (errorCode == 0)
                return "Success";

            var errorText = new StringBuilder(256);
            mciGetErrorString(errorCode, errorText, errorText.Capacity);
            return errorText.ToString();
        }

        /// <summary>
        /// Executes an MCI command and returns the result
        /// </summary>
        public static string ExecuteCommand(string command, int bufferSize = 256)
        {
            var returnValue = new StringBuilder(bufferSize);
            int result = mciSendString(command, returnValue, returnValue.Capacity, IntPtr.Zero);

            if (result != 0)
            {
                throw new InvalidOperationException($"MCI Error: {GetErrorMessage(result)}");
            }

            return returnValue.ToString();
        }

        /// <summary>
        /// Executes an MCI command without return value
        /// </summary>
        public static void ExecuteCommandNoReturn(string command)
        {
            int result = mciSendString(command, null, 0, IntPtr.Zero);

            if (result != 0)
            {
                // Include numeric result and command text to aid diagnostics
                throw new InvalidOperationException($"MCI Error {result} executing '{command}': {GetErrorMessage(result)}");
            }
        }
    }
}
