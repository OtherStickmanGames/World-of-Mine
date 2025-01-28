using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WebSocketSharp;

namespace Netcode.Transports.WebSocket
{
    public class NativeWebSocketClient : IWebSocketClient
    {
        private static readonly object ConnectionLock = new object();

        private WebSocketSharp.WebSocket Connection = null;
        public Queue<WebSocketEvent> EventQueue { get; } = new Queue<WebSocketEvent>();

        public ulong WaitTime
        {
            get
            {
                return (ulong)Connection.WaitTime.Milliseconds;
            }
        }

        public WebSocketState ReadyState
        {
            get
            {
                return Connection?.ReadyState ?? WebSocketState.Closed;
            }
        }

        public NativeWebSocketClient(string url)
        {
            Connection = new WebSocketSharp.WebSocket(url);

            Connection.OnOpen += OnOpen;
            Connection.OnMessage += OnMessage;
            Connection.OnError += OnError;
            Connection.OnClose += OnClose;
        }

        public void Connect()
        {
            if (ReadyState == WebSocketSharp.WebSocketState.Open)
            {
                throw new InvalidOperationException("Socket is already open");
            }

            if (ReadyState == WebSocketSharp.WebSocketState.Closing)
            {
                throw new InvalidOperationException("Socket is closing");
            }

            try
            {
                if(!Connection.Connect())
                {
                    throw new InvalidOperationException($"Cannot connect to url {Connection.Url}");
                }
            }
            catch (Exception e)
            {
                throw new WebSocketException("Connection failed", e);
            }
        }

        public void Close(CloseStatusCode code = CloseStatusCode.Normal, string reason = null)
        {
            if (ReadyState == WebSocketSharp.WebSocketState.Closing)
            {
                throw new InvalidOperationException("Socket is already closing");
            }

            if (ReadyState == WebSocketSharp.WebSocketState.Closed)
            {
                return;
            }

            try
            {
                Connection.Close(code, reason);
            }
            catch (Exception e)
            {
                throw new WebSocketException("Could not close socket", e);
            }
        }

        private static Queue<byte[]> _bufferPool = new Queue<byte[]>();
        public void Send(ArraySegment<byte> data)
        {
            if (ReadyState != WebSocketSharp.WebSocketState.Open)
            {
                throw new WebSocketException("Socket is not open");
            }

            try
            {
                if (data.Offset > 0 || data.Count < data.Array.Length)
                {
                    // STA Websockets cant take offsets nor buffer lenghts.
                    byte[] buf = new byte[data.Count];
                    Buffer.BlockCopy(data.Array, data.Offset, buf, 0, data.Count);

                    Connection.Send(buf);
                }
                else
                {
                    Connection.Send(data.Array);
                }
            }
            catch (Exception e)
            {
                throw new WebSocketException("Unknown error while sending the message", e);
            }
        }

        //public void Send(ArraySegment<byte> data)
        //{
        //    if (ReadyState != WebSocketSharp.WebSocketState.Open)
        //    {
        //        throw new WebSocketException("Socket is not open");
        //    }

        //    try
        //    {
        //        if (data.Offset > 0 || data.Count < data.Array.Length)
        //        {
        //            byte[] buffer;

        //            if (_bufferPool.Count > 0)
        //            {
        //                buffer = _bufferPool.Dequeue();
        //                if (buffer.Length < data.Count)
        //                {
        //                    buffer = new byte[data.Count];  // Создать новый буфер, если старый слишком мал
        //                }
        //            }
        //            else
        //            {
        //                buffer = new byte[data.Count];
        //            }

        //            // Безопасное копирование с учетом длины массива
        //            Buffer.BlockCopy(data.Array, data.Offset, buffer, 0, data.Count);

        //            Connection.Send(buffer);
        //            _bufferPool.Enqueue(buffer);
        //        }
        //        else
        //        {
        //            Connection.Send(data.Array);
        //        }
        //    }
        //    catch (Exception e)
        //    {
        //        throw new WebSocketException("Unknown error while sending the message", e);
        //    }
        //}

        public WebSocketEvent Poll()
        {
            lock (ConnectionLock)
            {
                if (EventQueue.Count > 0)
                {
                    return EventQueue.Dequeue();
                }
                else
                {
                    return new WebSocketEvent()
                    {
                        ClientId = 0,
                        Payload = null,
                        Type = WebSocketEvent.WebSocketEventType.Nothing,
                        Error = null,
                        Reason = null
                    };
                }
            }
        }

        public void OnOpen(object sender, EventArgs e)
        {
            lock (ConnectionLock)
            {
                EventQueue.Enqueue(new WebSocketEvent()
                {
                    ClientId = 0,
                    Payload = null,
                    Type = WebSocketEvent.WebSocketEventType.Open,
                    Error = null,
                    Reason = null
                });
            }
        }

        public void OnClose(object sender, CloseEventArgs e)
        {
            lock (ConnectionLock)
            {
                EventQueue.Enqueue(new WebSocketEvent()
                {
                    ClientId = 0,
                    Payload = null,
                    Type = WebSocketEvent.WebSocketEventType.Close,
                    Error = null,
                    Reason = e.Reason
                });
            }
        }

        public void OnError(object sender, ErrorEventArgs e)
        {
            lock (ConnectionLock)
            {
                EventQueue.Enqueue(new WebSocketEvent()
                {
                    ClientId = 0,
                    Payload = null,
                    Type = WebSocketEvent.WebSocketEventType.Error,
                    Error = e.Message,
                    Reason = null,
                });
            }
        }

        private static byte[] _receiveBuffer = new byte[192];  // Фиксированный буфер
        public void OnMessage(object sender, MessageEventArgs e)
        {
            lock (ConnectionLock)
            {
                //Array.Copy(e.RawData, _receiveBuffer, e.RawData.Length);
                EventQueue.Enqueue(new WebSocketEvent()
                {
                    ClientId = 0,
                    Payload = e.RawData,//_receiveBuffer,
                    Type = WebSocketEvent.WebSocketEventType.Payload,
                    Error = null,
                    Reason = null,
                });
            }
        }
    }
}
