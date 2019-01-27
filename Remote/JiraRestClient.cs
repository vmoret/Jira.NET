using System;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Jira.Remote
{
	class JiraRestClient
	{
		readonly RestClient m_restClient;
		
		public JiraRestClient(string url, string username, string password)
		{
			url = url.EndsWith("/", StringComparison.InvariantCulture) ? url : url + "/";
			
			m_restClient = new RestClient(url);
			m_restClient.Authenticator = new HttpBasicAuthenticator(username, password);
		}
		
		public JObject ExecuteRequest(Method method, string resource, object requestBody = null)
		{
			return method != Method.GET 
				? m_restClient.ExecuteRequest(method, resource, JsonConvert.SerializeObject(requestBody))
				: m_restClient.ExecuteRequest(method, resource);
		}
	}
}
