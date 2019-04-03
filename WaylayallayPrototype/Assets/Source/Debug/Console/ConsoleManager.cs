using System;
using System.Collections.Generic;
using UnityEngine;
using System.Reflection;
using UnibusEvent;
using System.Text;
using System.Linq;
using System.Collections;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine.UI;
using Simplex;

public class ConsoleManager : Singleton<ConsoleManager>
{
    #region Variables
    
    private const string MAIN_ASSEMBLY = "Assembly-CSharp";

    private const char DELIMITER = ';';
    private const string REPEATER = "!";
    private const string ALIAS_ASSIGNER = "=";
    private const char SPECIAL_ALIAS_MARKER = '"';

    private const string ARCHIVE_DUMP = "/consoleArchive.txt";
    private const string ALIAS_DUMP = "/consoleAliases";

    private List<string> m_archive = new List<string>();
    
    private static Dictionary<string, List<Command>> m_methods = null;
    private static Dictionary<string, string> m_specialAliases = new Dictionary<string, string>();

    private static Dictionary<Type, UnityEngine.Object[]> m_foundObjects = new Dictionary<Type, UnityEngine.Object[]>();

    private Console m_console;
    public Console Console
    {
        get
        {
            return m_console ?? (m_console = FindObjectOfType<Console>());
        }
    }
    
    private int m_selectedRow = -1;

    private bool m_consoleLocked = false;

    private BinaryFormatter m_binaryFormatter = new BinaryFormatter();

    #endregion

    #region Main

    protected override void Awake()
    {
        base.Awake();

        LoadArchive();
    }

    private void Update()
    {
        if (m_consoleLocked)
            return;

        if (Input.GetKeyDown(KeyCode.RightBracket))
        {
            if (!Console.IsOpen)
                Console.Open();
            else
                Console.Close();
        }
    }

    private void OnApplicationQuit()
    {
        DumpArchive();
    }

    public void OnOpen()
    {
        InitializeCheatCodeList();
        m_selectedRow = -1;
    }

    public string IncrementSelection()
    {
        if (m_selectedRow == -1)
            m_selectedRow = m_archive.Count - 1;
        else
            m_selectedRow = Mathf.Clamp(m_selectedRow - 1, 0, m_archive.Count - 1);

        return m_archive[m_selectedRow];
    }

    public string DecrementSelection()
    {
        if (m_selectedRow == -1)
            return "";

        m_selectedRow = Mathf.Clamp(m_selectedRow + 1, 0, m_archive.Count);

        if (m_selectedRow == m_archive.Count)
        {
            m_selectedRow = -1;
            return "";
        }
        return m_archive[m_selectedRow];
    }

    public void CancelSelection()
    {
        m_selectedRow = -1;
    }
    
    #endregion
    
    #region Console
    
    /// <summary>
    /// Completely reset the console.
    /// </summary>
    private void ClearConsole()
    {
        m_archive.Clear();
        Console.Clear();
    }

    /// <summary>
    /// Print the given object to the console. Optionally can make it italic. 
    /// (Italic is meant to represent information returned from a method.)
    /// </summary>
    public void Print(ConsoleOutput consoleOutput)
    {
        Console.ScrollToBottom();
        
        if (consoleOutput.String == REPEATER)
            return;
        
        if (consoleOutput.Navigable)
            m_archive.Add(consoleOutput.String);

        string newString = consoleOutput.String;

        newString = consoleOutput.Italic ? "<i>" + newString + "</i>" : newString;
        
        if (consoleOutput.Colour == ConsoleOutput.TextColour.RED)
            newString = newString.AddColour(Color.red);
        else if (consoleOutput.Colour == ConsoleOutput.TextColour.GREY)
            newString = newString.AddColour(Color.grey);

        Console.Print(newString);
    }

    public void Print(ConsoleOutput[] consoleOutputs)
    {
        for (int i = 0; i < consoleOutputs.Length; i++)
            Print(consoleOutputs[i]);
    }
    
    #endregion

    #region Cheat Code Parsing and Invocation

    /// <summary>
    /// Accept console input, convert it debug codes and invoke the result,
    /// or throw errors if failed.
    /// </summary>
    public void ProcessRawInput(string input)
    {
        string text = input.ToLowerInvariant();

        if (m_specialAliases.ContainsKey(text))
        {
            ProcessRawInput(m_specialAliases[text]);
            return;
        }

        string[] code = input.ToLowerInvariant().Split(' ');

        if (code.Length >= 3 && code[1] == ALIAS_ASSIGNER)
        {
            if (AssignNewAlias(code))
                return;
        }

        string[] codes = text.TrimStart(DELIMITER).TrimEnd(DELIMITER).Split(DELIMITER);

        StartCoroutine(Cr_ParseCodes(codes));
    }

    private IEnumerator Cr_ParseCodes(string[] codes)
    {
        ToggleConsoleLock(true);

        for (int i = 0; i < codes.Length; i++)
        {
            ParseCheatCode(codes[i]);
            yield return null;
        }

        ToggleConsoleLock(false);
    }

    /// <summary>
    /// Given a string representing a cheat code, attempt to convert it into
    /// a method name followed by parameters, delimited by spaced, and then call that method.
    /// </summary>
    private void ParseCheatCode(string unparsedCode)
    {
        unparsedCode = unparsedCode.TrimWhitespace();

        // "!" = repeat last command
        if (unparsedCode == REPEATER)
        {
            if (m_archive.Count > 0)
                ParseCheatCode(m_archive[m_archive.Count - 1]);

            return;
        }

        if (m_specialAliases.ContainsKey(unparsedCode))
        {
            ProcessRawInput(m_specialAliases[unparsedCode]);
            return;
        }

        string[] code = unparsedCode.ToLowerInvariant().Split(' ');

        if (code.Length >= 3 && code[1] == ALIAS_ASSIGNER)
        {
            AssignNewAlias(code);

            return;
        }

        Print(new ConsoleOutput(unparsedCode, ConsoleOutput.TextColour.WHITE, italic: false, navigable: true));

        // get method name
        string methodName = code[0];

        // get method parameters
        object[] methodParameters = new object[code.Length - 1];
        for (int i = 1; i < code.Length; i++)
            methodParameters[i - 1] = ParseParameter(code[i]);

        // make sure we have a cheat list
        InitializeCheatCodeList();

        // see if any of our cheats match 
        foreach (MethodInfo method in GetMethods(methodName))
            if (InvokeMethod(method, methodParameters))
                return;

        // we failed to find one, but now lets try casting all the parameters to strings
        for (int i = 0; i < methodParameters.Length; i++)
            methodParameters[i] = methodParameters[i].ToString();

        foreach (MethodInfo method in GetMethods(methodName))
            if (InvokeMethod(method, methodParameters))
                return;

        Print(new ConsoleOutput("Couldn't parse code: '" + unparsedCode + "'", italic: true, colour: ConsoleOutput.TextColour.RED, navigable: false));
        Console.SetInputText("");
    }

    /// <summary>
    /// If the given methodinfo's parameters match those of the given object array,
    /// invoke the method and print the result to the console.
    /// </summary>
    private bool InvokeMethod(MethodInfo method, object[] parameters)
    {
        ParameterInfo[] parametersInMethod = method.GetParameters();

        if (parametersInMethod.Length != parameters.Length)
            return false;

        for (int i = 0; i < parameters.Length; i++)
            if (!AreCompatibleTypes(parametersInMethod[i].ParameterType, parameters[i].GetType()))
                return false;

        object returned = method.Invoke(null, parameters);

        if (returned != null)
        {
            if (returned.GetType() == typeof(ConsoleOutput[]))
                Print((ConsoleOutput[])returned);
            else if (returned.GetType() == typeof(ConsoleOutput))
                Print((ConsoleOutput)returned);
            else
                Print(returned.ToString());
        }

        return true;
    }

    /// <summary>
    /// Create a new alias for the given code at runtime. Returns true if success.
    /// </summary>
    private bool AssignNewAlias(string[] fullCode)
    {
        string newAlias = fullCode[0];

        string newAliasLower = newAlias.ToLowerInvariant();

        if (newAlias == REPEATER)
        {
            Console.LogError("Can't assign the repeater character '" + REPEATER + "' as an alias!");
            return false;
        }
        else if (newAlias == ALIAS_ASSIGNER)
        {
            Console.LogError("Can't assign the alias assignment character '" + ALIAS_ASSIGNER + "' as an alias!");
            return false;
        }
        else if (newAlias == DELIMITER.ToString())
        {
            Console.LogError("Can't assign the delimiter character '" + DELIMITER + "' as an alias!");
            return false;
        }
        else if (m_methods.ContainsKey(newAliasLower))
        {
            Console.LogError(newAlias + " is already a code!");
            return false;
        }
        
        StringBuilder sb = new StringBuilder();
        for (int i = 2; i < fullCode.Length; i++)
        {
            sb.Append(fullCode[i]);

            if (i < fullCode.Length - 1)
                sb.Append(" ");
        }

        string substituteCode = sb.ToString();

        if (newAliasLower == substituteCode)
        {
            Console.LogError("Those are identical!");
            return false;
        }
        if (CheckForAliasLoop(newAliasLower, substituteCode))
        {
            StartInfiniteLoopSequence();
        }
        else if (m_specialAliases.ContainsKey(newAliasLower))
        {
            Console.Log("Remapping the code \"" + newAlias + "\" from \"" + m_specialAliases[newAliasLower] + "\" to \"" + substituteCode + "\".");

            m_specialAliases[newAliasLower] = substituteCode;
        }
        else
        {
            Console.Log("Mapping the code \"" + newAlias + "\" to \"" + substituteCode + "\".");

            m_specialAliases.Add(newAliasLower, substituteCode);
        }

        return true;
    }

    /// <summary>
    /// Returns true if both types are equal, or one is a float and two is an int.
    /// </summary>
    private bool AreCompatibleTypes(Type one, Type two)
    {
        if (one == two)
            return true;

        return (one == typeof(float) && two == typeof(int));
    }

    /// <summary>
    /// Load the list of methods with the "ConsoleCommand" attribute.
    /// </summary>
    private void InitializeCheatCodeList()
    {
        if (!m_methods.IsNullOrEmpty())
            return;
        
        m_methods = new Dictionary<string, List<Command>>();

        Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
        Type[] types;

        for (int i = 0; i < assemblies.Length; i++)
        {
            // comment out the following block to search all assemblies, not just the main unity one
            if (assemblies[i].ToString().StartsWith(MAIN_ASSEMBLY))
            {
                types = assemblies[i].GetTypes();
                for (int j = 0; j < types.Length; j++)
                {
                    foreach (MethodInfo method in Reflection.GetMethodsWithAttribute(types[j], typeof(ConsoleCommandAttribute)))
                    {
                        if (method.IsStatic)
                        {
                            ConsoleCommandAttribute attribute = method.GetCustomAttributes(typeof(ConsoleCommandAttribute), true)[0] as ConsoleCommandAttribute;

                            AddMethod(attribute, method);
                        }
                    }
                }
            }
        }
    }

    /// <summary>
    /// Given a string, try to convert it to an object of the appropriate type. (Bool, float, int, or string)
    /// </summary>
    private object ParseParameter(string parameter)
    {
        if (parameter == "true")
            return true;
        else if (parameter == "false")
            return false;

        float number;
        if (float.TryParse(parameter, out number))
        {
            if (number % 1f == 0f)
                return (int)number;

            return number;
        }

        return parameter;
    }

    /// <summary>
    /// Get a nicer name for the given type.
    /// </summary>
    private static string GetFriendlyTypeName(Type type)
    {
        if (type == typeof(bool))
            return "Bool";
        else if (type == typeof(float))
            return "Float";
        else if (type == typeof(int))
            return "Int";
        else if (type == typeof(string))
            return "String";

        return "Unknown";
    }

    /// <summary>
    /// Add a MethodInfo object to the dictionary.
    /// </summary>
    private void AddMethod(ConsoleCommandAttribute attribute, MethodInfo method)
    {
        string methodName = (attribute.HasCustomCode ? attribute.Code : method.Name).ToLowerInvariant();

        if (!m_methods.ContainsKey(methodName))
            m_methods.Add(methodName, new List<Command>());

        m_methods[methodName].Add(new Command(attribute, method));

        // a consolecommand attribute may be specified with secret aliases (e.g. for shorthands)
        foreach (string alias in attribute.Aliases)
        {
            methodName = alias.ToLowerInvariant();

            if (!m_methods.ContainsKey(methodName))
                m_methods.Add(methodName, new List<Command>());

            m_methods[methodName].Add(new Command(attribute, method, alias));
        }
    }

    /// <summary>
    /// Enumerate through all MethodInfo objects with the given name.
    /// </summary>
    private IEnumerable<MethodInfo> GetMethods(string methodName)
    {
        if (m_methods.ContainsKey(methodName))
            for (int i = 0; i < m_methods[methodName].Count; i++)
                yield return m_methods[methodName][i].Method;
    }
    
    /// <summary>
    /// Determine whether adding an alias would cause a loop.
    /// 
    /// e.g 'a = b; b = a; a'
    /// </summary>
    private bool CheckForAliasLoop(string newAlias, string current)
    {
        if (newAlias == current)
            return true;

        if (!m_specialAliases.ContainsKey(current))
            return false;

        if (m_specialAliases[current] == newAlias)
            return true;

        return CheckForAliasLoop(newAlias, m_specialAliases[current]);
    }

    /// <summary>
    /// Given the beginning of a code, will try to 'autocomplete' the code.
    /// 
    /// Returns true if something found, else returns false. Remainder of the code
    /// is given as an out param.
    /// </summary>
    public bool TryAutocomplete(string input, out string remainder)
    {
        remainder = "";

        input = input.ToLowerInvariant();
        
        // code is already complete
        if (m_methods.ContainsKey(input) || m_specialAliases.ContainsKey(input))
            return false;

        string found = "";

        List<string> allCodes = new List<string>(m_specialAliases.Count + m_methods.Count);

        foreach (KeyValuePair<string, string> kvp in m_specialAliases)
        {
            allCodes.Add(kvp.Key);
        }

        foreach (KeyValuePair<string, List<Command>> kvp in m_methods)
        {
            bool isSecret = true;

            for (int i = 0; i < kvp.Value.Count; i++)
            {
                if (!kvp.Value[i].Attribute.Secret)
                {
                    isSecret = false;
                    break;
                }
            }
            
            // make sure at least one of the commands covered by this code isn't secret
            if (!isSecret)
                allCodes.Add(kvp.Key);
        }

        allCodes = allCodes.OrderBy(a => a).ToList();

        for (int i = 0; i < allCodes.Count; i++)
        {
            if (allCodes[i].StartsWith(input))
            {
                found = allCodes[i];
                break;
            }
        }


        if (found.IsNullOrEmpty() || found.Length == input.Length)
            return false;

        remainder = found.Substring(input.Length, found.Length - input.Length);
        
        return true;
    }

    #endregion

    #region Cheats

    // cheats should probably go wherever they're most relevant, but im just putting some general ones here.
    
    [ConsoleCommand(description: "Clear the console.")]
    private static void Clear()
    {
        Instance.ClearConsole();
    }

    [ConsoleCommand(description: "Close the console.", aliases: new string[] { "Exit" })]
    private static void Quit()
    {
        Instance.Console.Close();
    }
    
    private static void ListAllSpecialAliases()
    {
        if (m_specialAliases.IsNullOrEmpty())
            return;

        List<ConsoleOutput> output = new List<ConsoleOutput>();

        int columnWidth = 30;

        output.Add(new string('=', 104));
        output.Add("Alias".FixLength(columnWidth) + " -> " + "Actual Command".FixLength(columnWidth));
        output.Add(new string('=', 104));

        foreach (KeyValuePair<string, string> kvp in m_specialAliases)
            output.Add(kvp.Key.FixLength(columnWidth) + " -> " + kvp.Value.FixLength(columnWidth));

        Instance.Print(output.ToArray());
    }

    private static void ListAllCodes(bool includingSecret = false)
    {
        List<ConsoleOutput> output = new List<ConsoleOutput>();

        int nameWidth = 25;
        int paramWidth = 17;
        int descriptionWidth = 60;

        output.Add("Name".FixLength(nameWidth) + " " + "Parameters".FixLength(paramWidth) + " " + "Description".FixLength(descriptionWidth));
        output.Add(new string('=', (nameWidth + paramWidth + descriptionWidth) + 2));

        List<string> strings = new List<string>();
        foreach (KeyValuePair<string, List<Command>> kvp in m_methods)
        {
            for (int i = 0; i < kvp.Value.Count; i++)
            {
                if (kvp.Value[i].Secret && !includingSecret)
                    continue;

                ParameterInfo[] info = kvp.Value[i].Method.GetParameters();

                StringBuilder sb = new StringBuilder();

                for (int j = 0; j < info.Length; j++)
                    sb.Append(GetFriendlyTypeName(info[j].ParameterType) + ", ");

                if (sb.Length > 3)
                    sb.Remove(sb.Length - 2, 2);
                
                strings.Add(kvp.Value[i].Name.FixLength(nameWidth) + " " + sb.ToString().FixLength(paramWidth) + " " + kvp.Value[i].Description.FixLength(descriptionWidth));
            }
        }

        strings = strings.OrderBy(q => q).ToList();

        for (int i = 0; i < strings.Count; i++)
        {
            output.Add(strings[i]);
        }
        
        Instance.Print(output.ToArray());
    }

    [ConsoleCommand(description: "List all available commands.")]
    private static void Help()
    {
        Instance.Print(new string('=', 104));
        Instance.Print("= Use the Up and Down arrows to navgiate between previous commands.".FixLength(103) + "=");
        Instance.Print("= Use '!' to repeat the previous command.".FixLength(103) + "=");
        Instance.Print("= Use ';' to combine multiple commands in the same line.".FixLength(103) + "=");
        Instance.Print("= You can give entire codes shorter names with '=', e.g. \"h = help\"".FixLength(103) + "=");
        Instance.Print(new string('=', 104));

        ListAllCodes();
        ListAllSpecialAliases();
    }

    [ConsoleCommand(description: "List all available commands, including secret ones.", secret: true)]
    private static void ReallyHelp()
    {
        Instance.Print(new string('=', 104));
        Instance.Print("= Use the Up and Down arrows to navgiate between previous commands.".FixLength(103) + "=");
        Instance.Print("= Use '!' to repeat the previous command.".FixLength(103) + "=");
        Instance.Print("= Use ';' to combine multiple commands in the same line.".FixLength(103) + "=");
        Instance.Print("= You can give entire codes shorter names with '=', e.g. \"h = help\"".FixLength(103) + "=");
        Instance.Print(new string('=', 104));

        ListAllCodes(includingSecret: true);
        ListAllSpecialAliases();
    }
    
    [ConsoleCommand(description: "List all Objects of the given type in the scene.")]
    private static void FindObjectsOfType(string input)
    {
        Type foundType;
        if (!FindObjects(input, out foundType, print: true))
            return;

        List<ConsoleOutput> output = new List<ConsoleOutput>();

        int columnWidth = 30;

        output.Add(new string('=', 61));
        output.Add("Index".FixLength(columnWidth) + " " + "Name".FixLength(columnWidth));
        output.Add(new string('=', 61));

        for (int i = 0; i < m_foundObjects[foundType].Length; i++)
        {
            output.Add(i.ToString().FixLength(columnWidth) + " " + m_foundObjects[foundType][i].name.FixLength(columnWidth));
        }
        
        Instance.Print(output.ToArray());
    }

    [ConsoleCommand(description: ("Destroy an object of the given type at the given index. (See FindObjectsOfType)"))]
    private static void Destroy(string input, int index)
    {
        Type foundType;
        if (!FindObjects(input, out foundType, print: false))
        {
            Console.LogError("Couldn't find objects of type '" + input + "'. For more information, use FindObjectsOfType.");
            return;
        }

        if (index < 0 || index >= m_foundObjects[foundType].Length)
        {
            Console.LogError("Index " + index + " is out of range! Must be between 0 and " + (m_foundObjects[foundType].Length - 1) + ".");
            return;
        }

        Component component = m_foundObjects[foundType][index] as Component;

        if (component != null)
            DestroyImmediate(component.gameObject);
        else
            DestroyImmediate(m_foundObjects[foundType][index]);
    }

    #endregion

    #region Other
    
    private static bool FindObjects(string input, out Type foundType, bool print = false)
    {
        string typeName = input.ToLowerInvariant();
        Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
        Type[] types;

        foundType = null;

        for (int i = 0; i < assemblies.Length; i++)
        {
            types = assemblies[i].GetTypes();

            for (int j = 0; j < types.Length; j++)
            {
                string[] nameSplit = types[j].Name.Split('.');
                {
                    if (nameSplit[nameSplit.Length - 1].ToLowerInvariant() == typeName && typeof(UnityEngine.Object).IsAssignableFrom(types[j]))
                    {
                        foundType = types[j];
                        break;
                    }
                }
            }
        }

        if (foundType == null)
        {
            if (print)
                Console.LogError("Couldn't find a type called " + input + "!");

            return false;
        }
        
        UnityEngine.Object[] found = FindObjectsOfType(foundType);

        if (found.IsNullOrEmpty())
        {
            if (print)
                Console.LogError("Couldn't find any objects of type " + input + "!");

            return false;
        }

        if (m_foundObjects.ContainsKey(foundType))
            m_foundObjects[foundType] = found;
        else
            m_foundObjects.Add(foundType, found);

        return true;
    }

    private void StartInfiniteLoopSequence()
    {
        StartCoroutine(Cr_InfiniteLoopSequence());
    }

    private IEnumerator Cr_InfiniteLoopSequence()
    {
        ToggleConsoleLock(true);
        
        string[] output = new string[]
        {
            "OH NO",
            "WHAT HAVE YOU DONE",
            "INFINITE LOOP DETECTED",
        };

        for (int i = 0; i < output.Length; i++)
        {
            Console.LogError(output[i]);

            yield return new WaitForSecondsRealtime(1f);
        }

        StringBuilder sb;

        int numberOfScreams = 100;

        for (int i = 0; i < numberOfScreams; i++)
        {
            sb = new StringBuilder();
            for (int j = 0; j < 100; j++)
            {
                if (UnityEngine.Random.Range(0, i) < (numberOfScreams / 8f))
                    sb.Append("A");
                else
                    sb.Append(UnityEngine.Random.Range(0, 2).ToString());
            }

            Console.LogError(sb.ToString());

            yield return new WaitForSecondsRealtime(0.02f);
        }

        yield return new WaitForSecondsRealtime(2f);

        output = new string[]
        {
            "...",
            "Hah.",
            "Just kidding."
        };

        for (int i = 0; i < output.Length; i++)
        {
            Console.LogError(output[i]);

            yield return new WaitForSecondsRealtime(1f);
        }

        ToggleConsoleLock(false);
    }

    private void ToggleConsoleLock(bool toggle)
    {
        Console.ToggleInputField(!toggle);
        m_consoleLocked = toggle;
    }

    #endregion
    
    #region IO

    /// <summary>
    /// Store the archive in a file to make the console persistent across sessions.
    /// </summary>
    private void DumpArchive()
    {
        File.WriteAllLines(Application.persistentDataPath + ARCHIVE_DUMP, m_archive.ToArray());

        using (FileStream fs = new FileStream(Application.persistentDataPath + ALIAS_DUMP, FileMode.Create))
        {
            m_binaryFormatter.Serialize(fs, m_specialAliases);
        }
    }

    /// <summary>
    /// Load the archive from a file to make the console persistent across sessions.
    /// </summary>
    private void LoadArchive()
    {
        if (!File.Exists(Application.persistentDataPath + ARCHIVE_DUMP))
            return;

        m_archive = File.ReadAllLines(Application.persistentDataPath + ARCHIVE_DUMP).ToList();

        for (int i = 0; i < m_archive.Count; i++)
        {
            Console.Print(m_archive[i]);
        }

        if (!File.Exists(Application.persistentDataPath + ALIAS_DUMP))
            return;

        using (FileStream fs = new FileStream(Application.persistentDataPath + ALIAS_DUMP, FileMode.Open))
        {
            Dictionary<string, string> specialAliasesObject = m_binaryFormatter.Deserialize(fs) as Dictionary<string, string>;

            if (specialAliasesObject != null)
                m_specialAliases = specialAliasesObject;
        }
    }

    #endregion
    private struct Command
    {
        public ConsoleCommandAttribute Attribute { get; private set; }
        public string Name { get; private set; }
        public MethodInfo Method { get; private set; }
        public string Description { get; private set; }
        public bool Secret { get; private set; }

        public Command(ConsoleCommandAttribute attribute, MethodInfo method) 
            : this(attribute, method, attribute.HasCustomCode ? attribute.Code : method.Name, attribute.Secret) { }

        public Command(ConsoleCommandAttribute attribute, MethodInfo method, string name, bool secret = true)
        {
            Attribute = attribute;
            Method = method;
            Description = attribute.Description;
            Secret = secret;
            Name = name;
        }
    }
}
public struct ConsoleOutput
{
    public enum TextColour
    {
        WHITE,
        GREY,
        RED
    }

    public string String { get; private set; }
    public TextColour Colour { get; private set; }
    public bool Italic { get; private set; }
    public bool Navigable { get; private set; }

    public ConsoleOutput(object str, TextColour colour = TextColour.WHITE, bool italic = true, bool navigable = false)
    {
        String = str.ToString();
        Colour = colour;
        Italic = italic;
        Navigable = navigable;
    }

    public static ConsoleOutput Error(object str)
    {
        return new ConsoleOutput(str, TextColour.RED);
    }

    public static implicit operator ConsoleOutput(string str)
    {
        return new ConsoleOutput(str, TextColour.GREY);
    }

    public static implicit operator string(ConsoleOutput console)
    {
        return console.ToString();
    }

    public override string ToString()
    {
        return String;
    }
}