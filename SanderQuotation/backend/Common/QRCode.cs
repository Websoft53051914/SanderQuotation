using Business.BusinessLogic;
using DocumentFormat.OpenXml.EMMA;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Graph.Models.TermStore;
using NPOI.SS.Formula.Functions;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System.IO.Ports;
using System.Net.Sockets;
using System.Text;
//using static Org.BouncyCastle.Math.EC.ECCurve;

namespace backend.Common
{
    public class QRCode
    {
        static public bool PrintWithUsb(string printerName, string content, string qrData)
        {
            // 註冊 Big5 編碼
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            //string printerName = "POS CP-Q3";

            // ESC/POS 指令
            byte[] reset = new byte[] { 0x1B, 0x40 };              // ESC @
            byte[] alignCenter = new byte[] { 0x1B, 0x61, 0x01 }; // ESC a 1 → 置中
            byte[] alignLeft = new byte[] { 0x1B, 0x61, 0x00 };   // ESC a 0 → 左對齊
            byte[] cut = new byte[] { 0x1D, 0x56, 0x00 };         // GS V 0 → 全切紙

            // 中文文字
            string text = content;

            text += "\r\n";

            byte[] textBytes = Encoding.GetEncoding("big5").GetBytes(text);

            // QR Code 文字
            //string qrData = qrData;

            // ESC/POS QR Code 指令 (CP-Q3 支援 ESC/POS)
            // 1. 設定模型（Model 2）
            byte[] model = new byte[] { 0x1D, 0x28, 0x6B, 0x04, 0x00, 0x31, 0x41, 0x32, 0x00 };
            // 2. 設定大小（Module size 6）
            //byte[] size = new byte[] { 0x1D, 0x28, 0x6B, 0x03, 0x00, 0x31, 0x43, 0x06 };
            byte[] size = new byte[] { 0x1D, 0x28, 0x6B, 0x03, 0x00, 0x31, 0x43, 0x0A }; // Module Size = 10

            // 3. 設定容錯等級（Level M）
            byte[] error = new byte[] { 0x1D, 0x28, 0x6B, 0x03, 0x00, 0x31, 0x45, 0x30 };
            // 4. 儲存 QR Code 資料
            byte[] store = new byte[8 + qrData.Length];
            store[0] = 0x1D;
            store[1] = 0x28;
            store[2] = 0x6B;
            store[3] = (byte)((qrData.Length + 3) & 0xFF);       // pL
            store[4] = (byte)(((qrData.Length + 3) >> 8) & 0xFF); // pH
            store[5] = 0x31;
            store[6] = 0x50;
            store[7] = 0x30;
            // 把 QR Data 放到 store 後面
            byte[] qrBytes = Encoding.ASCII.GetBytes(qrData);
            Array.Copy(qrBytes, 0, store, 8, qrBytes.Length);

            // 5. 列印 QR Code
            byte[] printQR = new byte[] { 0x1D, 0x28, 0x6B, 0x03, 0x00, 0x31, 0x51, 0x30 };

            byte[] newLines = new byte[] { 0x0A, 0x0A, 0x0A, 0x0A, 0x0A, 0x0A }; // LF 三次

            // 合併所有 byte[]
            byte[] dataToSend = Combine(reset, alignCenter, textBytes, model, size, error, store, printQR, newLines, cut);

            // 發送到印表機
            bool result = RawPrinterHelper.SendBytesToPrinter(printerName, dataToSend);
            return result;
        }

        static byte[] Combine(params byte[][] arrays)
        {
            int len = 0;
            foreach (var arr in arrays) len += arr.Length;
            byte[] result = new byte[len];
            int offset = 0;
            foreach (var arr in arrays)
            {
                Buffer.BlockCopy(arr, 0, result, offset, arr.Length);
                offset += arr.Length;
            }
            return result;
        }

        public static void Print(string printerIp, int printerPort, List<string> contents, string qrData)
        {
            try
            {
                // ESC/POS 指令組合：以 Epson 指令為例
                byte[] qrModel = new byte[] { 0x1D, 0x28, 0x6B, 0x04, 0x00, 0x31, 0x41, 0x32, 0x00 }; // 選擇 QR 模型
                byte[] qrSize = SetQrCodeSize(12);        // QR 模組大小（1–16）
                byte[] qrError = new byte[] { 0x1D, 0x28, 0x6B, 0x03, 0x00, 0x31, 0x45, 0x30 };       // 容錯等級：30 = L
                byte[] qrStorePrefix = new byte[] { 0x1D, 0x28, 0x6B };
                byte[] qrStoreHeader = new byte[] { 0x31, 0x50, 0x30 };                              // 儲存 QR 資料
                byte[] qrDataBytes = Encoding.UTF8.GetBytes(qrData);
                int qrDataLen = qrDataBytes.Length + 3;

                byte[] qrStore = new byte[qrStorePrefix.Length + 2 + qrStoreHeader.Length + qrDataBytes.Length];
                Buffer.BlockCopy(qrStorePrefix, 0, qrStore, 0, qrStorePrefix.Length);
                qrStore[3] = (byte)(qrDataLen % 256); // LSB
                qrStore[4] = (byte)(qrDataLen / 256); // MSB
                Buffer.BlockCopy(qrStoreHeader, 0, qrStore, 5, qrStoreHeader.Length);
                Buffer.BlockCopy(qrDataBytes, 0, qrStore, 8, qrDataBytes.Length);

                byte[] qrPrint = new byte[] { 0x1D, 0x28, 0x6B, 0x03, 0x00, 0x31, 0x51, 0x30 };       // 列印 QR Code
                byte[] fullCut = new byte[] { 0x1D, 0x56, 0x41, 0x00 }; // ESC V A n（n=0） 切斷

                using (TcpClient client = new TcpClient(printerIp, printerPort))
                using (NetworkStream stream = client.GetStream())
                {
                    // 設定字體大小
                    byte[] fontSize = new byte[] { 0x1D, 0x21, 0x00 }; // 0x11 放大 2 倍寬高 0x00：原始大小
                    stream.Write(fontSize, 0, fontSize.Length);

                    //foreach (var item in contents)
                    //{
                    //    var newitem = "GB2312" + item;
                    //    byte[] data = Encoding.GetEncoding("GB2312").GetBytes(newitem);
                    //    stream.Write(data, 0, data.Length);
                    //    stream.Write(Encoding.UTF8.GetBytes("\n"), 0, 1); // 換行
                    //}

                    foreach (var item in contents)
                    {
                        byte[] data = Encoding.GetEncoding("gb18030").GetBytes(item);
                        stream.Write(data, 0, data.Length);
                        stream.Write(Encoding.UTF8.GetBytes("\n"), 0, 1); // 換行
                    }

                    stream.Write(Encoding.UTF8.GetBytes("\n\n"), 0, 2); // 推紙2行

                    //置中
                    byte[] alignCenter = new byte[] { 0x1B, 0x61, 0x01 };
                    stream.Write(alignCenter, 0, alignCenter.Length);

                    stream.Write(qrModel, 0, qrModel.Length);
                    stream.Write(qrSize, 0, qrSize.Length);
                    stream.Write(qrError, 0, qrError.Length);
                    stream.Write(qrStore, 0, qrStore.Length);
                    stream.Write(qrPrint, 0, qrPrint.Length);

                    stream.Write(Encoding.UTF8.GetBytes("\n\n\n"), 0, 3); // 推紙5行

                    stream.Write(fullCut, 0, fullCut.Length); //切斷
                }
            }
            catch (Exception ex)
            {

                throw;
            }
        }

        private static byte[] SetQrCodeSize(int size)
        {
            if (size < 1) size = 1;
            if (size > 16) size = 16;

            return new byte[] { 0x1D, 0x28, 0x6B, 0x03, 0x00, 0x31, 0x43, (byte)size };
        }

        /// <summary>
        /// 將圖片轉換為 ESC/POS 光柵位圖指令
        /// </summary>
        /// <param name="imagePath"></param>
        /// <returns></returns>
        private static byte[] ConvertImageToRasterCommand(string imagePath)
        {
            using var image = Image.Load<Rgba32>(imagePath);

            // 1. 轉黑白並限制寬度為384像素（印表機寬度，必要時請調整）
            int maxWidth = 384;
            if (image.Width > maxWidth)
            {
                image.Mutate(x => x.Resize(maxWidth, 0));
            }

            image.Mutate(x => x.BinaryThreshold(0.5f)); // 簡單轉黑白

            int width = image.Width;
            int height = image.Height;
            int widthBytes = (width + 7) / 8;

            // 2. 準備影像點資料
            byte[] imageBytes = new byte[widthBytes * height];

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    var pixel = image[x, y];
                    bool isBlack = pixel.R < 128; // 黑色定義

                    if (isBlack)
                    {
                        int byteIndex = y * widthBytes + x / 8;
                        imageBytes[byteIndex] |= (byte)(0x80 >> (x % 8));
                    }
                }
            }

            // 3. 組合 ESC/POS 光柵位圖指令
            List<byte> command = new();

            command.Add(0x1D); // GS
            command.Add(0x76); // 'v'
            command.Add(0x30); // '0'
            command.Add(0x00); // m = 0（正常倍率）

            command.Add((byte)(widthBytes % 256));     // xL
            command.Add((byte)(widthBytes / 256));     // xH
            command.Add((byte)(height % 256));         // yL
            command.Add((byte)(height / 256));         // yH

            command.AddRange(imageBytes);

            return command.ToArray();
        }

        /// <summary>
        /// 列印郵件收據
        /// </summary>
        /// <param name="printerIp"></param>
        /// <param name="port"></param>
        /// <param name="contents"></param>
        /// <param name="imagePath"></param>
        public static void PrintMailRecMESt(string printerIp, int port, List<string> contents, string imagePath)
        {
            byte[] cmd = ConvertImageToRasterCommand(imagePath);

            using (TcpClient client = new TcpClient(printerIp, port))
            using (NetworkStream stream = client.GetStream())
            {
                // 設定字體大小
                byte[] fontSize = new byte[] { 0x1D, 0x21, 0x00 }; // 0x11 放大 2 倍寬高 0x00：原始大小
                stream.Write(fontSize, 0, fontSize.Length);

                foreach (var item in contents)
                {
                    byte[] data = Encoding.GetEncoding("gb18030").GetBytes(item);
                    stream.Write(data, 0, data.Length);
                    stream.Write(Encoding.UTF8.GetBytes("\n"), 0, 1); // 換行
                }
                stream.Write(Encoding.UTF8.GetBytes("\n"), 0, 1);
                //圖片
                stream.Write(cmd, 0, cmd.Length);
                stream.Write(Encoding.UTF8.GetBytes("\n\n\n\n"), 0, 4);

                stream.Write(new byte[] { 0x1D, 0x56, 0x41, 0x00 });
            }

        }
        public static void PrintMailRecMESt(string printerIp, int port, List<string> contents, IWebHostEnvironment webhost, IConfiguration config)
        {
            //收據圖片
            string imgPath = Path.Combine(webhost.ContentRootPath, "wwwroot", "image", config["RecMEStFileName"]);

            byte[] cmd = ConvertImageToRasterCommand(imgPath);

            using (TcpClient client = new TcpClient(printerIp, port))
            using (NetworkStream stream = client.GetStream())
            {
                // 設定字體大小
                byte[] fontSize = new byte[] { 0x1D, 0x21, 0x00 }; // 0x11 放大 2 倍寬高 0x00：原始大小
                stream.Write(fontSize, 0, fontSize.Length);

                foreach (var item in contents)
                {
                    byte[] data = Encoding.GetEncoding("gb18030").GetBytes(item);
                    stream.Write(data, 0, data.Length);
                    stream.Write(Encoding.UTF8.GetBytes("\n"), 0, 1); // 換行
                }
                stream.Write(Encoding.UTF8.GetBytes("\n"), 0, 1);
                //圖片
                stream.Write(cmd, 0, cmd.Length);
                stream.Write(Encoding.UTF8.GetBytes("\n\n\n\n"), 0, 4);

                stream.Write(new byte[] { 0x1D, 0x56, 0x41, 0x00 });
            }

        }

    }
}
