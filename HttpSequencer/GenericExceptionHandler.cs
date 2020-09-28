using System;
using System.Collections.Generic;
using System.Text;

namespace HttpSequencer
{
    public static class ExceptionHandler
    {
		public static void GenericExceptionHandler(List<(string/*hint*/, Exception)> exceptions, Exception e, string hint)
		{
			var addItem = (hint, e);
			exceptions.Add(addItem);
			Console.Error.WriteLine($"\n{e.Message}\n{hint}\n{e.ToString()}\n");
			if (e.InnerException != null)
				Console.Error.WriteLine(e.InnerException.ToString());
		}
	}
}
