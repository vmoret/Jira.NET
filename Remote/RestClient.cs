using System;
using System.IO;
using System.Net;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Jira.Remote
{
	class RestClient
	{
		readonly string m_url;
		
		public IAuthenticator Authenticator { get; set; }
		
		public RestClient(string url)
		{
			m_url = url.EndsWith("/", StringComparison.InvariantCulture) ? url : url + "/";
		}
		
		string ReallyExecuteRequest(Method method, string url, string requestBody = null)
		{
     		var request = WebRequest.Create(url) as HttpWebRequest;
            request.ContentType = "application/json";
            request.Method = method.ToString().ToUpper();
            
            if (requestBody != null)
            {
                using (var writer = new StreamWriter(request.GetRequestStream()))
                {
                    writer.Write(requestBody);
                }
            }
            
            Authenticator.Authenticate(request);
            
            var response = request.GetResponse() as HttpWebResponse;
     
            string responseBody = string.Empty;
            using (var reader = new StreamReader(response.GetResponseStream()))
            {
                responseBody = reader.ReadToEnd();
            }
     
            return responseBody;
		}
		
		public JObject ExecuteRequest(Method method, string resource, string requestBody = null)
		{
			var url = string.Concat(m_url, resource);
			try
			{
				return JsonConvert.DeserializeObject<JObject>(ReallyExecuteRequest(method, url, requestBody));
			}
			catch (Exception ex)
			{
				var err = new JObject();
				err["message"] = ex.Message;
				return err;
			}
		}
	}
}
