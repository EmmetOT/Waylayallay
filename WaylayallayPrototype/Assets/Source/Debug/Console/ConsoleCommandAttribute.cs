using Simplex;
using System;
using System.Collections.Generic;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public class ConsoleCommandAttribute : Attribute
{
    public string Code { get; private set; }
    public bool HasCustomCode { get; private set; }

    private string[] m_aliases;

    public string Description { get; private set; }
    public bool Secret { get; private set; }
    
    public ConsoleCommandAttribute(string code = "", string description = "", string[] aliases = null, bool secret = false)
    {
        m_aliases = aliases;
        Code = code.TrimWhitespace().Replace(" ", "");
        HasCustomCode = !code.IsNullOrEmpty();
        Description = description;
        Secret = secret;
    }

    public IEnumerable<string> Aliases
    {
        get
        {
            if (m_aliases != null)
                for (int i = 0; i < m_aliases.Length; i++)
                    if (!m_aliases[i].IsNullOrEmpty() && (!HasCustomCode || m_aliases[i] != Code))
                        yield return m_aliases[i];
        }
    }
}
