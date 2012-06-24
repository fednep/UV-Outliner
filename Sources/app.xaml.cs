using System;
using System.Windows;
using System.Data;
using System.Xml;
using System.Configuration;
using System.Windows.Threading;
using UVOutliner.UnhandledException;
using System.Reflection;


namespace UVOutliner
{
    /// <summary>
    /// Interaction logic for app.xaml
    /// </summary>

    public partial class app : Application
    {
        private string __ExecutableFile;
        private bool __NoAutoLoad;

        void AppStartup(object sender, StartupEventArgs args)
        {
            if (args.Args.Length > 0)
            {
                if (args.Args[0] == "/noautoload")
                    __NoAutoLoad = true;
                else
                {
                    Properties["FileNameToOpen"] = args.Args[0];
                }
            }

            __ExecutableFile = Environment.GetCommandLineArgs()[0];
    
            MainWindow mainWindow = new MainWindow();
            mainWindow.Dispatcher.UnhandledException += new System.Windows.Threading.DispatcherUnhandledExceptionEventHandler(Dispatcher_UnhandledException);
            mainWindow.Show();
        }

       public void Dispatcher_UnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {           
            wnd_Exception exceptionWindow = new wnd_Exception();
            exceptionWindow.ShowException(e.Exception.Message, e.Exception);
            e.Handled = true;
        }

        void app_Startup(object sender, StartupEventArgs e)
        {

        }

        public bool NoAutoLoad
        {
            get
            {
                return __NoAutoLoad;
            }
        }

        public string ExecutableFile
        {
            get
            {
                return __ExecutableFile;
            }
        }

        public static Version CurrentVersion
        {
            get
            {
                Assembly assem = Assembly.GetEntryAssembly();
                AssemblyName assemName = assem.GetName();
                return assemName.Version;
            }
        }

        #region DoEvents
        private static DispatcherOperationCallback exitFrameCallback = new DispatcherOperationCallback(ExitFrame);

        /// <summary>
        /// Processes all UI messages currently in the message queue.
        /// </summary>
        public static void DoEvents()
        {
            DoEvents(Dispatcher.CurrentDispatcher);
        }

        /// <summary>
        /// Processes all UI messages currently in the message queue.
        /// </summary>
        public static void DoEvents(Dispatcher dispatcher)
        {
            // Create new nested message pump.
            DispatcherFrame nestedFrame = new DispatcherFrame();
            // Dispatch a callback to the current message queue, when getting called,
            // this callback will end the nested message loop.
            // note that the priority of this callback should be lower than the that of UI event messages.
            DispatcherOperation exitOperation = dispatcher.BeginInvoke(
                                                  DispatcherPriority.Background, exitFrameCallback, nestedFrame);
            // pump the nested message loop, the nested message loop will
            // immediately process the messages left inside the message queue.
            Dispatcher.PushFrame(nestedFrame);
            // If the "exitFrame" callback doesn't get finished, Abort it.
            if (exitOperation.Status != DispatcherOperationStatus.Completed)
                exitOperation.Abort();
        }

        private static Object ExitFrame(Object state)
        {
            DispatcherFrame frame = state as DispatcherFrame;

            // Exit the nested message loop.
            frame.Continue = false;
            return null;
        }
        #endregion
    }
}