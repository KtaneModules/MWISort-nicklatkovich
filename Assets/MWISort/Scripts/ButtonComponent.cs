using UnityEngine;

public class ButtonComponent : MonoBehaviour {
	private int? _digit = null;
	public int? digit {
		get { return _digit; }
		set {
			_digit = value;
			UpdateText();
		}
	}

	private bool _next = false;
	public bool next {
		get { return _next; }
		set {
			_next = value;
			UpdateText();
		}
	}

	public TextMesh Text;
	public KMSelectable Selectable;

	public Vector2Int position;

	private void Start() {
		UpdateText();
	}

	public void UpdateText() {
		Text.text = digit == null ? (next ? "_" : "") : digit.ToString();
	}
}
