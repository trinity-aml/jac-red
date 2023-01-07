namespace JacRed.Models.AppConf
{
    public class TrackerSettings
    {
        public TrackerSettings(string host, bool useproxy = false, LoginSettings login = null)
        {
            this.host = host;
            this.useproxy = useproxy;

            if (login != null)
                this.login = login;
        }


        public string host { get; set; }

        public bool useproxy { get; set; }

        /// <summary>
        /// 5 запросов в минуту
        /// </summary>
        public int parseDelay { get; set; } = 12_000;

        public LoginSettings login { get; set; } = new LoginSettings();
    }
}
