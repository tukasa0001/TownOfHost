using System;
using System.Collections.Generic;
using System.Text;

namespace TownOfHost
{
    class Cloud
    {
        public static bool CheckCheat(byte callId, ref string text)
        {

            switch (callId)
            {
                case 85:
                    text = "开挂（使用AUM）";
                    break;
            }

            if (text == "") return false;
            else return true;
        }


    }
}
