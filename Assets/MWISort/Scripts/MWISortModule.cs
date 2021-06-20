using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using KModkit;

public class MWISortModule : MonoBehaviour {
	public const int LAYERS_COUNT = 5;
	public const float BUTTONS_X_OFFSET = .015f;
	public const float BUTTONS_Y_OFFSET = .015f;
	public const float BUTTONS_Z_OFFSET = .02f;
	public const float SECONDS_FOR_INPUT = 10f;

	private static int moduleIdCounter = 1;

	public KMSelectable Selectable;
	public KMAudio Audio;
	public KMBombModule BombModule;
	public KMBombInfo BombInfo;
	public ResetButtonComponent ResetButton;
	public ButtonComponent ButtonPrefab;

	private int moduleId;
	private int startingTimeInMinutes;
	private float lastInputTime = 0f;
	private Vector2Int next;
	private ButtonComponent[][] buttons;

	private void Start() {
		moduleId = moduleIdCounter++;
		buttons = new ButtonComponent[LAYERS_COUNT][];
		for (int i = 0; i < LAYERS_COUNT; i++) {
			buttons[i] = new ButtonComponent[10];
			for (int j = 0; j < 10; j++) {
				ButtonComponent button = Instantiate(ButtonPrefab);
				buttons[i][j] = button;
				button.transform.parent = transform;
				button.transform.localPosition = new Vector3((j - 4.5f) * BUTTONS_X_OFFSET, BUTTONS_Y_OFFSET, (1 - i) * BUTTONS_Z_OFFSET);
				button.transform.localRotation = Quaternion.identity;
				button.transform.localScale = Vector3.one;
				button.Selectable.Parent = Selectable;
				button.position = new Vector2Int(i, j);
				button.Selectable.OnInteract += () => { PressButton(button); return false; };
			}
		}
		ResetButton.Selectable.OnInteract += () => { Reset(); return false; };
		Selectable.Children = buttons.SelectMany(bs => bs.Select(b => b.Selectable)).Concat(new[] { ResetButton.Selectable }).ToArray();
		Selectable.UpdateChildren();
		BombModule.OnActivate += Activate;
	}

	private void Update() {
		if (next.x >= LAYERS_COUNT) return;
		if (lastInputTime == 0f) return;
		if (Time.time > lastInputTime + SECONDS_FOR_INPUT) {
			Debug.LogFormat("[MWISort #{0}] More than 10 seconds have passed since the last entry. Strike!", moduleId);
			Strike();
		}
	}

	private void Activate() {
		startingTimeInMinutes = Mathf.FloorToInt(BombInfo.GetTime() / 60f);
		HashSet<int> digits = new HashSet<int>(Enumerable.Range(0, 10));
		foreach (ButtonComponent button in buttons[0]) {
			button.digit = digits.PickRandom();
			digits.Remove(button.digit.Value);
		}
		// next = new Vector2Int(1, 0);
		next = new Vector2Int(1, Random.Range(0, 10));
		buttons[next.x][next.y].next = true;
		Debug.LogFormat("[MWISort #{0}] Display #0 digits: {1}", moduleId, buttons[0].Select(b => b.digit.Value.ToString()).Join(""));
	}

	private void PressButton(ButtonComponent button) {
		if (next.x >= LAYERS_COUNT) return;
		if (button.digit == null) return;
		int digit = button.digit.Value;
		if (!IsPossibleDigit(digit)) {
			Strike();
			return;
		}
		Audio.PlaySoundAtTransform("MWISortDigitPressed", button.transform);
		lastInputTime = Time.time;
		buttons[next.x][next.y].digit = digit;
		buttons[next.x][next.y].next = false;
		HashSet<Vector2Int> unsetPositions = new HashSet<Vector2Int>(buttons[next.x].Where(b => b.digit == null).Select(b => b.position));
		if (unsetPositions.Count == 0) {
			Debug.LogFormat("[MWISort #{0}] Display #{1} answer: {2}", moduleId, next.x, buttons[next.x].Select(b => b.digit.Value.ToString()).Join(""));
			next = new Vector2Int(next.x + 1, Random.Range(0, 10));
		} else next = unsetPositions.PickRandom();
		// next = new Vector2Int(next.x, next.y + 1);
		// if (next.y >= 10) next = new Vector2Int(next.x + 1, 0);
		if (next.x >= LAYERS_COUNT) {
			if (Enumerable.Range(0, 10).Any(i => buttons[LAYERS_COUNT - 1][i].digit != i)) {
				Debug.LogFormat("[MWISort #{0}] Display #{1} not sorted. Strike!", moduleId, LAYERS_COUNT - 1);
				Strike();
			} else {
				Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.CorrectChime, transform);
				Debug.LogFormat("[MWISort #{0}] Display #{1} sorted. Module solved!", moduleId, LAYERS_COUNT - 1);
				BombModule.HandlePass();
				ResetButton.solved = true;
			}
		} else buttons[next.x][next.y].next = true;
	}

	private void Reset() {
		if (!ResetButton.active || ResetButton.solved) return;
		Audio.PlaySoundAtTransform("MWISortResetPressed", ResetButton.transform);
		Debug.LogFormat("[MWISort #{0}] Reset pressed", moduleId);
		for (int i = 1; i < LAYERS_COUNT; i++) {
			for (int j = 0; j < 10; j++) {
				buttons[i][j].digit = null;
				buttons[i][j].next = false;
			}
		}
		next = new Vector2Int(1, Random.Range(0, 10));
		// next = new Vector2Int(1, 0);
		buttons[next.x][next.y].next = true;
		ResetButton.active = false;
	}

	private void Strike() {
		BombModule.HandleStrike();
		lastInputTime = 0f;
		if (next.x > 1 || next.y > 0) ResetButton.active = true;
	}

	private bool IsPossibleDigit(int digit) {
		for (int j = 0; j < 10; j++) {
			if (buttons[next.x][j].digit == digit) {
				Debug.LogFormat("[MWISort #{0}] Duplicate digits are pressed. Display #{1}: {2}. Strike!", moduleId, next.x, buttons[next.x].Select(b => (
					b.position.y == next.y ? digit.ToString() : (b.digit == null ? "_" : b.digit.Value.ToString())
				)).Join(""));
				return false;
			}
		}
		if (next.x == 1) {
			if (next.y == startingTimeInMinutes % 10) {
				if (digit == buttons[0][next.y].digit) return true;
				Debug.LogFormat("[MWISort #{0}] Display #{1}. Digit #{2} not equal to digit above it. Strike!", moduleId, next.x, next.y);
				return false;
			}
			if (digit != buttons[0][next.y].digit) return true;
			Debug.LogFormat("[MWISort #{0}] Display #{1}. Digit #{2} equal to digit above it. Strike!", moduleId, next.x, next.y);
			return false;
		}
		if (next.x == 2) {
			if (digit == BombInfo.GetModuleIDs().Count % 10) {
				if (buttons[1][next.y].digit == digit) return true;
				Debug.LogFormat("[MWISort #{0}] Display #{1}. Digit \"{2}\" not equal to digit above it. Strike!", moduleId, next.x, digit);
				return false;
			}
			if (digit != buttons[1][next.y].digit) return true;
			Debug.LogFormat("[MWISort #{0}] Display #{1}. Digit \"{2}\" equal to digit above it. Strike!", moduleId, next.x, digit);
			return false;
		}
		if (next.x == 3) {
			if (next.y == BombInfo.GetBatteryCount() % 10) {
				if (digit == buttons[2][next.y].digit) return true;
				Debug.LogFormat("[MWISort #{0}] Display #{1}. Digit #{2} not equal to digit above it. Strike!", moduleId, next.x, next.y);
				return false;
			}
			if (digit != buttons[2][next.y].digit) return true;
			Debug.LogFormat("[MWISort #{0}] Display #{1}. Digit #{2} equal to digit above it. Strike!", moduleId, next.x, next.y);
			return false;
		}
		if (next.y == BombInfo.GetBatteryCount() % 10) return true;
		if (digit != buttons[3][next.y].digit) return true;
		Debug.LogFormat("[MWISort #{0}] Display #{1}. Digit #{2} equal to digit above it. Strike!", moduleId, next.x, next.y);
		return false;
	}
}
