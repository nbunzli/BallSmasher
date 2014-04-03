using UnityEngine;
using System.Collections;

public class GameLogic : MonoBehaviour 
{
	// Objects in scene
	public GameObject[] SpherePrefabs;				// Prefabs for normal spheres (blue, gree, red, yellow)
	public GameObject[] PowerupPrefabs;				// Prefabs for powerups (wild, nuke)
	public GameObject[] ColorSelectors;				// Buttons on left panel to change current color
	public GameObject[] CurrentColorIndicators;		// Fire particles to show which color is currently selected
	public GameObject   SphereExplosion;			// Spawned when a sphere is tapped and destroyed
	public GameObject	GameOverExplosion;			// Big explosion when the game ends
	public GameObject   RedLine;					// Line which the spheres must stay under
	public GameObject   ScoreText;					// Top left corner during game. More than 4 digits will overflow into the game area (though with the current difficulty settings that won't be a problem)
	public GameObject   GameOverText;				// "Game over"
	public GameObject   GameOverScoreText;			// "Score: x"
	public GameObject   GameOverHighScoreText;		// "High score: x"
	public GameObject   TitleText;					// "Ball Smasher"
	public GameObject   InstructionText;			// Tutorial screen text
	public GameObject   AudioWarning;				// Plays when spheres are above the red line - and also includes explosion sound for game over
	public GameObject	AudioChangeColor;			// Plays when user changes color using buttons on left panel
	public GameObject   Music;
	public GUISkin      Skin;
	
	// Vars to tweak difficulty
	public float SpawnAttemptFrequency = 0.05f;		// How often we attempt to spawn a sphere
	public float SpawnProbability = 0.2f;			// Probability of spawning a sphere in a single attempt
	public float SpawnProbabilityGrowth = 0.005f;	// How much SpawnProbability grows per second
	public float PowerupProbability = 0.08f;		// Probability of spawning a powerup instead of a normal sphere
	public float GameOverTime = 3.0f;				// If one or more spheres are above the red line for this long, the game ends
	public float TouchFudgeFactor = 1.25f;			// Increases hit area of sphere
		
	// Private vars
	ArrayList	Spheres = new ArrayList();			// Currently spawned spheres
	float 		CurrentSpawnProbability;			// SpawnProbability + (SpawnProbabilityGrowth * Time)
	float		SpawnAttemptCountdown;				// Time until next spawn attempt
	int 		CurrentColor;						// Which color the user has selected
	bool 		bNuke;								// True if nuke powerup is active
	float 		WildCountdown;						// >0 if wild powerup is active
	float 		TimeOverLine;						// How long one or more spheres has been over the red line
	float 		GameOverGUICountdown;				// Countdown to show GUI after game over (don't show it instantly due to frantic tapping)
	int 		Score;
	float 		ScreenHeight;
	float 		ScreenWidth;
	
	enum GameState
	{
		None,
		FirstEntry,
		Game,
		GameOver,
		Tutorial,
	};
	GameState CurrentState = GameState.None;

	// Use this for initialization
	void Start() 
	{
		CurrentState = GameState.FirstEntry;
		
		ScreenHeight = Camera.main.pixelHeight;
		ScreenWidth = Camera.main.pixelWidth;
		
		for(int i = 0; i < 4; i++)
			CurrentColorIndicators[i].SetActive(false);

		HideAllText();
		TitleText.SetActive(true);
	}
	
	void StartGame()
	{
		CurrentState = GameState.Game;
		CurrentSpawnProbability = SpawnProbability;
		SpawnAttemptCountdown = SpawnAttemptFrequency;
		CurrentColor = 0;
		CurrentColorIndicators[0].SetActive(true);
		bNuke = false;
		WildCountdown = 0.0f;
		TimeOverLine = 0.0f;
		Score = 0;
		ScoreText.GetComponent<TextMesh>().text = Score.ToString();
		DestroyAllSpheres();
		
		if(!Music.GetComponent<AudioSource>().isPlaying)
			Music.GetComponent<AudioSource>().Play();
		
		HideAllText();
		ScoreText.SetActive(true);
	}
	
	void StartTutorial()
	{
		CurrentState = GameState.Tutorial;
		
		HideAllText();
		InstructionText.SetActive(true);
	}
	
	void GameOver()
	{
		CurrentState = GameState.GameOver;
		
		GameObject ExplosionObject = Instantiate(GameOverExplosion) as GameObject;
		ExplosionObject.transform.position = new Vector3(0,0,-1);
		Destroy(ExplosionObject, 2.0f);
		
		DestroyAllSpheres();
		
		for(int i = 0; i < 4; i++)
			CurrentColorIndicators[i].SetActive(false);
		
		if(Score > PlayerPrefs.GetInt("HighScore"))
			PlayerPrefs.SetInt("HighScore", Score);
		
		HideAllText();
		GameOverText.SetActive(true);
		GameOverScoreText.GetComponent<TextMesh>().text = "Your score:" + Score;
		GameOverScoreText.SetActive(true);
		GameOverHighScoreText.GetComponent<TextMesh>().text = "High score:" + PlayerPrefs.GetInt("HighScore"); 
		GameOverHighScoreText.SetActive(true);
		
		GameOverGUICountdown = 2.0f;
	}
	
	void HideAllText()
	{
		ScoreText.SetActive(false);
		GameOverText.SetActive(false);
		GameOverScoreText.SetActive(false);
		GameOverHighScoreText.SetActive(false);
		TitleText.SetActive(false);
		InstructionText.SetActive(false);
	}
	
	void Update()
	{
		if(Input.GetKeyDown(KeyCode.Escape)) 
			Application.Quit();
			
		if(CurrentState == GameState.Game)
			UpdateGame();
		else if(CurrentState == GameState.GameOver)
			UpdateGameOver();
	}

	void UpdateGame() 
	{
		// Rotate currently selected color (or all if a powerup is active)
		for(int i = 0; i < ColorSelectors.Length; i++)
			if(CurrentColor == i || bNuke || WildCountdown > 0.0f)
				ColorSelectors[i].transform.Rotate(new Vector3(0,15,0) * Time.deltaTime);

		if(WildCountdown > 0.0f)
		{
			WildCountdown -= Time.deltaTime;
			if(WildCountdown <= 0.0f)
			{
				WildCountdown = 0.0f;
				
				if(!bNuke)
				{
					// Reset current color indicators
					for(int j = 0; j < CurrentColorIndicators.Length; j++)
						if(j != CurrentColor)
							CurrentColorIndicators[j].SetActive(false);
				}
			}
		}
		
		UpdateSpawning();

		ProcessInput();
		
		CheckGameOverCondition();
	}
	
	void UpdateSpawning()
	{
		CurrentSpawnProbability += SpawnProbabilityGrowth * Time.deltaTime;
		SpawnAttemptCountdown -= Time.deltaTime;
		if(SpawnAttemptCountdown <= 0.0f)
		{
			// Attempt to spawn a sphere
			SpawnAttemptCountdown = SpawnAttemptFrequency;
			if(Random.Range(0.0f, 1.0f) < CurrentSpawnProbability)
			{
				// Spawn a sphere
				GameObject[] PrefabList;
				if(Random.Range(0.0f, 1.0f) < PowerupProbability)
					PrefabList = PowerupPrefabs;	// Spawning a powerup
				else
					PrefabList = SpherePrefabs;		// Spawning a normal colored sphere
				
				int Idx = Random.Range(0, PrefabList.Length);
				GameObject NewSphere = Instantiate(PrefabList[Idx]) as GameObject;
				
				// Give each sphere a random position within our walls, and a random sideways force so that they all don't boringly fall straight down
				Vector3 Position = new Vector3(Random.Range(-5.9f, 5.9f), 6.0f, 0.0f);
				Vector2 Force = new Vector2(Random.Range(-300.0f, 300.0f), 0.0f);
				NewSphere.transform.position = Position;
				NewSphere.rigidbody2D.AddForce(Force);
				
				Spheres.Add(NewSphere);
			}
		}
	}
	
	void ProcessInput()
	{	
#if UNITY_EDITOR || UNITY_STANDALONE
		// For PC, use mouse input
		if(Input.GetMouseButtonDown(0))
		{
			Vector3 InputPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
#else
		// For device, use touch input
		// Multi touch is ok, just check if we're in TouchPhase Began
		for(int t = 0; t < Input.touchCount; t++)
		{
			Vector3 InputPos;
			if(Input.touches[t].phase == TouchPhase.Began)
				InputPos = Camera.main.ScreenToWorldPoint(Input.touches[t].position);
			else
				continue;
#endif
			
			// Radius of spheres is 0.625
			float FudgedRadius = 0.625f * TouchFudgeFactor;
			
			// We've got input - now check the position of each sphere
			for(int i = Spheres.Count - 1; i >= 0; i--)
			{
				GameObject CurrentSphere = (GameObject)Spheres[i];
				int CurrentSphereColor = CurrentSphere.GetComponent<Sphere>().Color;
				if(Vector2.Distance(InputPos, CurrentSphere.transform.position) < FudgedRadius)
				{
					if(bNuke && CurrentSphereColor < 10)
					{
						// Nuke powerup is active and user is tapping a normal sphere
						SpawnExplosion(CurrentSphere.transform.position);
						Destroy(CurrentSphere);
						Spheres.RemoveAt(i);
						Score++;
						Nuke(CurrentSphereColor);
						ScoreText.GetComponent<TextMesh>().text = Score.ToString();
						bNuke = false;
						
						if(WildCountdown <= 0.0f)
						{
							// Reset current color indicators
							for(int j = 0; j < CurrentColorIndicators.Length; j++)
								if(j != CurrentColor)
									CurrentColorIndicators[j].SetActive(false);
						}
						break;
					}
					else if(CurrentSphereColor >= 10)
					{
						// User is tapping a powerup
						if(CurrentSphereColor == 10)
							WildCountdown += 5.0f;
						else if(CurrentSphereColor == 11)
							bNuke = true;				
						
						// Turn on all current color indicators, since during powerups user can tap any color of sphere
						for(int j = 0; j < CurrentColorIndicators.Length; j++)
							CurrentColorIndicators[j].SetActive(true);
						
						SpawnExplosion(CurrentSphere.transform.position);
						Destroy(CurrentSphere);
						Spheres.RemoveAt(i);
					}
					else if(CurrentSphereColor == CurrentColor || WildCountdown > 0.0f)
					{
						// User is tapping a normal colored sphere
						SpawnExplosion(CurrentSphere.transform.position);
						Destroy(CurrentSphere);
						Spheres.RemoveAt(i);
						Score++;
						ScoreText.GetComponent<TextMesh>().text = Score.ToString();
					}
				}
			}
			
			// Check color selectors to see if user is changing color
			if(!bNuke && WildCountdown <= 0.0f)
			{
				for(int i = 0; i < ColorSelectors.Length; i++)
				{
					if(Vector2.Distance(InputPos, ColorSelectors[i].transform.position) < 0.75f)
					{
						AudioChangeColor.GetComponent<AudioSource>().Play();
						CurrentColorIndicators[CurrentColor].SetActive(false);
						CurrentColor = i;
						CurrentColorIndicators[CurrentColor].SetActive(true);
					}
				}
			}
		}
	}
	
	void CheckGameOverCondition()
	{
		bool bOverLine = false;
		for(int i = 0; i < Spheres.Count; i++)
		{
			GameObject Curr = (GameObject)Spheres[i];
			
			// Sphere radius = 0.625
			// Red line thickness = 0.1
			// To check if over line, sphere position + sphere radius > red line position + half of line thickness
			if(!Curr.GetComponent<Sphere>().IsSafe() && Curr.transform.position.y + 0.575f > RedLine.transform.position.y)
			{
				bOverLine = true;
				break;
			}
		}
			
		if(bOverLine)
		{
			if(TimeOverLine == 0.0f)
				AudioWarning.GetComponent<AudioSource>().Play();
			
			TimeOverLine += Time.deltaTime;
			
			if(TimeOverLine > GameOverTime)
				GameOver();
		}
		else
		{
			TimeOverLine = 0.0f;
			if(AudioWarning.GetComponent<AudioSource>().isPlaying)
				AudioWarning.GetComponent<AudioSource>().Stop();
		}
	}
	
	void DestroyAllSpheres()
	{
		for(int i = Spheres.Count - 1; i  >= 0; i--)
		{
			Destroy((GameObject)Spheres[i]);
			Spheres.RemoveAt(i);
		}
	}
	
	// Destroys all spheres of this color - called when the nuke powerup is used
	void Nuke(int Color)
	{
		for(int i = Spheres.Count - 1; i  >= 0; i--)
		{
			GameObject CurrentSphere = (GameObject)Spheres[i];
			int CurrentColor = CurrentSphere.GetComponent<Sphere>().Color;
			if(CurrentColor == Color)
			{				
				Destroy((GameObject)Spheres[i]);
				Spheres.RemoveAt(i);
				Score++;
			}
		}
	}

	void UpdateGameOver()
	{
		// Don't show the GUI immediately after the game ends, due to frantic tapping
		if(GameOverGUICountdown > 0.0f)
			GameOverGUICountdown -= Time.deltaTime;
	}

	void OnGUI()
	{
		GUI.skin = Skin;
		if(CurrentState == GameState.FirstEntry  || (CurrentState == GameState.GameOver && GameOverGUICountdown <= 0.0f))
		{
			if(GUI.Button(MakeGUIRect(new Vector2(0.55f, 0.6f), new Vector2(0.25f, 0.15f)), CurrentState == GameState.FirstEntry ? "Play" : "Play Again"))
				StartGame();
				
			if(GUI.Button(MakeGUIRect(new Vector2(0.55f, 0.8f), new Vector2(0.25f, 0.15f)), "Instructions"))
				StartTutorial();
		}
		else if(CurrentState == GameState.Tutorial)
		{
			if(GUI.Button(MakeGUIRect(new Vector2(0.55f, 0.8f), new Vector2(0.25f, 0.15f)), "Play"))
				StartGame();
		}
	}
	
	Rect MakeGUIRect(Vector2 RelativePos, Vector2 RelativeScale)
	{
		float width = ScreenWidth * RelativeScale.x;
		float height = ScreenHeight * RelativeScale.y;
		float left = (ScreenWidth * RelativePos.x) - (width / 2);
		float top = (ScreenHeight * RelativePos.y) - (height / 2);
		return new Rect(left, top, width, height);
	}
	
	void SpawnExplosion(Vector3 Pos)
	{
		GameObject NewExplosion = Instantiate(SphereExplosion) as GameObject;
		
		// I removed the particles from the SphereExplosion prefab because it was causing the game to run slowly on my (admittedly old) phone.
		// Re-add this line if you add particles to the SphereExplosion prefab.
		//NewExplosion.GetComponent<ParticleSystem>().renderer.sortingLayerName = "Foreground";
		
		NewExplosion.transform.position = Pos;
		Destroy(NewExplosion, 0.4f);
	}
}
