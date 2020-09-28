using SharpYaml.Serialization;
using System;
using System.IO;
using System.Reflection;

namespace YamlExtensions
{
    public static class YamlLoad
	{
		/// <summary>
		/// Loads a yaml file into POCO of type Y
		/// </summary>
		/// <typeparam name="Y">POCO representing structure of the yaml</typeparam>
		/// <param name="filename">yaml filename. If null, will use a default of a yaml file with the same name as the calling assembly</param>
		/// <returns>Instance of Y</returns>
		static public Y Load<Y>(string filename) where Y : class, new()
		{
			try
			{
				var exePath = Assembly.GetExecutingAssembly().CodeBase;

				var fallbackYamlFilename
					= Path.ChangeExtension(
						Path.GetFileName(exePath), "yaml");

				var useFilename = filename != null
					? filename
					: File.Exists(fallbackYamlFilename)
						? fallbackYamlFilename
						: throw new ArgumentException($"{fallbackYamlFilename} not found.");

				return LoadYamlUnguarded<Y>(useFilename);
			}
			catch (Exception e)
			{
				Console.Error.WriteLine(e.Message);
				return null;
			}
		}

		static private T LoadYamlUnguarded<T>(string filename)
		{
			using (var reader = new StringReader(File.ReadAllText(filename)))
			{
				var yaml = new YamlStream();

				yaml.Load(reader);

				var mapping = (YamlMappingNode)yaml.Documents[0].RootNode;

				return mapping.ToPoco<T>();
			}
		}

	}
}
