namespace HexaEngine.Logging
{
    using HexaEngine.Windows;
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Text;

    public class DebugListener : TraceListener
    {
        private readonly BufferedStream stream;

        public DebugListener(string file)
        {
            Application.ApplicationClosing += Application_ApplicationClosing;
            var fileInfo = new FileInfo(file);
            fileInfo.Directory.Create();
            stream = new(File.Create(file));
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
        }

        private void Application_ApplicationClosing(object sender, EventArgs e)
        {
            stream.Flush();
            stream.Close();
        }

        public override void Write(string message)
        {
            stream.Write(Encoding.UTF8.GetBytes(message));
        }

        public override void WriteLine(string message)
        {
            stream.Write(Encoding.UTF8.GetBytes(message + "\n"));
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            WriteLine(e.ExceptionObject);
            if (e.IsTerminating)
            {
                stream.Flush();
                stream.Close();
            }
        }
    }
}