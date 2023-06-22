using System.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Ports;

namespace Cynox.IO.Connections
{
    /// <summary>
    /// <see cref="IConnection"/> to be used for serial port connections.
    /// </summary>
    public class SerialPortConnection : IConnection
    {
        private readonly SafeSerialPort _Port;

        /// <summary>
        /// Creates a new instance.
        /// </summary>
        /// <param name="port">The serial port name (e.g. COM1)</param>
        /// <param name="baud">The desired baud rate (e.g. 9600, 38400, 115200). Default = 9600.</param>
        public SerialPortConnection(string port, int baud = 9600)
        {
            _Port = new SafeSerialPort { Parity = Parity.None, DataBits = 8, StopBits = StopBits.One };
            _Port.PortName = port;
            _Port.BaudRate = baud;
            _Port.DataReceived += PortOnDataReceived;
        }

        /// <summary>
        /// The <see cref="SerialPort"/> 
        /// </summary>
        public SerialPort Port => _Port;

        /// <summary>
        /// Gets or sets the serial port name.
        /// </summary>
        public string PortName {
            get => _Port.PortName;
            set => _Port.PortName = value;
        }

        /// <summary>
        /// Gets or sets the baud rate.
        /// </summary>
        public int BaudRate {
            get => _Port.BaudRate;
            set => _Port.BaudRate = value;
        }

        /// <summary>
        /// Gets a list of serial port names for the current computer.
        /// </summary>
        /// <returns>List of port names.</returns>
        public IList<string> GetPortNames() => SerialPort.GetPortNames().ToList();

        private void PortOnDataReceived(object sender, SerialDataReceivedEventArgs serialDataReceivedEventArgs)
        {
            try
            {
                var buf = new byte[_Port.BytesToRead];
                _Port.Read(buf, 0, buf.Length);
                OnDataReceived(new List<byte>(buf));
            }
            catch (Exception ex)
            {
                throw new ConnectionException("Error reading data from port.", ex);
            }
        }

        private void OnDataReceived(IList<byte> data)
        {
            DataReceived?.Invoke(this, new ConnectionDataReceivedEventArgs(data));
        }

        #region IModControlConnection

        /// <inheritdoc />
        public event Action<object, ConnectionDataReceivedEventArgs> DataReceived;

        /// <summary>
        /// Checks if the serial port is currently open.
        /// </summary>
        public bool IsConnected => _Port != null && _Port.IsOpen;

        /// <summary>
        /// Opens the serial port.
        /// </summary>
        /// <exception cref="ConnectionException"></exception>
        public void Connect()
        {
            try
            {
                if (!_Port.IsOpen)
                {
                    _Port.Open();
                }
            }
            catch (Exception ex)
            {
                throw new ConnectionException("Failed to open port.", ex);
            }
        }

        /// <summary>
        /// Closed the serial port.
        /// </summary>
        /// <exception cref="ConnectionException"></exception>
        public void Disconnect()
        {
            try
            {
                _Port?.Close();
            }
            catch (IOException)
            {
                // Tritt auf, wenn ein virtueller Port entfernt wurde.
            }
            catch (Exception ex)
            {
                throw new ConnectionException("Failed to close port.", ex);
            }
        }

        /// <inheritdoc />
        public void Send(IList<byte> data)
        {
            try
            {
                _Port?.Write(data.ToArray(), 0, data.Count);
            }
            catch (Exception ex)
            {
                throw new ConnectionException("Failed to send data.", ex);
            }
        }

        /// <inheritdoc cref="IConnection"/>
        public override string ToString()
        {
            return $"{PortName}@{BaudRate}";
        }

        #endregion

        #region IDisposable

        private bool _Disposed;

        /// <inheritdoc/>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Protected implementation of Dispose pattern.
        /// </summary>
        /// <param name="disposing"></param>
        protected virtual void Dispose(bool disposing)
        {
            if (_Disposed)
            {
                return;
            }

            if (disposing)
            {
                Disconnect();
                _Port?.Dispose();
            }

            _Disposed = true;
        }

        #endregion
    }
}
