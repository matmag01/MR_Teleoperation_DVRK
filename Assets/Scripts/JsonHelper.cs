// written by Bekwnn, 2015
﻿// contributed by Guney Ozsan, 2016
// modified by Guanhao Fu, 2020﻿

using System.Text.RegularExpressions;
using System.Collections.Generic;
using UnityEngine;

public class JsonHelper
{

    public static string GetSetPointPosition(string jsonString, string handle)
    {
        string pattern = "\"" + handle + "\"\\s*:\\s*\\{";
        // "Position"\s*:\s*\{
        Regex regx = new Regex(pattern);
        Match match = regx.Match(jsonString);

        if (match.Success)
        {
            int bracketCount = 1;
            int i;
            int startOfObj = match.Index + match.Length;
            for (i = startOfObj; bracketCount > 0; i++)
            {
                if (jsonString[i] == '{') bracketCount++;
                else if (jsonString[i] == '}') bracketCount--;
            }
            return "{" + jsonString.Substring(startOfObj, i - startOfObj);
        }

        //no match, return null
        return null;
    }

    public static string GetPosition(string jsonString, string handle)
    {
        //string pattern = "\"" + handle + "\"\\s*:\\s*\\{";
        string pattern = "\"" + handle + "\"\\s*:";
        // "Position"\s*:\s*\{
        Regex regx = new Regex(pattern);
        Match match = regx.Match(jsonString);
        
        if (match.Success)
        {
            int bracketCount = 1;
            int i;
            int startOfObj = match.Index + match.Length;
            for (i = startOfObj; bracketCount > 0; i++)
            {
                if (jsonString[i] == '{') bracketCount++;
                else if (jsonString[i] == '}') bracketCount--;
            }
            return "{" + jsonString.Substring(startOfObj, i - startOfObj);
        }

        //no match, return null
        return null;
    }

    public static string GetTranslation(string jsonString, string handle)
    {
        string pattern = "\"" + handle + "\":";

        Regex regx = new Regex(pattern);
        Match match = regx.Match(jsonString);

        if (match.Success)
        {
            int bracketCount = 1;
            int i;
            int startOfObj = match.Index + match.Length;
            for (i = startOfObj; bracketCount > 0; i++)
            {
                if (jsonString[i] == '{') bracketCount++;
                else if (jsonString[i] == '}') bracketCount--;
            }
            string trans_string = jsonString.Substring(startOfObj, i - startOfObj - 1);
 
            return  trans_string;
        }

        //no match, return null
        return null;
    }
    
    public static string GetRotation(string jsonString, string handle)
    {
        string pattern = "\"" + handle + "\":";

        Regex regx = new Regex(pattern);
        Match match = regx.Match(jsonString);

        if (match.Success)
        {
            int bracketCount = 1;
            int i;
            int startOfObj = match.Index + match.Length;
            for (i = startOfObj; bracketCount > 0; i++)
            {
                if (jsonString[i] == '{') bracketCount++;
                else if (jsonString[i] == '\"') bracketCount--;
            }
            return jsonString.Substring(startOfObj, i - startOfObj - 2);
        }

        //no match, return null
        return null;
    }

    public static string JawAngle(string dVRK_msg, string handle)
    {
        Regex regx = new Regex(handle);
        Match match = regx.Match(dVRK_msg);
        int startOfObj = match.Index + match.Length;
        int bracketCount = 0;
        int i;
        for (i = startOfObj; bracketCount < 2; i++)
        {
            if (dVRK_msg[i] == '[') bracketCount++;
            else if (dVRK_msg[i] == ']') bracketCount++;
        }
        return dVRK_msg.Substring(startOfObj + 1, i - startOfObj - 2);
    }

    public static string Joints(string dVRK_msg, string handle)
    {
        Regex regx = new Regex(handle);
        Match match = regx.Match(dVRK_msg);
        int startOfObj = match.Index + match.Length;
        int bracketCount = 0;
        int i;
        for (i = startOfObj; bracketCount < 2; i++)
        {
            if (dVRK_msg[i] == '[') bracketCount++;
            else if (dVRK_msg[i] == ']') bracketCount++;
        }
        return dVRK_msg.Substring(startOfObj + 1, i - startOfObj - 2);
    }

}
