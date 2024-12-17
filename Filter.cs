using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace TCPServer
{
    public class Filter
    {
        // Лапласиан
        private readonly int[,] kernel = {
            { -1, -1, -1 },
            { -1,  8, -1 },
            { -1, -1, -1 }
        };

        public Filter() { }

        public Bitmap HighFrequencyFilter(Bitmap image)
        {
            Console.WriteLine("соло поток");
            int width = image.Width;
            int height = image.Height;
            Bitmap result = new Bitmap(width, height);

            BitmapData sourceData = image.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
            BitmapData resultData = result.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.WriteOnly, PixelFormat.Format24bppRgb);

            int bytesPerPixel = Image.GetPixelFormatSize(PixelFormat.Format24bppRgb) / 8;
            int stride = sourceData.Stride;
            IntPtr sourceScan0 = sourceData.Scan0;
            IntPtr resultScan0 = resultData.Scan0;

            byte[] sourceBuffer = new byte[stride * height];
            byte[] resultBuffer = new byte[stride * height];

            Marshal.Copy(sourceScan0, sourceBuffer, 0, sourceBuffer.Length);

            // Применение ядра 3x3 требует обхода с 1 до height-1 и 1 до width-1
            for (int y = 1; y < height - 1; y++)
            {
                for (int x = 1; x < width - 1; x++)
                {
                    int pixelIndexCenter = (y * stride) + (x * bytesPerPixel);

                    int sumR = 0, sumG = 0, sumB = 0;

                    for (int ky = -1; ky <= 1; ky++)
                    {
                        for (int kx = -1; kx <= 1; kx++)
                        {
                            int pixelIndex = ((y + ky) * stride) + ((x + kx) * bytesPerPixel);

                            byte B = sourceBuffer[pixelIndex];
                            byte G = sourceBuffer[pixelIndex + 1];
                            byte R = sourceBuffer[pixelIndex + 2];

                            int factor = kernel[ky + 1, kx + 1];
                            sumB += B * factor;
                            sumG += G * factor;
                            sumR += R * factor;
                        }
                    }

                    // Применяем ограничение диапазона
                    byte newB = (byte)Clamp(sumB);
                    byte newG = (byte)Clamp(sumG);
                    byte newR = (byte)Clamp(sumR);

                    resultBuffer[pixelIndexCenter] = newB;
                    resultBuffer[pixelIndexCenter + 1] = newG;
                    resultBuffer[pixelIndexCenter + 2] = newR;
                }
            }

            Marshal.Copy(resultBuffer, 0, resultScan0, resultBuffer.Length);

            image.UnlockBits(sourceData);
            result.UnlockBits(resultData);

            return result;
        }

        public Bitmap HighFrequencyFilterParallel(Bitmap image, int threadCount)
        {
            Console.WriteLine("мультипоток");
            int width = image.Width;
            int height = image.Height;
            Bitmap result = new Bitmap(width, height);

            BitmapData sourceData = image.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
            BitmapData resultData = result.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.WriteOnly, PixelFormat.Format24bppRgb);

            int bytesPerPixel = Image.GetPixelFormatSize(PixelFormat.Format24bppRgb) / 8;
            int stride = sourceData.Stride;
            byte[] sourceBuffer = new byte[stride * height];
            byte[] resultBuffer = new byte[stride * height];

            Marshal.Copy(sourceData.Scan0, sourceBuffer, 0, sourceBuffer.Length);
            image.UnlockBits(sourceData);

            CountdownEvent countdown = new CountdownEvent(height - 2);

            for (int y = 1; y < height - 1; y++)
            {
                int currentY = y;
                ThreadPool.QueueUserWorkItem(_ =>
                {
                    try
                    {
                        for (int x = 1; x < width - 1; x++)
                        {
                            int pixelIndexCenter = (currentY * stride) + (x * bytesPerPixel);

                            int sumR = 0, sumG = 0, sumB = 0;

                            for (int ky = -1; ky <= 1; ky++)
                            {
                                for (int kx = -1; kx <= 1; kx++)
                                {
                                    int pixelIndex = ((currentY + ky) * stride) + ((x + kx) * bytesPerPixel);

                                    byte B = sourceBuffer[pixelIndex];
                                    byte G = sourceBuffer[pixelIndex + 1];
                                    byte R = sourceBuffer[pixelIndex + 2];

                                    int factor = kernel[ky + 1, kx + 1];

                                    sumB += B * factor;
                                    sumG += G * factor;
                                    sumR += R * factor;
                                }
                            }

                            byte newB = (byte)Clamp(sumB);
                            byte newG = (byte)Clamp(sumG);
                            byte newR = (byte)Clamp(sumR);

                            resultBuffer[pixelIndexCenter] = newB;
                            resultBuffer[pixelIndexCenter + 1] = newG;
                            resultBuffer[pixelIndexCenter + 2] = newR;
                        }
                    }
                    finally
                    {
                        countdown.Signal();
                    }
                });
            }

            countdown.Wait();

            Marshal.Copy(resultBuffer, 0, resultData.Scan0, resultBuffer.Length);
            result.UnlockBits(resultData);

            return result;
        }

        static int Clamp(int value) => Math.Max(0, Math.Min(255, value));
    }
}
