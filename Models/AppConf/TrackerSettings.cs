namespace JacRed.Models.AppConf
{
    public class TrackerSettings
    {
        public TrackerSettings(string host, bool useproxy = false, LoginSettings login = null, int reqMinute = 20)
        {
            this.host = host;
            this.useproxy = useproxy;
            this.reqMinute = reqMinute;

            if (login != null)
                this.login = login;
        }


        public string host { get; }

        public string cookie { get; set; }

        public bool useproxy { get; set; }

        public int reqMinute { get; set; }

        public int parseDelay => reqMinute >= 60 || reqMinute <= 0 ? 1000 : (60 / reqMinute) * 1000;

        public LoginSettings login { get; set; } = new LoginSettings();
    }
}
