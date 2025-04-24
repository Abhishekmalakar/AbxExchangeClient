using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace AbxExchangeClient
{
   
    public class AbxClient
    {
        private const string HOST = "127.0.0.1";
        private const int PORT = 3000;
        private const int PACKET_SIZE = 17;

        public async Task<List<Packet>> GetAllPacketsAsync()
        {
            var packets = new Dictionary<int, Packet>();

            using var client = new TcpClient();
            await client.ConnectAsync(HOST, PORT);
            using var stream = client.GetStream();

            // ✅ Send 2-byte request: [CallType = 1, ResendSeq = 0]
            await stream.WriteAsync(new byte[] { 1, 0 });

            // ✅ Read all packets
            byte[]? packetBytes;
            while ((packetBytes = await ReadFullPacket(stream)) != null)
            {
                var packet = ParsePacket(packetBytes);
                packets[packet.Sequence] = packet;
            }

            // ✅ Handle missing packets
            if (packets.Count == 0)
                return packets.Values.ToList();

            int maxSeq = packets.Keys.Max();
            var missing = Enumerable.Range(1, maxSeq).Where(seq => !packets.ContainsKey(seq));

            foreach (int seq in missing)
            {
                var resend = await RequestMissingPacket(seq);
                if (resend != null)
                    packets[seq] = resend;
            }

            return packets.Values.OrderBy(p => p.Sequence).ToList();
        }

        private async Task<Packet?> RequestMissingPacket(int sequence)
        {
            using var client = new TcpClient();
            await client.ConnectAsync(HOST, PORT);
            using var stream = client.GetStream();

            // ✅ Resend request: [CallType = 2, Sequence = 1 byte]
            await stream.WriteAsync(new byte[] { 2, (byte)sequence });

            var buffer = await ReadFullPacket(stream);
            return buffer != null ? ParsePacket(buffer) : null;
        }

        private async Task<byte[]?> ReadFullPacket(NetworkStream stream)
        {
            byte[] buffer = new byte[PACKET_SIZE];
            int offset = 0;

            while (offset < PACKET_SIZE)
            {
                int read = await stream.ReadAsync(buffer, offset, PACKET_SIZE - offset);
                if (read == 0) return null; // Disconnected
                offset += read;
            }

            return buffer;
        }

        private Packet ParsePacket(byte[] data)
        {
            string symbol = Encoding.ASCII.GetString(data, 0, 4);
            char side = (char)data[4];
            int quantity = ReadInt32BigEndian(data, 5);
            int price = ReadInt32BigEndian(data, 9);
            int sequence = ReadInt32BigEndian(data, 13);

            return new Packet
            {
                Symbol = symbol,
                Side = side,
                Quantity = quantity,
                Price = price,
                Sequence = sequence
            };
        }

        private int ReadInt32BigEndian(byte[] buffer, int index)
        {
            return (buffer[index] << 24) |
                   (buffer[index + 1] << 16) |
                   (buffer[index + 2] << 8) |
                   buffer[index + 3];
        }
    }
}
