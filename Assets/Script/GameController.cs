using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class GameController : MonoBehaviour
{
    public enum AILevel { None = 0, Easy = 1, Hard = 2 }

    [Header("Grid (Buttons used as click targets)")]
    public Button[] cellButtons = new Button[9];

    [Header("Grid Sprites")] 
    public Sprite spriteX;
    public Sprite spriteO;

    [Header("Status UI")] 
    public Text statusText;
    public Button resetButton;
	public Button retractButton;
	public RectTransform winLine; // centered line to rotate/show on win

    [Header("Turn Indicator")] 
    public Image xIcon;
    public Image oIcon;
    public Image xArrow; // shown when it's X's turn
    public Image oArrow; // shown when it's O's turn

    [Header("AI Settings")] 
    public AILevel aiLevel = AILevel.None;
    public Player aiPlaysAs = Player.O; // default: AI plays O, human starts as X

	[Header("AI Settings UI")] 
	public Dropdown aiLevelDropdown;
	public Dropdown aiSideDropdown; // 0: X, 1: O

    private TicTacToe game = new TicTacToe();
    private bool aiTurnPending;
    private readonly List<bool> moveWasByAI = new List<bool>(9);
    private bool gameOverSfxPlayed;

    private void Awake()
    {
        // Wire cell button clicks
        if (cellButtons != null)
        {
            for (int i = 0; i < cellButtons.Length; i++)
            {
                int idx = i;
                if (cellButtons[i] != null)
                {
                    cellButtons[i].onClick.AddListener(() => OnCellClicked(idx));

                    // Hide built-in button label/graphics if present
                    var text = cellButtons[i].GetComponentInChildren<Text>();
                    if (text != null) text.enabled = false;

                    // Remove highlight/pressed visuals and start hidden
                    cellButtons[i].transition = Selectable.Transition.None;
                    var img = cellButtons[i].GetComponent<Image>();
                    if (img != null)
                    {
                        var c = img.color;
                        c.a = 0f;
                        img.color = c;
                    }
                }
            }
        }

		// Wire reset
        if (resetButton != null)
        {
            resetButton.onClick.AddListener(OnResetClicked);
        }

		// Wire retract
		if (retractButton != null)
		{
			retractButton.onClick.AddListener(OnRetractClicked);
		}

		// Wire AI level dropdown
		if (aiLevelDropdown != null)
		{
			aiLevelDropdown.value = (int)aiLevel;
			aiLevelDropdown.onValueChanged.AddListener(OnAILevelDropdownChanged);
		}

		// Wire AI side dropdown
		if (aiSideDropdown != null)
		{
			aiSideDropdown.value = (aiPlaysAs == Player.X) ? 0 : 1;
			aiSideDropdown.onValueChanged.AddListener(OnAISideDropdownChanged);
		}

		UpdateAISideDropdownVisibility();

    }

    private void Start()
    {
        game.Reset();
        moveWasByAI.Clear();
        gameOverSfxPlayed = false;
        if (SoundManager.Instance != null) SoundManager.Instance.StartMusicIfNeeded();
        RenderBoard();
		// If AI starts, queue AI move
		TryQueueAIMove();
    }

    private void OnCellClicked(int index)
    {
        if (game.IsGameOver) return;
        if (IsAITurn()) return; // block human clicks on AI turn

        if (game.MakeMove(index))
        {
            moveWasByAI.Add(false);
            if (SoundManager.Instance != null) SoundManager.Instance.PlayPlace();
            RenderBoard();
            TryQueueAIMove();
        }
    }

    private void OnResetClicked()
    {
        // Cancel any pending AI action
        if (aiTurnPending)
        {
            CancelInvoke(nameof(PerformAIMove));
            aiTurnPending = false;
        }

        game.Reset();
        moveWasByAI.Clear();
        gameOverSfxPlayed = false;
        RenderBoard();
        // If AI is set to play as X, queue its move after reset
        TryQueueAIMove();
    }

	private void OnRetractClicked()
	{
		// Cancel any pending AI move to avoid race
		if (aiTurnPending)
		{
			CancelInvoke(nameof(PerformAIMove));
			aiTurnPending = false;
		}

		int toUndo = 0;
		int historyCount = moveWasByAI.Count;
		if (historyCount > 0)
		{
            bool lastWasAI = moveWasByAI[historyCount - 1];
            // If last was AI, undo 2 (or as many as available); if last was human, undo 1
            toUndo = lastWasAI ? Mathf.Min(2, historyCount) : 1;
		}

		if (toUndo > 0)
		{
            int undone = game.UndoMoves(toUndo);
			if (undone > 0)
			{
                int remove = Mathf.Min(undone, moveWasByAI.Count);
                if (remove > 0)
                {
                    moveWasByAI.RemoveRange(moveWasByAI.Count - remove, remove);
                }
				RenderBoard();
				// If it's AI's turn after undo, queue AI move per current difficulty
				TryQueueAIMove();
			}
		}
	}

    private void RenderBoard()
    {
        for (int i = 0; i < 9; i++)
        {
            var cell = game.GetCell(i);
            if (cellButtons != null && i < cellButtons.Length && cellButtons[i] != null)
            {
                var img = cellButtons[i].GetComponent<Image>();
                if (img != null)
                {
                    if (cell == Player.X) img.sprite = spriteX;
                    else if (cell == Player.O) img.sprite = spriteO;

                    var c = img.color;
                    c.a = (cell == Player.None) ? 0f : 1f;
                    img.color = c;
                }
                bool isEmpty = cell == Player.None;
                bool allowClick = !game.IsGameOver && isEmpty && !IsAITurn();
                cellButtons[i].interactable = allowClick;
            }
        }

        // Status message
        if (statusText != null)
        {
            if (game.IsGameOver)
            {
                if (game.Winner == Player.None)
                {
                    statusText.text = "Draw!";
                }
                else
                {
                    statusText.text = $"{game.Winner} wins!";
                }
            }
            else
            {
                statusText.text = $"{game.CurrentPlayer}'s turn";
            }
        }

        // Turn indicator visuals
        UpdateTurnIndicator();

		// Win line
		UpdateWinLine();

		// Update retract interactable when board empty
		if (retractButton != null)
		{
			retractButton.interactable = game.GetMoveCount() > 0;
		}

        // Try play end-of-game SFX per PvAI rule
        TryPlayGameOverHuman();
    }

    private void UpdateTurnIndicator()
    {
        if (xIcon == null && oIcon == null && xArrow == null && oArrow == null) return;

        bool isGameActive = !game.IsGameOver;

        // Ensure icons use provided sprites
        if (xIcon != null && spriteX != null) xIcon.sprite = spriteX;
        if (oIcon != null && spriteO != null) oIcon.sprite = spriteO;

        // Reset icon colors
        if (xIcon != null) xIcon.color = Color.white;
        if (oIcon != null) oIcon.color = Color.white;

        // Arrow visibility
        if (xArrow != null) xArrow.enabled = isGameActive && game.CurrentPlayer == Player.X;
        if (oArrow != null) oArrow.enabled = isGameActive && game.CurrentPlayer == Player.O;

        if (!isGameActive)
        {
            // Dim non-winner icon for a subtle finish effect
            if (game.Winner == Player.X)
            {
                if (oIcon != null) oIcon.color = new Color(1f, 1f, 1f, 0.35f);
            }
            else if (game.Winner == Player.O)
            {
                if (xIcon != null) xIcon.color = new Color(1f, 1f, 1f, 0.35f);
            }
        }
    }

    private bool IsAITurn()
    {
        if (aiLevel == AILevel.None) return false;
        return game.CurrentPlayer == aiPlaysAs && !game.IsGameOver;
    }

    private void TryQueueAIMove()
    {
        if (!IsAITurn()) return;
        if (aiTurnPending) return;
        aiTurnPending = true;
        // Defer AI move slightly so UI updates first
        Invoke(nameof(PerformAIMove), 0.05f);
    }

    private void PerformAIMove()
    {
        aiTurnPending = false;
        if (!IsAITurn()) return;

        var level = aiLevel == AILevel.Easy ? TicTacToeAI.AILevel.Easy : (aiLevel == AILevel.Hard ? TicTacToeAI.AILevel.Hard : TicTacToeAI.AILevel.None);
        int move = TicTacToeAI.ChooseMove(game, level, aiPlaysAs);
        if (move >= 0)
        {
            game.MakeMove(move);
            moveWasByAI.Add(true);
            if (SoundManager.Instance != null) SoundManager.Instance.PlayPlace();
            RenderBoard();
            // If human's next, ensure buttons become interactable
            TryQueueAIMove();
        }
    }

	public void OnAILevelDropdownChanged(int value)
	{
		// 0=None, 1=Easy, 2=Hard
		int clamped = Mathf.Clamp(value, 0, 2);
		aiLevel = (AILevel)clamped;
		// If turning AI off, cancel any pending AI move
		if (aiLevel == AILevel.None && aiTurnPending)
		{
			CancelInvoke(nameof(PerformAIMove));
			aiTurnPending = false;
		}
		RenderBoard();
		UpdateAISideDropdownVisibility();
		TryQueueAIMove();
	}

	private void UpdateWinLine()
	{
		if (winLine == null)
		{
			return;
		}
		if (!game.IsGameOver || game.Winner == Player.None || game.WinningLineIndex < 0)
		{
			winLine.gameObject.SetActive(false);
			return;
		}

		int idx = game.WinningLineIndex;
		float zRot = 0f;
		int midCellIndex = 4; // default to center
		switch (idx)
		{
			// horizontals 0..2
			case 0: zRot = 0f; midCellIndex = 1; break; // top row center cell
			case 1: zRot = 0f; midCellIndex = 4; break; // middle row center cell
			case 2: zRot = 0f; midCellIndex = 7; break; // bottom row center cell
			// verticals 3..5
			case 3: zRot = 90f; midCellIndex = 3; break; // left column center cell
			case 4: zRot = 90f; midCellIndex = 4; break; // middle column center cell
			case 5: zRot = 90f; midCellIndex = 5; break; // right column center cell
			// diagonals 6..7
			case 6: zRot = -45f; midCellIndex = 4; break; // main diag (0-4-8)
			case 7: zRot = 45f; midCellIndex = 4; break;  // anti diag (2-4-6)
		}
		// Rotate
		winLine.localRotation = Quaternion.Euler(0f, 0f, zRot);
		// Position at the middle cell of the winning line
		if (cellButtons != null && midCellIndex >= 0 && midCellIndex < cellButtons.Length && cellButtons[midCellIndex] != null)
		{
			var midRt = cellButtons[midCellIndex].GetComponent<RectTransform>();
			if (midRt != null)
			{
				winLine.position = midRt.position; // world position to match Canvas layer
			}
		}
		winLine.gameObject.SetActive(true);
	}

	private void UpdateAISideDropdownVisibility()
	{
		if (aiSideDropdown != null)
		{
			bool show = aiLevel != AILevel.None;
			aiSideDropdown.gameObject.SetActive(show);
		}
	}

	public void OnAISideDropdownChanged(int value)
	{
		// 0=X, 1=O
		int clamped = Mathf.Clamp(value, 0, 1);
		var newSide = clamped == 0 ? Player.X : Player.O;
		if (aiPlaysAs != newSide)
		{
			// Cancel pending AI move to avoid double moves
			if (aiTurnPending)
			{
				CancelInvoke(nameof(PerformAIMove));
				aiTurnPending = false;
			}
			aiPlaysAs = newSide;
			RenderBoard();
			// If it's now AI's turn, queue it immediately (may cause consecutive AI moves by design)
			TryQueueAIMove();
		}
	}

    private void TryPlayGameOverHuman()
    {
        if (!game.IsGameOver || gameOverSfxPlayed) return;
        var winner = game.Winner;
        if (winner == Player.None) return; // draw

        // Require that throughout the game, human played exactly one side and AI played exactly the other.
        var historyPlayers = game.GetMovePlayersHistory();
        if (historyPlayers.Length == 0) return;
        if (historyPlayers.Length != moveWasByAI.Count) return; // safety guard

        bool humanPlayedX = false, humanPlayedO = false, aiPlayedX = false, aiPlayedO = false;
        for (int i = 0; i < historyPlayers.Length; i++)
        {
            var p = historyPlayers[i];
            if (moveWasByAI[i])
            {
                if (p == Player.X) aiPlayedX = true; else if (p == Player.O) aiPlayedO = true;
            }
            else
            {
                if (p == Player.X) humanPlayedX = true; else if (p == Player.O) humanPlayedO = true;
            }
        }

        bool humanExactlyOne = (humanPlayedX ^ humanPlayedO);
        bool aiExactlyOne = (aiPlayedX ^ aiPlayedO);
        if (!humanExactlyOne || !aiExactlyOne) return;
        bool opposite = (humanPlayedX && aiPlayedO) || (humanPlayedO && aiPlayedX);
        if (!opposite) return;

        Player humanSide = humanPlayedX ? Player.X : Player.O;

        if (SoundManager.Instance != null)
        {
            if (winner == humanSide) SoundManager.Instance.PlayWin();
            else SoundManager.Instance.PlayLose();
        }
        gameOverSfxPlayed = true;
    }
}


