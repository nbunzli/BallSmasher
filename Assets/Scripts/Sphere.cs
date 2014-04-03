using UnityEngine;
using System.Collections;

public class Sphere : MonoBehaviour
{
	// 0 = Blue
	// 1 = Green
	// 2 = Red
	// 3 = Yellow
	// 10 = Wild
	// 11 = Nuke
	public int Color;

	// How long the game over condition is ignored after spawning
	public float SafeTime = 2.0f;

	float SafeCountdown;
	bool bSafe = true;

	void Start() 
	{
		SafeCountdown = SafeTime;
		bSafe = true;
	}
	
	void Update() 
	{
		if(SafeCountdown > 0.0f)
		{
			SafeCountdown -= Time.deltaTime;
			if(SafeCountdown <= 0.0f)
			{
				bSafe = false;
			}
		}
	}

	public bool IsSafe()
	{
		return bSafe;
	}
}
