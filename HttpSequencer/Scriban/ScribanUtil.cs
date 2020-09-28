using System;
using System.Collections.Generic;
using System.Text;
using Scriban;
using Scriban.Runtime;

namespace HttpSequencer
{
    public static class ScribanUtil
    {
		private static string ScribanCleanName(dynamic m) { return m.Name; }
		public static string ScribanParse(string template, dynamic model, MemberRenamerDelegate f = null)
		{
			try
			{
				return ScribanRawParse(template, model, f);
			}
			catch (Exception e)
			{
				var errors = ScribanErrors(template, model, f);
				var msg = $"{e.Message} {template} parse error with model\n";
				Console.Error.WriteLine(msg);
				if (e.InnerException != null)
					Console.Error.WriteLine($"\n{e.InnerException.Message}\n");

				//ExceptionHandler.GenericExceptionHandler(Program.Exceptions, e, msg);
				throw;
			};
		}

		private static string ScribanRawParse(string template, dynamic model, MemberRenamerDelegate f = null) => Template.Parse(template).Render(model, f ?? ScribanCleanName);

		private static List<Scriban.Parsing.LogMessage> ScribanErrors(string template, dynamic model, MemberRenamerDelegate f = null) => Template.Parse(template).Messages;

	}
}
