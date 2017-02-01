using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

/// <summary>
/// Handles the Paddle related logic. 
/// Reacts to the inputs and renders the paddle according to the game state.
/// </summary>
public class Paddle : MonoBehaviour {

	#region control members
	/// <summary>
	/// Flag that tells whether this paddle is controlled by the 
	/// client or not.
	/// </summary>
	private bool isPaddleControlledByThisClient;

	/// <summary>
	/// The paddle model object to which the changes in the game world are reflected.
	/// </summary>
	private SimpleGameObject paddleModel;

	/// <summary>
	/// The game model.
	/// </summary>
	private GameLogic gameModel;

	/// <summary>
	/// The game identifier.
	/// </summary>
	private String gameId;

	/// <summary>
	/// The player number.
	/// </summary>
	private short playerNumber;

	/// <summary>
	/// Lower and Higher bounds in X. Used to limit how much the paddle can move in the X coordinate.
	/// </summary>
	private float lowerBoundGridX;
	private float higherBoundGridX;

	/// <summary>
	/// Function that is executed in the game model to update the paddle movement
	/// </summary>
	private delegate void MovePaddle(float x, float y); 
	private MovePaddle movePaddleFunction;

	/// <summary>
	/// Dictionary that is used to store the inputs from the game. These inputs are used for 
	/// the server reconciliation (e.g.: when the game receives the server updates, it goes back to the 
	/// state returned by the server and fast forwards to the current state of the client by executing the inputs).
	/// 
	/// The key is the tick and teh value is the list of inputs from that tick.
	/// 
	/// </summary>
	private Dictionary<long, List<PaddleMovementRequest>> inputs;

	/// <summary>
	/// Queue that has the replay movements that is used to simulate the opponents paddle
	/// </summary>
	private Queue<PaddleMovementRequest> replayQueue;

	/// <summary>
	/// Object used to detect the swipes on the screen
	/// </summary>
	private SwipeControl swipeControl;

	#endregion

	#region main methods

	void Start () {
		inputs = new Dictionary<long, List<PaddleMovementRequest>> ();	
		replayQueue = new Queue<PaddleMovementRequest>();
	}
	
	void Update () {
		Render ();
	}

	private void Render() {
		if (!LeanTween.isTweening (gameObject)) {
			Vector3 paddleCurPos = gameObject.transform.position;
			if (isPaddleControlledByThisClient) {
				// rendering the paddle based on the model
				paddleCurPos.x = paddleModel.x;
				paddleCurPos.y = paddleModel.y;
				LeanTween.move (gameObject, paddleCurPos, 0.1f);
			} else if (replayQueue.Count > 0) {
				PaddleMovementRequest movement = replayQueue.Dequeue ();
				paddleCurPos.x = movement.paddle.x;
				paddleCurPos.y = movement.paddle.y;
				LeanTween.move (gameObject, paddleCurPos, 0.1f);
			}
		} 
	}

	/// <summary>
	/// Detects the input and stores it
	/// </summary>
	private void DetectInput(SwipeControl.SWIPE_DIRECTION iDirection) {

		if (gameModel == null) {
			return;
		}

		if (this.isPaddleControlledByThisClient) {

			float curX = paddleModel.x;
			float curY = paddleModel.y;

			switch ( iDirection ) {
			case SwipeControl.SWIPE_DIRECTION.SD_LEFT:
				curX--;
				break;
			case SwipeControl.SWIPE_DIRECTION.SD_RIGHT:
				curX++;
				break;
			}

			this.movePaddleFunction(curX, curY);
			// storing the input
			long currentTick = gameModel.currentTick;
			PaddleMovementRequest paddleMovementRequest = new PaddleMovementRequest();
			paddleMovementRequest.gameId = gameId;
			paddleMovementRequest.paddle = paddleModel;
			paddleMovementRequest.playerNumber = playerNumber;
			paddleMovementRequest.tick = currentTick;
			List<PaddleMovementRequest> listOfMovements = null;
			if (!inputs.ContainsKey (currentTick)) {
				inputs.Add (currentTick, new List<PaddleMovementRequest> ());
			}
			listOfMovements = inputs [currentTick];
			listOfMovements.Add (paddleMovementRequest);
			inputs[currentTick] = listOfMovements;
		}

	}

	/// <summary>
	/// Synchronizes where the paddle should be according to the updated game state
	/// </summary>
	/// <param name="serverState">Server state.</param>
	public void SynchronizeWithServerState(GameLogic updatedGameState) {

		GameLogic currentState = gameModel;

		if (isPaddleControlledByThisClient == false) {
			return;
		}

		SimpleGameObject currentPaddle = currentState.GetPaddleByPlayerNumber (playerNumber);
		SimpleGameObject serverRefreshedPaddle = updatedGameState.GetPaddleByPlayerNumber (playerNumber);

		// updates the current state's paddle if the positions differ
		if (currentPaddle.x != serverRefreshedPaddle.x) {
			currentState.SetPaddleByPlayerNumber (playerNumber, serverRefreshedPaddle);
		}

	}

	/// <summary>
	/// Enqueues the opponent movements, so in the update we can replay his movements
	/// </summary>
	/// <param name="replay">Replay.</param>
	public void EnqueueOpponentReplay(PaddleMovementRequest[] replay) {
		if(replay != null && replay.Length > 0) {
			foreach(PaddleMovementRequest paddleMovement in replay) {
				replayQueue.Enqueue (paddleMovement);
			}
		}
	}

	#endregion

	#region getters and setters

	public void SetIsPaddleControlledByThisClient(bool isPaddleControlled) {
		this.isPaddleControlledByThisClient = isPaddleControlled;
	}

	public void SetGameModel(String gameId, GameLogic gameModel, short playerNumber) {
		if (playerNumber == 1) {
			this.paddleModel = gameModel.paddle1;
			this.movePaddleFunction = gameModel.MovePaddle1;
		} else {
			this.paddleModel = gameModel.paddle2;
			this.movePaddleFunction = gameModel.MovePaddle2;
		}
		this.playerNumber = playerNumber;
		this.gameId = gameId;
		this.gameModel = gameModel;
		this.lowerBoundGridX = gameModel.lowerBoundGridX;
		this.higherBoundGridX = gameModel.higherBoundGridX;
	}

	public SimpleGameObject GePaddleModel() {
		return paddleModel;
	}

	public PaddleMovementRequest GetPaddlePosition() {
		PaddleMovementRequest paddleMovementRequest = new PaddleMovementRequest ();
		paddleMovementRequest.gameId = gameId;
		paddleMovementRequest.paddle = paddleModel;
		paddleMovementRequest.playerNumber = playerNumber;
		paddleMovementRequest.tick = gameModel.currentTick;
		return paddleMovementRequest;
	}

	public List<PaddleMovementRequest> GetInputsForTick(long tick) {
		if (inputs.ContainsKey (tick)) {
			return inputs[tick];
		}
		return null;
	}

	public void SetSwipeControl(SwipeControl swipeControl) {
		this.swipeControl = swipeControl;
		swipeControl.SetMethodToCall (DetectInput);
	}

	#endregion

}
