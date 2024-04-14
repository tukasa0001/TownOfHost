//using System.Collections.Generic;
//using System.Linq;
//using NCMBClient;
//using Newtonsoft.Json.Linq;
//using TownOfHostForE.Attributes;
//using TownOfHostForE.Roles.Core;

//namespace TownOfHostForE
//{
//    class DBSetting
//    {
//        private static NCMB db;
//        public static bool dbInited = false;

//        public static List<string> crewRoleList = new();
//        public static List<string> impRoleList = new();
//        public static List<string> newtRoleList = new();
//        public static List<string> animRoleList = new();
//        public static List<string> addonList = new();

//        [PluginModuleInitializer]
//        public static void Init()
//        {
//            var applicationKey = "a0a76d58df0de612e07018e74253babd9e0021d9138a0d3d565e36e9b7e8eb19";
//            var clientKey = "229df98e9ffbd32cb83f0e48cb51e982bf2c0eb376a50bffe4f5c7644ef0c609";

//            string dummy = @"{
//                 CPU: 'Intel',
//                 Drives: [
//                    'DVD read/writer',
//                    '500 gigabyte hard drive'
//                 ]
//            }";

//            JObject o = JObject.Parse(dummy);

//            db = new NCMB(applicationKey, clientKey);
//            if (db == null)
//            {
//                Logger.Info("初期化失敗", "DB");
//                return;
//            }
//            dbInited = true;
//        }

//        public static void sendDB(SendData sendData)
//        {
//            if (!dbInited) return;
//            var Result = new NCMBObject(sendData.TableStringDate);
//            Result.Set("RoomState", sendData.RoomState)
//                  .Set("RoomCode", sendData.RoomCode)
//                  .Set("REIKAITOUHYOU", BetWinTeams.BetWinTeamMode.GetBool().ToString())
//                  .Set("ModVersion", sendData.ModVersion)
//                  .Set("PlayerCount", "PC:" +sendData.PlayerCount)
//                  .Set("Date", sendData.Date)
//                  .Set("HostName", sendData.HostName)
//                  .Set("HostFriendCode", sendData.HostFriendCode)
//                  .Set("Map", sendData.Map)
//                  .Set("WinnerTeam", sendData.WinnerTeam);

//            SetDbClass(ref Result);

//            Result.Save();
//            ClearList();
//        }

//        private static void SetDbClass(ref NCMBObject Result)
//        {
//            if (impRoleList != null && impRoleList.Count > 0)
//            {
//                for (int roleCount = 0; roleCount < impRoleList.Count(); roleCount++)
//                {
//                    Result.Set("RoleIsImposter" + roleCount.ToString(), impRoleList[roleCount]);
//                }
//            }
//            if (newtRoleList != null && newtRoleList.Count > 0)
//            {
//                for (int roleCount = 0; roleCount < newtRoleList.Count(); roleCount++)
//                {
//                    Result.Set("RoleIsNeutral" + roleCount.ToString(), newtRoleList[roleCount]);
//                }
//            }
//            if (animRoleList != null && animRoleList.Count > 0)
//            {
//                for (int roleCount = 0; roleCount < animRoleList.Count(); roleCount++)
//                {
//                    Result.Set("RoleIsAnimals" + roleCount.ToString(), animRoleList[roleCount]);
//                }
//            }
//            if (crewRoleList != null && crewRoleList.Count > 0)
//            {
//                for (int roleCount = 0; roleCount < crewRoleList.Count(); roleCount++)
//                {
//                    Result.Set("RoleIsCrewmate" + roleCount.ToString(), crewRoleList[roleCount]);
//                }
//            }
//            if (addonList != null && addonList.Count() > 0)
//            {
//                for (int addonCount = 0; addonCount < addonList.Count(); addonCount++)
//                {
//                    Result.Set("RoleIsAddon" + addonCount.ToString(), addonList[addonCount]);
//                }
//            }
//        }

//        public static void SetDbRoleList(PlayerControl pc)
//        {
//            //db送信用
//            var role = PlayerState.GetByPlayerId(pc.PlayerId).MainRole;

//            if (role == CustomRoles.NotAssigned) return;
//            if (role.IsAddOn() || role == CustomRoles.Lovers)
//            {
//                DBSetting.addonList.Add(Utils.GetRoleName(role));
//            }
//            else
//            {
//                setMainRole(role);
//            }

//            foreach (var subRole in PlayerState.GetByPlayerId(pc.PlayerId).SubRoles)
//            {
//                if (subRole == CustomRoles.NotAssigned) return;
//                if (subRole.IsAddOn() || subRole == CustomRoles.Lovers)
//                {
//                    DBSetting.addonList.Add(Utils.GetRoleName(subRole));
//                }
//                else
//                {
//                    setMainRole(role);
//                }
//            }
//        }

//        private static void ClearList()
//        {

//            impRoleList.Clear();
//            newtRoleList.Clear();
//            animRoleList.Clear();
//            crewRoleList.Clear();
//            addonList.Clear();
//        }

//        private static void setMainRole(CustomRoles role)
//        {
//            if (role.IsImpostor())
//            {
//                DBSetting.impRoleList.Add(Utils.GetRoleName(role));
//            }
//            else if (role.IsNeutral())
//            {
//                DBSetting.newtRoleList.Add(Utils.GetRoleName(role));
//            }
//            else if (role.IsAnimals())
//            {
//                DBSetting.animRoleList.Add(Utils.GetRoleName(role));
//            }
//            else
//            {
//                DBSetting.crewRoleList.Add(Utils.GetRoleName(role));
//            }
//        }



//        public class SendData
//        {
//            private string _tableStringDate;
//            private string _roomCode;
//            private string _modVersion;
//            private string _date;
//            private string _hostName;
//            private string _hostFriendCode;
//            private string _roomState;
//            private string _winnerTeam;
//            private string _map;
//            private string _playerCount;
//            private List<string> _roleNameList;
//            private List<string> _addonList;

//            public string TableStringDate
//            {
//                get
//                {
//                    return _tableStringDate;
//                }
//                set
//                {
//                    _tableStringDate = value;
//                }
//            }
//            public string RoomCode
//            {
//                get
//                {
//                    return _roomCode;
//                }
//                set
//                {
//                    _roomCode = value;
//                }
//            }
//            public string ModVersion
//            {
//                get
//                {
//                    return _modVersion;
//                }
//                set
//                {
//                    _modVersion = value;
//                }
//            }
//            public string Date
//            {
//                get
//                {
//                    return _date;
//                }
//                set
//                {
//                    _date = value;
//                }
//            }
//            public string HostName
//            {
//                get
//                {
//                    return _hostName;
//                }
//                set
//                {
//                    _hostName = value;
//                }
//            }
//            public string HostFriendCode
//            {
//                get
//                {
//                    return _hostFriendCode;
//                }
//                set
//                {
//                    _hostFriendCode = value;
//                }
//            }
//            public string RoomState
//            {
//                get
//                {
//                    return _roomState;
//                }
//                set
//                {
//                    _roomState = value;
//                }
//            }
//            public string WinnerTeam
//            {
//                get
//                {
//                    return _winnerTeam;
//                }
//                set
//                {
//                    _winnerTeam = value;
//                }
//            }
//            public string Map
//            {
//                get
//                {
//                    return _map;
//                }
//                set
//                {
//                    _map = value;
//                }
//            }
//            public string PlayerCount
//            {
//                get
//                {
//                    return _playerCount;
//                }
//                set
//                {
//                    _playerCount = value;
//                }
//            }
//            public List<string> RoleNameList
//            {
//                get
//                {
//                    return _roleNameList;
//                }
//                set
//                {
//                    _roleNameList = value;
//                }
//            }
//            public List<string> AddonList
//            {
//                get
//                {
//                    return _addonList;
//                }
//                set
//                {
//                    _addonList = value;
//                }
//            }
//        }
//    }
//}
