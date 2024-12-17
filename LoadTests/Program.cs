using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace LoadTest
{
    class Program
    {
        static IPAddress _serverAddress = IPAddress.Parse("127.0.0.1");
        static int _port = 8888;
        static int _clientCount = 5; // Количество клиентов
        static int _requestsPerClient = 100; // Количество запросов на клиента
        static List<string> _imagePaths;
        static Stopwatch _stopWatch;

        static async Task Main(string[] args)
        {
            // Указание директории с изображениями
            string imageDirectory = args.Length > 0 ? args[0] : "InputImages";
            if (!Directory.Exists(imageDirectory))
            {
                Console.WriteLine($"Директория '{imageDirectory}' не найдена.");
                return;
            }

            // Загрузка списка изображений
            _imagePaths = GetImagesFromDirectory(imageDirectory);
            if (_imagePaths.Count == 0)
            {
                Console.WriteLine("В указанной директории отсутствуют изображения.");
                return;
            }

            Console.WriteLine($"Найдено {_imagePaths.Count} изображений. Начало нагрузочного тестирования...");

            var tasks = new List<Task>();
            _stopWatch = new Stopwatch();

            _stopWatch.Start();
            for (int i = 0; i < _clientCount; i++)
            {
                tasks.Add(SimulateClient(i));
            }

            await Task.WhenAll(tasks);

            _stopWatch.Stop();

            Console.WriteLine($"Нагрузочное тестирование завершено. Время: {_stopWatch.ElapsedMilliseconds / 1000} с");
            Console.ReadKey();
        }

        static async Task SimulateClient(int clientId)
        {
            IPEndPoint serverEndPoint = new IPEndPoint(_serverAddress, _port);
            try
            {
                for (int i = 0; i < _requestsPerClient; i++)
                {
                    using (TcpClient client = new TcpClient())
                    {
                        await client.ConnectAsync(serverEndPoint.Address, serverEndPoint.Port);
                        Console.WriteLine($"Клиент {clientId} подключился (запрос {i + 1})");

                        Bitmap bitmap = LoadImageForRequest(i);
                        await SendAndReceiveImage(client, bitmap, clientId, i);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка у клиента {clientId}: {ex.Message}");
            }
        }

        static async Task SendAndReceiveImage(TcpClient client, Bitmap bitmap, int clientId, int requestId)
        {
            NetworkStream stream = client.GetStream();

            using (MemoryStream ms = new MemoryStream())
            {
                bitmap.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                byte[] imageBytes = ms.ToArray();
                int totalBytes = imageBytes.Length;

                // Отправка данных
                byte[] sizeBytes = BitConverter.GetBytes(totalBytes);
                byte[] secondBytes = BitConverter.GetBytes(true); // Мультипоток true / false

                byte[] combinedBytes = sizeBytes.Concat(secondBytes).ToArray();

                await stream.WriteAsync(combinedBytes, 0, combinedBytes.Length);
                await stream.WriteAsync(imageBytes, 0, imageBytes.Length);
                await stream.FlushAsync();
            }

            try
            {
                // Чтение длины изображения
                byte[] lengthBuffer = new byte[4];
                await stream.ReadAsync(lengthBuffer, 0, lengthBuffer.Length);
                int imageLength = BitConverter.ToInt32(lengthBuffer, 0);

                // Чтение времени обработки
                byte[] timeBuffer = new byte[8];
                await stream.ReadAsync(timeBuffer, 0, timeBuffer.Length);
                double processingTime = BitConverter.ToDouble(timeBuffer, 0);

                // Чтение данных изображения
                byte[] imageBuffer = new byte[imageLength];
                int totalBytesRead = 0;
                while (totalBytesRead < imageLength)
                {
                    int bytesRead = await stream.ReadAsync(imageBuffer, totalBytesRead, imageLength - totalBytesRead);
                    if (bytesRead == 0) break;
                    totalBytesRead += bytesRead;
                }

                // Сохранение обработанного изображения
                string outputDirectory = "OutputImages";
                if (!Directory.Exists(outputDirectory))
                {
                    Directory.CreateDirectory(outputDirectory);
                }

                string outputFilePath = Path.Combine(outputDirectory, $"client{clientId}_request{requestId + 1}.png");
                using (MemoryStream ms = new MemoryStream(imageBuffer))
                {
                    Bitmap processedBitmap = new Bitmap(ms);
                    processedBitmap.Save(outputFilePath, System.Drawing.Imaging.ImageFormat.Png);
                }

                Console.WriteLine($"Клиент {clientId} (запрос {requestId + 1}): Обработанное изображение сохранено как {outputFilePath}. Время обработки: {processingTime:F2} мс");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Клиент {clientId} (запрос {requestId + 1}): Ошибка: {ex.Message}");
            }
        }




        static List<string> GetImagesFromDirectory(string directory)
        {
            // Получение списка файлов изображений
            var validExtensions = new[] { ".png", ".jpg", ".jpeg", ".bmp" };
            return Directory.GetFiles(directory)
                            .Where(file => validExtensions.Contains(Path.GetExtension(file).ToLower()))
                            .ToList();
        }

        static Bitmap LoadImageForRequest(int requestIndex)
        {
            // Выбор изображения по индексу запроса
            string imagePath = _imagePaths[requestIndex % _imagePaths.Count];
            return new Bitmap(imagePath);
        }
    }
}
