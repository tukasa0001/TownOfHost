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

    private CustomRpcSender() { }
    public CustomRpcSender(SendOption sendOption, bool isUnsafe)
    {
        stream = MessageWriter.Get(sendOption);

        this.sendOption = sendOption;
        this.isUnsafe = isUnsafe;
    }
    public static CustomRpcSender Create(SendOption sendOption = SendOption.None, bool isUnsafe = false)
    {
        return new CustomRpcSender(sendOption, isUnsafe);
    }


    public enum ActionTypes
    {
        Free = 0,
    }
}