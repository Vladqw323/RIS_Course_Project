using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TCPServer
{
    public class TCPServer
    {
        private Filter _filter;
        private TcpListener _server;
        private IPEndPoint _ipEndPoint;

        public TCPServer(IPEndPoint iPEndPoint)
        {
            _ipEndPoint = iPEndPoint;

            _filter = new Filter();
            _server = new TcpListener(iPEndPoint);
            _server.Start();

            Console.WriteLine("Сервер запущен. Ожидание подключений... ");
        }

        public async void Listen()
        {
            try
            {
                while (true)
                {
                    TcpClient tcpClient = await _server.AcceptTcpClientAsync();
                    Console.WriteLine($"Входящее подключение: {tcpClient.Client.RemoteEndPoint}");

                    new Thread(HandleClient).Start(tcpClient);
                }
            }
            finally
            {
                _server.Stop();
            }
        }

        private async void HandleClient(object tcpClient)
        {
            TcpClient client = (TcpClient)tcpClient;
            List<byte> imageDataChunks = new List<byte>();

            try
            {
                bool isFirstMessage = true;
                bool isThread = false;

                int totalSize = 0;

                 while (client.Connected)
                 {
                     byte[] buffer = new byte[1024];
                     int bytesRead = await client.GetStream().ReadAsync(buffer, 0, buffer.Length);

                     if (bytesRead == 0)
                     {
                         break;
                     }

                     if (isFirstMessage)
                     {
                         totalSize = BitConverter.ToInt32(buffer, 0);
                         isThread = buffer[4] == 1;

                         imageDataChunks = new List<byte>(totalSize);
                         isFirstMessage = false;
                         continue;
                     }

                     imageDataChunks.AddRange(buffer.Take(bytesRead));
                     if (imageDataChunks.Count == totalSize)
                     {
                         byte[] imageData = imageDataChunks.ToArray();

                         Console.WriteLine($"Receive byte: {imageData.Length}");

                         (Bitmap output, double time) = OperFilter(imageData, isThread);
                         SendImage(client, output, time);

                         buffer = null;
                         imageData = null;
                         imageDataChunks.Clear();
                         isFirstMessage = true;
                         totalSize = 0;
                     }
                 }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception: {ex}");
            }
        }

        private async Task<string> GetMessageAsync(TcpClient client)
        {
            StringBuilder sb = new StringBuilder();
            byte[] buffer = new byte[1024];
            int bytesRead = 0;

            NetworkStream stream = client.GetStream();

            do
            {
                bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                sb.Append(Encoding.UTF8.GetString(buffer, 0, bytesRead));
            } while (stream.DataAvailable);

            return sb.ToString();
        }

        private void SendImage(TcpClient client, Bitmap bitmap, double? time = null)
        {
            byte[] imageData;

            using (MemoryStream ms = new MemoryStream())
            {
                bitmap.Save(ms, ImageFormat.Png);
                imageData = ms.ToArray();
            }

            byte[] lengthPrefix = BitConverter.GetBytes(imageData.Length);

            byte[] finalBuffer;
            if (time.HasValue)
            {
                byte[] timeBytes = BitConverter.GetBytes(time.Value);
                finalBuffer = lengthPrefix.Concat(timeBytes).Concat(imageData).ToArray();
            }
            else
            {
                finalBuffer = lengthPrefix.Concat(imageData).ToArray();
            }

            NetworkStream stream = client.GetStream();
            stream.Write(finalBuffer, 0, finalBuffer.Length);
        }





        private (Bitmap, double) OperFilter(byte[] imageData, bool isThread)
        {
            Bitmap bitmap = new Bitmap(new MemoryStream(imageData));
            TimeHelper timeHelper = new TimeHelper();

            timeHelper.Start();
            Bitmap result = isThread ? _filter.HighFrequencyFilterParallel(bitmap, 2) : _filter.HighFrequencyFilter(bitmap);
            double time = timeHelper.Stop();

            bitmap.Dispose();
            timeHelper = null;

            return (result, time);
        }
    }
}
