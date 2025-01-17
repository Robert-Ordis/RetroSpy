﻿/*!
 * RetroExporter.ControllerStateEmitter
 *
 * Copyright (c) 2022 Robert_Ordis
 *
 * Released under the MIT license.
 * see https://opensource.org/licenses/MIT
 *
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using System.Net;
using System.Net.Sockets;

using static System.Console;
using MessagePack;

namespace RetroExporter
{
    [MessagePackObject]
    public class PktFrame
    {
        [Key("t")]
        public long timestamp { get; set; }

        [Key("v")]
        public Dictionary<string, dynamic> values { get; private set; }

        public PktFrame()
        {
            this.values = new Dictionary<string, dynamic>();
        }
    }



    public class ControllerStateEmitter : IDisposable
    {

        private bool _disposed = false;

        // The count[bytes] for threshold to emit msgpack data or to wait.
        private readonly int threshold_count;

        // UDP Socket Client. Since emitted data is represent in msgpack,
        // sending as datagram is preffered to stream.
        private UdpClient udp_client;
        private readonly int udp_port;

        // Destinations for this.udp_client. All packets will be disposed while this is empty.
        private HashSet<IPEndPoint> udp_dests;

        // temporary buffer;
        
        private List<byte[]> buffers;
        private int buffer_pushed;

        // Async sending will be written in my hand. Not by SendAsync(). (WHY IS THIS FUNC NOT ABLE TO SPECIFY TO DETACH TASK?)
        private CancellationTokenSource cts;

        // Specify the keys to pack and send.
        private Dictionary<string, string> send_keys;

        private PktFrame pkt_frame;

        public ControllerStateEmitter(int threshold_count, int udp_port = 0)
        {
            this.threshold_count = threshold_count;
            this.udp_client = null;
            if (udp_port <= 0 || udp_port > 0xFFFF)
            {
                this.udp_port = 0;
            }
            else
            {
                this.udp_port = udp_port;
            }
            this.udp_dests = new HashSet<IPEndPoint>();

            this.buffers = new List<byte[]>();
            this.buffer_pushed = 0;

            this.cts = new CancellationTokenSource();

            this.send_keys = new Dictionary<string, string>();

            this.pkt_frame = new PktFrame();
        }
        ~ControllerStateEmitter()
        {
            this.Dispose();
        }

        public void Dispose()
        {
            if (this._disposed)
            {
                return;
            }
            this.cts.Cancel();
            this.udp_client?.Close();
            this.udp_client = null;
            
            this.cts.Dispose();
            this._disposed = true;
        }

        public void enableUdpClient()
        {
            if (this.udp_client == null)
            {
                if (this.udp_port <= 0 || this.udp_port > 0x0000FFFF)
                {
                    this.udp_client = new UdpClient(udp_port);
                }
                else
                {
                    this.udp_client = new UdpClient();
                }
            }
        }

        public void appendDestUdp(IPEndPoint endpoint)
        {
            this.udp_dests.Add(endpoint);
        }

        public int getDestUdpCount()
        {
            return this.udp_dests.Count;
        }

        public void registerOutput(string src_name, string out_name = null)
        {
            if (out_name is null)
            {
                out_name = src_name;
            }
            this.send_keys[src_name] = out_name;
        }

        public void push(
            long timestamp,
            IReadOnlyDictionary<string, bool> btns,
            IReadOnlyDictionary<string, float> analogs,
            IReadOnlyDictionary<string, int> raw_analogs
        )
        {

            this.pkt_frame.timestamp = timestamp;
            this.pkt_frame.values.Clear();
            foreach (var e in this.send_keys)
            {
                if (btns.ContainsKey(e.Key))
                {
                    this.pkt_frame.values[e.Value] = btns[e.Key];
                    continue;
                }
                if (analogs.ContainsKey(e.Key))
                {
                    this.pkt_frame.values[e.Value] = analogs[e.Key];
                    continue;
                }
                if (raw_analogs.ContainsKey(e.Key))
                {
                    this.pkt_frame.values[e.Value] = raw_analogs[e.Key];
                    continue;
                }
            }


            if (this.pkt_frame.values.Count > 0)
            {
                //Console.WriteLine(MessagePackSerializer.SerializeToJson(this.pkt_frame, MessagePack.Resolvers.ContractlessStandardResolver.Options));
                var generated = MessagePackSerializer.Serialize(this.pkt_frame, MessagePack.Resolvers.ContractlessStandardResolver.Options);

                this.buffer_pushed += generated.Length;
                this.buffers.Add(generated);

                if (this.buffer_pushed > this.threshold_count)
                {
                    this.requestSend();
                }
            }

        }

        private void requestSend()
        {
            var len = this.buffer_pushed;
            var barrays = this.buffers.ToArray();
            this.buffer_pushed = 0;
            this.buffers.Clear();
            if(this.udp_client == null || len <= 0)
            {
                return;
            }
            var c = Task.Factory.StartNew(() => {
                var sent = new byte[len + 64];
                var offset = 0;
                foreach(var barray in barrays)
                {
                    System.Buffer.BlockCopy(barray, 0, sent, offset, barray.Length);
                    offset += barray.Length;
                }

                foreach(var dest in this.udp_dests)
                {
                    this.udp_client.Send(sent, len, dest);
                }
            }, this.cts.Token);

        }

    }
}
