using UnityEngine;

public class ResetButtonComponent : MonoBehaviour {
	private bool _active = false;
	public bool active {
		get { return _active; }
		set {
			_active = value;
			UpdateText();
		}
	}

	public TextMesh Text;
	public KMSelectable Selectable;

	private void Start() {
		UpdateText();
	}

	public void UpdateText() {
		Text.text = active ? "RESET" : "";
	}
}
