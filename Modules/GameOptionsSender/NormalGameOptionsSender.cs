using System;
using System.Linq;
using System.Collections.Generic;
using Il2CppSystem.Linq;
using InnerNet;
using Hazel;
using AmongUs.GameOptions;

namespace TownOfHost.Modules
{
    public class NormalGameOptionsSender : GameOptionsSender
    {
        public override IGameOptions BasedGameOptions =>
            GameOptionsManager.Instance.CurrentGameOptions;
        public override bool IsDirty
        {
            get
            {
                if (_logicOptions == null || !GameManager.Instance.LogicComponents.Contains(_logicOptions))
                {
                    foreach (var glc in GameManager.Instance.LogicComponents)
                        if (_logicOptions.TryCast<LogicOptions>(out var lo))
                            _logicOptions = lo;
                }
                return _logicOptions != null && _logicOptions.IsDirty;
            }
        }
        private LogicOptions _logicOptions;

        public override IGameOptions BuildGameOptions()
            => BasedGameOptions;
    }
}