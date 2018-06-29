using System;
using System.ComponentModel;
using System.IO;
using System.Threading;

namespace FileSystemMonitorMicroService
{
    ////////////////////////////////////////////////////////////////////////////////////////////////////
    /// <summary>   A recovering file system watcher. </summary>
    ///
    /// <seealso cref="T:FileSystemMonitorMicroService.BufferingFileSystemWatcher"/>
    ////////////////////////////////////////////////////////////////////////////////////////////////////

    public class RecoveringFileSystemWatcher : BufferingFileSystemWatcher
    {
        /// <summary>   The directory monitor interval. </summary>
        public TimeSpan DirectoryMonitorInterval = TimeSpan.FromMinutes(5);
        /// <summary>   The directory retry interval. </summary>
        public TimeSpan DirectoryRetryInterval = TimeSpan.FromSeconds(5);

        /// <summary>   The monitor timer. </summary>
        private Timer _monitorTimer;

        /// <summary>   True if this object is recovering. </summary>
        private bool _isRecovering;
        /// <summary>   The trace. </summary>
        private static log4net.ILog _trace = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Initializes a new instance of the FileSystemMonitorMicroService.RecoveringFileSystemWatcher
        /// class.
        /// </summary>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public RecoveringFileSystemWatcher()
            : base()
        { }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Initializes a new instance of the FileSystemMonitorMicroService.RecoveringFileSystemWatcher
        /// class.
        /// </summary>
        ///
        /// <param name="path"> Full pathname of the file. </param>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public RecoveringFileSystemWatcher(string path)
            : base(path, "*.*")
        { }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Initializes a new instance of the FileSystemMonitorMicroService.RecoveringFileSystemWatcher
        /// class.
        /// </summary>
        ///
        /// <param name="path">     Full pathname of the file. </param>
        /// <param name="filter">   Specifies the filter. </param>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public RecoveringFileSystemWatcher(string path, string filter)
            : base(path, filter)
        { }

        /// <summary>   To allow consumer to cancel default error handling. </summary>
        private EventHandler<FileWatcherErrorEventArgs> _onErrorHandler = null;
        /// <summary>   Occurs when Error. </summary>
        public new event EventHandler<FileWatcherErrorEventArgs> Error
        {
            add => _onErrorHandler += value;
            remove => _onErrorHandler -= value;
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Gets or sets a value indicating whether the raising events is enabled. </summary>
        ///
        /// <value> True if enable raising events, false if not. </value>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public new bool EnableRaisingEvents
        {
            get => base.EnableRaisingEvents;
            set
            {
                if (value == EnableRaisingEvents)
                    return;

                base.EnableRaisingEvents = value;
                if (EnableRaisingEvents)
                {
                    base.Error += BufferingFileSystemWatcher_Error;
                    Start();
                }
                else
                {
                    base.Error -= BufferingFileSystemWatcher_Error;
                }
            }
        }

        /// <summary>   Starts this object. </summary>
        private void Start()
        {
            _trace.Debug("");

            try
            {
                _monitorTimer = new Timer(_monitorTimer_Elapsed);

                Disposed += (_, __) =>
                {
                    _monitorTimer.Dispose();
                    _trace.Info("Obeying cancel request");
                };

                ReStartIfNeccessary(TimeSpan.Zero);
            }
            catch (Exception ex)
            {
                _trace.Error($"Unexpected error: {ex}");
                throw;
            }
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Monitor timer elapsed. </summary>
        ///
        /// <exception cref="DirectoryNotFoundException">   Thrown when the requested directory is not
        ///                                                 present. </exception>
        ///
        /// <param name="state">    The state. </param>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        private void _monitorTimer_Elapsed(object state)
        {
            _trace.Debug("!!");
            _trace.Info($"Watching:{Path}");

            try
            {
                if (!Directory.Exists(Path))
                {
                    throw new DirectoryNotFoundException($"Directory not found {Path}");
                }

                _trace.Info($"Directory {Path} accessibility is OK.");
                if (!EnableRaisingEvents)
                {
                    EnableRaisingEvents = true;
                    if (_isRecovering)
                        _trace.Warn("<= Watcher recovered");
                }

                ReStartIfNeccessary(DirectoryMonitorInterval);
            }
            catch (Exception ex) when (ex is FileNotFoundException || ex is DirectoryNotFoundException)
            {
                //Handles race condition too: Path loses accessibility between .Exists() and .EnableRaisingEvents 
                if (ExceptionWasHandledByCaller(ex))
                    return;

                if (_isRecovering)
                {
                    _trace.Warn("...retrying");
                }
                else
                {
                    _trace.Warn($@"=> Directory {Path} Is Not accessible.
                                 - Will try to recover automatically in {DirectoryRetryInterval}!");
                    _isRecovering = true;
                }

                EnableRaisingEvents = false;
                _isRecovering = true;
                ReStartIfNeccessary(DirectoryRetryInterval);
            }
            catch (Exception ex)
            {
                _trace.Error($"Unexpected error: {ex}");
                throw;
            }
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Re start if neccessary. </summary>
        ///
        /// <param name="delay">    The delay. </param>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        private void ReStartIfNeccessary(TimeSpan delay)
        {
            _trace.Debug("");
            try
            {
                _monitorTimer.Change(delay, Timeout.InfiniteTimeSpan);
            }
            catch (ObjectDisposedException)
            { } //ignore timer disposed     
        }

       ////////////////////////////////////////////////////////////////////////////////////////////////////
       /// <summary>    Event handler. Called by BufferingFileSystemWatcher for error events. </summary>
       ///
       /// <exception cref="GetException">  Thrown when a Get error condition occurs. </exception>
       ///
       /// <param name="sender">    Source of the event. </param>
       /// <param name="e">         Error event information. </param>
       ////////////////////////////////////////////////////////////////////////////////////////////////////

       private void BufferingFileSystemWatcher_Error(object sender, ErrorEventArgs e)
        {
            //These exceptions have the same HResult
            var NetworkNameNoLongerAvailable = -2147467259; //occurs on network outage
            var AccessIsDenied = -2147467259; //occurs after directory was deleted

            _trace.Debug("");

            var ex = e.GetException();
            if (ExceptionWasHandledByCaller(e.GetException()))
                return;

            //The base FSW does set .EnableRaisingEvents=False AFTER raising OnError()
            EnableRaisingEvents = false;

            if (ex is InternalBufferOverflowException || ex is EventQueueOverflowException)
            {
                _trace.Warn(ex.Message);
                _trace.Error(@"This should Not happen with short event handlers!
                             - Will recover automatically.");
                ReStartIfNeccessary(DirectoryRetryInterval);
            }
            else if (ex is Win32Exception && (ex.HResult == NetworkNameNoLongerAvailable | ex.HResult == AccessIsDenied))
            {
                _trace.Warn(ex.Message);
                _trace.Warn("Will try to recover automatically!");
                ReStartIfNeccessary(DirectoryRetryInterval);
            }
            else
            {
                _trace.Error($@"Unexpected error: {ex}
                             - Watcher is disabled!");
                throw ex;
            }
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Exception was handled by caller. </summary>
        ///
        /// <param name="ex">   The ex. </param>
        ///
        /// <returns>   True if it succeeds, false if it fails. </returns>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        private bool ExceptionWasHandledByCaller(Exception ex)
        {
            //Allow consumer to handle error
            if (_onErrorHandler != null)
            {
                FileWatcherErrorEventArgs e = new FileWatcherErrorEventArgs(ex);
                InvokeHandler(_onErrorHandler, e);
                return e.Handled;
            }

            return false;
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Executes the handler on a different thread, and waits for the result. </summary>
        ///
        /// <param name="eventHandler"> The event handler. </param>
        /// <param name="e">            File watcher error event information. </param>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        private void InvokeHandler(EventHandler<FileWatcherErrorEventArgs> eventHandler, FileWatcherErrorEventArgs e)
        {
            if (eventHandler != null)
            {
                if (SynchronizingObject?.InvokeRequired == true)
                    SynchronizingObject.BeginInvoke(eventHandler, new object[] { this, e });
                else
                    eventHandler(this, e);
            }
        }
    }
}
