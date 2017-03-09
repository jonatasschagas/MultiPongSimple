using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using System;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using UnityEngine.UI;
using System.IO;

public class SocketClient : MonoBehaviour {

	private const int SERVER_PORT = 9090; // define > init
	private const string SERVER_HOST = "184.73.61.23";
	//private const string SERVER_HOST = "localhost";

	const int READ_BUFFER_SIZE = 255;
	const int PORT_NUM = 10000;

	public bool logMessages;

	private bool socketReady = false;
	private TcpClient socketClient;
	private NetworkStream theStream;
	private StreamWriter stWriter;
	private StreamReader stReader;
	private Queue<String> messagesToBeSent;
	private Queue<String> receivedMessages;
	private Thread senderNetworkThread;
	private Thread receiverNetworkThread;

	void Start() {
		messagesToBeSent = new Queue<String>();
		receivedMessages = new Queue<String>();
		receiverNetworkThread = new Thread (ReceiverThread);
		senderNetworkThread = new Thread (SenderThread);
	}

	void Update () {
		if (socketReady && !receiverNetworkThread.IsAlive && !senderNetworkThread.IsAlive) {
			Debug.Log ("Starting Network Threads");
			receiverNetworkThread.Start ();
			senderNetworkThread.Start ();
		}
	}

	public void Connect() {
		Debug.Log ("Initializing Socket Client: " + SERVER_HOST + ":" + SERVER_PORT);
		try {
			socketClient = new TcpClient(SERVER_HOST, SERVER_PORT);
			theStream = socketClient.GetStream();
			stWriter = new StreamWriter(theStream);
			stReader = new StreamReader(theStream);
			socketReady = true;
		}
		catch (Exception e) {
			Debug.Log("Socket error:" + e);
		}
		Debug.Log ("Socket Client initialized");
	}

	private void Send(string message) {
		if (logMessages) {
			Debug.Log ("Sending message: " + message);
		}
		try {
			if (!socketReady) {
				return;
			}
			stWriter.Write(message + "\r\n");
			stWriter.Flush();
		} catch (Exception err) {
			Debug.Log("Error sending message to server: " + err.ToString());
		}
	}

	private void ReceiverThread() {		
		while (socketClient.Connected) {
			try {
				String message = "";
				if (theStream.DataAvailable) {
					Byte[] inStream = new Byte[socketClient.SendBufferSize];
					theStream.Read(inStream, 0, inStream.Length);
					message += System.Text.Encoding.UTF8.GetString(inStream);
				}
				if(message != "") {
					if(logMessages) {
						Debug.Log("Received: >> " + message);
					}
					if(message.Contains("|")) {
						foreach(String jsonMessage in message.Split('|')) {
							receivedMessages.Enqueue(jsonMessage);
						}
					} else {
						receivedMessages.Enqueue(message);
					}
				}
			} catch (Exception err) {
				Debug.Log(err.ToString());
			}
		}
		Debug.Log ("Stopping receiver thread. Socket disconnected");
	}

	private void SenderThread() {		
		while (socketClient.Connected) {
			try {
				while(messagesToBeSent.Count > 0) {
					Send(messagesToBeSent.Dequeue());
				}
			} catch (Exception err) {
				Debug.Log(err.ToString());
			}
		}
		Debug.Log ("Stopping sender thread. Socket disconnected");
	}

	public Queue<string> GetMessageQueue() {
		return receivedMessages;
	}

	public void SendMessage(String message) {
		messagesToBeSent.Enqueue (message);
	}

	public void CloseSocket() {
		if (!socketReady)
			return;
		stWriter.Close();
		stReader.Close();
		socketClient.Close();
		socketReady = false;
	}

	//keep connection alive, reconnect if connection lost
	public void MaintainConnection(){
		if(!theStream.CanRead) {
			Connect();
		}
	}

	public bool IsConnected() {
		return socketReady;
	}

}
