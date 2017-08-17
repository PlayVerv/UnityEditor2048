using UnityEditor;

public class Menu2048 : EditorWindow {

	[MenuItem ("Game/2048/Play 2048 !", priority = 0)]
	static void Open2048Game() {
		EditorWindow window = EditorWindow.GetWindow (typeof(Editor2048Board), false, "Play 2048!");
		window.autoRepaintOnSceneChange = true;
		window.wantsMouseMove = true;
	}

	[MenuItem ("Game/2048/Settings", priority = 1)]
	static void Open2048Setting() {
        ScriptableWizard.DisplayWizard<Setting2048>("2048 Setting.", "Save");
	}
}