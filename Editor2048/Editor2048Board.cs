using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;

public class Editor2048Board : EditorWindow
{
    public class Cell
    {
        public int value;
        public float currentX;
        public float currentY;
        public float nextX;
        public float nextY;
        public float scale = 1.0f;
    }

    enum Dimensions
    {
        Row,
        Column,
    }

    private float scaleSpeed = 25.0f; //TODO:Move this to setting
    private float moveSpeed = 50.0f; //TODO:Move this to setting

    private int goal;
	private int size;
	private int currentScores;
	private Cell[][] board;

    private bool isGameOver = false;
	private bool isWon = false;

    private System.Random random;
    private bool isRandomSeed = true;
    private int randomSeed;
    private Stack<Cell[][]> boardRecords;
    private List<KeyCode> keyRecords;
    private Vector2 recordScrollView;

    private Queue<System.Action> animaEndActionQueue = new Queue<System.Action>();
    private List<Cell> animatedCells = new List<Cell>();
    private List<Cell> mergedCells = new List<Cell>();

    void OnEnable()
	{
        ResetBoard();
    }

	void OnGUI()
	{
        //Game informations
        EditorGUI.LabelField(new Rect(10, 10, 60, 25), "SCORE");
        EditorGUI.LabelField(new Rect(60, 10, 80, 20), currentScores.ToString(), GUI.skin.button);
        EditorGUI.LabelField(new Rect(10, 30, 60, 25), "BEST");
        EditorGUI.LabelField(new Rect(60, 30, 80, 20), Setting2048.BestScores.ToString(), GUI.skin.button);

        EditorGUI.LabelField(new Rect(160, 10, 60, 25), "GOAL");
		EditorGUI.LabelField(new Rect(200, 10, 80, 20), goal.ToString(), GUI.skin.box);
        EditorGUI.LabelField(new Rect(160, 30, 100, 25), "RANDOM SEED");
        isRandomSeed = EditorGUI.Toggle(new Rect(260, 30, 15, 25), isRandomSeed);
        if (isRandomSeed)
        {
            if(GUI.Button(new Rect(280, 30, 100, 20), randomSeed.ToString()))
            {
                System.Random r = new System.Random();
                randomSeed = r.Next();
            }
        }
        else
            randomSeed = EditorGUI.IntField(new Rect(280, 30, 100, 20), randomSeed);

        if (GUI.Button(new Rect(300, 10, 80, 20), "New Game"))
        {
            ResetBoard();
            addNewCell();
        }
        if (GUI.Button(new Rect(400, 10, 80, 20), "Back"))
        {
            Move(KeyCode.Backspace);
        }

        //Save default gui color
        Color defaultGuiColor = GUI.color;

        //Calculate size
        float topSection = 60;
        float paddingTop = 10;
        float paddingLeft = 20;
        float maxBoardSize = (position.width > position.height - topSection) ? position.height - (topSection + paddingTop) : position.width - paddingLeft;
        float gridSize = maxBoardSize / (float)size;

        //According to editor window's size to draw board.
        drawBoard(size, gridSize);
        drawKeyRecords(size, gridSize, keyRecords);

        //Actions
        HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));
		Event e = Event.current;
        if ((e.type == EventType.keyUp) &&
            (e.keyCode == KeyCode.UpArrow || e.keyCode == KeyCode.DownArrow || e.keyCode == KeyCode.LeftArrow || e.keyCode == KeyCode.RightArrow))
        {
            Move(e.keyCode);
        }

        //Merged cells behind moving cells.
        foreach (Cell c in mergedCells)
            drawCell(c, gridSize, maxBoardSize, Setting2048.GetCellStyle());

        //Create a sorted list, make the large number cells are above small one.
        List<Cell> list = (from row in board
                             from cell in row
                             where cell != null
                             orderby cell.value ascending
                             select cell).ToList();
        foreach(Cell c in list)
        {
            drawCell(c, gridSize, maxBoardSize, Setting2048.GetCellStyle());
        }

        //Return gui color
        GUI.color = defaultGuiColor;

        //Gameover mask
        drawGameOverMask(size, gridSize);
    }

    void OnInspectorUpdate()
    {
        if (animatedCells.Count != 0 || mergedCells.Count != 0)
        {
            List<Cell> removeList = new List<Cell>();
            foreach (Cell c in animatedCells)
            {
                if (isCellAnimFinished(c)) removeList.Add(c);
            }
            if (removeList.Count != 0) animatedCells = animatedCells.Except(removeList).ToList();
            Repaint();
        }
        else
        {
            if (animaEndActionQueue.Count != 0) animaEndActionQueue.Dequeue().Invoke();
        }
    }

    void ResetBoard()
    {
        if(isRandomSeed)
        {
            System.Random r = new System.Random();
            randomSeed = r.Next();
        }
        random = new System.Random(randomSeed);

        currentScores = 0;
        animatedCells.Clear();
        mergedCells.Clear();
        animaEndActionQueue.Clear();
        isGameOver = false;
		isWon = false;

        size = Setting2048.Size;
        goal = Setting2048.GoalScores;

        board = new Cell[size][];
        for (int y = 0; y < size; y++)
            board[y] = new Cell[size];
        keyRecords = new List<KeyCode>();
        boardRecords = new Stack<Cell[][]>();
    }

    void drawCell(Cell c, float gridSize, float boardSize, GUIStyle style)
    {
        float scaledSize = gridSize * c.scale;
        float diffSize = scaledSize - gridSize;
        float x = 12 + c.currentX * gridSize + ((gridSize - 5) / 2.0f) - ((scaledSize - 5) / 2.0f);
        float y = 62 + boardSize - (c.currentY + 1) * gridSize + ((gridSize - 5) / 2.0f) - ((scaledSize - 5) / 2.0f);
        Rect rect = new Rect(x, y, scaledSize - 5, scaledSize - 5);
        GUI.color = Setting2048.GetColor(c.value);
        GUI.Button(rect, c.value.ToString(), style);
    }

    void drawBoard(int size, float gridSize)
    {
        for (int i = 0; i <= size; i++)
        {
            Vector3 drawStartPoint = new Vector3(10, 60, 0);
            Vector3 verticalTop = new Vector3(i * gridSize, 0, 0);
            Vector3 verticalBottom = new Vector3(0, size * gridSize, 0);
            Vector3 horizonLeft = new Vector3(0, i * gridSize, 0);
            Vector3 horizonRight = new Vector3(size * gridSize, 0, 0);
            Handles.color = Setting2048.BoardLineColor;
            Handles.DrawLine(drawStartPoint + verticalTop, drawStartPoint + verticalTop + verticalBottom);
            Handles.DrawLine(drawStartPoint + horizonLeft, drawStartPoint + horizonLeft + horizonRight);
        }
    }

    void drawKeyRecords(int size, float gridSize, List<KeyCode> record)
    {
        int paddingTop = 60;
        int paddingLeft = 10;
        float boardWidth = size * gridSize;

        float startX = boardWidth + paddingLeft + 5;
        float startY = paddingTop + 5;
        GUILayout.BeginArea(new Rect(startX, startY, 50, position.height - 75), GUI.skin.textArea);
        recordScrollView = GUILayout.BeginScrollView(recordScrollView, false, false, GUIStyle.none, new GUIStyle(GUI.skin.verticalScrollbar));
        GUILayout.BeginVertical();
        for(int i = 0; i < record.Count; ++i)
        {
            string label = (record[i] == KeyCode.UpArrow) ? "^"
                : (record[i] == KeyCode.DownArrow) ? "v"
                : (record[i] == KeyCode.LeftArrow) ? "<"
                : (record[i] == KeyCode.RightArrow) ? ">"
                : "B";
            GUILayout.Button(label);
        }
        GUILayout.EndVertical();
        GUILayout.EndScrollView();
        GUILayout.EndArea();
    }

    void drawGameOverMask(int size, float gridSize)
    {
        if (!isWon && !isGameOver)
            return;

        int buttonWidth = 140;
        int buttonHeight = 40;
        EditorGUI.DrawRect(new Rect(10, 60, size * gridSize, size * gridSize), new Color(0.5f, 0.5f, 0.5f, 0.5f));
        Rect rect = new Rect(12 + (size * gridSize - buttonWidth) / 2, (60 - (buttonHeight / 2)) + (size / 2 * gridSize), buttonWidth, buttonHeight);

        if (isWon)
        {
            GUI.color = Color.cyan;
            GUI.Button(rect, "CONGRATULATIONS!");
        }
        else if (isGameOver)
        {
            GUI.color = Color.red;
            GUI.Button(rect, "GAME OVER!");
        }
    }

    //**************************************************************************************//
    void addNewCell()
	{
        int count = Setting2048.AddCellsPerMove;
        for(int i = 0; i < count; ++i)
        {
            int[] choices = new int[10] { 2, 2, 2, 2, 2, 2, 2, 2, 2, 4 };
            int randomValue = choices[random.Next(0, 10)];

            List<Cell> cells = getEmptyCells();
            if (cells.Count <= 0)
                return;

            Cell randomCell = cells[random.Next(0, cells.Count)];
            randomCell.value = randomValue;
            randomCell.scale = 0.1f;
            board[(int)randomCell.currentY][(int)randomCell.currentX] = randomCell;

            addAnimeCell(randomCell);
        }
	}

    void addAnimeCell(Cell c)
    {
        if (animatedCells.Contains(c))
            return;
        animatedCells.Add(c);
    }


    //**************************************************************************************//
    List<Cell> getEmptyCells()
	{
        List<Cell> list = new List<Cell>();
        for (int y = 0; y < size; ++y)
        {
            for (int x = 0; x < size; ++x)
            {
                if (board[y][x] != null)
                    continue;

                list.Add(new Cell() { currentX = x, currentY = y, nextX = x, nextY = y });
            }
        }
        return list;
    }

    int getCellValue(int x, int y)
    {
        if (x < 0 || y < 0 || x >= size || y >= size)
            return -1;

        return getCellValue(board[y][x]);
    }

    int getCellValue(Cell cell)
    {
        return (cell == null) ? 0 : cell.value;
    }

    Cell[] getLineCells(int index, Dimensions dim)
    {
        Cell[] cells = new Cell[size];
        for (int i = 0; i < size; i++) {
            cells[i] = (dim == Dimensions.Row) ? board[index][i] : board[i][index];
        }
        return cells;
    }

    int getSpace(Cell[] line, int index, int direction)
    {
        int result = 0;
        if (index + direction < 0 || index + direction >= size)
            return 0;

        if (line[index + direction] == null)
        {
            result++;
            result += getSpace(line, index + direction, direction);
        }
        return result;
    }

    void setCellDest(Cell cell, Dimensions dim, int pos)
    {
        if (dim == Dimensions.Row)
            cell.nextX = pos;
        else
            cell.nextY = pos;
    }


    //**************************************************************************************//
    bool isCellAnimFinished(Cell c)
    {
        c.scale = Mathf.MoveTowards(c.scale, 1.0f, scaleSpeed * Time.fixedDeltaTime);
        c.currentX = Mathf.MoveTowards(c.currentX, c.nextX, moveSpeed * Time.fixedDeltaTime);
        c.currentY = Mathf.MoveTowards(c.currentY, c.nextY, moveSpeed * Time.fixedDeltaTime);
        if (Mathf.Abs(c.currentY - c.nextY) < 0.1f) c.currentY = c.nextY;
        if (Mathf.Abs(c.currentX - c.nextX) < 0.1f) c.currentX = c.nextX;
        if (c.scale == 1 && c.currentX == c.nextX && c.currentY == c.nextY)
        {
            Cell mergeCell = mergedCells.Find(m => m.currentX == c.currentX && m.currentY == c.currentY);
            if(mergeCell != null)
            {
                c.scale = 1.2f;
                c.value += mergeCell.value;

                //Add scores
                currentScores += mergeCell.value;
                if (c.value >= goal) isWon = true;
                Setting2048.BestScores = currentScores;

                mergedCells.Remove(mergeCell);
                return false;
            }
            return true;
        }
        return false;
    }

    bool isCellCanMove()
	{
        for (int y = 0; y < size; ++y) {
            for (int x = 0; x < size; ++x) {
                int cellValue = getCellValue(x, y);

                if (cellValue == 0)
                    return true;

                if (cellValue == getCellValue(x + 1, y) ||
                    cellValue == getCellValue(x - 1, y) ||
                    cellValue == getCellValue(x, y + 1) ||
                    cellValue == getCellValue(x, y - 1))
                    return true;
            }
		}
		return false;
	}

    bool isCellCanMerge(Cell[] line, int index, int direction)
    {
        if (index + direction < 0 || index + direction >= size)
            return false;

        if (line[index + direction] == null)
            return false;

        if (getCellValue(line[index + direction]) != getCellValue(line[index]))
            return false;

        return true;
    }


    //**************************************************************************************//
    void Move(KeyCode key)
    {
        if (isWon ||
            isGameOver ||
            animatedCells.Count != 0 ||
            mergedCells.Count != 0 ||
            animaEndActionQueue.Count != 0)
            return;

        if(key == KeyCode.Backspace)
        {
            if (boardRecords.Count == 0)
                return;

            board = boardRecords.Pop();
            keyRecords.Add(KeyCode.Backspace);
            return;
        }

        //Record move event
        boardRecords.Push(board.Copy());
        keyRecords.Add(key);

        Dimensions dim = (key == KeyCode.LeftArrow || key == KeyCode.RightArrow) ? Dimensions.Row : Dimensions.Column;
        int direction = (key == KeyCode.LeftArrow || key == KeyCode.DownArrow) ? -1 : 1;
        moveCells(direction, dim, 0);
    }

    void moveCells(int direction, Dimensions dim, int stopCount)
    {
        bool anyCellMoved = false;
        for (int i = 0; i < size; ++i)
        {
            Cell[] cells = getLineCells(i, dim);
            if(moveOrMergeCellLine(cells, direction, dim, false))
                anyCellMoved = true;
        }

        if(anyCellMoved)
        {
            animaEndActionQueue.Enqueue(() => moveCells(direction, dim, 0));
        }
        else
        {
            bool anyCellMerged = false;
            for (int i = 0; i < size; ++i)
            {
                Cell[] cells = getLineCells(i, dim);
                if (moveOrMergeCellLine(cells, direction, dim, true))
                    anyCellMerged = true;
            }

            if (anyCellMerged)
            {
                animaEndActionQueue.Enqueue(() => moveCells(direction, dim, 0));
            }
            else
            {
                if(stopCount < 2)
                    animaEndActionQueue.Enqueue(() => moveCells(direction, dim, stopCount + 1));
                else
                    animaEndActionQueue.Enqueue(() => {
                        addNewCell();
                        //Check if player can move or not
                        if (!isCellCanMove()) isGameOver = true;
                    });
            }
        }
    }

    bool moveOrMergeCellLine(Cell[] line, int direction, Dimensions dim, bool isMerge)
    {
        bool anyCellMoved = false;
        for (int i = 0; i < size; ++i)
        {
            int index = (direction < 0) ? i : size - i - 1; // LEFT/DOWN : 0 to N,  RIGHT/UP : N to 0
            if (line[index] == null)
                continue;

            int space = (isMerge) ? 1 : getSpace(line, index, direction);
            if (space == 0)
                continue;

            if (isMerge && !isCellCanMerge(line, index, direction))
                continue;

            Cell movingCell = line[index];
            if (mergedCells.Contains(movingCell))
                continue;

            anyCellMoved = true;
            int targetPos = index + space * direction;
            setCellDest(movingCell, dim, targetPos);

            if(isMerge)
            {
                Cell mergedCell = board[(int)movingCell.nextY][(int)movingCell.nextX];
                mergedCells.Add(mergedCell);
            }

            board[(int)movingCell.nextY][(int)movingCell.nextX] = movingCell;
            board[(int)movingCell.currentY][(int)movingCell.currentX] = null;
            line[targetPos] = movingCell;
            line[index] = null;
            addAnimeCell(movingCell);
        }
        return anyCellMoved;
    }
}

public static class CellExtension
{
    public static Editor2048Board.Cell[][] Copy(this Editor2048Board.Cell[][] src)
    {
        var board = new Editor2048Board.Cell[src.Length][];
        for (int x = 0; x < src.Length; ++x)
        {
            var line = src[x];
            board[x] = new Editor2048Board.Cell[line.Length];

            for (int y = 0; y < line.Length; ++y)
            {
                var cell = line[y];
                if (cell == null)
                    continue;
                board[x][y] = new Editor2048Board.Cell()
                {
                    value = cell.value,
                    currentX = cell.currentX,
                    currentY = cell.currentY,
                    nextX = cell.nextX,
                    nextY = cell.nextY,
                    scale = cell.scale,
                };
            }
        }
        return board;
    }
}