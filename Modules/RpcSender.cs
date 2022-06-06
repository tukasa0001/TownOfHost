using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HarmonyLib;
using Hazel;

public class CustomRpcSender
{
    public MessageWriter stream;
    public SendOption sendOption;
    public bool isUnsafe;

    private ActionTypes latestActionType;



    public enum ActionTypes
    {
        Free = 0,
    }
}