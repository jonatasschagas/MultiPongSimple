using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;
using System;

public class ServerMessageDispatcher : MonoBehaviour {

	private SocketClient socketClient;

	void Start () {
		socketClient = gameObject.GetComponent<SocketClient> ();
	}
	
	public void SendMessage(System.Object messageObject) {
		if (!socketClient.IsConnected ()) {
			socketClient.Connect ();
		}
		socketClient.SendMessage (JsonConvert.SerializeObject(messageObject));
	}

	void Update () {
		Queue<string> messageQueue = socketClient.GetMessageQueue ();
		if(messageQueue.Count > 0) {
			while (messageQueue.Count > 0) {
				String jsonMessage = null;
				try {
					jsonMessage = messageQueue.Dequeue ();
					EVENT_TYPE eventType = EVENT_TYPE.CONNECT;
					System.Object messageObject = null;

					// this part we do the parsing of the messages
					if (jsonMessage.Contains ("GameConnectionResponse")) {
						eventType = EVENT_TYPE.CONNECT;
						messageObject = JsonConvert.DeserializeObject<GameConnectionResponse>(jsonMessage);
					} else if(jsonMessage.Contains("StartGameResponse")) {
						eventType = EVENT_TYPE.START_GAME_RESPONSE;
						messageObject = JsonConvert.DeserializeObject<StartGameResponse>(jsonMessage);
					} else if(jsonMessage.Contains("GameLogic")) {
						eventType = EVENT_TYPE.GAME_STATE;
						messageObject = JsonConvert.DeserializeObject<GameLogic>(jsonMessage);
					} else if(jsonMessage.Contains("PaddleMovementRequest")) {
						eventType = EVENT_TYPE.PADDLE_MOVEMENT;
						messageObject = JsonConvert.DeserializeObject<PaddleMovementRequest>(jsonMessage);
					} 

					if(messageObject != null) {
						// dispatching the object
						EventManager.Instance.PostNotification(eventType, this, messageObject);
					}

				} catch (Exception e) {
					Debug.Log ("Error deserializing message: " + jsonMessage);
					Debug.LogError (e);
				}
			}
		}
	}
}
