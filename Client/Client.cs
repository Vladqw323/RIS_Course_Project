using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ClientApp
{
    public class Client1 
    {
        private TcpClient _client;

        public Client1(IPEndPoint iPEndPoint)
        {
            _client = new TcpClient();

            _client.Connect(iPEndPoint);
        }

        public async Task<(Bitmap, double)> SendImage(Bitmap bitmap, bool thread)
        {
            try
            {
                NetworkStream stream = _client.GetStream();

                using (MemoryStream ms = new MemoryStream())
                {
                    bitmap.Save(ms, System.Drawing.Imaging.ImageFormat.Png);

                    byte[] imageBytes = ms.ToArray();
                    int totalBytes = imageBytes.Length;
                    int offset = 0;

                    byte[] firstBytes = BitConverter.GetBytes(totalBytes);
                    byte[] secondBytes = BitConverter.GetBytes(thread);

                    byte[] combinedBytes = firstBytes.Concat(secondBytes).ToArray();
                    await SendDataAsync(stream, combinedBytes);

                    while (offset < totalBytes)
                    {
                        int chunkSize = Math.Min(1024, totalBytes - offset);

                        byte[] chunk = new byte[chunkSize];
                        Array.Copy(imageBytes, offset, chunk, 0, chunkSize);


                        await stream.WriteAsync(chunk, 0, chunkSize);
                        await stream.FlushAsync();

                        offset += chunkSize;
                    }
                }

                (Bitmap image, double time) = await ReceiveImageWithTimeAsync(stream);

                return (image, time);

            }
            catch (Exception ex)
            {
                MessageBox.Show($"{ex.ToString()}", "Ошибка");
            }

            return (null, 0);

        }

        private async Task<(Bitmap, double)> ReceiveImageWithTimeAsync(NetworkStream stream)
        {
            // Получение длины данных изображения
            byte[] lengthBuffer = new byte[4];
            await stream.ReadAsync(lengthBuffer, 0, lengthBuffer.Length);
            int imageLength = BitConverter.ToInt32(lengthBuffer, 0);

            // Получение времени
            byte[] timeBuffer = new byte[8]; // double занимает 8 байтов
            await stream.ReadAsync(timeBuffer, 0, timeBuffer.Length);
            double time = BitConverter.ToDouble(timeBuffer, 0);

            // Получение изображения
            byte[] imageBuffer = new byte[imageLength];
            int bytesRead = 0;
            while (bytesRead < imageLength)
            {
                int read = await stream.ReadAsync(imageBuffer, bytesRead, imageLength - bytesRead);
                if (read == 0)
                {
                    throw new Exception("Неожиданное завершение потока при чтении изображения.");
                }
                bytesRead += read;
            }

            using (MemoryStream ms = new MemoryStream(imageBuffer))
            {
                Bitmap bitmap = new Bitmap(ms);
                return (bitmap, time);
            }
        }



        private async Task<byte[]> ReceiveDataWithLengthAsync(NetworkStream stream)
        {
            byte[] lengthBuffer = new byte[4];
            int bytesRead = await stream.ReadAsync(lengthBuffer, 0, lengthBuffer.Length);
            if (bytesRead != 4)
                throw new Exception("Ошибка чтения длины данных.");

            int dataLength = BitConverter.ToInt32(lengthBuffer, 0);
            byte[] dataBuffer = new byte[dataLength];
            int totalBytesRead = 0;

            while (totalBytesRead < dataLength)
            {
                bytesRead = await stream.ReadAsync(
                    dataBuffer,
                    totalBytesRead,
                    dataLength - totalBytesRead
                );

                if (bytesRead == 0)
                    throw new Exception("Неожиданное завершение потока.");

                totalBytesRead += bytesRead;
            }

            return dataBuffer;
        }


        private static async Task SendDataAsync(NetworkStream stream, byte[] data)
        {
            await stream.WriteAsync(data, 0, data.Length);
            await stream.FlushAsync();
        }
    }
}



