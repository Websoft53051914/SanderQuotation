using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace backend.Common
{
    public static class RawPrinterHelper
    {
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        public class DOCINFOA
        {
            [MarshalAs(UnmanagedType.LPStr)] public string pDocName;
            [MarshalAs(UnmanagedType.LPStr)] public string pOutputFile;
            [MarshalAs(UnmanagedType.LPStr)] public string pDataType;
        }

        // ------------------------
        // Windows API (winspool)
        // ------------------------
        [DllImport("winspool.Drv", EntryPoint = "OpenPrinterA", SetLastError = true)]
        public static extern bool OpenPrinter(string szPrinter, out IntPtr hPrinter, IntPtr pd);

        [DllImport("winspool.Drv", EntryPoint = "ClosePrinter", SetLastError = true)]
        public static extern bool ClosePrinter(IntPtr hPrinter);

        [DllImport("winspool.Drv", EntryPoint = "StartDocPrinterA", SetLastError = true)]
        public static extern bool StartDocPrinter(IntPtr hPrinter, int level, [In] DOCINFOA di);

        [DllImport("winspool.Drv", EntryPoint = "EndDocPrinter", SetLastError = true)]
        public static extern bool EndDocPrinter(IntPtr hPrinter);

        [DllImport("winspool.Drv", EntryPoint = "StartPagePrinter", SetLastError = true)]
        public static extern bool StartPagePrinter(IntPtr hPrinter);

        [DllImport("winspool.Drv", EntryPoint = "EndPagePrinter", SetLastError = true)]
        public static extern bool EndPagePrinter(IntPtr hPrinter);

        [DllImport("winspool.Drv", EntryPoint = "WritePrinter", SetLastError = true)]
        public static extern bool WritePrinter(IntPtr hPrinter, IntPtr pBytes, int dwCount, out int dwWritten);

        // ------------------------
        // Linux CUPS 列印
        // ------------------------
        private static bool SendBytesToPrinter_Linux(string printerName, byte[] bytes)
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "lp",
                    Arguments = $"-d \"{printerName}\"",
                    RedirectStandardInput = true,
                    UseShellExecute = false
                };

                using var process = Process.Start(psi);

                process.StandardInput.BaseStream.Write(bytes, 0, bytes.Length);
                process.StandardInput.BaseStream.Flush();
                process.StandardInput.Close();

                process.WaitForExit();

                return process.ExitCode == 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Linux printing failed: {ex}");
                return false;
            }
        }

        // ------------------------
        // 主入口（自動判斷平台）
        // ------------------------
        public static bool SendBytesToPrinter(string printerName, byte[] bytes)
        {
            // 1. Linux → CUPS
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                return SendBytesToPrinter_Linux(printerName, bytes);

            // 2. Windows → Winspool
            IntPtr hPrinter;
            var di = new DOCINFOA() { pDocName = "ESC/POS Print", pDataType = "RAW" };
            bool success = false;

            if (OpenPrinter(printerName, out hPrinter, IntPtr.Zero))
            {
                if (StartDocPrinter(hPrinter, 1, di))
                {
                    StartPagePrinter(hPrinter);

                    IntPtr unmanagedPointer = Marshal.AllocCoTaskMem(bytes.Length);
                    Marshal.Copy(bytes, 0, unmanagedPointer, bytes.Length);
                    success = WritePrinter(hPrinter, unmanagedPointer, bytes.Length, out _);
                    Marshal.FreeCoTaskMem(unmanagedPointer);

                    EndPagePrinter(hPrinter);
                    EndDocPrinter(hPrinter);
                }
                ClosePrinter(hPrinter);
            }
            return success;
        }
    }
}