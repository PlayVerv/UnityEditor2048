using UnityEngine;
using UnityEditor;

public class Setting2048 : ScriptableWizard
{
	public int boardSize;
    public Color boardLineColor;
    public int goalScore;
    public int addCellsPerMove;
    public int fontSize;
    public Color fontColor;
    public Font font;

	void OnEnable()
	{
        boardSize = Size;
        fontSize = FontSize;
        fontColor = FontColor;
        boardLineColor = BoardLineColor;
        goalScore = GoalScores;
        addCellsPerMove = AddCellsPerMove;
        font = Font;
    }

    void OnWizardUpdate() {
        helpString = "Please set the size and goal score!";
    }

    void OnWizardCreate() {
        Size = boardSize;
        FontSize = fontSize;
        FontColor = fontColor;
        BoardLineColor = boardLineColor;
        GoalScores = goalScore;
        AddCellsPerMove = addCellsPerMove;
        Font = font;
    }

    public static int Size
    {
        get
        {
            if (!EditorPrefs.HasKey("2048Size"))
                EditorPrefs.SetInt("2048Size", 4);
            return EditorPrefs.GetInt("2048Size");
        }
        set
        {
            EditorPrefs.SetInt("2048Size", value);
        }
    }

    public static Color BoardLineColor
    {
        get
        {
            if (!EditorPrefs.HasKey("2048BoardLineColor"))
                EditorPrefs.SetString("2048BoardLineColor", ColorToHex(Color.black));
            return HexToColor(EditorPrefs.GetString("2048BoardLineColor"));
        }
        set
        {
            EditorPrefs.SetString("2048BoardLineColor", ColorToHex(value));
        }
    }

    private static Font Font
    {
        get
        {
            if (!EditorPrefs.HasKey("2048Font"))
                return null;
            return AssetDatabase.LoadAssetAtPath<Font>(EditorPrefs.GetString("2048Font"));
        }
        set
        {
            EditorPrefs.SetString("2048Font", AssetDatabase.GetAssetPath(value));
        }
    }

    private static int FontSize
    {
        get
        {
            if (!EditorPrefs.HasKey("2048FontSize"))
                EditorPrefs.SetInt("2048FontSize", 20);
            return EditorPrefs.GetInt("2048FontSize");
        }
        set
        {
            EditorPrefs.SetInt("2048FontSize", value);
        }
    }

    private static Color FontColor
    {
        get
        {
            if (!EditorPrefs.HasKey("2048FontColor"))
                EditorPrefs.SetString("2048FontColor", ColorToHex(Color.black));
            return HexToColor(EditorPrefs.GetString("2048FontColor"));
        }
        set
        {
            EditorPrefs.SetString("2048FontColor", ColorToHex(value));
        }
    }

    public static int GoalScores
    {
        get
        {
            if (!EditorPrefs.HasKey("2048Goal"))
                EditorPrefs.SetInt("2048Goal", 2048);
            return EditorPrefs.GetInt("2048Goal");
        }
        set
        {
            EditorPrefs.SetInt("2048Goal", value);
        }
    }

    public static int BestScores
    {
        get
        {
            if (!EditorPrefs.HasKey("2048BestScores"))
                EditorPrefs.SetInt("2048BestScores", 0);
            return EditorPrefs.GetInt("2048BestScores");
        }
        set
        {
            if(!EditorPrefs.HasKey("2048BestScores") || EditorPrefs.GetInt("2048BestScores") < value)
                EditorPrefs.SetInt("2048BestScores", value);
        }
    }

    public static int AddCellsPerMove
    {
        get
        {
            if (!EditorPrefs.HasKey("2048AddCellsPerMove"))
                EditorPrefs.SetInt("2048AddCellsPerMove", 1);
            return EditorPrefs.GetInt("2048AddCellsPerMove");
        }
        set
        {
            EditorPrefs.SetInt("2048AddCellsPerMove", value);
        }
    }

    //TODO:Let users create there color palette.
    public static Color GetColor(int index)
    {
        switch (index)
        {
            case 2: return new Color32(255, 255, 192, 255);
            case 4: return new Color32(255, 255, 96, 255);
            case 8: return new Color32(255, 192, 96, 255);
            case 16: return new Color32(255, 128, 96, 255);
            case 32: return new Color32(255, 64, 96, 255);
            case 64: return new Color32(255, 200, 128, 255);
            case 128: return new Color32(255, 200, 128, 255);
            case 256: return new Color32(255, 200, 128, 255);
            case 512: return new Color32(255, 200, 128, 255);
            case 1024: return new Color32(255, 200, 128, 255);
            case 2048: return new Color32(255, 200, 128, 255);
            case 4096: return new Color32(255, 200, 128, 255);
            case 8192: return new Color32(255, 200, 128, 255);
            default: return new Color32(33, 33, 33, 255);
        }
    }

    public static GUIStyle GetCellStyle()
    {
        GUIStyle style = new GUIStyle(GUI.skin.button);
        style.fontSize = FontSize;
        style.normal.textColor = FontColor;
        style.fontStyle = FontStyle.Bold;
        style.font = Font;
        return style;
    }

    private static string ColorToHex(Color32 c)
    {
        return c.r.ToString("X2") + c.g.ToString("X2") + c.b.ToString("X2");
    }

    private static Color HexToColor(string hex)
    {
        hex = hex.Replace("0x", "");//in case the string is formatted 0xFFFFFF
        hex = hex.Replace("#", "");//in case the string is formatted #FFFFFF
        byte a = 255;//assume fully visible unless specified in hex
        byte r = byte.Parse(hex.Substring(0, 2), System.Globalization.NumberStyles.HexNumber);
        byte g = byte.Parse(hex.Substring(2, 2), System.Globalization.NumberStyles.HexNumber);
        byte b = byte.Parse(hex.Substring(4, 2), System.Globalization.NumberStyles.HexNumber);
        //Only use alpha if the string has enough characters
        if (hex.Length == 8)
        {
            a = byte.Parse(hex.Substring(6, 2), System.Globalization.NumberStyles.HexNumber);
        }
        return new Color32(r, g, b, a);
    }
}
