using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;

namespace Protocol {
	public class ObjectPipe : IDisposable {
		readonly BinaryFormatter Formatter = new BinaryFormatter();
		readonly Stream Stream;
		readonly Dictionary<Type, ConcurrentQueue<object>> Incoming = new Dictionary<Type, ConcurrentQueue<object>>();
		readonly Thread ReaderThread;

		public ObjectPipe(Stream stream) {
			Stream = stream;
			ReaderThread = new Thread(Reader);
			ReaderThread.Start();
		}

		public void Push<T>(T obj) => Formatter.Serialize(Stream, obj);
		
		public T Pull<T>() {
			ConcurrentQueue<object> queue;
			// TODO: Proper synchronization primitives here
			while(!Incoming.TryGetValue(typeof(T), out queue))
				Thread.Sleep(50);
			object obj;
			while(!queue.TryDequeue(out obj))
				Thread.Sleep(10);
			return (T) obj;
		}

		void Reader() {
			while(true) {
				var obj = Formatter.Deserialize(Stream);
				if(!Incoming.TryGetValue(obj.GetType(), out var queue))
					queue = Incoming[obj.GetType()] = new ConcurrentQueue<object>();
				queue.Enqueue(obj);
			}
		}

		public void Dispose() {
			Stream?.Dispose();
			ReaderThread.Abort();
		}
	}
}