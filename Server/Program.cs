using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Threading;

namespace Server {
	class Program {
		public static readonly X509Certificate2Collection ServerCert = new X509Certificate2Collection(); 
		public static readonly X509Certificate2Collection ClientCert = new X509Certificate2Collection(); 
		
		static void Main(string[] args) {
			ServerCert.Import(Convert.FromBase64String(Environment.GetEnvironmentVariable("SERVER_PFX")));
			ClientCert.Import(Convert.FromBase64String(Environment.GetEnvironmentVariable("CLIENT_CERT")));
			
			var server = new TcpListener(IPAddress.Any, 12345);
			server.Start();
			Console.WriteLine("Running server");
			while(true)
				new Thread(_client => new ClientDomain((TcpClient) _client).Start()).Start(server.AcceptTcpClient());
		}
	}
}