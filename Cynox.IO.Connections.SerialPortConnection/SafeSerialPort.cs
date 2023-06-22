using System;
using System.IO.Ports;

namespace Cynox.IO.Connections {
	/// <summary>
	/// Wrapper around <see cref="SerialPort"/> that aims to prevent crashes when a virtual COM-Port is removed.
	/// </summary>
	internal class SafeSerialPort : SerialPort {
		public new void Open() {
			if (!IsOpen) {
				base.Open();
				GC.SuppressFinalize(BaseStream);
			}
		}

		public new void Close() {
			if (IsOpen) {
				GC.ReRegisterForFinalize(BaseStream);
				base.Close();
			}			
		}

		protected override void Dispose(bool disposing) {
			try {
				GC.ReRegisterForFinalize(BaseStream);
				base.Dispose(disposing);
			} catch (Exception) {
				// This may throw with something like "Safe handle has been closed" if the USB is disconnected
			}
		}
	}
}
