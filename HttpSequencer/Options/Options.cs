using CommandLine;
using System.Collections.Generic;
using System.Linq;

namespace HttpSequencer
{
	public class Options
	{
		[Option('y', "yaml file", Required = false, HelpText = "Yaml filename")]
		public string YamlFile { get; set; }

		public YamlOptions YamlDirect { get; set; }

		[Option('a', "auth", Required = false, HelpText = "Header authorization value")]
		public string Auth { get; set; }

		public static IEnumerable<string> GetCommands()
		{
			return typeof(Options).GetProperties()
				.Where(prop => prop.IsDefined(typeof(OptionAttribute), false))
				.Select(s => s.Name)
				.ToArray();
		}
	}
}
