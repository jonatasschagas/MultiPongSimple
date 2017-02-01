using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

/// <summary>
/// Handles the ball related logic.
/// 
/// Updates the ball according to the latest state returned by the server.
/// 
/// </summary>
public class Ball : MonoBehaviour {

	#region control members

	private GameLogic gameModel;

	#endregion

	#region main methods

	void Start () {}
	
	void Update () {

		// rendering ball
		Vector3 ballCurPos = transform.position;
		ballCurPos.x = gameModel.ball.x;
		ballCurPos.y = gameModel.ball.y;
		if (!LeanTween.isTweening (gameObject)) {
			LeanTween.move (gameObject, ballCurPos, 0.5f);
		}

	}

	/// <summary>
	/// Synchronizes where the ball should be according to the updated game state
	/// </summary>
	/// <param name="serverState">Server state.</param>
	public void SynchronizeWithServerState(GameLogic updatedGameState) {
		SimpleGameObject currentBall = gameModel.ball;
		SimpleGameObject updatedBall = updatedGameState.ball;

		// updates the current state of the ball if the local position and the refreshed one differ
		if (currentBall.x != updatedBall.x || currentBall.y != updatedBall.y) {
			gameModel.ball = updatedBall;
		}
	}

	#endregion

	#region getters and setters

	public void SetGameModel(String gameId, GameLogic gameModel) {
		this.gameModel = gameModel;
	}

	#endregion

}
