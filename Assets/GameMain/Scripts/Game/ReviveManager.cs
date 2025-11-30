using System;
using UnityGameFramework.Runtime;

namespace StarForce
{
    public class ReviveManager
    {
        private static ReviveManager s_Instance;
        public static ReviveManager Instance => s_Instance ?? (s_Instance = new ReviveManager());

        public int ReviveCount
        {
            get => GameEntry.Setting.GetInt(Constant.Setting.ReviveCount, 2); // Default 2
            set
            {
                GameEntry.Setting.SetInt(Constant.Setting.ReviveCount, value);
                GameEntry.Setting.Save();
            }
        }

        public void CheckDailyRefresh()
        {
            string lastDateStr = GameEntry.Setting.GetString(Constant.Setting.LastReviveDate, "");
            string todayStr = DateTime.Now.ToString("yyyy-MM-dd");

            if (lastDateStr != todayStr)
            {
                // New day, add 2 revives
                ReviveCount += 2;
                GameEntry.Setting.SetString(Constant.Setting.LastReviveDate, todayStr);
                GameEntry.Setting.Save();
                Log.Info($"Daily Revive Refresh! Current Count: {ReviveCount}");
            }
        }

        public bool ConsumeRevive()
        {
            if (ReviveCount > 0)
            {
                ReviveCount--;
                return true;
            }
            return false;
        }

        public void AddRevive(int amount)
        {
            ReviveCount += amount;
            Log.Info($"Added {amount} revives. Current Count: {ReviveCount}");
        }
    }
}
