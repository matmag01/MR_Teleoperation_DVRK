using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

public class TipReceiver : MonoBehaviour
{
    public static Vector2Int latestTip = new Vector2Int(-1, -1);
    private TcpListener listener;
    private Thread listenerThread;
    public int port = 5000;

    void Start()
    {
        listenerThread = new Thread(ListenForData);
        listenerThread.IsBackground = true;
        listenerThread.Start();
    }

    void ListenForData()
    {
        listener = new TcpListener(IPAddress.Any, port);
        listener.Start();
        while (true)
        {
            try
            {
                using (TcpClient client = listener.AcceptTcpClient())
                using (NetworkStream stream = client.GetStream())
                {
                    byte[] buffer = new byte[256];
                    int bytesRead = stream.Read(buffer, 0, buffer.Length);
                    string data = Encoding.UTF8.GetString(buffer, 0, bytesRead).Trim();
                    Debug.Log("Received data: " + data);
                    string[] tokens = data.Split(',');
                    if (tokens.Length == 2 &&
                        int.TryParse(tokens[0], out int x) &&
                        int.TryParse(tokens[1], out int y))
                    {
                        latestTip = new Vector2Int(x, y);
                        Debug.Log("Parsed tip: " + latestTip);
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError("TCP Error: " + e.Message);
            }
        }
    }

    void OnApplicationQuit()
    {
        listener?.Stop();
        listenerThread?.Abort();
    }
}
