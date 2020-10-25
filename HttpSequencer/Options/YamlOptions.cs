using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace HttpSequencer
{
	public class YamlOptions
	{
		public KeyValueList header { get; set; }
		public int? client_timeout_seconds { get; set; }
		public string run_id { get; set; }
		public string base_url { get; set; }
		public List<SequenceItem> sequence_items { get; set; }
	}


	public class KeyValueList : List<KeyValuePair<string /*name*/, string /*value*/>> 
	{
		public KeyValueList() { }
		public KeyValueList(List<KeyValuePair<string, string>> toAdd)
        {
			this.AddRange(toAdd);
        }
	}

	//[Flags]
	//public enum LoggerOutput
	//{
	//	Console,
	//	NLog,
	//	All = Console | NLog,
	//}

	//public class Logger
	//{
	//	/// <summary>
	//	/// NLog, Console, All
	//	/// </summary>
	//	public LoggerOutput type { get; set; }
	//	public string on_start_sequence { get; set; }
	//	public string on_error { get; set; }
	//	public string on_success { get; set; }
	//}


	public class SequenceItem
	{
        public string command { get; set; }



		/// <summary>
		/// MAke this a scriban parsed template to render the stack of breadcrumbs describing the progress
		/// </summary>
		public string breadcrumb { get; set; }



		public UrlRequest send { get; set; }
		/// <summary>
		/// Number of retrys to do instantly after the fail
		/// </summary>
		public int? max_instant_retrys;
        
		public HttpArchive har;

        /// <summary>
        /// Number of retrys to do after the other peer sequences are finished. This is a delayed retry
        /// </summary>
        public int? max_delayed_retrys { get; set;}
		/// <summary>
		/// Set to true to treat the model as an array, with each array item being processed individually
		/// Set to false to treat the model as a single object
		/// </summary>
		public bool is_model_array { get; set; }
		public int? take_only_n { get; set; }
		public Run run { get; set; }
		public bool is_abort_on_exception { get; set; }

		public SequenceCheck check { get; set; }
	}

    public class HttpArchive
    {
		/// <summary>
		/// Har file contents as direct yaml
		/// </summary>
		public HarSharp.Har direct { get; set; }
		/// <summary>
		/// File reference to a har file
		/// </summary>
		public string file { get; set; }
		/// <summary>
		/// Url of a har file
		/// </summary>
		public string url { get; set; }
		/// <summary>
		/// Json string holding the har file
		/// </summary>
		public string json { get; set; }
	}

    public class SequenceCheck
    {
		public string pass_template { get; set; }
		public bool IsPass(dynamic model)
        {
			var result = bool.Parse(ScribanUtil.ScribanParse(pass_template, model));
			return result;
		}
    }

    public class Save
	{
		public string folder { get; set; }
		public string content_filename { get; set; }
		public bool content_is_binary { get; set; }
		public string response_filename { get; set; }
	}

	public class Run
	{
		public string exec { get; set; }
		public string args { get; set; }
		public Save save { get; set; }
		public bool use_shell_execute { get; set; }
	}

	public class UrlRequest
	{
		public string http_method { get; set; }
		/// <summary>
		/// To override the one set at the level above
		/// </summary>
		public string base_url { get; set; }
		public string url { get; set; }
		public string content_type { get; set; }
		public KeyValueList query { get; set; }
		public KeyValueList header { get; set; }
		public string body { get; set; }
		public string save_body_filename { get; set; }
		public string save_response_filename { get; set; }

	}

	//public class NextSequenceItemOptions
	//{
	//	/// <summary>
	//	/// Set this to the max number of items to pass to the next command
	//	/// </summary>
	//	public int? stop_after_nth_item { get; set; }
	//	public bool abort_on_exception { get; set; }
	//	public string command { get; set; }
	//	public dynamic replace_response_with { get; set; }
	//	public int? parallel_batch_size { get; set; }
	//}
}
