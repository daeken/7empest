using System;
using System.IO;
using System.Net.Security;
using System.Net.Sockets;
using System.Runtime.Loader;
using System.Security.Authentication;

// ReSharper disable ClassNeverInstantiated.Global

namespace Server {
	public class ClientDomain {
		readonly TcpClient Client;
		readonly Stream Stream;
		readonly AssemblyLoadContext Context = new AssemblyLoadContext("ClientDomain", true);
		
		public ClientDomain(TcpClient client) {
			Client = client;
			Stream = client.GetStream();
		}

		~ClientDomain() {
			try {
				Stream.Close();
				Client.Close();
			} catch(Exception) {}

			Context.Unload();
		}

		public void Start() {
			try {
				var ssl = new SslStream(Stream, false, (_, clientCert, __, ___) => clientCert.Equals(Program.ClientCert[0]));
				ssl.AuthenticateAsServer(Program.ServerCert[0], true, SslProtocols.Tls13, false);
				using var sw = new StreamWriter(ssl);
				sw.WriteLine("SUCCESS!");
			} catch(Exception e) {
				Console.WriteLine(e);
			}
		}
	}
}