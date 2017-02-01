using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum EVENT_TYPE {
	CONNECT,
	START_GAME_REQUEST,
	START_GAME_RESPONSE,
	PADDLE_MOVEMENT,
	GAME_STATE
};

public interface IListener {

	void OnEvent(EVENT_TYPE EventType, Component sender, System.Object param = null);

}
