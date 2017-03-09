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
			//LeanTween.move (gameObject, ballCurPos, gameModel.speed);
			float diffX = Math.Abs(transform.position.x - ballCurPos.x);
			float diffY = Math.Abs(transform.position.y - ballCurPos.y);
			if (diffX > 0.5f || diffY > 0.5f) {
				// smoothing the sudden difference in between 
				// the previous and current position of the ball
				LeanTween.move (gameObject, ballCurPos, 0.25f);
			} else {
				LeanTween.move (gameObject, ballCurPos, 0f);
			}
		}

	}

	#endregion

	#region getters and setters

	public void SetGameModel(String gameId, GameLogic gameModel) {
		this.gameModel = gameModel;
	}

	#endregion

}
