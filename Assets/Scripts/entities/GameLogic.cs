using UnityEngine;
using System;

public class GameLogic {

	public String type = "GameLogic";
	public SimpleGameObject ball;
	public SimpleGameObject lastBounce;
	public SimpleGameObject paddle1;
	public SimpleGameObject paddle2;

	// bounds
	public int lowerBoundGridX = 0;
	public int higherBoundGridX = 4;
	public int lowerBoundGridY = 0;
	public int higherBoundGridY = 7;

	public bool hasStarted;
	public bool paused;
	public bool gameOver;
	public bool player1Connected = false;
	public bool player2Connected = false;

	public bool up;
	public bool right;

	// winner == 1 -> paddle 1 won
	// winner == 2 -> paddle 2 won
	public int winner;

	public static int FPS = 60;
	public long currentTick;
	public long bounceAngle;
	public static float START_SPEED = 1.0f/20;
	public float speed = START_SPEED;
	public float paddleSpeed = 0.1f;
	public int player1Score = 0;
	public int player2Score = 0;

	public int sizeArrayAngles = 50;
	public int[] arrayAngles;
	public int arrayAnglesCurrentIndex = 0;

	public bool isGoalPause;
	public long goalUnpauseTick;

	private long FIXED_ANGLE = 35;

	public GameLogic() {
		ball = new SimpleGameObject ();
		paddle1 = new SimpleGameObject ();
		paddle2 = new SimpleGameObject ();
		Restart ();
		hasStarted = false;
	}

	public void Restart() {
		arrayAnglesCurrentIndex = 0;
		hasStarted = true;
		gameOver = false;
		paddle1.x = higherBoundGridX/2;
		paddle1.y = 0;
		ball.x = higherBoundGridX/2;
		ball.y = 0;
		paddle2.x = higherBoundGridX/2;
		paddle2.y = higherBoundGridY;
		currentTick = 0;
		bounceAngle = FIXED_ANGLE;
		lastBounce = new SimpleGameObject();
		lastBounce.x = ball.x;
		lastBounce.y = ball.y;
		up = true;
		right = true;
		isGoalPause = false;
	}

	public void ManuallyMoveBall(float x){
		if 	(gameOver) {
			return;
		}
		ball.x = x;
		lastBounce.x = x;
	}

	public void MovePaddle1(bool right){
		if (gameOver) {
			return;
		}

		if (right) {
			paddle1.x += paddleSpeed;
		} else {
			paddle1.x -= paddleSpeed;
		}

		if (paddle1.x < 0) {
			paddle1.x = 0;
		} else if(paddle1.x > higherBoundGridX) {
			paddle1.x = higherBoundGridX;
		}

		if (paused) {
			ManuallyMoveBall (paddle1.x);
		}
	}

	public void MovePaddle2(bool right){
		if (gameOver) {
			return;
		}

		if (right) {
			paddle2.x += paddleSpeed;
		} else {
			paddle2.x -= paddleSpeed;
		}

		if (paddle2.x < 0) {
			paddle2.x = 0;
		} else if (paddle2.x > higherBoundGridX){
			paddle2.x = higherBoundGridX;
		}

		if (paused) {
			ManuallyMoveBall (paddle2.x);
		}
	}

	public SimpleGameObject GetPaddleByPlayerNumber(short playerNumber) {
		if (playerNumber == 1) {
			return paddle1;
		} else {
			return paddle2;
		}
	}

	public void SetPaddleByPlayerNumber(short playerNumber, SimpleGameObject paddle) {
		if (playerNumber == 1) {
			this.paddle1 = paddle;
		} else {
			this.paddle2 = paddle;
		}
	}

	public void StartGame() {
		hasStarted = true;
	}

	public void Update() {
			
		if (gameOver || !hasStarted) {
			return;
		}

		currentTick++;

		// increasing the difficulty
		if (currentTick % 100 == 0 && speed < 0.2f) {
			speed += 0.01f;
		}

		if (up) {

			if (right) {
				ball.x = ball.x + MathUtils.Cos (bounceAngle) * speed;
				ball.y = ball.y + MathUtils.Sin (bounceAngle) * speed;
			} else {
				ball.x = ball.x - MathUtils.Sin (bounceAngle) * speed;
				ball.y = ball.y + MathUtils.Cos (bounceAngle) * speed;
			}

		} else {

			if (right) {
				ball.x = ball.x + MathUtils.Cos (bounceAngle) * speed;
				ball.y = ball.y - MathUtils.Sin (bounceAngle) * speed;
			} else {
				ball.x = ball.x - MathUtils.Sin (bounceAngle) * speed;
				ball.y = ball.y - MathUtils.Cos (bounceAngle) * speed;
			}

		}

		// verifies if it hits the walls
		if (ball.x > higherBoundGridX) {
			right = false;
		} else if (ball.x < lowerBoundGridX) {
			right = true;
		}

		// verifies if it hit the paddles
		if (MathUtils.Abs(ball.y, paddle2.y) <= 0.2f && HasCollided(paddle2.x, ball.x)) {
			up = false;
			bounceAngle = GetAngle ();
		} else if (MathUtils.Abs(ball.y, paddle1.y) <= 0.2f && HasCollided(paddle1.x, ball.x)) {
			up = true;
			bounceAngle = GetAngle ();
		}

		// pause to show the goal sign
		if(isGoalPause && currentTick > goalUnpauseTick) {
			isGoalPause = false;	
			speed = START_SPEED;
			// reset the game depending on who scored
			if (ball.y < lowerBoundGridY) {
				// player 1 starts
				ball.x = paddle1.x;
				ball.y = lowerBoundGridY;
				up = true;
				right = true;
			} else if (ball.y > higherBoundGridY){
				ball.x = paddle2.x;
				ball.y = higherBoundGridY;
				up = false;
				right = false;
			}

		} else if (ball.y < (lowerBoundGridY - 1) && !isGoalPause) {
			isGoalPause = true;
			// 2 seconds to show the Goal sign
			goalUnpauseTick = currentTick + FPS * 2;
			player2Score++;
		} else if (ball.y > (higherBoundGridY + 1) && !isGoalPause){
			isGoalPause = true;
			// 2 seconds to show the Goal sign
			goalUnpauseTick = currentTick + FPS * 2;
			player1Score++;
		}

	}

	int GetAngle() {
		if (arrayAngles == null || arrayAnglesCurrentIndex >= sizeArrayAngles) {
			arrayAngles = new int[sizeArrayAngles];
			for (int i = 0; i < sizeArrayAngles; i++) {
				arrayAngles [i] = (int)MathUtils.GetRandom (10, 50);
			}
			arrayAnglesCurrentIndex = 0;
		}
		return arrayAngles[arrayAnglesCurrentIndex];
	}

	bool DetectPaddleCollision() {
		return HasCollided (paddle1.x, ball.x) || HasCollided (paddle2.x, ball.x);
	}

	public bool HasCollided(float xPaddle, float xBall) {
		return xBall >= xPaddle && xBall <= xPaddle + 1 || xBall + 1 >= xPaddle && xBall + 1 <= xPaddle + 1 ? true : false;
	}

	public void Copy(GameLogic gameLogic) {

		ball = gameLogic.ball;
		lastBounce = gameLogic.lastBounce;
		paddle1 = gameLogic.paddle1;
		paddle2 = gameLogic.paddle2;

		hasStarted = gameLogic.hasStarted;
		gameOver = gameLogic.gameOver;
		player1Connected = gameLogic.player1Connected;
		player2Connected = gameLogic.player2Connected;

		winner = gameLogic.winner;

		speed = gameLogic.speed;
		currentTick = gameLogic.currentTick;
		bounceAngle = gameLogic.bounceAngle;
		paused = gameLogic.paused;
		arrayAngles = gameLogic.arrayAngles;
		arrayAnglesCurrentIndex = gameLogic.arrayAnglesCurrentIndex;
		sizeArrayAngles = gameLogic.sizeArrayAngles;
	}

}
