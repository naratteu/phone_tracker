using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Text.RegularExpressions;
using System.Text;
using System;
using System.Net;
using System.Net.Sockets;
public class p2p : MonoBehaviour
{
    public int port = 8181;

    Queue<string> res = new Queue<string>();
    void newRes(string s)
    {
        res.Enqueue(s);
    }

    private void Update()
    {
        try
        {
            while (res.Count > 0)
            {
                string jsonstring = res.Dequeue();
                jsonstring = jsonstring.Substring(0, jsonstring.IndexOf('}') + 1);//뒤에 쓰레기값이 붙는경우가 있음
                JSONObject json = JSONValue.Parse(jsonstring).Obj;
                switch (json.GetString("t"))
                {
                    case "abg":
                        transform.eulerAngles = new Vector3(
                            x: -json.GetFloat("b"),
                            z: -json.GetFloat("g"),
                            y: -json.GetFloat("a")
                            );
                        break;
                    case "xyz":
                        //transform.Translate(
                        //    x: up(json.GetFloat("x")),
                        //    y: up(json.GetFloat("y")),
                        //    z: up(json.GetFloat("z"))
                        //    );
                        break;
                }
            }
        }
        catch(Exception e)
        {

        }
    }

    float up(float v)
    {
        if (Mathf.Abs(v) < 0.25f) return 0;
        return v/100f;
    }

    // Start is called before the first frame update
    void Start()
    {
        
        //wsServer ws = new wsServer("127.0.0.1", 8181);
        wsServer ws = new wsServer(null, port, newRes);
        //wsServer ws = new wsServer(GetLocalIPAddress(), 8181);

    }
    public static string GetLocalIPAddress()
    {
        var host = Dns.GetHostEntry(Dns.GetHostName());
        foreach (var ip in host.AddressList)
        {
            if (ip.AddressFamily == AddressFamily.InterNetwork)
            {
                return ip.ToString();
            }
        }
        throw new Exception("No network adapters with an IPv4 address in the system!");
    }
}
class wsServer
{


    // 초기화 시 입력 받는 값들.
    public int port = 0;


    // 읽기에 사용되는 값들.
    NetworkStream clientStream = null;
    byte[] readBuffer = new byte[500000];



    // 동작에 사용되는 멤버 변수들.
    TcpListener listner = null;
    TcpClient client = null;
    Action<string> newRes;

    // 생성자를 만들어 주도록 합니다.
    public wsServer(string _addr, int _port, Action<string> _newRes)
    {
        newRes = _newRes;

        Debug.Log("wsServer - " + _addr + ":" + _port);
        // 입력 받은 값들을 저장해 줍니다.
        port = _port;


        if(_addr == null)
        {
            listner = new TcpListener(IPAddress.Any, port);
        }
        else
        {
            listner = new TcpListener(IPAddress.Parse(_addr), port);
        }


        listner.Start();
        Debug.Log("웹소켓 서버를 오픈하도록 합니다.");
        


        listner.BeginAcceptTcpClient(OnServerConnect, null);
        Debug.Log("클라이언트와의 접속을 기다립니다.");



    }


    // 서버의 접속이 들어 왔을 때 처리하는 메소드 입니다.
    void OnServerConnect(IAsyncResult ar)
    {

        client = listner.EndAcceptTcpClient(ar);
        Debug.Log("한 클라이언트가 접속했습니다.");

        // 클라이언트의 접속을 다시 대기.
        listner.BeginAcceptTcpClient(OnServerConnect, null);

        // 현재의 클라이언트로 부터 데이터를 받아 옵니다.
        clientStream = client.GetStream();
        clientStream.BeginRead(readBuffer, 0, readBuffer.Length, onAcceptReader, null);





    }


    // 클라이언트의 데이터를 읽어 오는 메소드 입니다.
    void onAcceptReader(IAsyncResult ar)
    {

        // 받은 데이터의 길이를 확인합니다.
        int receiveLength = clientStream.EndRead(ar);


        // 받은 데이터가 없는 경우는 접속이 끊어진 경우 입니다.
        if (receiveLength <= 0)
        {
            Debug.Log("접속이 끊어졌습니다.");
            // 에코 메시지 받기 시작.
            clientStream.BeginRead(readBuffer, 0, readBuffer.Length, onEchoReader, null);
            return;
        }


        // 받은 메시지를 출력합니다.
        string newMessage = Encoding.UTF8.GetString(readBuffer, 0, receiveLength);
        Debug.Log(
            string.Format("받은 메시지:\n{0}", newMessage)
        );


        // 첫 3문자가 GET으로 시작하지 않는 경우, 잘못된 접속이므로 종료합니다.
        if (!Regex.IsMatch(newMessage, "^GET"))
        {
            Debug.Log("잘못된 접속 입니다.");
            //client.Close();
            return;
        }

        // 클라이언트로 응답을 돌려 줍니다.
        const string eol = "\r\n"; // HTTP/1.1 defines the sequence CR LF as the end-of-line marker



        // 보낼 메시지.
        string resMessage = "HTTP/1.1 101 Switching Protocols" + eol
            + "Connection: Upgrade" + eol
            + "Upgrade: websocket" + eol
            + "Sec-WebSocket-Accept: " + Convert.ToBase64String(
                System.Security.Cryptography.SHA1.Create().ComputeHash(
                    Encoding.UTF8.GetBytes(
                        new Regex("Sec-WebSocket-Key: (.*)").Match(newMessage).Groups[1].Value.Trim() + "258EAFA5-E914-47DA-95CA-C5AB0DC85B11"
                    )
                )
            ) + eol
            + eol
            ;



        // 보낸 메시지를 출력해 봅니다.
        Debug.Log(
            string.Format("보낸 메시지:\n{0}", resMessage)
        );



        // 메시지를 보내 줍니다.
        Byte[] response = Encoding.UTF8.GetBytes(resMessage);
        clientStream.Write(response, 0, response.Length);



        // 에코 메시지 받기 시작.
        clientStream.BeginRead(readBuffer, 0, readBuffer.Length, onEchoReader, null);


    }



    // 에코 메시지를 받아 오는 부분 입니다.
    void onEchoReader(IAsyncResult ar)
    {

        // 받은 데이터의 길이를 확인합니다.
        int receiveLength = clientStream.EndRead(ar);



        // 받은 데이터가 6인 경우는 종료 상태 일 뿐이므로, 종료 데이터를 보내고 우리도 접속을 종료합니다.
        if (receiveLength == 6)
        {
            Debug.Log("접속 해제 요청이 와 접속을 종료합니다.");
            client.Close();
            return;
        }



        // 받은 데이터가 없는 경우는 접속이 끊어진 경우 입니다.
        if (receiveLength <= 0)
        {
            Debug.Log("접속이 끊어졌습니다.");
            return;
        }




        BitArray maskingCheck = new BitArray(new byte[] { readBuffer[1] });
        int receivedSize = (int)readBuffer[1];

        byte[] mask = new byte[] { readBuffer[2], readBuffer[3], readBuffer[4], readBuffer[5] };

        if (maskingCheck.Get(0))
        {
            Debug.Log("마스킹 되어 있습니다.");
            receivedSize -= 128;            // 마스킹으로 인해 추가된 값을 빼 줍니다.
        }
        else
        {
            Debug.Log("마스킹 되어 있지 않습니다.");
        }


        // 문자열을 길이를 파악합니다.
        Debug.Log("받은 데이터 길이 비트 : "+ receivedSize);
        Debug.Log("받은 데이터 길이 : " + receiveLength);




        // 받은 메시지를 디코딩해 줍니다.
        byte[] decodedByte = new byte[receivedSize];
        for (int _i = 0; _i < receivedSize; _i++)
        {

            int curIndex = _i + 6;
            decodedByte[_i] = (byte)(readBuffer[curIndex] ^ mask[_i % 4]);

        }


        // 받은 메시지를 출력합니다.
        // string newMessage = Encoding.UTF8.GetString( readBuffer, 6, receiveLength - 6 );
        string newMessage = Encoding.UTF8.GetString(decodedByte, 0, receivedSize);
        Debug.Log(
            string.Format("받은 메시지:{0}", newMessage)
        );



        string sendSource = newMessage;// "이 문자열이 보이면 성공!";
        byte[] sendMessage = Encoding.UTF8.GetBytes(sendSource);


        // 보낼 메시지를 만들어 줍니다.
        List<byte> sendByteList = new List<byte>();


        // 첫 데이터의 정보를 만들어 추가합니다.
        BitArray firstInfor = new BitArray(
            new bool[]{

                    true                // FIN
                    , false             // RSV1
                    , false             // RSV2
                    , false             // RSV3

                    // opcode (0x01: 텍스트)
                    , false
                    , false
                    , false
                    , true

            }
        );
        byte[] inforByte = new byte[1];
        firstInfor.CopyTo(inforByte, 0);
        sendByteList.Add(inforByte[0]);

        // 문자열의 길이를 추가합니다.
        sendByteList.Add((byte)sendMessage.Length);

        // 실제 데이터를 추가합니다.
        sendByteList.AddRange(sendMessage);

        newRes?.Invoke(sendSource);









        // 보낸 메시지를 출력해 봅니다.
        Debug.Log(
            string.Format("보낸 메시지:\n{0}", sendSource)
        );



        // 받은 메시지를 그대로 보내 줍니다.
        //clientStream.Write(sendByteList.ToArray(), 0, sendByteList.Count);
        Debug.Log(string.Format("보낸 메시지 길이:{0}", sendByteList.Count));




        // 또 다음 메시지를 받을 수 있도록 대기 합니다.
        clientStream.BeginRead(readBuffer, 0, readBuffer.Length, onEchoReader, null);


    }




}