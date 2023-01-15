using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Hazel;
using InnerNet;
using UnityEngine;
using VentLib.Interfaces;

namespace VentLib;

public static class ParameterHelper
{
    public static Type[] AllowedTypes =
    {
        typeof(bool), typeof(byte), typeof(float), typeof(int), typeof(sbyte), typeof(string), typeof(uint),
        typeof(ulong), typeof(ushort), typeof(Vector2), typeof(InnerNetObject), typeof(IRpcSendable<>)
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
                throw new ArgumentException($"\"Parameter \"{p.Name}\" cannot be type {p.ParameterType}\". Allowed Types: [{String.Join(", ", AllowedTypes.GetEnumerator())}");
            return p.ParameterType;
        }).ToArray();
    }

    public static object[] Cast(Type[] parameters, MessageReader reader) => parameters.Select(p => reader.ReadDynamic(p)).ToArray();

    public static dynamic ReadDynamic(this MessageReader reader, Type parameter)
    {
        if (parameter.IsAbstract)
            return AbstractConstructors.Transform(reader, parameter);
        if (parameter == typeof(bool))
            return reader.ReadBoolean();
        if (parameter == typeof(byte))
            return reader.ReadByte();
        if (parameter == typeof(float))
            return reader.ReadSingle();
        if (parameter == typeof(int))
            return (reader.ReadInt32());
        if (parameter == typeof(sbyte))
            return reader.ReadSByte();
        if (parameter == typeof(string))
            return reader.ReadString();
        if (parameter == typeof(uint))
            return reader.ReadUInt32();
        if (parameter == typeof(ulong))
            return reader.ReadUInt64();
        if (parameter == typeof(ushort))
            return reader.ReadUInt16();
        if (parameter == typeof(Vector2))
            return NetHelpers.ReadVector2(reader);
        if (parameter == typeof(GameData))
            return reader.ReadNetObject<GameData>();
        if (parameter == typeof(GameManager))
            return reader.ReadNetObject<GameManager>();
        if (parameter == typeof(VoteBanSystem))
            return reader.ReadNetObject<VoteBanSystem>();
        if (parameter == typeof(MeetingHud))
            return reader.ReadNetObject<MeetingHud>();
        if (parameter == typeof(CustomNetworkTransform))
            return reader.ReadNetObject<CustomNetworkTransform>();
        if (parameter == typeof(LobbyBehaviour))
            return reader.ReadNetObject<LobbyBehaviour>();
        if (parameter == typeof(PlayerControl))
            return reader.ReadNetObject<PlayerControl>();
        if (parameter == typeof(PlayerPhysics))
            return reader.ReadNetObject<PlayerPhysics>();
        if (parameter == typeof(ShipStatus))
            return reader.ReadNetObject<ShipStatus>();
        if (parameter.IsAssignableTo(typeof(IList)))
        {
            Type genericType = parameter.GetGenericArguments()[0];
            object objectList = Activator.CreateInstance(parameter);
            MethodInfo Add = AccessTools.Method(parameter, "Add");

            ushort amount = reader.ReadUInt16();
            for (uint i = 0; i < amount; i++)
                Add.Invoke(objectList, new object[] {reader.ReadDynamic(genericType)});
            return (IEnumerable<dynamic>)objectList;
        }

        if (parameter.IsAssignableTo(typeof(IRpcWritable)))
        {
            Type rpcableType = parameter.GetGenericArguments().Any() ? parameter.GetGenericArguments()[0] : parameter;
            object rpcable = rpcableType.GetConstructor(BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic, Array.Empty<Type>())!.Invoke(null);
            return rpcableType.GetMethod("Read")!.Invoke(rpcable, new object[] { reader });
        }

        throw new ArgumentException($"Invalid Parameter Type {parameter}");
    }

}