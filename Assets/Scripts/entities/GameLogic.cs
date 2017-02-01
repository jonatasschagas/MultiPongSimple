using System;


public class GameLogic {

	public String type = "GameLogic";
	public SimpleGameObject ball;
	public SimpleGameObject paddle1;
	public SimpleGameObject paddle2;

	public PaddleMovementRequest[] paddle1Replay;
	public PaddleMovementRequest[] paddle2Replay;

	// how much the ball moves at every step
	public float step = 0.05f;

	// current angle the ball bounces
	public float currentAngle = 45f;

	// bounds
	public int lowerBoundGridX = 0;
	public int higherBoundGridX = 11;
	public int lowerBoundGridY = 0;
	public int higherBoundGridY = 11;

	// fake bools (used to make code portable between C# and Java
	private static int TRUE = 1;
	private static int FALSE = 0;

	// fake int bools
	public int hasStarted;
	public int gameOver;
	public int incrementX;
	public int incrementY;

	public int player1Connected = FALSE;
	public int player2Connected = FALSE;

	// winner == 1 -> paddle 1 won
	// winner == 2 -> paddle 2 won
	public int winner;

	public long currentTick = 0;

	public GameLogic() {
		ball = new SimpleGameObject ();
		paddle1 = new SimpleGameObject ();
		paddle2 = new SimpleGameObject ();
		incrementX = TRUE;
		incrementY = TRUE;
		hasStarted = FALSE;
		paddle1.x = higherBoundGridX/2;
		paddle1.y = 0;
		ball.x = higherBoundGridX/2;
		ball.y = 0;
		paddle2.x = higherBoundGridX/2;
		paddle2.y = higherBoundGridY;
		currentTick = 0;
	}

	public void ManuallyMoveBall(float x, float y){
		if 	(gameOver == TRUE) {
			return;
		}
		ball.x = x;
		ball.y = y;
	}

	public void MovePaddle1(float x, float y){
		if (gameOver == TRUE) {
			return;
		}
		paddle1.x = x;
		paddle1.y = y;
		if (hasStarted == FALSE) {
			ManuallyMoveBall (x, y);
		}
	}

	public void MovePaddle2(float x, float y){
		if (gameOver == TRUE) {
			return;
		}
		paddle2.x = x;
		paddle2.y = y;
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
		hasStarted = TRUE;
	}

	public void MoveBall() {

		if (gameOver == TRUE || hasStarted == FALSE) {
			return;
		}

		currentTick++;

		if ((ball.x + 1) > higherBoundGridX) {
			incrementX = FALSE;
		} else if(ball.x < lowerBoundGridX) {
			incrementX = TRUE;
		}

		if ((ball.y + 1) > higherBoundGridY) {
			incrementY = FALSE;
			/*if (HasCollided(paddle2.x, ball.x) == TRUE) {
				//currentAngle = MathUtils.GetRandom (10f, 60f);
				currentAngle = 45f;
			} else {
				// paddle didnt hit the ball
				gameOver = TRUE;
				winner = 1;
				return;
			}*/

		} else if(ball.y < lowerBoundGridY) {
			incrementY = TRUE;
			// paddle hit the ball
			/*if (HasCollided(paddle1.x, ball.x) == TRUE) {
				//currentAngle = MathUtils.GetRandom (10f, 60f);
				currentAngle = 45f;
			} else {
				// paddle didnt hit the ball
				gameOver = TRUE;
				winner = 2;
				return;
			}*/

		}

		double prevX = ball.x;
		if (incrementX == TRUE) {
			ball.x += step;
		} else {
			ball.x -= step;
		}

		float deltaX = MathUtils.Abs(ball.x,prevX);
		if (incrementY == TRUE) {
			ball.y+= MathUtils.Tan (MathUtils.DegreesToRad(currentAngle)) * deltaX;
		} else {
			ball.y-= MathUtils.Tan (MathUtils.DegreesToRad(currentAngle)) * deltaX;
		}

	}

	int HasCollided(float xPaddle, float xBall) {
		return xBall >= xPaddle && xBall <= xPaddle + 1 || xBall + 1 >= xPaddle && xBall + 1 <= xPaddle + 1 ? TRUE : FALSE;
	}

}
