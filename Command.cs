using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PulsarModLoader.Chat.Commands.CommandRouter;

namespace LevelScanner
{
	internal class Command : ChatCommand
	{
		public override string[] CommandAliases()
		{
			return new string[] { "levelscanner" };
		}

		public override string Description() => "show the interior?";

		public override void Execute(string arguments)
		{
			NewScanner.ShowInterior.Value = !NewScanner.ShowInterior.Value;
		}
	}
}
