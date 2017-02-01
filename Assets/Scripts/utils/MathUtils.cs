using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MathUtils {

	public static float GetRandom(float lowerBound, float higherBound) {
		return UnityEngine.Random.Range (lowerBound, higherBound);
	}

	public static float Tan(float angleInRad) {
		return UnityEngine.Mathf.Tan (angleInRad);
	}

	public static float DegreesToRad(float angleInDegrees) {
		return UnityEngine.Mathf.Deg2Rad * angleInDegrees;
	}

	public static float Abs(double val1, double val2){
		return UnityEngine.Mathf.Abs ((float)val1 - (float)val2);
	}

}
