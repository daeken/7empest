using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using CommandLine;
using KubeClient;
using KubeClient.Models;

#pragma warning disable 1998

// ReSharper disable ClassNeverInstantiated.Global

namespace Client {
	[Verb("start", HelpText = "Start Tempest on Kubernetes")]
	class StartOptions {
	}

	[Verb("shutdown", HelpText = "Shut down Tempest")]
	class ShutdownOptions {
		[Option('f', "force", Required = false, HelpText = "Force shutdown immediately")]
		public bool Force { get; set; }
	}
	
	[Verb("restart", HelpText = "Restart Tempest")]
	class RestartOptions {
		[Option('f', "force", Required = false, HelpText = "Force shutdown immediately")]
		public bool Force { get; set; }
	}
	
	class Program {
		static Task<int> Main(string[] args) =>
			Parser.Default.ParseArguments<StartOptions, ShutdownOptions, RestartOptions>(args)
				.MapResult<StartOptions, ShutdownOptions, RestartOptions, Task<int>>(
					Start, 
					Shutdown,
					async options => {
						var status = await Shutdown(new ShutdownOptions { Force = options.Force });
						if(status != 0) return status;
						return await Start(new StartOptions());
					}, 
					async _ => 1);

		static async Task<int> Start(StartOptions options) {
			var client = KubeApiClient.Create(new KubeClientOptions {
				ApiEndPoint = new Uri("http://localhost:8001"),
				KubeNamespace = "tempest"
			});
			
			if(!(await client.NamespacesV1().List()).Items.Exists(x => x.Metadata.Name == "tempest"))
				await client.NamespacesV1().Create(new NamespaceV1 {
					Metadata = new ObjectMetaV1 { Name = "tempest" }
				});

			if((await client.ServicesV1().List()).Items.Count != 0) {
				Console.Error.WriteLine("Tempest services already exist! Shut down existing services to start anew");
				return 1;
			}

			var svc = new ServiceV1 {
				Metadata = new ObjectMetaV1 { Name = "tempest", Namespace = "tempest" },
				Kind = "Service",
				Spec = new ServiceSpecV1 {
					Selector = { ["app"] = "tempest" }
				}
			};
			svc.Spec.Ports.Add(new ServicePortV1 { Protocol = "TCP", Port = 12345, TargetPort = 12345 });
			svc.Spec.Type = "NodePort";
			await client.ServicesV1().Create(svc);

			Console.WriteLine("Generating server certificate");
			var serverCert = CertGenerator.Generate();
			Console.WriteLine("Generating client certificate");
			var clientCert = CertGenerator.Generate();
			Console.WriteLine("Done, creating deployment");

			await client.DeploymentsV1().Create(new DeploymentV1 {
				Metadata = new ObjectMetaV1 { Name = "tempest", Namespace = "tempest" },
				Kind = "Deployment",
				Spec = new DeploymentSpecV1 {
					Replicas = (await client.NodesV1().List()).Items.Count,
					Selector = new LabelSelectorV1 {
						MatchLabels = { ["app"] = "tempest" }
					},
					Template = new PodTemplateSpecV1 {
						Metadata = new ObjectMetaV1 { Labels = { ["app"] = "tempest" } },
						Spec = new PodSpecV1 {
							Containers = {
								new ContainerV1 {
									Name = "tempest",
									Image = "daeken/tempest-server:v1",
									ImagePullPolicy = "Always",
									Ports = { new ContainerPortV1 { ContainerPort = 12345 } }, 
									Env = { new EnvVarV1 {
										Name = "SERVER_PFX", 
										Value = Convert.ToBase64String(serverCert.Export(X509ContentType.Pfx))
									}, new EnvVarV1 {
										Name = "CLIENT_CERT", 
										Value = Convert.ToBase64String(clientCert.Export(X509ContentType.Cert))
									} }
								}
							}
						}
					}
				}
			});
			
			Console.WriteLine("Services created, waiting for ready state");
			
			while(true) {
				var deployment = await client.DeploymentsV1().Get("tempest");
				if(deployment.Status.ReadyReplicas != null &&
				   deployment.Status.ReadyReplicas == deployment.Status.Replicas)
					break;
			}
			
			var tempestPath = Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
				".tempest");
			try { File.Delete(Path.Join(tempestPath, "server.cert")); } catch(Exception) {}
			try { File.Delete(Path.Join(tempestPath, "client.pfx")); } catch(Exception) {}
			try { File.Delete(Path.Join(tempestPath, "nodes")); } catch(Exception) {}
			Directory.CreateDirectory(tempestPath);
			await using(var fp = File.OpenWrite(Path.Join(tempestPath, "server.cert")))
				fp.Write(serverCert.Export(X509ContentType.Cert));
			await using(var fp = File.OpenWrite(Path.Join(tempestPath, "client.pfx")))
				fp.Write(serverCert.Export(X509ContentType.Pfx));
			var ips = new List<string>();
			await using(var fp = File.OpenWrite(Path.Join(tempestPath, "nodes"))) {
				await using var sw = new StreamWriter(fp);
				foreach(var elem in await client.NodesV1().List()) {
					var ip = elem.Status.Addresses[2].Address;
					sw.WriteLine(ip);
					ips.Add(ip);
				}
			}
			Console.WriteLine($"Tempest started successfully on {(await client.DeploymentsV1().Get("tempest")).Status.Replicas} nodes");
			Console.WriteLine("Checking authentication");

			var pclient = new Protocol.Client(new TcpClient(ips[0], 12345));

			return 0;
		}

		static async Task<int> Shutdown(ShutdownOptions options) {
			var client = KubeApiClient.Create(new KubeClientOptions {
				ApiEndPoint = new Uri("http://localhost:8001"),
				KubeNamespace = "tempest"
			});
			
			await client.DeploymentsV1().Delete("tempest");
			await client.ServicesV1().Delete("tempest");
			
			Console.WriteLine("Tempest shut down successfully");
			
			return 0;
		}
	}
}