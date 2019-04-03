using UnityEngine;
using UnityEngine.UI;
using System.IO;
using Simplex;

public class Console : MonoBehaviour
{
    #region Variables
    
    private const int MAX_CHARS_IN_ARCHIVE = 14000;    // mesh starts to get too large after this number

    private bool m_isOpen = false;
    public bool IsOpen { get { return m_isOpen; } }

    [SerializeField]
    private Canvas m_canvas;

    [Header("Console")]

    [SerializeField]
    private ScrollRect m_scrollRect;

    [SerializeField]
    private InputField m_inputField;

    [SerializeField]
    private Text m_archiveText;

    private bool m_changedInputThisFrame = false;

    private string TypedString
    {
        get
        {
            return m_inputField.text.RemoveColouredText();
        }
    }

    private string GuessedString
    {
        get
        {
            return m_inputField.text.RemoveMarkup();
        }
    }

    private bool IsCaretAtEnd
    {
        get
        {
            return m_inputField.caretPosition == m_inputField.text.RemoveColouredText().Length;
        }
    }
    
    private bool m_autocompleteOnNextRight = true;

    #endregion

    #region UI Stuff
    
    private void Awake()
    {
        DontDestroyOnLoad(this);
    }

    public void Open()
    {
        m_canvas.gameObject.SetActive(true);
        m_isOpen = true;
        ConsoleManager.Instance.OnOpen();
        m_inputField.interactable = true;
        SetInputText("");
    }

    public void Close()
    {
        m_canvas.gameObject.SetActive(false);
        m_isOpen = false;
        m_inputField.interactable = false;
        m_inputField.DeactivateInputField();
    }

    private void Update()
    {
        if (m_isOpen)
        {
            m_changedInputThisFrame = false;

            // prevent the carat from going into the autocomplete
            m_inputField.caretPosition = Mathf.Min(m_inputField.caretPosition, m_inputField.text.RemoveColouredText().Length);
            
            if (m_inputField.text.StartsWith("`"))
                m_inputField.text = m_inputField.text.TrimStart('`');

            // prevent the forward delete key from removing the prediction text
            if (Input.GetKey(KeyCode.Delete))
            {
                string textAfterCaret = m_inputField.text.Substring(m_inputField.caretPosition, m_inputField.text.Length - m_inputField.caretPosition);

                if (textAfterCaret.StartsWith("color=\"#808080\">"))
                    m_inputField.text = m_inputField.text.Substring(0, m_inputField.caretPosition);
            }

            if (Input.GetKeyDown(KeyCode.LeftArrow))
                m_autocompleteOnNextRight = false;

            if (Input.GetKeyDown(KeyCode.RightArrow))
                OnRightArrowClicked();

            if (Input.GetKeyDown(KeyCode.Return))
                OnEnterClicked();

            if (Input.GetKeyDown(KeyCode.UpArrow))
                SetInputText(ConsoleManager.Instance.IncrementSelection());

            if (Input.GetKeyDown(KeyCode.DownArrow))
                SetInputText(ConsoleManager.Instance.DecrementSelection());
        }
    }

    #endregion

    #region Controls

    /// <summary>
    /// Highlight the input field.
    /// </summary>
    public void OnLeftClickUp()
    {
        m_inputField.ActivateInputField();
        m_inputField.caretPosition = m_inputField.text.Length;
        m_autocompleteOnNextRight = true;
    }
    
    /// <summary>
    /// When enter (or 'return') is clicked, try to run the cheat code entered.
    /// </summary>
    private void OnEnterClicked()
    {
        ConsoleManager.Instance.CancelSelection();

        if (m_inputField.text.Length == 0)
            return;
        
        ConsoleManager.Instance.ProcessRawInput(GuessedString);

        m_inputField.text = "";

        m_inputField.ActivateInputField();
        m_inputField.caretPosition = m_inputField.text.Length;
        m_autocompleteOnNextRight = true;
    }

    private void OnRightArrowClicked()
    {
        if (m_inputField.text.IsNullOrEmpty())
            return;

        if (IsCaretAtEnd)
        {
            if (m_autocompleteOnNextRight)
            {
                m_autocompleteOnNextRight = false;

                Autocomplete();

                return;
            }
            else
            {
                m_autocompleteOnNextRight = true;
            }
        }
    }

    public void ScrollToBottom()
    {
        m_scrollRect.ScrollToBottom();
    }

    public void ToggleInputField(bool toggle)
    {
        m_inputField.interactable = toggle;

        if (toggle)
        {
            OnLeftClickUp();
        }
    }

    private void Autocomplete()
    {
        string trimmedInput = m_inputField.text.RemoveColouredText();
        string remainder;

        if (ConsoleManager.Instance.TryAutocomplete(trimmedInput, out remainder))
        {
            string guessed = trimmedInput + remainder;

            m_inputField.text = guessed + " ";
            m_inputField.caretPosition = m_inputField.text.Length;
            m_autocompleteOnNextRight = true;
        }
    }

    private void Predict()
    {
        string input = m_inputField.text.RemoveColouredText();

        m_changedInputThisFrame = true;

        if (input.Length == 0)
        {
            m_inputField.text = "";
            return;
        }

        string remainder;

        if (ConsoleManager.Instance.TryAutocomplete(input, out remainder))
        {
            m_inputField.textComponent.supportRichText = true;

            m_inputField.text = input + remainder.AddColour(Color.grey);
        }
        else
        {
            m_inputField.text = input;
        }
    }

    #endregion

    #region Console

    /// <summary>
    /// Completely reset the console.
    /// </summary>
    public void Clear()
    {
        m_archiveText.text = "";
        SetInputText("");
    }
    
    /// <summary>
    /// Set the text of the input field.
    /// </summary>
    public void SetInputText(string str)
    {
        m_inputField.textComponent.supportRichText = false;

        m_inputField.text = str;
        m_inputField.ActivateInputField();
        m_inputField.caretPosition = m_inputField.text.Length;
        m_autocompleteOnNextRight = true;
    }

    public void OnInputChanged()
    {
        if (m_changedInputThisFrame)
            return;
        
        Predict();

        if (m_inputField.caretPosition == TypedString.Length)
            m_autocompleteOnNextRight = true;
    }

    public void Print(string str)
    {
        if (m_archiveText == null)
            return;

        string newArchiveString = (m_archiveText.text + "\n" + str);

        if (newArchiveString.Length > MAX_CHARS_IN_ARCHIVE)
            m_archiveText.text = Strings.TrimByLines(newArchiveString, MAX_CHARS_IN_ARCHIVE, "\n");
        else
            m_archiveText.text = newArchiveString;
    }

    public static void Log(ConsoleOutput output, bool forceShowConsole = false)
    {
        if (!ConsoleManager.Instance.Console.m_isOpen)
            ConsoleManager.Instance.Console.Open();

        ConsoleManager.Instance.Print(output);
    }
    
    public static void Log(object str, bool forceShowConsole = false)
    {
        Log(str.ToString(), forceShowConsole);
    }

    public static void LogError(object str, bool forceShowConsole = false)
    {
        Log(ConsoleOutput.Error(str), forceShowConsole);
    }

    #endregion
}
