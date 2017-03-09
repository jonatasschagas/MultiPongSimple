using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MathUtils {

	public static float GetRandom(float lowerBound, float higherBound) {
		return UnityEngine.Random.Range (lowerBound, higherBound);
	}

	public static float Tan(float angle) {
		return UnityEngine.Mathf.Tan (DegreesToRad(angle));
	}

	public static float Cos(float angle) {
		return UnityEngine.Mathf.Cos (DegreesToRad(angle));
	}

	public static float Sin(float angle) {
		return UnityEngine.Mathf.Sin (DegreesToRad(angle));
	}

	public static float DegreesToRad(float angleInDegrees) {
		return UnityEngine.Mathf.Deg2Rad * angleInDegrees;
	}

	public static float Abs(double val1, double val2){
		return UnityEngine.Mathf.Abs ((float)val1 - (float)val2);
	}

}
