using System.Collections.Generic;

namespace RPG.Data
{
    public class Chat
	{
		public IEnumerable<string> Logs => _logs;
		private readonly List<string> _logs = new();

		public void Log(string msg)
			=> _logs.Add(msg);
	}
}
