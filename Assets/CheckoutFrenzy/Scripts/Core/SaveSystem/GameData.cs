using System.Collections.Generic;

namespace CryingSnow.CheckoutFrenzy.Core
{
    [System.Serializable]
    public class GameData
    {
        public string StoreName { get; set; }
        public string NameColor { get; set; }
        public decimal PlayerMoney { get; set; }
        public int CurrentLevel { get; set; }
        public int CurrentExperience { get; set; }

        public List<FurnitureData> SavedFurnitures { get; set; }
        public List<BoxData> SavedBoxes { get; set; }
        public List<EntityData> SavedFurnitureBoxes { get; set; }

        public List<CustomPrice> CustomPrices { get; set; }

        public decimal PendingOrdersValue { get; set; }
        public decimal UnpaidProductsValue { get; set; }

        public HashSet<int> LicensedProducts { get; set; }
        public HashSet<int> OwnedLicenses { get; set; }

        public int ExpansionLevel { get; set; }
        public bool IsWarehouseUnlocked { get; set; }

        public int TotalDays { get; set; }
        public int TotalMinutes { get; set; }

        public SummaryData CurrentSummary { get; set; }
        public MissionData CurrentMission { get; set; }

        public List<EmployeeData> HiredEmployees { get; set; }

        public System.DateTime LastSaved { get; set; }
        public System.TimeSpan TotalPlaytime { get; set; }

        public List<Bill> Bills { get; set; }
        public List<Loan> Loans { get; set; }

        public List<EntityData> SavedCleanables { get; set; }

        public GameData() { }

        public void Initialize()
        {
            StoreName = GameConfig.Instance.DefaultStoreName;
            NameColor = "#FFFFFF";

            PlayerMoney = GameConfig.Instance.StartingMoney;
            CurrentLevel = 1;

            SavedFurnitures = new List<FurnitureData>();
            SavedBoxes = new List<BoxData>();
            SavedFurnitureBoxes = new List<EntityData>();
            CustomPrices = new List<CustomPrice>();
            LicensedProducts = new HashSet<int>();
            OwnedLicenses = new HashSet<int>();

            TotalDays = 1;

            var openTime = GameConfig.Instance.OpenTime;
            int totalMinutes = TimeRange.ToMinutes(openTime.StartHour, openTime.StartMinute);
            TotalMinutes = totalMinutes;

            CurrentSummary = new SummaryData(PlayerMoney);
            CurrentMission = new MissionData(1);

            HiredEmployees = new List<EmployeeData>();
            Bills = new List<Bill>();
            Loans = new List<Loan>();

            SavedCleanables = new List<EntityData>();
        }
    }
}
