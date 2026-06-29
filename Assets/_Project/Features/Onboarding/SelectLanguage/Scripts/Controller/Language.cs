using System;
using UnityEngine;

[Serializable]
public class Language
{
    public string prefix;
    public string name;
    public SystemLanguage systemLanguage;

    public Language(string prefix, string name, SystemLanguage systemLanguage)
    {
        this.prefix = prefix;
        this.name = name;
        this.systemLanguage = systemLanguage;
    }
}