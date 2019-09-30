using System;

namespace Tempest {
	public class TempestException : Exception {
		public TempestException(string message) : base(message) {}
	}
}