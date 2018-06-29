﻿using System;
using System.ComponentModel;

////////////////////////////////////////////////////////////////////////////////////////////////////
/// <summary>   Additional information for file watcher error events. </summary>
///
/// <seealso cref="T:System.ComponentModel.HandledEventArgs"/>
////////////////////////////////////////////////////////////////////////////////////////////////////

public class FileWatcherErrorEventArgs : HandledEventArgs
{
    /// <summary>   The error. </summary>
    public readonly Exception Error;

    ////////////////////////////////////////////////////////////////////////////////////////////////////
    /// <summary>   Initializes a new instance of the FileWatcherErrorEventArgs class. </summary>
    ///
    /// <param name="exception">    The exception. </param>
    ////////////////////////////////////////////////////////////////////////////////////////////////////

    public FileWatcherErrorEventArgs(Exception exception)
    {
        this.Error = exception;
    }
}
