using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

/// <summary>
/// Handles the Paddle related logic. 
/// Reacts to the inputs and renders the paddle according to the game state.
/// </summary>
public class Paddle : MonoBehaviour {

	private float DETECT_INPUT_INTERVAL_SECONDS = 0.1f ;

	private float DETECT_MOBILE_INPUT_INTERVAL_SECONDS = 0.1f ;

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
	private delegate void MovePaddle(bool right); 
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
	/// Swipe controller 
	/// </summary>
	private SwipeControl swipeControl;

	/// <summary>
	/// Tells whether the game is being run in a mobile device or if its run in a computer
	/// </summary>
	private bool isMobile;

	/// <summary>
	/// Cool down time to detect input
	/// </summary>
	private float detectInputCooldown;

	private Animator animator;

	#endregion

	#region main methods

	void Start () {
		inputs = new Dictionary<long, List<PaddleMovementRequest>> ();	
		isMobile = false;
		animator = gameObject.GetComponent<Animator> ();
	}
	
	void Update () {
		DetectInputFromComputer ();
		Render ();
		animator.SetFloat ("paddle_and_ball_x_diff", Mathf.Abs(paddleModel.x - gameModel.ball.x));
		animator.SetFloat ("paddle_and_ball_y_diff", Mathf.Abs(paddleModel.y - gameModel.ball.y));
	}

	private void Render() {
		if (!LeanTween.isTweening (gameObject)) {
			Vector3 paddleCurPos = gameObject.transform.position;
			// rendering the paddle based on the model
			paddleCurPos.x = paddleModel.x;

			if (isPaddleControlledByThisClient) {
				// make it feel snappy for the local player
				LeanTween.move (gameObject, paddleCurPos, 0.01f);
			} else {
				// delay a bit the movement for the opponent to give a feel of smoothness"
				LeanTween.move (gameObject, paddleCurPos, 0.25f);
			}
		} 
	}

	/// <summary>
	/// Detects if the input is from a computer.
	/// </summary>
	private void DetectInputFromComputer() {
		if (this.isPaddleControlledByThisClient) { 
			if (Input.GetKey(KeyCode.LeftArrow)) {
				UpdatePaddlePosition(false);
			} else if (Input.GetKey(KeyCode.RightArrow)) {
				UpdatePaddlePosition(true);
			}
		}
	}

	/// <summary>
	/// Detects the input if it's a mobile device
	/// </summary>
	private void DetectInputFromMobile(SwipeControl.SWIPE_DIRECTION iDirection) {
		if (iDirection == SwipeControl.SWIPE_DIRECTION.SD_RIGHT) {
			UpdatePaddlePosition (true);
		} else if (iDirection == SwipeControl.SWIPE_DIRECTION.SD_LEFT) {
			UpdatePaddlePosition (false);
		}
	}

	private void UpdatePaddlePosition(bool increaseX) {

		if (gameModel == null) {
			return;
		}

		this.movePaddleFunction(increaseX);

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

	public void UpdatePaddleModel(GameLogic gameModel) {
		if (playerNumber == 1) {
			this.paddleModel = gameModel.paddle1;
		} else {
			this.paddleModel = gameModel.paddle2;
		}
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
		swipeControl.SetMethodToCall (DetectInputFromMobile);
		this.isMobile = true;
	}

	#endregion

}
