using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Net;
using System.Net.Sockets;
using System.Net.Security;
using System.Security.Authentication;
using System.Text;
using System.Security.Cryptography.X509Certificates;
using System.IO;
using System.Net.WebSockets;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;

public class p2p2 : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        string ip = "127.0.0.1";
        int port = 80;
        var server = new TcpListener(IPAddress.Any, port);

        server.Start();
        Debuglog("Server has started on {0}:{1}, Waiting for a connection...", ip, port);

        client = server.AcceptTcpClient();
        Debuglog("A client connected.");

        stream = client.GetStream();
    }
    TcpClient client;
    NetworkStream stream;

    // Update is called once per frame
    void Update()
    {
        {
            if (client == null) return;
            if (stream == null) return;
            if (!stream.DataAvailable) return;
            if (client.Available < 3) return; // match against "get"

            byte[] bytes = new byte[client.Available];
            stream.Read(bytes, 0, client.Available);
            string s = Encoding.UTF8.GetString(bytes);

            if (Regex.IsMatch(s, "^GET", RegexOptions.IgnoreCase))
            {
                Debuglog("=====Handshaking from client=====\n{0}", s);

                // 1. Obtain the value of the "Sec-WebSocket-Key" request header without any leading or trailing whitespace
                // 2. Concatenate it with "258EAFA5-E914-47DA-95CA-C5AB0DC85B11" (a special GUID specified by RFC 6455)
                // 3. Compute SHA-1 and Base64 hash of the new value
                // 4. Write the hash back as the value of "Sec-WebSocket-Accept" response header in an HTTP response
                string swk = Regex.Match(s, "Sec-WebSocket-Key: (.*)").Groups[1].Value.Trim();
                string swka = swk + "258EAFA5-E914-47DA-95CA-C5AB0DC85B11";
                byte[] swkaSha1 = System.Security.Cryptography.SHA1.Create().ComputeHash(Encoding.UTF8.GetBytes(swka));
                string swkaSha1Base64 = Convert.ToBase64String(swkaSha1);

                // HTTP/1.1 defines the sequence CR LF as the end-of-line marker
                byte[] response = Encoding.UTF8.GetBytes(
                    "HTTP/1.1 101 Switching Protocols\r\n" +
                    "Connection: Upgrade\r\n" +
                    "Upgrade: websocket\r\n" +
                    "Sec-WebSocket-Accept: " + swkaSha1Base64 + "\r\n\r\n");

                stream.Write(response, 0, response.Length);
            }
            else
            {
                Debug.Log(BitConverter.ToString(bytes));
                Debug.Log(Encoding.Default.GetString(bytes));
                bool fin = (bytes[0] & 0b10000000) != 0,
                    mask = (bytes[1] & 0b10000000) != 0; // must be true, "All messages from the client to the server have this bit set"

                int opcode = bytes[0] & 0b00001111, // expecting 1 - text message
                    msglen = mask ? bytes[1] - 128 : bytes[1], // & 0111 1111
                    offset = 2;
                Debug.Log("msglen : " + msglen);
                if (msglen == 126)
                {
                    // was ToUInt16(bytes, offset) but the result is incorrect
                    msglen = BitConverter.ToUInt16(new byte[] { bytes[3], bytes[2] }, 0);
                    offset = 4;
                }
                else if (msglen == 127)
                {
                    Debuglog("TODO: msglen == 127, needs qword to store msglen");
                    // i don't really know the byte order, please edit this
                    // msglen = BitConverter.ToUInt64(new byte[] { bytes[5], bytes[4], bytes[3], bytes[2], bytes[9], bytes[8], bytes[7], bytes[6] }, 0);
                    // offset = 10;
                }

                if (msglen == 0)
                    Debuglog("msglen == 0");
                else if (mask)
                {
                    byte[] decoded = new byte[msglen];
                    byte[] masks = new byte[4] { bytes[offset], bytes[offset + 1], bytes[offset + 2], bytes[offset + 3] };
                    offset += 4;

                    for (int i = 0; i < msglen; ++i)
                        decoded[i] = (byte)(bytes[offset + i] ^ masks[i % 4]);

                    string text = Encoding.UTF8.GetString(decoded);
                    Debuglog("{0}", text);
                }
                else
                    Debuglog("mask bit not set");
            }
        }
    }
    static void Debuglog(String format, params object[] args)
    {
        Debug.Log(string.Format(format, args));
    }
    public static void Main()
    {
        string ip = "127.0.0.1";
        int port = 80;
        var server = new TcpListener(IPAddress.Parse(ip), port);

        server.Start();
        Debuglog("Server has started on {0}:{1}, Waiting for a connection...", ip, port);

        TcpClient client = server.AcceptTcpClient();
        Debuglog("A client connected.");

        NetworkStream stream = client.GetStream();

        // enter to an infinite cycle to be able to handle every change in stream
        //while (true)
        {
            while (!stream.DataAvailable) ;
            while (client.Available < 3) ; // match against "get"

            byte[] bytes = new byte[client.Available];
            stream.Read(bytes, 0, client.Available);
            string s = Encoding.UTF8.GetString(bytes);

            if (Regex.IsMatch(s, "^GET", RegexOptions.IgnoreCase))
            {
                Debuglog("=====Handshaking from client=====\n{0}", s);

                // 1. Obtain the value of the "Sec-WebSocket-Key" request header without any leading or trailing whitespace
                // 2. Concatenate it with "258EAFA5-E914-47DA-95CA-C5AB0DC85B11" (a special GUID specified by RFC 6455)
                // 3. Compute SHA-1 and Base64 hash of the new value
                // 4. Write the hash back as the value of "Sec-WebSocket-Accept" response header in an HTTP response
                string swk = Regex.Match(s, "Sec-WebSocket-Key: (.*)").Groups[1].Value.Trim();
                string swka = swk + "258EAFA5-E914-47DA-95CA-C5AB0DC85B11";
                byte[] swkaSha1 = System.Security.Cryptography.SHA1.Create().ComputeHash(Encoding.UTF8.GetBytes(swka));
                string swkaSha1Base64 = Convert.ToBase64String(swkaSha1);

                // HTTP/1.1 defines the sequence CR LF as the end-of-line marker
                byte[] response = Encoding.UTF8.GetBytes(
                    "HTTP/1.1 101 Switching Protocols\r\n" +
                    "Connection: Upgrade\r\n" +
                    "Upgrade: websocket\r\n" +
                    "Sec-WebSocket-Accept: " + swkaSha1Base64 + "\r\n\r\n");

                stream.Write(response, 0, response.Length);
            }
            else
            {
                bool fin = (bytes[0] & 0b10000000) != 0,
                    mask = (bytes[1] & 0b10000000) != 0; // must be true, "All messages from the client to the server have this bit set"

                int opcode = bytes[0] & 0b00001111, // expecting 1 - text message
                    msglen = bytes[1] - 128, // & 0111 1111
                    offset = 2;

                if (msglen == 126)
                {
                    // was ToUInt16(bytes, offset) but the result is incorrect
                    msglen = BitConverter.ToUInt16(new byte[] { bytes[3], bytes[2] }, 0);
                    offset = 4;
                }
                else if (msglen == 127)
                {
                    Debuglog("TODO: msglen == 127, needs qword to store msglen");
                    // i don't really know the byte order, please edit this
                    // msglen = BitConverter.ToUInt64(new byte[] { bytes[5], bytes[4], bytes[3], bytes[2], bytes[9], bytes[8], bytes[7], bytes[6] }, 0);
                    // offset = 10;
                }

                if (msglen == 0)
                    Debuglog("msglen == 0");
                else if (mask)
                {
                    byte[] decoded = new byte[msglen];
                    byte[] masks = new byte[4] { bytes[offset], bytes[offset + 1], bytes[offset + 2], bytes[offset + 3] };
                    offset += 4;

                    for (int i = 0; i < msglen; ++i)
                        decoded[i] = (byte)(bytes[offset + i] ^ masks[i % 4]);

                    string text = Encoding.UTF8.GetString(decoded);
                    Debuglog("{0}", text);
                }
                else
                    Debuglog("mask bit not set");
            }
        }
    }
}
