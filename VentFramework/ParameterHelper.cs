using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Hazel;
using InnerNet;
using TownOfHost.Extensions;
using UnityEngine;

namespace VentWork;

public static class ParameterHelper
{
    public static Type[] AllowedTypes =
    {
        typeof(bool), typeof(byte), typeof(float), typeof(int), typeof(sbyte), typeof(string), typeof(uint),
        typeof(ulong), typeof(ushort), typeof(Vector2), typeof(InnerNetObject), typeof(RpcSendable<>)
    };

    public static bool IsTypeAllowed(Type type)
    {
        if (!type.IsAssignableTo(typeof(IEnumerable)) || type.GetGenericArguments().Length == 0)
            return type.IsAssignableTo(typeof(IRpcWritable)) || AllowedTypes.Any(type.IsAssignableTo);

        return IsTypeAllowed(type.GetGenericArguments()[0]);
    }


    public static Type[] Verify(ParameterInfo[] parameters)
    {
        return parameters.Select(p =>
        {
            if (!IsTypeAllowed(p.ParameterType))
                throw new ArgumentException($"\"Parameter \"{p.Name}\" cannot be type {p.ParameterType}\". Allowed Types: {AllowedTypes.PrettyString()}");
            /*if (p.IsOptional)
                throw new ArgumentException($"Optional parameter {p} is not allowed for methods annotated with ModRPC");*/
            return p.ParameterType;
        }).ToArray();
    }

    public static object[] Cast(Type[] parameters, MessageReader reader) => parameters.SelectMany(p => reader.ReadDynamic(p)).ToArray();

    public static IEnumerable<dynamic> ReadDynamic(this MessageReader reader, Type parameter)
    {
        List<dynamic> castList = new();
        if (parameter == typeof(bool))
            castList.Add(reader.ReadBoolean());
        else if (parameter == typeof(byte))
            castList.Add(reader.ReadByte());
        else if (parameter == typeof(float))
            castList.Add(reader.ReadSingle());
        else if (parameter == typeof(int))
            castList.Add(reader.ReadInt32());
        else if (parameter == typeof(sbyte))
            castList.Add(reader.ReadSByte());
        else if (parameter == typeof(string))
            castList.Add(reader.ReadString());
        else if (parameter == typeof(uint))
            castList.Add(reader.ReadUInt32());
        else if (parameter == typeof(ulong))
            castList.Add(reader.ReadUInt64());
        else if (parameter == typeof(ushort))
            castList.Add(reader.ReadUInt16());
        else if (parameter == typeof(Vector2))
            castList.Add(NetHelpers.ReadVector2(reader));
        else if (parameter == typeof(GameData))
            castList.Add(reader.ReadNetObject<GameData>());
        else if (parameter == typeof(GameManager))
            castList.Add(reader.ReadNetObject<GameManager>());
        else if (parameter == typeof(VoteBanSystem))
            castList.Add(reader.ReadNetObject<VoteBanSystem>());
        else if (parameter == typeof(MeetingHud))
            castList.Add(reader.ReadNetObject<MeetingHud>());
        else if (parameter == typeof(CustomNetworkTransform))
            castList.Add(reader.ReadNetObject<CustomNetworkTransform>());
        else if (parameter == typeof(LobbyBehaviour))
            castList.Add(reader.ReadNetObject<LobbyBehaviour>());
        else if (parameter == typeof(PlayerControl))
            castList.Add(reader.ReadNetObject<PlayerControl>());
        else if (parameter == typeof(PlayerPhysics))
            castList.Add(reader.ReadNetObject<PlayerPhysics>());
        else if (parameter == typeof(ShipStatus))
            castList.Add(reader.ReadNetObject<ShipStatus>());
        else if (parameter.IsAssignableTo(typeof(IList)))
        {
            if (!parameter.GetGenericArguments().Any())
                System.Console.Write(parameter);
            Type genericType = parameter.GetGenericArguments()[0];
            object objectList = Activator.CreateInstance(parameter);
            MethodInfo Add = AccessTools.Method(parameter, "Add");

            uint amount = reader.ReadPackedUInt32();
            for (uint i = 0; i < amount; i++)
                foreach (dynamic dyn in reader.ReadDynamic(genericType))
                    Add.Invoke(objectList, new object[] {dyn});
            return (IEnumerable<dynamic>)objectList;
        }
        else if (parameter.IsAssignableTo(typeof(IRpcWritable)))
        {
            Type rpcableType = parameter.GetGenericArguments().Any() ? parameter.GetGenericArguments()[0] : parameter;
            object rpcable = rpcableType.GetConstructor(BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic, Array.Empty<Type>())!.Invoke(null);
            castList.Add(rpcableType.GetMethod("Read")!.Invoke(rpcable, new object[] { reader }));
        } else throw new ArgumentException($"Invallid Parameter Type {parameter}");

        return castList;
    }

}