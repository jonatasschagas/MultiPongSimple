using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.UI;

public class GameController : MonoBehaviour, IListener {

	#region constants
	private const float SERVER_PULL_INTERVAL_SECONDS = 0.15f; 
	#endregion

	#region options
	public bool singlePlayer = false;
	#endregion

	#region control members
	private string gameId;
	private short playerNumber;
	private ServerMessageDispatcher serverMessageDispatcher;
	private bool isConnected;
	private float reSendToServerCoolDown = 0;
	private GameLogic gameLogic;
	private bool startMessageSent = false;
	#endregion

	#region buttons
	public GameObject singlePlayerBtn;
	public GameObject multiPlayerBtn;
	#endregion

	#region prefabs
	public GameObject paddle1Prefab;
	public GameObject paddle2Prefab;
	public GameObject ballPrefab;
	public GameObject swipeControlPrefab;
	#endregion

	#region game objects
	private Paddle myPaddle;
	private Paddle opponentPaddle;
	private Ball ball;
	private SwipeControl swipeControl;
	#endregion

	#region getters

	public GameLogic GetGameModel() {
		return gameLogic;
	}

	public int GetPlayerNumber() {
		return playerNumber;
	}

	#endregion

	#region main methods

	void Start () {
		serverMessageDispatcher = gameObject.GetComponent<ServerMessageDispatcher> ();
		RegisterEventListeners ();
		Application.targetFrameRate = GameLogic.FPS;
	}
	
	void Update () {

		if (gameLogic == null) {
			return;
		}

		// start the game for multiplayer game
		if (singlePlayer == false && playerNumber == 1 && DidOpponentConnect()) {
			SendGameStartMessage ();
			startMessageSent = true;
		} 

		// updates the opponent
		if (singlePlayer) {
			UpdateAI ();
		} else {
			UpdateServer();
		}

		if(gameLogic.hasStarted && !gameLogic.gameOver) {
			// update game if it has started
			gameLogic.Update ();
		}

	}

	private bool DidOpponentConnect() {
		return gameLogic != null &&
		gameLogic.player1Connected &&
		gameLogic.player2Connected &&
		gameLogic.hasStarted == false &&
		startMessageSent == false;
	}

	/// <summary>
	/// Updates the opponent AI when playing Single Player
	/// </summary>
	private void UpdateAI() {
		if (gameLogic != null) {
			SimpleGameObject ballObj = gameLogic.ball;
			SimpleGameObject paddle2Obj = gameLogic.paddle2;
			if (ballObj.x > paddle2Obj.x) {
				gameLogic.MovePaddle2 (true);
			} else if (ballObj.x < paddle2Obj.x){
				gameLogic.MovePaddle2 (false);
			}
		}
	}

	/// <summary>
	/// Updates the multiplayer game
	/// </summary>
	private void UpdateServer() {
		reSendToServerCoolDown -= Time.deltaTime;
		if (isConnected && reSendToServerCoolDown <= 0.0f) {

			if(gameLogic != null && gameLogic.hasStarted) {
				// send new paddle movement to the server
				Debug.Log("Sending new paddle movement to the server");
				serverMessageDispatcher.SendMessage (myPaddle.GetPaddlePosition ());
			}

			Debug.Log("Request Game State Update");
			// send request to update the GameState
			GetGameLogicRequest getGameLogicRequest = new GetGameLogicRequest();
			getGameLogicRequest.clientTick = gameLogic.currentTick;
			getGameLogicRequest.gameId = gameId;
			serverMessageDispatcher.SendMessage(getGameLogicRequest);

			reSendToServerCoolDown = SERVER_PULL_INTERVAL_SECONDS;
		}
	}


	#endregion

	#region event listener registration

	/// <summary>
	/// Registers the event listeners.
	/// </summary>
	private void RegisterEventListeners() {
		EventManager.Instance.AddListener (EVENT_TYPE.CONNECT, this);
		EventManager.Instance.AddListener (EVENT_TYPE.START_GAME_RESPONSE, this);
		EventManager.Instance.AddListener (EVENT_TYPE.START_GAME_REQUEST, this);
		EventManager.Instance.AddListener (EVENT_TYPE.PADDLE_MOVEMENT, this);
		EventManager.Instance.AddListener (EVENT_TYPE.GAME_STATE, this);
	}

	/// <summary>
	/// Handles the events
	/// </summary>
	public void OnEvent(EVENT_TYPE eventType, Component sender, System.Object param = null) {
		switch (eventType) {
		case EVENT_TYPE.CONNECT:
			ProcessConnectEvent ((GameConnectionResponse)param);
			break;
		case EVENT_TYPE.START_GAME_RESPONSE:
			ProcessStartGame ((StartGameResponse)param);
			break;
		case EVENT_TYPE.PADDLE_MOVEMENT:
			ProcessPaddleMovement ((PaddleMovementRequest)param);
			break;
		case EVENT_TYPE.GAME_STATE:
			ProcessGetGameState ((GameLogic)param);
			break;
		}
	}

	#endregion

	#region event listener actions

	/// <summary>
	/// Synchronizes the game state with the server state (reconciliation).
	/// 
	/// 1. Stores the current state
	/// 2. Goes back to the state returned to by the server
	/// 3. Fast forwards from the server state the current state applying the inputs
	/// 4. Replaces the current state with the now "up to date" server state
	/// 
	/// </summary>
	/// <param name="serverState">Server state.</param>
	private void ProcessGetGameState(GameLogic serverState) {
		Debug.Log ("Sync with the server.");

		GameLogic currentState = gameLogic;
		currentState.player1Connected = serverState.player1Connected;
		currentState.player2Connected = serverState.player2Connected;

		// fast forwarding by executing the inputs
		for (long tick = serverState.currentTick; tick <= currentState.currentTick; tick++) {
			List<PaddleMovementRequest> inputsPerTick = myPaddle.GetInputsForTick(tick);
			if(inputsPerTick != null) {
				foreach (PaddleMovementRequest input in inputsPerTick) {
					if (input.playerNumber == 1) {
						serverState.paddle1 = input.paddle;	
					} else {
						serverState.paddle2 = input.paddle;
					}
				}
			}
			serverState.Update ();
		}

		// updating the entities according to the server's update
		currentState.Copy (serverState);
		// updating paddles
		//myPaddle.UpdatePaddleModel (currentState);
		opponentPaddle.UpdatePaddleModel (currentState);
	}

	/// <summary>
	/// Processes the remote paddle movement
	/// </summary>
	/// <param nam e="paddleMovementRequest">Paddle movement request.</param>
	private void ProcessPaddleMovement(PaddleMovementRequest paddleMovementRequest) {
		Debug.Log ("Process Paddle Movement: " + JsonUtility.ToJson(paddleMovementRequest));
		if (paddleMovementRequest.playerNumber == 1) {
			gameLogic.paddle1 = paddleMovementRequest.paddle;
		} else {
			gameLogic.paddle2 = paddleMovementRequest.paddle;
		}
	}

	#endregion

	#region connect

	/// <summary>
	/// Sets the game into "Single Player Mode"
	/// </summary>
	public void SinglePlayerBtn() {
		singlePlayer = true;
		StartGameBtn ();
	}

	/// <summary>
	/// Sets the game into "Multiplayer Mode"
	/// </summary>
	public void MultiPlayerBtn() {
		singlePlayer = false;
		StartGameBtn ();
	}

	/// <summary>
	/// This method is used by the "start button". It initializes the game (single or multiplayer)
	/// </summary>
	private void StartGameBtn() {
		singlePlayerBtn.SetActive(false);
		multiPlayerBtn.SetActive(false);
		if (!singlePlayer) {
			GameConnectionRequest gameConnectionRequest = new GameConnectionRequest ();
			gameConnectionRequest.deviceId = SystemInfo.deviceUniqueIdentifier;
			serverMessageDispatcher.SendMessage (gameConnectionRequest);
		} else {
			playerNumber = 1;
			InitializeGameSession ();
			ProcessStartGame (null);
		}
	}


	/// <summary>
	/// Processes the connect event by initializing the game session
	/// </summary>
	/// <param name="gameConnectionResponse">Game connection response.</param>
	private void ProcessConnectEvent(GameConnectionResponse gameConnectionResponse) {
		gameId = gameConnectionResponse.gameId;
		playerNumber = gameConnectionResponse.playerNumber; 
		Debug.Log ("Creating game: gameId " + gameId + ", player: " + playerNumber);
		InitializeGameSession ();
		isConnected = true;
	}

	#endregion

	#region start game

	/// <summary>
	/// Sends the game start message to the server.
	/// </summary>
	private void SendGameStartMessage() {
		Debug.Log ("Sending request to start the game");
		StartGameRequest startGameRequest = new StartGameRequest ();
		startGameRequest.gameId = gameId;
		startGameRequest.playerNumber = playerNumber;
		serverMessageDispatcher.SendMessage (startGameRequest);
	}

	/// <summary>
	/// When the start game message comes from the server it starts the game
	/// </summary>
	/// <param name="startGameResponse">Start game response.</param>
	private void ProcessStartGame(StartGameResponse startGameResponse) {
		Debug.Log ("Starting game now");
		opponentPaddle.GetComponent<Renderer>().enabled = true;
		ball.GetComponentsInChildren<Renderer>()[0].enabled = true;
		gameLogic.StartGame ();
	}


	#endregion

	#region private helper methods

	private void InitializeGameSession() {
		gameLogic = new GameLogic ();
	
		swipeControl = Instantiate (swipeControlPrefab).GetComponent<SwipeControl>();

		// initializing the paddles
		short otherPlayerNumber = 0;
		if (playerNumber == 1) {
			myPaddle = CreatePaddle (gameLogic.paddle1, paddle1Prefab).GetComponent<Paddle> ();
			opponentPaddle = CreatePaddle (gameLogic.paddle2, paddle2Prefab).GetComponent<Paddle> ();	
			otherPlayerNumber = 2;
		} else {
			myPaddle = CreatePaddle (gameLogic.paddle2, paddle2Prefab).GetComponent<Paddle> ();	
			opponentPaddle = CreatePaddle (gameLogic.paddle1, paddle1Prefab).GetComponent<Paddle> ();	
			otherPlayerNumber = 1;
		}

		// only if we are running in mobile
		//#if UNITY_IOS
		myPaddle.SetSwipeControl (swipeControl);
		//#endif

		myPaddle.SetIsPaddleControlledByThisClient (true);
		myPaddle.SetGameModel (gameId, gameLogic, playerNumber);

		opponentPaddle.SetIsPaddleControlledByThisClient (false);
		opponentPaddle.SetGameModel (gameId, gameLogic, otherPlayerNumber);
		opponentPaddle.GetComponent<Renderer>().enabled = false;

		// initializing the ball
		ball = Instantiate (ballPrefab).GetComponent<Ball>();
		ball.SetGameModel (gameId, gameLogic);
		ball.GetComponentsInChildren<Renderer>()[0].enabled = false;
	}

	private GameObject CreatePaddle(SimpleGameObject paddleVO, GameObject prefab) {
		GameObject paddle = Instantiate<GameObject>(prefab);
		paddle.transform.position = new Vector3 (paddleVO.x, paddle.transform.position.y, 2);
		return paddle;
	}


	#endregion

}
