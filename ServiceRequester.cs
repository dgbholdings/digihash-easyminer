using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace DigiHash
{
    public class ServiceRequester
    {

        private HttpClient CreateClient()
        {
            var client = new HttpClient()
            {
                //BaseAddress = new Uri("http://dev-window.cloudapp.net:3000")
                BaseAddress = new Uri("https://easyminer.digihash.co")
            };

            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            var byteArray = Encoding.ASCII.GetBytes("digihash-admin:wp545648");
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));

            return client;
        }

        public async void Get(string action, KeyValuePair<string, string>[] parameters, Action<string> success, Action<string> fail)
        {
            using (var client = this.CreateClient())
            {                
                //Concate parameter to action                
                if (parameters != null)
                {
                    var content = new FormUrlEncodedContent(parameters);
                    action = action + "?" + content.ReadAsStringAsync().Result;
                }

                try
                {
                    var response = await client.GetAsync(action);
                    var content = await response.Content.ReadAsStringAsync();
                    if (response.IsSuccessStatusCode)
                        success(content);
                    else
                        fail(string.Format("Error Code: {0}, Message: {1}", response.StatusCode, content));
                }
                catch(Exception exception)
                {
                    fail(exception.Message);
                }
            }
        }

        public async void Post(string action, string json, Action<string> success, Action<string> fail)
        {
            using (var client = this.CreateClient())
            {
                try
                {
                    var response = await client.PostAsync(action, new StringContent(json, Encoding.UTF8, "application/json"));
                    var content = await response.Content.ReadAsStringAsync();
                    if (response.IsSuccessStatusCode)
                        success(content);
                    else
                        fail(content);
                }
                catch (Exception exception)
                {
                    fail(exception.Message);
                }
            }
        }

    }
}
