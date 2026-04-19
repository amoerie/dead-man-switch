using System.IO;
using Serilog.Core;
using Serilog.Events;
using Serilog.Formatting.Display;
using Xunit;

namespace DeadManSwitch.Tests.Logging
{
    internal sealed class XUnitTestOutputSink : ILogEventSink
    {
        private readonly ITestOutputHelper _output;
        private readonly MessageTemplateTextFormatter _formatter;

        public XUnitTestOutputSink(ITestOutputHelper output, string outputTemplate)
        {
            _output = output;
            _formatter = new MessageTemplateTextFormatter(outputTemplate);
        }

        public void Emit(LogEvent logEvent)
        {
            if (_output == null)
                return;
            using var writer = new StringWriter();
            _formatter.Format(logEvent, writer);
            _output.WriteLine(writer.ToString().TrimEnd('\r', '\n'));
        }
    }
}
