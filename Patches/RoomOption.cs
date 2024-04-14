using System;
using Il2CppInterop.Runtime;
using AmongUs.GameOptions;
using Hazel;
using UnityEngine;
using System.Linq.Expressions;

namespace TownOfHostForE
{
    //引用：SuperNewRoles
    public static class RoomOption
    {
        public static void RpcSyncOption(this IGameOptions gameOptions, int TargetClientId = -1)
        {
            GameManager gm = NormalGameManager.Instance;
            MessageWriter writer = MessageWriter.Get(SendOption.Reliable);

            // 書き込み {}は読みやすさのためです。
            if (TargetClientId < 0)
            {
                writer.StartMessage(5);
                writer.Write(AmongUsClient.Instance.GameId);
                return;
            }
            else
            {
                writer.StartMessage(6);
                writer.Write(AmongUsClient.Instance.GameId);
                if (TargetClientId == PlayerControl.LocalPlayer.GetClientId()) return;
                writer.WritePacked(TargetClientId);
            }

            {
                writer.StartMessage(1); //0x01 Data
                {
                    writer.WritePacked(gm.NetId);
                    writer.StartMessage((byte)4);
                    writer.WriteBytesAndSize(gm.LogicOptions.gameOptionsFactory.ToBytes(gameOptions,false));
                    writer.EndMessage();
                }
                writer.EndMessage();
            }
            writer.EndMessage();

            AmongUsClient.Instance.SendOrDisconnect(writer);
            writer.Recycle();
        }
    }
    public static unsafe class FastDestroyableSingleton<T> where T : MonoBehaviour
    {
        private static readonly IntPtr _fieldPtr;
        private static readonly Func<IntPtr, T> _createObject;
        static FastDestroyableSingleton()
        {
            _fieldPtr = IL2CPP.GetIl2CppField(Il2CppClassPointerStore<DestroyableSingleton<T>>.NativeClassPtr, nameof(DestroyableSingleton<T>._instance));
            var constructor = typeof(T).GetConstructor(new[] { typeof(IntPtr) });
            var ptr = Expression.Parameter(typeof(IntPtr));
            var create = Expression.New(constructor!, ptr);
            var lambda = Expression.Lambda<Func<IntPtr, T>>(create, ptr);
            _createObject = lambda.Compile();
        }

        public static T Instance
        {
            get
            {
                IntPtr objectPointer;
                IL2CPP.il2cpp_field_static_get_value(_fieldPtr, &objectPointer);
                return objectPointer == IntPtr.Zero ? DestroyableSingleton<T>.Instance : _createObject(objectPointer);
            }
        }
    }
}
