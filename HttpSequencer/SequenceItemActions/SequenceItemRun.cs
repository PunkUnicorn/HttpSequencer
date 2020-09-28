using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace HttpSequencer.SequenceItemActions
{
    public class SequenceItemRun : ISequenceItemAction
    {
        private readonly SequenceItem sequenceItem;
        private readonly RunState state;
        private readonly Options commandLineOptions;
        private readonly YamlOptions yamlOptions;

        public SequenceItemRun(RunState state, SequenceItem sequence)
        {
            this.sequenceItem = sequence;
            this.state = state;
        }

		//public bool IsFail { get; private set; }

        public async Task<object> Action(object model)
        {
            if (this.sequenceItem.run == null)
                return null;

            try
            { 
                this.state.Log.Info($"Running {this.sequenceItem.run.exec}...");

                var scribanModel = new { run_id = this.state.RunId, command_args = commandLineOptions, model, sequence_item = this.sequenceItem };

                // Exec run external program
                var workingExec = ScribanUtil.ScribanParse(this.sequenceItem?.run?.exec ?? "", scribanModel);
                var workingArgs = ScribanUtil.ScribanParse(this.sequenceItem?.run?.args ?? "", scribanModel);

                this.state.ProgressLog.Progress($" running exec '{workingExec}', with args '{workingArgs}'...");

                var itsStandardInput = (model != null)
                    ? JsonConvert.SerializeObject(model)
                    : "";

                var execReturn = ProcessExecute(this.sequenceItem.run, workingExec, workingArgs, itsStandardInput);
                var responseContentLength = execReturn?.Length ?? 0;
                var responseContent = execReturn;

				var responseModel = SequenceItemStatic.GetResponseItems(this.state, this.sequenceItem, execReturn);

				return responseModel;
			}
			catch (Exception e)
            {
				//GenericExceptionHandler(e, o?.Command ?? ("-" + nameof(ProcessCommandItem)));
				state.Exceptions.Add( (e.Message, e) );
				if (this.sequenceItem.is_abort_on_exception)
					throw;
			}

			return null;

		}

		/// <summary>
		/// Execute an external program. It gets the previous_response through stdin, and gives its output through stdout, and errors through stderr
		/// </summary>
		/// <param name="run">The yaml run: block</param>
		/// <param name="workingExec">Name of the executable to run</param>
		/// <param name="workingArgs">The executables command arguments</param>
		/// <param name="inputmodel">Data to pass through to the running program as its stdin</param>
		/// <returns>The contents of the executables stdout</returns>
		private string ProcessExecute(Run run, string workingExec, string workingArgs, string inputmodel)
		{
			var outputBuilder = new StringBuilder();
			var errorOutputBuilder = new StringBuilder();

			var processStartInfo = new ProcessStartInfo();
			processStartInfo.CreateNoWindow = true;
			processStartInfo.RedirectStandardOutput = true;
			processStartInfo.RedirectStandardInput = true;
			processStartInfo.RedirectStandardError = true;

			processStartInfo.UseShellExecute = run.use_shell_execute;
			processStartInfo.Arguments = workingArgs;
			processStartInfo.FileName = workingExec;

			var process = new Process();
			process.StartInfo = processStartInfo;
			process.EnableRaisingEvents = true;
			process.OutputDataReceived += new DataReceivedEventHandler
			(
				delegate (object sender, DataReceivedEventArgs e) { outputBuilder.Append(e.Data); }
			);
			process.ErrorDataReceived += new DataReceivedEventHandler
			(
				delegate (object sender, DataReceivedEventArgs e) { errorOutputBuilder.Append(e.Data); }
			);

			process.Start();

			using (var myStreamWriter = process.StandardInput)
			{
				// Start the process

				process.BeginOutputReadLine();
				process.BeginErrorReadLine();

				myStreamWriter.Write(inputmodel);
				myStreamWriter.Flush();
			}

			process.WaitForExit();
			process.CancelOutputRead();
			process.CancelErrorRead();

			var exitCode = process.ExitCode;

			// Use the output, and propagate errors
			string output = outputBuilder.ToString();
			if (errorOutputBuilder.Length > 0)
			{
				var errorDetail = errorOutputBuilder.ToString();
				this.state.Log.Error(errorDetail);
				throw new Exception(errorDetail);
			}

			if (exitCode != 0)
			{
				var errorMsg = $"Failed due to '{workingExec}' returning non-zero exitcode: {exitCode}";
				this.state.Log.Error(errorMsg);
				//this.state.ProgressLog.Fail(this.sequenceItem);
				throw new Exception(errorMsg);
			}
			return output;
		}
	}
}