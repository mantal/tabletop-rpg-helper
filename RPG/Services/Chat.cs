using System.Collections.Generic;

namespace RPG.Services
{
    public class Chat
	{
		private readonly Discord _discord;
		public IEnumerable<string> Logs => _logs;
		private readonly List<string> _logs = new();

		public Chat(Discord discord)
		{
			_discord = discord;
		}

		public void Log(string message)
		{
			_logs.Add(message);
            _ = _discord.Send(message, "[Personnage]");
		}
	}
}
