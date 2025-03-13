using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace DevilStudio
{
    public class OAuthHelper
    {
        private HttpListener _listener;

        public async Task<string> StartLocalHttpServerAsync()
        {
            _listener = new HttpListener();
            _listener.Prefixes.Add("http://localhost:5000/callback/");
            _listener.Start();

            var context = await _listener.GetContextAsync();


            string code = context.Request.QueryString["code"];
            string responseString = "<html><body>Authorized Successfully. Now You can close this window</body></html>";
            byte[] buffer = Encoding.UTF8.GetBytes(responseString);
            context.Response.ContentLength64 = buffer.Length;
            context.Response.OutputStream.Write(buffer, 0, buffer.Length);
            context.Response.OutputStream.Close();
            context.Response.Close();

            _listener.Stop();
            return code;
        }
    }
}
