namespace MiniAdmin.Hud
{
    /// <summary>Cached number strings: no allocation for the values a HUD shows most.</summary>
    public static class NumberText
    {
        private const int CacheSize = 1000;

        private static readonly string[] Cache = Build();

        public static string Of(int value) => (uint)value < CacheSize ? Cache[value] : value.ToString();

        private static string[] Build()
        {
            string[] values = new string[CacheSize];
            for (int i = 0; i < values.Length; i++)
            {
                values[i] = i.ToString();
            }
            return values;
        }
    }
}
