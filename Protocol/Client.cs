using System;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;

namespace Protocol {
	public class Client {
		X509Certificate2Collection CertCollection;
		
		public Client(TcpClient client) {
			if(CertCollection == null) {
				CertCollection = new X509Certificate2Collection();
				CertCollection.Import(File.ReadAllBytes(Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
					".tempest", "client.pfx")));
			}

			var stream = client.GetStream();
			var ssl = new SslStream(stream);
			ssl.AuthenticateAsClient("tempest", CertCollection, SslProtocols.Tls13, false);
			
			var sr = new StreamReader(ssl);
			Console.WriteLine(sr.ReadLine());
		}
	}
}