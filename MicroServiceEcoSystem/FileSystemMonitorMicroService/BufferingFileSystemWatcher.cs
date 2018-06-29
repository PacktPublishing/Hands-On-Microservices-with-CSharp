using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FileSystemMonitorMicroService
{
    ////////////////////////////////////////////////////////////////////////////////////////////////////
    /// <summary>
    /// <devdoc>
    /// Features:
    /// - Buffers FileSystemWatcher events in a BlockinCollection to prevent
    /// InternalBufferOverflowExceptions.
    /// - Does not break the original FileSystemWatcher API.
    /// - Supports reporting existing files via a new Existed event.
    /// - Supports sorting events by oldest (existing) file first.
    /// - Supports an new event Any reporting any FSW change.
    /// - Offers the Error event in Win Forms designer (via [Browsable[true)]
    /// - Does not prevent duplicate files occurring.
    /// Notes:
    ///   We contain FilSystemWatcher to follow the principal composition over inheritance and
    ///   because System.IO.FileSystemWatcher is not designed to be inherited from: Event handlers
    ///   and Dispose(disposing) are not virtual.
    /// </devdoc>
    /// </summary>
    ///
    /// <seealso cref="T:System.ComponentModel.Component"/>
    ////////////////////////////////////////////////////////////////////////////////////////////////////

    public class BufferingFileSystemWatcher : Component
    {
        /// <summary>   The contained fsw. </summary>
        private FileSystemWatcher _containedFSW;

        /// <summary>   The on existed handler. </summary>
        private FileSystemEventHandler _onExistedHandler;
        /// <summary>   The on all changes handler. </summary>
        private FileSystemEventHandler _onAllChangesHandler;
        /// <summary>   The on created handler. </summary>
        private FileSystemEventHandler _onCreatedHandler;
        /// <summary>   The on changed handler. </summary>
        private FileSystemEventHandler _onChangedHandler;
        /// <summary>   The on deleted handler. </summary>
        private FileSystemEventHandler _onDeletedHandler;
        /// <summary>   The on renamed handler. </summary>
        private RenamedEventHandler _onRenamedHandler;
        /// <summary>   The on error handler. </summary>
        private ErrorEventHandler _onErrorHandler;

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// We use a single buffer for all change types. Alternatively we could use one buffer per event
        /// type, costing additional enumerate tasks.
        /// </summary>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        private BlockingCollection<FileSystemEventArgs> _fileSystemEventBuffer;
        /// <summary>   The cancellation token source. </summary>
        private CancellationTokenSource _cancellationTokenSource;

        #region Contained FileSystemWatcher

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Initializes a new instance of the FileSystemMonitorMicroService.BufferingFileSystemWatcher
        /// class.
        /// </summary>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public BufferingFileSystemWatcher()
        {
            _containedFSW = new FileSystemWatcher();
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Initializes a new instance of the FileSystemMonitorMicroService.BufferingFileSystemWatcher
        /// class.
        /// </summary>
        ///
        /// <param name="path"> The full pathname of the file. </param>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public BufferingFileSystemWatcher(string path)
        {
            _containedFSW = new FileSystemWatcher(path, "*.*");
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Initializes a new instance of the FileSystemMonitorMicroService.BufferingFileSystemWatcher
        /// class.
        /// </summary>
        ///
        /// <param name="path">     The full pathname of the file. </param>
        /// <param name="filter">   The filter. </param>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public BufferingFileSystemWatcher(string path, string filter)
        {
            _containedFSW = new FileSystemWatcher(path, filter);
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Gets or sets a value indicating whether the raising events is enabled. </summary>
        ///
        /// <value> True if enable raising events, false if not. </value>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public bool EnableRaisingEvents
        {
            get => _containedFSW.EnableRaisingEvents;
            set
            {
                if (_containedFSW.EnableRaisingEvents == value) return;

                StopRaisingBufferedEvents();
                _cancellationTokenSource = new CancellationTokenSource();

                //We EnableRaisingEvents, before NotifyExistingFiles
                //  to prevent missing any events
                //  accepting more duplicates (which may occur anyway).
                _containedFSW.EnableRaisingEvents = value;
                if (value)
                    RaiseBufferedEventsUntilCancelled();
            }
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Gets or sets the filter. </summary>
        ///
        /// <value> The filter. </value>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public string Filter
        {
            get => _containedFSW.Filter;
            set => _containedFSW.Filter = value;
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Gets or sets a value indicating whether the subdirectories should be included.
        /// </summary>
        ///
        /// <value> True if include subdirectories, false if not. </value>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public bool IncludeSubdirectories
        {
            get => _containedFSW.IncludeSubdirectories;
            set => _containedFSW.IncludeSubdirectories = value;
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Gets or sets the size of the internal buffer. </summary>
        ///
        /// <value> The size of the internal buffer. </value>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public int InternalBufferSize
        {
            get => _containedFSW.InternalBufferSize;
            set => _containedFSW.InternalBufferSize = value;
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Gets or sets the notify filter. </summary>
        ///
        /// <value> The notify filter. </value>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public NotifyFilters NotifyFilter
        {
            get => _containedFSW.NotifyFilter;
            set => _containedFSW.NotifyFilter = value;
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Gets or sets the full pathname of the file. </summary>
        ///
        /// <value> The full pathname of the file. </value>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public string Path
        {
            get => _containedFSW.Path;
            set => _containedFSW.Path = value;
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Gets or sets the synchronizing object. </summary>
        ///
        /// <value> The synchronizing object. </value>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public ISynchronizeInvoke SynchronizingObject
        {
            get => _containedFSW.SynchronizingObject;
            set => _containedFSW.SynchronizingObject = value;
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Gets or sets the <see cref="T:System.ComponentModel.ISite" /> of the
        /// <see cref="T:System.ComponentModel.Component" />.
        /// </summary>
        ///
        /// <value>
        /// The <see cref="T:System.ComponentModel.ISite" /> associated with the
        /// <see cref="T:System.ComponentModel.Component" />, or <see langword="null" /> if the
        /// <see cref="T:System.ComponentModel.Component" /> is not encapsulated in an
        /// <see cref="T:System.ComponentModel.IContainer" />, the
        /// <see cref="T:System.ComponentModel.Component" /> does not have an
        /// <see cref="T:System.ComponentModel.ISite" /> associated with it, or the
        /// <see cref="T:System.ComponentModel.Component" /> is removed from its
        /// <see cref="T:System.ComponentModel.IContainer" />.
        /// </value>
        ///
        /// <seealso cref="P:System.ComponentModel.Component.Site"/>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public override ISite Site
        {
            get => _containedFSW.Site;
            set => _containedFSW.Site = value;
        }

        #endregion

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Gets or sets a value indicating whether the order by oldest first. </summary>
        ///
        /// <value> True if order by oldest first, false if not. </value>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        [DefaultValue(false)]
        public bool OrderByOldestFirst { get; set; } = false;

        /// <summary>   Size of the event queue. </summary>
        private int _eventQueueSize = int.MaxValue;

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Gets or sets the event queue capacity. </summary>
        ///
        /// <value> The event queue capacity. </value>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public int EventQueueCapacity
        {
            get => _eventQueueSize;
            set => _eventQueueSize = value;
        }

        #region New BufferingFileSystemWatcher specific events
        /// <summary>   Occurs when Existed. </summary>
        public event FileSystemEventHandler Existed
        {
            add => _onExistedHandler += value;
            remove => _onExistedHandler -= value;
        }

        /// <summary>   Occurs when All. </summary>
        public event FileSystemEventHandler All
        {
            add
            {
                if (_onAllChangesHandler == null)
                {
                    _containedFSW.Created += BufferEvent;
                    _containedFSW.Changed += BufferEvent;
                    _containedFSW.Renamed += BufferEvent;
                    _containedFSW.Deleted += BufferEvent;
                }
                _onAllChangesHandler += value;
            }
            remove
            {
                _containedFSW.Created -= BufferEvent;
                _containedFSW.Changed -= BufferEvent;
                _containedFSW.Renamed -= BufferEvent;
                _containedFSW.Deleted -= BufferEvent;
                _onAllChangesHandler -= value;
            }
        }

        #endregion

        #region Standard FSW events

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// - The _fsw events add to the buffer.
        /// - The public events raise from the buffer to the consumer.
        /// </summary>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public event FileSystemEventHandler Created
        {
            add
            {
                if (_onCreatedHandler == null)
                    _containedFSW.Created += BufferEvent;
                _onCreatedHandler += value;
            }
            remove
            {
                _containedFSW.Created -= BufferEvent;
                _onCreatedHandler -= value;
            }
        }

        /// <summary>   Occurs when Changed. </summary>
        public event FileSystemEventHandler Changed
        {
            add
            {
                if (_onChangedHandler == null)
                    _containedFSW.Changed += BufferEvent;
                _onChangedHandler += value;
            }
            remove
            {
                _containedFSW.Changed -= BufferEvent;
                _onChangedHandler -= value;
            }
        }

        /// <summary>   Occurs when Deleted. </summary>
        public event FileSystemEventHandler Deleted
        {
            add
            {
                if (_onDeletedHandler == null)
                    _containedFSW.Deleted += BufferEvent;
            }
            remove
            {
                _containedFSW.Deleted -= BufferEvent;
                _onDeletedHandler -= value;
            }
        }

        /// <summary>   Occurs when Renamed. </summary>
        public event RenamedEventHandler Renamed
        {
            add
            {
                if (_onRenamedHandler == null)
                    _containedFSW.Renamed += BufferEvent;
                _onRenamedHandler += value;
            }
            remove
            {
                _containedFSW.Renamed -= BufferEvent;
                _onRenamedHandler -= value;
            }
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Buffer event. </summary>
        ///
        /// <param name="_">    The. </param>
        /// <param name="e">    File system event information. </param>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        private void BufferEvent(object _, FileSystemEventArgs e)
        {
            if (!_fileSystemEventBuffer.TryAdd(e))
            {
                var ex = new EventQueueOverflowException($"Event queue size {_fileSystemEventBuffer.BoundedCapacity} events exceeded.");
                InvokeHandler(_onErrorHandler, new ErrorEventArgs(ex));
            }
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Stops raising buffered events. </summary>
        ///
        /// <param name="_">    (Optional) The. </param>
        /// <param name="__">   (Optional) Event information. </param>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        private void StopRaisingBufferedEvents(object _ = null, EventArgs __ = null)
        {
            _cancellationTokenSource?.Cancel();
            _fileSystemEventBuffer = new BlockingCollection<FileSystemEventArgs>(_eventQueueSize);
        }

        /// <summary>   Occurs when Error. </summary>
        public event ErrorEventHandler Error
        {
            add
            {
                if (_onErrorHandler == null)
                    _containedFSW.Error += BufferingFileSystemWatcher_Error;
                _onErrorHandler += value;
            }
            remove
            {
                if (_onErrorHandler == null)
                    _containedFSW.Error -= BufferingFileSystemWatcher_Error;
                _onErrorHandler -= value;
            }
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Event handler. Called by BufferingFileSystemWatcher for error events. </summary>
        ///
        /// <param name="sender">   Source of the event. </param>
        /// <param name="e">        Error event information. </param>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        private void BufferingFileSystemWatcher_Error(object sender, ErrorEventArgs e)
        {
            InvokeHandler(_onErrorHandler, e);
        }
        #endregion

        /// <summary>   Raises the buffered events until cancelled event. </summary>
        private void RaiseBufferedEventsUntilCancelled()
        {
            Task.Run(() =>
            {
                try
                {
                    if (_onExistedHandler != null || _onAllChangesHandler != null)
                        NotifyExistingFiles();

                    foreach (FileSystemEventArgs e in _fileSystemEventBuffer.GetConsumingEnumerable(_cancellationTokenSource.Token))
                    {
                        if (_onAllChangesHandler != null)
                            InvokeHandler(_onAllChangesHandler, e);
                        else
                        {
                            switch (e.ChangeType)
                            {
                                case WatcherChangeTypes.Created:
                                    InvokeHandler(_onCreatedHandler, e);
                                    break;
                                case WatcherChangeTypes.Changed:
                                    InvokeHandler(_onChangedHandler, e);
                                    break;
                                case WatcherChangeTypes.Deleted:
                                    InvokeHandler(_onDeletedHandler, e);
                                    break;
                                case WatcherChangeTypes.Renamed:
                                    InvokeHandler(_onRenamedHandler, e as RenamedEventArgs);
                                    break;
                            }
                        }
                    }
                }
                catch (OperationCanceledException)
                { } //ignore
                catch (Exception ex)
                {
                    BufferingFileSystemWatcher_Error(this, new ErrorEventArgs(ex));
                }
            });
        }

        /// <summary>   Notifies the existing files. </summary>
        private void NotifyExistingFiles()
        {
            var searchSubDirectoriesOption = (IncludeSubdirectories) ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
            if (OrderByOldestFirst)
            {
                var sortedFileNames = from fi in new DirectoryInfo(Path).GetFiles(Filter, searchSubDirectoriesOption)
                                      orderby fi.LastWriteTime ascending
                                      select fi.Name;
                foreach (var fileName in sortedFileNames)
                {
                    InvokeHandler(_onExistedHandler, new FileSystemEventArgs(WatcherChangeTypes.All, Path, fileName));
                    InvokeHandler(_onAllChangesHandler, new FileSystemEventArgs(WatcherChangeTypes.All, Path, fileName));
                }
            }
            else
            {
                foreach (var fileName in Directory.EnumerateFiles(Path, Filter, searchSubDirectoriesOption)
                    .Select(System.IO.Path.GetFileName))
                {
                    InvokeHandler(_onExistedHandler, new FileSystemEventArgs(WatcherChangeTypes.All, Path, fileName));
                    InvokeHandler(_onAllChangesHandler, new FileSystemEventArgs(WatcherChangeTypes.All, Path, fileName));
                }
            }
        }

        #region InvokeHandlers

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Executes the handler on a different thread, and waits for the result. </summary>
        ///
        /// <param name="eventHandler"> The event handler. </param>
        /// <param name="e">            File system event information. </param>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        private void InvokeHandler(FileSystemEventHandler eventHandler, FileSystemEventArgs e)
        {
            if (eventHandler != null)
            {
                if (_containedFSW.SynchronizingObject?.InvokeRequired == true)
                    _containedFSW.SynchronizingObject.BeginInvoke(eventHandler, new object[] { this, e });
                else
                    eventHandler(this, e);
            }
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Executes the handler on a different thread, and waits for the result. </summary>
        ///
        /// <param name="eventHandler"> The event handler. </param>
        /// <param name="e">            Renamed event information. </param>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        private void InvokeHandler(RenamedEventHandler eventHandler, RenamedEventArgs e)
        {
            if (eventHandler != null)
            {
                if (_containedFSW.SynchronizingObject?.InvokeRequired == true)
                    _containedFSW.SynchronizingObject.BeginInvoke(eventHandler, new object[] { this, e });
                else
                    eventHandler(this, e);
            }
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Executes the handler on a different thread, and waits for the result. </summary>
        ///
        /// <param name="eventHandler"> The event handler. </param>
        /// <param name="e">            Error event information. </param>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        private void InvokeHandler(ErrorEventHandler eventHandler, ErrorEventArgs e)
        {
            if (eventHandler != null)
            {
                if (_containedFSW.SynchronizingObject?.InvokeRequired == true)
                    _containedFSW.SynchronizingObject.BeginInvoke(eventHandler, new object[] { this, e });
                else
                    eventHandler(this, e);
            }
        }
        #endregion

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Releases the unmanaged resources used by the <see cref="T:System.ComponentModel.Component" />
        /// and optionally releases the managed resources.
        /// </summary>
        ///
        /// <param name="disposing">    <see langword="true" /> to release both managed and unmanaged
        ///                             resources; <see langword="false" /> to release only unmanaged
        ///                             resources. </param>
        ///
        /// <seealso cref="M:System.ComponentModel.Component.Dispose(bool)"/>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _cancellationTokenSource?.Cancel();
                _containedFSW?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
