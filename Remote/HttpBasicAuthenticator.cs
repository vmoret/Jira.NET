using System;
using System.Net;
using System.Text;

namespace Jira.Remote
{
	public class HttpBasicAuthenticator : IAuthenticator
	{
		private readonly string m_authorization;
		
		public HttpBasicAuthenticator(string username, string password)
		{
			m_authorization = "Basic " + GetBase64EncodedCredentials(username, password);
		}
		
		public void Authenticate(WebRequest request)
		{
			request.Headers.Add("Authorization", m_authorization);
		}
		
		string GetBase64EncodedCredentials(string username, string password)
        {
            string mergedCredentials = string.Format("{0}:{1}", username, password);
            byte[] byteCredentials = Encoding.UTF8.GetBytes(mergedCredentials);
            return Convert.ToBase64String(byteCredentials);
        }
	}
}
