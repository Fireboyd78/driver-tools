using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

using System.Net;
using System.Net.Sockets;

namespace Antilli
{
    public class AntilliClient
    {
        string m_hostname;
        int m_port;

        TcpClient m_client;
        NetworkStream m_stream;

        bool m_connected;

        public bool IsConnected
        {
            get { return m_connected; }
        }

        public bool Connect()
        {
            if (m_connected)
            {
                Trace.WriteLine($"Connection already established.");
                return true;
            }

            try
            {
                m_client = new TcpClient();

                m_client.Connect(m_hostname, m_port);
                m_stream = m_client.GetStream();
                
                m_connected = true;
            }
            catch (Exception e)
            {
                Trace.WriteLine($"Connection failure: {e.Message}");
                m_connected = false;
            }
            
            return m_connected;
        }

        public void Disconnect()
        {
            if (m_connected)
            {
                m_client.Close();
                m_client = null;

                if (m_stream != null)
                {
                    m_stream.Close();
                    m_stream = null;
                }

                m_connected = false;
            }
        }

        public void Send(string message)
        {
            var buffer = Encoding.UTF8.GetBytes(message);

            Send(buffer);
        }

        public void Send(byte[] buffer)
        {
            if (!m_connected)
                throw new InvalidOperationException("No connection to send data to!");

            var length = buffer.Length;

            try
            {
                m_stream.Write(buffer, 0, length);
            }
            catch (Exception e)
            {
                Trace.WriteLine($"Failed to send {length} bytes of data: {e.Message}");
            }
        }

        public int Receive(byte[] buffer, int length = -1)
        {
            if (!m_connected)
                throw new InvalidOperationException("No connection to read data from!");

            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer), "Buffer cannot be null or empty.");

            // really dude
            if (buffer.Length == 0)
                return 0;

            if (length != -1)
            {
                if (buffer.Length < length)
                {
                    Trace.WriteLine($"Cannot receive data because the requested length exceeds the buffer size.");
                    return 0;
                }
            }
            else
            {
                // use buffer length
                length = buffer.Length;
            }

            var bytesRead = 0;

            try
            {
                bytesRead = m_stream.Read(buffer, 0, length);
            }
            catch (Exception e)
            {
                Trace.WriteLine($"Failed to read {length} bytes of data: {e.Message}");
            }

            return bytesRead;
        }

        public AntilliClient(string hostname, int port)
        {
            m_hostname = hostname;
            m_port = port;
        }
    }
}
