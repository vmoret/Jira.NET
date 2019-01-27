using System;
using System.Net;

namespace Jira.Remote
{
	interface IAuthenticator
	{
		void Authenticate(WebRequest request);
	}
}
