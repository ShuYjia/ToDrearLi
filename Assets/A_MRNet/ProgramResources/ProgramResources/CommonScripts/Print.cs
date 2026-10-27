using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Bpf.Diagnostics;

public class Print 
{
   public static void Edit(string str)
    {
        switch (Application.platform)
        {
            case RuntimePlatform.WindowsEditor:

                break;
            default:
                break;
        }
    }
    public static void Player(string str)
    {
        switch (Application.platform)
        {
            case RuntimePlatform.WindowsEditor:
                break;
            default:
                BpfLog.Info(BpfLogCategory.TexCore, "print.player", null, null, BpfLogField.Of("str", str));
                break;
        }
    }
}
