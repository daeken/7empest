using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Tempest {
	public sealed class TempestQuery<T> : IEnumerable<T> {
		readonly IEnumerable<T> Source;
		readonly Connection Connection;
		
		internal TempestQuery(IEnumerable<T> source, Connection connection) {
			Source = source;
			Connection = connection ?? Connection.Global;
			if(Connection == null)
				throw new TempestException("No connection to Tempest server");
		}

		public IEnumerator<T> GetEnumerator() => Source.GetEnumerator();
		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
	}
}