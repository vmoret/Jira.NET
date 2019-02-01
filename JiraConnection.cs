using System;

using Newtonsoft.Json.Linq;

using Jira.Remote;

namespace Jira
{
	public class JiraConnection
	{
		readonly JiraRestClient m_client;
		
		public JiraConnection(string url, string username, string password)
		{
			if (string.IsNullOrEmpty(url))
			{
				throw new ArgumentOutOfRangeException("url", "Expected a non empty string");
			}
			m_client = new JiraRestClient(url, username, password);
		}
		
		public static string GetKey(JObject obj)
		{
			return obj["key"].ToString();
		}
		
		public JObject GetIssue(string issueKey)
		{
			return m_client.ExecuteRequest(Method.GET, "issue/" + issueKey);
		}
		
		public JObject Search(string jql, int startAt, int maxResults, string[] fields)
		{
			var data = new JObject();
			data["jql"] = jql;
			data["startAt"] = startAt;
			data["maxResults"] = maxResults;
			data["fields"] = new JArray(fields);
			
			return m_client.ExecuteRequest(Method.POST, "search", data);
		}
		
		public JObject CreateIssue(JObject fields)
		{
			var data = new JObject();
			data["fields"] = fields;
			
			return m_client.ExecuteRequest(Method.POST, "issue", data);
		}
		
		public JObject UpdateIssue(string issueKey, JObject fields)
		{
			var data = new JObject();
			data["fields"] = fields;
			return m_client.ExecuteRequest(Method.PUT, "issue/" + issueKey, data);
		}
		
		public JObject LinkIssue(string inwardKey, string outwardKey, IssueLinkType linkType)
		{
			var data = new JObject();
			data["type"] = new JObject();
			data["type"]["name"] = linkType.ToString();
			data["inwardIssue"] = new JObject();
			data["inwardIssue"]["key"] = inwardKey;
			data["outwardIssue"] = new JObject();
			data["outwardIssue"]["key"] = outwardKey;
			return m_client.ExecuteRequest(Method.POST, "issueLink/", data);
		}
		
		public bool ChangeStatus(string issueKey, string statusName)
		{
			try
			{
				var transitions = m_client.ExecuteRequest(Method.GET, "issue/" + issueKey + "/transitions")["transitions"];
				foreach (var transition in transitions)
				{
					if (transition["name"].ToString() == statusName)
					{
						var data = new JObject();
						data["transition"] = new JObject();
						data["transition"]["id"] = transition["id"];
						m_client.ExecuteRequest(Method.POST, "issue/" + issueKey + "/transitions", data);
						return true;
					}
				}
				return false;
			}
			catch (Exception)
			{
				return false;
			}
		}
		
		public JObject AddComment(string issueKey, string body)
		{
			var data = new JObject();
			data["body"] = body;
			return m_client.ExecuteRequest(Method.POST, "issue/" + issueKey + "/comment", data);
		}
		
		public JObject GetIssueWatchers(string issueIdOrKey)
		{
			return m_client.ExecuteRequest(Method.GET, "issue/" + issueIdOrKey + "/watchers");
		}
		
		public JObject AddWatcher(string issueIdOrKey, string username)
		{
			return m_client.ExecuteRequest(Method.POST, "issue/" + issueIdOrKey + "/watchers", username);
		}
		
		public JObject RemoveWatcher(string issueIdOrKey, string username)
		{
			return m_client.ExecuteRequest(Method.DELETE, "issue/" + issueIdOrKey + "/watchers?username=" + username);
		}
	}
}