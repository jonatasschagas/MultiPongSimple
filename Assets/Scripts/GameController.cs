using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.UI;

public class GameController : MonoBehaviour, IListener {

	#region constants
	private const float SERVER_PULL_INTERVAL_SECONDS = 0.5f; 
	private const int 	FPS = 60;
	#endregion

	#region control members
	private string gameId;
	private short playerNumber;
	private ServerMessageDispatcher serverMessageDispatcher;
	private bool isConnected;
	private float reSendToServerCoolDown = 0;
	private GameLogic gameLogic;
	#endregion

	#region UI
	public Text statusText; 
	#endregion

	#region prefabs
	public GameObject startBtn;
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

	#region main methods

	void Start () {
		serverMessageDispatcher = gameObject.GetComponent<ServerMessageDispatcher> ();
		RegisterEventListeners ();
		Application.targetFrameRate = FPS;
	}
	
	void Update () {

		if (gameLogic == null) {
			return;
		}

		UpdateServer();

		if(gameLogic.hasStarted == 1 && gameLogic.gameOver == 0) {
			// update game if it has started
			gameLogic.MoveBall ();
		}

	}

	private void UpdateServer() {
		reSendToServerCoolDown -= Time.deltaTime;
		if (isConnected && reSendToServerCoolDown <= 0.0f) {

			// send new paddle movement to the server
			Debug.Log("Sending new paddle movement to the server");
			serverMessageDispatcher.SendMessage (myPaddle.GetPaddlePosition ());

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

		if(currentState.player1Connected == 1 && currentState.player2Connected == 1 && playerNumber == 1) {
			statusText.text = "Prepare to start! Game about to begin...";
			// starting the game after 3 seconds
			Invoke("SendGameStartMessage", 3);
			return;
		}

		// fast forwarding by executing the inputs
		for (long tick = serverState.currentTick; tick <= currentState.currentTick; tick++) {
			List<PaddleMovementRequest> inputsPerTick = myPaddle.GetInputsForTick(tick);
			if(inputsPerTick != null) {
				foreach (PaddleMovementRequest input in inputsPerTick) {
					SimpleGameObject paddle = input.paddle;
					if (input.playerNumber == 1) {
						serverState.MovePaddle1 (paddle.x, paddle.y);
					} else {
						serverState.MovePaddle2 (paddle.x, paddle.y);
					}
				}
			}
			serverState.MoveBall ();
		}

		// updating the entities according to the server's update
		myPaddle.SynchronizeWithServerState (serverState);
		if (playerNumber == 1) {
			opponentPaddle.EnqueueOpponentReplay (serverState.paddle2Replay);
		} else {
			opponentPaddle.EnqueueOpponentReplay (serverState.paddle1Replay);
		}
		ball.SynchronizeWithServerState (serverState);

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

	/// <summary>
	/// When the start game message comes from the server it starts the game
	/// </summary>
	/// <param name="startGameResponse">Start game response.</param>
	private void ProcessStartGame(StartGameResponse startGameResponse) {
		Debug.Log ("Starting game now");
		statusText.text = "";
		gameLogic.StartGame ();
	}

	/// <summary>
	/// Processes the remote paddle movement
	/// </summary>
	/// <param nam e="paddleMovementRequest">Paddle movement request.</param>
	private void ProcessPaddleMovement(PaddleMovementRequest paddleMovementRequest) {
		Debug.Log ("Process Paddle Movement: " + JsonUtility.ToJson(paddleMovementRequest));
		if (paddleMovementRequest.playerNumber == 1) {
			gameLogic.MovePaddle1 (paddleMovementRequest.paddle.x, paddleMovementRequest.paddle.y);
		} else {
			gameLogic.MovePaddle2 (paddleMovementRequest.paddle.x, paddleMovementRequest.paddle.y);
		}
	}

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

	#endregion

	#region button actions

	/// <summary>
	/// This method is used by the "start button". It initializes the game by
	/// connecting to the server.
	/// </summary>
	public void ConnectToServer() {
		Destroy (startBtn);
		GameConnectionRequest gameConnectionRequest = new GameConnectionRequest ();
		gameConnectionRequest.deviceId = SystemInfo.deviceUniqueIdentifier;
		serverMessageDispatcher.SendMessage (gameConnectionRequest);
	}

	#endregion

	#region private helper methods

	private void InitializeGameSession() {
		gameLogic = new GameLogic ();
		AlignCamera ();

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

		myPaddle.SetSwipeControl (swipeControl);

		myPaddle.SetIsPaddleControlledByThisClient (true);
		myPaddle.SetGameModel (gameId, gameLogic, playerNumber);

		opponentPaddle.SetIsPaddleControlledByThisClient (false);
		opponentPaddle.SetGameModel (gameId, gameLogic, otherPlayerNumber);

		// initializing the ball
		ball = Instantiate (ballPrefab).GetComponent<Ball>();
		ball.SetGameModel (gameId, gameLogic);
	}

	private void AlignCamera() {
		Camera.main.transform.position = new Vector3 (5.5f,5.2f, -10);
	}

	private GameObject CreatePaddle(SimpleGameObject paddleVO, GameObject prefab) {
		GameObject paddle = Instantiate<GameObject>(prefab);
		paddle.transform.position = new Vector3 (paddleVO.x, paddleVO.y, 2);
		return paddle;
	}


	#endregion

}
