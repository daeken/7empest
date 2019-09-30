using System;
using System.Diagnostics;
using System.IO;
using System.Security;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace Client {
	public static class CertGenerator {
		public static X509Certificate2 Generate() {
			var rsa = RSA.Create(2048);
			Console.WriteLine("Foo?");
			Console.ReadLine();
			var req = new CertificateRequest(new X500DistinguishedName("CN=tempest"), rsa, HashAlgorithmName.SHA512,
				RSASignaturePadding.Pss); // TODO: Figure out why TF this is crashing on Mac
			Console.WriteLine("Signing... ?");
			return req.CreateSelfSigned(DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddYears(10));

			/*var rng = new Random();
			var br = $"{rng.Next():X}-{rng.Next():X}";
			var keyfile = Path.Join(Path.GetTempPath(), $"{br}-key.pem");
			var certfile = Path.Join(Path.GetTempPath(), $"{br}-cert.pem");
			var combfile = Path.Join(Path.GetTempPath(), $"{br}-cert.pfx");
			using(var process = new Process()) {
				process.StartInfo.UseShellExecute = true;
				process.StartInfo.FileName = "openssl";
				process.StartInfo.Arguments =
					$"req -newkey rsa:2048 -nodes -keyout {keyfile} -x509 -days 3650 -out {certfile} -subj /C=US/ST=Foo/L=Bar/O=Baz/OU=Hax/CN=tempest";
				// TODO: O hai command injection
				process.Start();
				process.WaitForExit();
			}
			using(var process = new Process()) {
				process.StartInfo.UseShellExecute = true;
				process.StartInfo.FileName = "openssl";
				process.StartInfo.Arguments =
					$"pkcs12 -inkey {keyfile} -in {certfile} -export -out {combfile} -passout pass:tempfakepass";
				// TODO: O hai command injection
				process.Start();
				process.WaitForExit();
			}

			Console.WriteLine("....?!?!?!");
			Console.ReadLine();
			var cert = new X509Certificate2(combfile, "tempfakepass",
				X509KeyStorageFlags.Exportable | X509KeyStorageFlags.MachineKeySet);
			var rcert = new X509Certificate2(cert.Export(X509ContentType.Pfx, "omgwtfhax"), "omgwtfhax", X509KeyStorageFlags.Exportable);
			Console.WriteLine("....?");
			Console.ReadLine();
			return rcert;*/
		}
	}
}