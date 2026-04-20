using System;
using System.Diagnostics;

namespace Infrastructure.Managers
{
    public class FacebookManager
    {
        private readonly string r_AppId;
        private string m_UserName = null;
        public string UserName { get { return m_UserName; } }

        public FacebookManager(string i_AppId)
        {
            r_AppId = i_AppId;
        }

        public void Login()
        {
            string loginUrl = $"https://www.facebook.com/v19.0/dialog/oauth?client_id={r_AppId}&redirect_uri=https://www.facebook.com/connect/login_success.html&response_type=token";

            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = loginUrl,
                    UseShellExecute = true
                });

                // Simulate a successful login after opening the browser.
                m_UserName = "Facebook User"; 
            }
            catch (Exception)
            {
                // Fallback for systems where opening browser fails
            }
        }
    }
}
