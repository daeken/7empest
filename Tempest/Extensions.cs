using System.Collections.Generic;

namespace Tempest {
	public static class Extensions {
		public static TempestQuery<T> AsTempest<T>(this IEnumerable<T> source, Connection connection = null) =>
			new TempestQuery<T>(source, connection);
	}
}