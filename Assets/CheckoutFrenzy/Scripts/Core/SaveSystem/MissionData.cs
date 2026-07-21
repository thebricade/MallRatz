namespace CryingSnow.CheckoutFrenzy.Core
{
    [System.Serializable]
    public class MissionData
    {
        public int MissionID { get; set; }
        public int Progress { get; set; }
        public bool IsComplete { get; set; }

        public MissionData() { }

        public MissionData(int missionId)
        {
            MissionID = missionId;
        }
    }
}
