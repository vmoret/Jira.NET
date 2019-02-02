using System;
using System.Collections.Generic;
using System.Web;

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
		
		public JObject Search(string jql, int startAt, int maxResults, string[] fields)
		{
			var data = new JObject();
			data["jql"] = jql;
			data["startAt"] = startAt;
			data["maxResults"] = maxResults;
			data["fields"] = new JArray(fields);
			
			return m_client.ExecuteRequest(Method.POST, "search", data);
		}
		
		string BuildQueryString(params string[] keyOrValues)
		{
			var list = new List<string>();
			for (int i=0; i<keyOrValues.Length-1; i+=2)
			{
				var value = HttpUtility.UrlEncode(keyOrValues[i+1]);
				if (value == null)
				{
					continue;
				}
				var key = HttpUtility.UrlEncode(keyOrValues[i]);
				list.Append(string.Format("{0}={1}", key, value));
			}
			return "?" + string.Join("&", list.ToArray());
		}
		
		#region Issue
		
		/**
		 * Returns a full representation of the issue for the given issue key.
		 * 
		 * An issue JSON consists of the issue key, a collection of fields, a 
		 * link to the workflow transition sub-resource, and (optionally) the HTML
		 * rendered values of any fields that support it (e.g. if wiki syntax is 
		 * enabled for the description or comments).
		 * 
		 * The `fields` param gives a comma-separated list of fields to include
		 * in the response. This can be used to retrieve a subset of fields.
		 * A particular field can be excluded by prefixing it with a minus.
		 * 
		 * The `expand` param is used to include, hidden by default, parts of 
		 * response.
		 * 
		 * The `properties` param is similar to `fields` and specifies a 
		 * comma-separated list of issue properties to include. Unlike `fields`,
		 * properties are not included by default. To include them all send 
		 * `properties=*all`. You can also include only specified properties or
		 * exclude some properties with a minus (-) sign.
		 * 
		 * See also https://docs.atlassian.com/software/jira/docs/api/REST/7.12.0/#api/2/issue-getIssue
		 */
		public JObject GetIssue(string issueIdOrKey, string fields=null, 
		                        string expand=null, string properties=null)
		{
			var query = BuildQueryString("fields", fields, "expand", expand, 
			                             "properties", properties);
			var url = "issue/" + issueIdOrKey + query;
			return m_client.ExecuteRequest(Method.GET, url);
		}
		
		/**
		 * Creates an issue or a sub-task from a JSON representation. 
		 * 
		 * The fields that can be set on create, in either the fields parameter
		 * or the update parameter can be determined using the
		 * /rest/api/2/issue/createmeta resource. If a field is not configured
		 * to appear on the create screen, then it will not be in the createmeta,
		 * and a field validation error will occur if it is submitted.
		 * 
		 * Creating a sub-task is similar to creating a regular issue, with two
		 * important differences:
		 * 
		 * the `issueType` field must correspond to a sub-task issue type
		 * (you can use /issue/createmeta to discover sub-task issue types),
		 * and you must provide a `parent` field in the issue create request
		 * containing the id or key of the parent issue.
		 * 
		 * See also https://docs.atlassian.com/software/jira/docs/api/REST/7.12.0/#api/2/issue-createIssue
		 */
		public JObject CreateIssue(JObject fields=null, JObject update=null)
		{
			var data = new JObject();
			if (fields != null)
			{
				data["fields"] = fields;
			}
			if (update != null)
			{
				data["update"] = update;
			}
			return m_client.ExecuteRequest(Method.POST, "issue", data);
		}
		
		/**
		 * Edits an issue from a JSON representation.
		 * 
		 * The issue can either be updated by setting explicit the field value(s)
		 * or by using an operation to change the field value.
		 * 
		 * Specifying a "field_id": field_value in the "fields" is a shorthand
		 * for a "set" operation in the "update" section.
		 * Field should appear either in "fields" or "update", not in both.
		 * 
		 * The `notifyUsers` param sends the email with notification that the
		 * issue was updated to users that watch it. Admin or project admin
		 * permissions are required to disable the notification.
		 * 
		 * See also https://docs.atlassian.com/software/jira/docs/api/REST/7.12.0/#api/2/issue-editIssue
		 */
		public JObject EditIssue(string issueIdOrKey, JObject fields=null,
		                         JObject update=null, bool notifyUsers=true)
		{
			var data = new JObject();
			if (fields != null)
			{
				data["fields"] = fields;
			}
			if (update != null)
			{
				data["update"] = update;
			}
			var url = "issue/" + issueIdOrKey;
			if (!notifyUsers) 
			{
				url += "?notifyUsers=false";
			}
			return m_client.ExecuteRequest(Method.PUT, url, data);
		}
		
		#endregion
		
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
		
		public bool ChangeStatus(string issueIdOrKey, string statusName)
		{
			try
			{
				var transitions = m_client.ExecuteRequest(Method.GET, "issue/" + issueIdOrKey + "/transitions")["transitions"];
				foreach (var transition in transitions)
				{
					if (transition["name"].ToString() == statusName)
					{
						var data = new JObject();
						data["transition"] = new JObject();
						data["transition"]["id"] = transition["id"];
						m_client.ExecuteRequest(Method.POST, "issue/" + issueIdOrKey + "/transitions", data);
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
		
		public JObject AddComment(string issueIdOrKey, string body)
		{
			var data = new JObject();
			data["body"] = body;
			return m_client.ExecuteRequest(Method.POST, "issue/" + issueIdOrKey + "/comment", data);
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