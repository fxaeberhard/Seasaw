using UnityEngine;
using System.Collections;
using UnityEngine.UI;

//7855
public class Balance: MonoBehaviour
{
	
	enum State
	{
		Waiting,
		Countdown,
		Over,
		Running
	}
	
	private State currentState = State.Waiting;
	public int COUNTDOWNDURATION = 0;
	private bool isPaused = false;
	private int countdown = 0;
	float speed = 0.5f;
	public float INITIALSPEED = 0.1f;
	private float position = 0;
	string msgLeft = "";
	string msgRight = "";
	Animator animator;
	private GameObject tweenObject;
	bool goingUp = true;

	/**
	 */
	public int MAXANGLE = 27;
	private float angularSpeed = 0;
	public Slider slider ;
	public float characterHeight = 0.5f;
	public float seesawRadius = 3;
	public float characterWeight = 30 * 9.81f;
	bool  canMoveForward = true;
	bool canMoveBackward = true;
	float Treshold = 0.1f;
	float  xAcc = 0.0f;
	float accelerometerUpdateInterval = 1.0f / 60.0f;
	float lowPassKernelWidthInSeconds = 1.0f;
	float shakeDetectionThreshold = 2.0f;
	float  lowPassFilterFactor ;
	Vector3  lowPassValue = Vector3.zero;
	Vector3  acceleration ;
	Vector3  deltaAcceleration ;
	Transform myTransform;
	// Use this for initialization
	void Start ()
	{
		tweenObject = new GameObject ();
		print ("Start() " + countdown);
		//transform.eulerAngles = new Vector3 (0, 0, 5);
		if (SystemInfo.supportsGyroscope) {
			Input.gyro.enabled = true;
		}

		animator = this.gameObject.GetComponent<Animator> ();
		animator.speed = 0;
		//	animator.StopPlayback ();
		//	animator.StartPlayback ();
		//animator
//			animator.playbackTime = 30;
		//animator.StartPlayback ();
		//	animator.speed = -1;


		canMoveForward = true;
		canMoveBackward = true;   		
		lowPassFilterFactor = accelerometerUpdateInterval / lowPassKernelWidthInSeconds;
		shakeDetectionThreshold *= shakeDetectionThreshold;
		lowPassValue = Input.acceleration;
			
		LowPassFilterFactor = AccelerometerUpdateInterval / LowPassKernelWidthInSeconds;
	}
	
	float LowPassKernelWidthInSeconds = 1.0f;
	// The greater the value of LowPassKernelWidthInSeconds, the slower the filtered value will converge towards current input sample (and vice versa). You should be able to use LowPassFilter() function instead of avgSamples().
	
	float AccelerometerUpdateInterval = 1.0f / 60.0f;
	float LowPassFilterFactor ;
	
	//	Vector3  lowPassValue = Vector3.zero; // should be initialized with 1st sample
	
	Vector3 IphoneAcc ;
	Vector3 IphoneDeltaAcc  ;
	
	Vector3 LowPassFilter (Vector3 newSample)
	{
		lowPassValue = Vector3.Lerp (lowPassValue, newSample, LowPassFilterFactor);
		return lowPassValue;
	}
	
	void SetState (State state)
	{
		print ("SetState(" + state + ")");
		switch (state) {
		case State.Countdown:
			animator.PlayInFixedTime ("Up", -1, 0);
//			animator.GetCurrentAnimatorStateInfo(0).normalizedTime = 0;
			animator.speed = 0;
			StartCoroutine (getReady ());
			break;
		case State.Over:
			speed = 0;
			//animator.speed = 0;
			break;
		case State.Running:
			msgLeft = "";
			msgRight = "";
			animator.speed = 1;
			animator.SetFloat ("speedMult", INITIALSPEED);
			break;
		case State.Waiting:
			break;
		}
		currentState = state;
	}

	// Update is called once per frame
	void Update ()
	{

		AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo (0);

		//print ("Update(): " + Input.touchCount+"*"+state.normalizedTime+"*"+state.fullPathHash);
		//if (state.normalizedTime > 0.6) {
		//animator.SetTrigger("isReverse");
		//}

		switch (currentState) {
		case State.Waiting:
			if (Input.touchCount > 0) {// && Input.GetTouch(0).phase == TouchPhase.Began
				
				SetState (State.Countdown);
				return;

				float pos1 = Input.GetTouch (0).position.x;
				float pos2 = Input.GetTouch (1).position.x;

				if ((pos1 < Screen.width / 3 && pos2 > Screen.width * 2 / 3) 
					|| (pos2 < Screen.width / 3 && pos1 > Screen.width * 2 / 3)) {
					print ("touch");
					SetState (State.Countdown);
				}
			}
			return;
		}
		
		
		if (currentState != State.Running || isPaused)
			return;

		position = state.normalizedTime;
		//position += speed * Time.deltaTime;
		//transform.eulerAngles = new Vector3 (0, 0, 40 * position);
		/*if (position >= 1 || position <= -1) {
			speed = -speed;
		}*/

		string tmsg = "";
		
		float abspost = Mathf.Abs (position);

		IphoneAcc = Input.acceleration;
		IphoneDeltaAcc = IphoneAcc - LowPassFilter (IphoneAcc);

		float cspeedmult = animator.GetFloat ("speedMult");

		if (cspeedmult < 0) {
			abspost = 1 - position;
		}
		
		//print (abspost +"*" + cspeedmult+"*"+ position);
		
		print ("shake " + IphoneDeltaAcc.x + "*" + Mathf.Sign (IphoneDeltaAcc.x) + "*" + ", speed:" + abspost + "*" + cspeedmult + "*" + position);


		if (abspost > 1.15) {
			tmsg = "Missed";
			SetState (State.Over);
			if (cspeedmult > 0) {
				animator.SetTrigger ("lostLeft");
			} else {
				animator.SetFloat ("speedMult", Mathf.Abs (cspeedmult));
				animator.SetTrigger ("lostRight");
			}

		} else 		if (Mathf.Abs (IphoneDeltaAcc.x) >= .05) {

			//print ("shake " + IphoneDeltaAcc.x + "*" + Mathf.Sign (IphoneDeltaAcc.x) + "*"  + ", speed:" + abspost+"*"+cspeedmult);



			if (Mathf.Sign (IphoneDeltaAcc.x) != -Mathf.Sign (cspeedmult)
			    /*&& Mathf.Approximately (Mathf.Sign (speed), posSign)*/) { // ensure the guy is pushing in the right direction)
				print ("right sign" + abspost);
				if (abspost > 0.6 && abspost < 0.75) { // Fumble
					tmsg = "too early";
					speed = Mathf.Sign (speed) * -1 * INITIALSPEED;
					animator.SetFloat ("speedMult", Mathf.Sign (cspeedmult) * -1 * INITIALSPEED);
					//animator.SetTrigger("isReverse");
					//goingUp = !goingUp;
					print ("REVERSE early");
					
					AudioSource audio = GetComponent<AudioSource>();
					audio.Play();
				} else  if (abspost > 0.85 && abspost < 0.95) {// Perfect
					tmsg = "perfect";
					print ("REVERSE perfect");
					speed = -1.1f * speed;
					animator.SetFloat ("speedMult", -1.04f * cspeedmult);

					AudioSource audio = GetComponent<AudioSource>();
					audio.Play();
				} else if (abspost > 0.75) { // Good
					tmsg = "good";
					print ("REVERSE good");
					speed = -1.05f * speed;
					animator.SetFloat ("speedMult", -1.01f * cspeedmult);
					//if (cspeedmult>0){
					//AudioSource audio = GetComponent<AudioSource>();
					//audio.Play();
				} else {
					print ("but ignored");
				}
			}
		}
		if (tmsg != "") {
			if (cspeedmult < 0) {
				msgLeft = tmsg;
				//msgRight = "";
			} else {
				msgRight = tmsg;
			}
		}
		return;
		
		Gyroscope m_gyroscope = Input.gyro;
		m_gyroscope.enabled = true;
		print (m_gyroscope.attitude + "*" + m_gyroscope.enabled + "*" + SystemInfo.supportsGyroscope + "*" + Input.acceleration + "*" + Input.gyro.userAcceleration);
		//		print (transform.localRotation);
		//		print (transform.localEulerAngles);
		acceleration = Input.acceleration;
		lowPassValue = Vector3.Lerp (lowPassValue, acceleration, lowPassFilterFactor);
		deltaAcceleration = (acceleration - lowPassValue);
		
		if (deltaAcceleration.sqrMagnitude >= shakeDetectionThreshold) {
			print ("in");
			//print(acceleration);
			float zCord;
			//var currentAccX: float=deltaAcceleration.x;
			float currentAccX = acceleration.x;
			
			if (canMoveForward && canMoveBackward) {
				print ("one");
				zCord = Mathf.Lerp (0, currentAccX, Time.time);
				transform.Translate (0, 0, zCord * Time.deltaTime);
			}     
			if (!canMoveForward && acceleration.x < 0) {
				print ("t");
				zCord = Mathf.Lerp (0, currentAccX, Time.time);
				transform.Translate (0, 0, zCord * Time.deltaTime);
			}    
			if (!canMoveBackward && acceleration.x > 0) {
				print ("three");
				zCord = Mathf.Lerp (0, currentAccX, Time.time);
				transform.Translate (0, 0, zCord * Time.deltaTime);
			}    
		}		
	}
	// GUI
	void OnGUI ()
	{
		
		float xPos = tweenObject.transform.localPosition.x;
		float yPos = tweenObject.transform.localPosition.y;
		
		//if (GUI.Button (new Rect (xPos, yPos, 100, 40), new GUIContent ("click me"))) {
			//	iTween.MoveTo(tweenObject, 1, null, Random.Range(0, Screen.width-100), Random.Range(0, Screen.height-40), null);
		//}
		//print ("OnGUI():" + currentState+", "+countdown);
		var 		largeFont = new GUIStyle();

		largeFont.fontSize = 32;
		switch (currentState) {
		case State.Waiting:
			// Make a group on the centre of the screen    
			GUI.BeginGroup (new Rect (Screen.width / 2 - 100, 50, 200, 175));
			
			// Make a box to show the group on the screen        
			GUI.color = Color.white;    
			GUI.Label (new Rect (0, 0, 200, 175), "Touch to start", largeFont);
			
			GUI.EndGroup ();
			return;
		}
		if (currentState == State.Countdown) {    
			
			// Get Ready countdown   
			
			// Make a group on the centre of the screen    
			GUI.BeginGroup (new Rect (Screen.width / 2 - 100, 50, 200, 175));
			
			// Make a box to show the group on the screen        
			GUI.color = Color.white;    
				GUI.Label (new Rect (0, 0, 200, 175), "" + countdown, largeFont);
			
			// display countdown    
			//			GUI.color = Color.white;    
			//			GUI.Box (Rect (10, 25, 180, 140), countdown);
			
			// End GUI group class    
			GUI.EndGroup ();
		} else {
			//	GUI.BeginGroup (new Rect (Screen.width / 2 - 100, 50, 200, 175));
			//	GUI.BeginGroup (new Rect (0, 50, 200, 175));
			
			// Make a box to show the group on the screen        
			//GUI.color = Color.red;    
			GUI.skin.label.alignment = TextAnchor.UpperLeft;
			GUI.Label (new Rect (20, Screen.height / 2, 200, 175), "" + msgLeft);
			GUI.skin.label.alignment = TextAnchor.UpperRight;
			GUI.Label (new Rect (Screen.width - 220, Screen.height / 2, 200, 175), "" + msgRight);
			
			GUI.Label (new Rect (Screen.width / 2, 10, 40, 20), "" + position);
			
			if (currentState == State.Over) {
				if (GUI.Button (new Rect (Screen.width / 2 - 30, Screen.height / 2 - 15, 60, 30), "Restart")) {
					SetState (State.Countdown);
				}
			} 
			// display countdown    
			//			GUI.color = Color.white;    
			//			GUI.Box (Rect (10, 25, 180, 140), countdown);
			
			// End GUI group class    
			//	GUI.EndGroup ();
		}
		
		if (isPaused) {
			var groupWidth = 120;
			var groupHeight = 150;
			
			var screenWidth = Screen.width;
			var screenHeight = Screen.height;
			
			var groupX = (screenWidth - groupWidth) / 2;
			var groupY = (screenHeight - groupHeight) / 2;
			
			GUI.BeginGroup (new Rect (groupX, groupY, groupWidth, groupHeight));
			GUI.Box (new Rect (0, 0, groupWidth, groupHeight), "Paused");
			
			if (GUI.Button (new Rect (10, 30, 100, 30), "Resume")) {
				resume ();
			}
			if (GUI.Button (new Rect (10, 70, 100, 30), "Restart")) {
				Application.LoadLevel ("SEESAW");
			}
			if (GUI.Button (new Rect (10, 110, 100, 30), "Quit")) {
				Application.Quit ();
			}
			
			GUI.EndGroup ();
		}
	}

	IEnumerator getReady ()
	{ 			
		countdown = COUNTDOWNDURATION;
		print ("getReady(): " + countdown);
		while (countdown >1) {
			yield return new WaitForSeconds (1);  
			countdown--;
			//print ("getReady():"+countdown);
		}
		SetState (State.Running);
	}
	
	public void pause ()
	{
		isPaused = true;
	}
	
	public void resume ()
	{
		isPaused = false;
	}
}
