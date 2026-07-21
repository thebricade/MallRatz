using UnityEngine;

namespace CryingSnow.CheckoutFrenzy.Core
{
    public class Mission : ScriptableObject
    {
        public int missionId;
        public MissionGoal goalType;
        public int targetId;
        public int goalAmount;
        public int reward;
    }
}
