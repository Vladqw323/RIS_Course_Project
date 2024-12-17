using System;
using System.Net;

namespace TCPServer
{
    internal class Program
    {
        static IPAddress _localAddress = IPAddress.Parse("127.0.0.1");
        static int _port = 8888;
        static IPEndPoint _localIPEndPoint = new IPEndPoint(_localAddress, _port);

        static private TCPServer _server;

        static void Main(string[] args)
        {
            _server = new TCPServer(_localIPEndPoint);
            _server.Listen();
            Console.ReadKey();
        }
    }
}
