using System;
using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.CSharp;
using UnityEngine;

public static class StringUtils
{
    public static string CreateFileName (string name)
    {
        name = StripNonAlphanumDot (name);
        if (!Char.IsUpper (name, 0))
        {
            TextInfo textInfo = new CultureInfo ("en-US", false).TextInfo;
            name = textInfo.ToTitleCase (name);
        }
        var invalids = System.IO.Path.GetInvalidFileNameChars ();
        return String.Join ("_", name.Split (invalids, StringSplitOptions.RemoveEmptyEntries)).TrimEnd ('.');
    }

    public static string StripNonAlphanumDot (string name)
    {
        Regex nonAlphanum = new Regex ("[^a-zA-Z0-9.]");
        name = nonAlphanum.Replace (name, "");
        return name;
    }
}