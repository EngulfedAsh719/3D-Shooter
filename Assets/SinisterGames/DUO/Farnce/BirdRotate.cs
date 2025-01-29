

using System;
using UnityEngine;

[AddComponentMenu("Scripts/Quests/SpinningSymbol")]


public class BirdRotate : MonoBehaviour

{

	public float Rotates = -30.0F;

	private void Update()
	{
		transform.Rotate(0,Rotates*Time.deltaTime,0);
	}
}