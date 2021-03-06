﻿using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Logging
{
  /// <summary>
  /// Interface to logging functionality
  /// 
  /// Initialise logging by adding something like the following at process start
  /// 
  /// var logEncoder = new XmlLogEncoder();
  /// var byteWriter = new LazyStreamByteWriter(FileStreamFactory.CreateApplicationDataFileStream);
  /// var logWriter = new LogWriter(logEncoder, new[] { byteWriter });
  /// Logger.AddLogWriter(logWriter);
  /// </summary>
  public static class Logger
  {
    /// <summary>
    /// Log calls with level above this value will be discarded
    /// </summary>
    public static LogLevel LogLevel { get; set; } = LogLevel.Warning;


    private static List<ILogWriter> LogWriters { get; } = new List<ILogWriter>();

    private static int LogWriterCount { get; set; }


    private static readonly object mLock = new object();


    /// <summary>
    /// A log writer instance is only added once. Subsequent calls to add the same log writer will have no effect.
    /// </summary>
    public static void AddLogWriter(ILogWriter logWriter)
    {
      if (logWriter == null)
        return;

      lock (mLock)
      {
        if (LogWriters.Contains(logWriter))
          return;

        LogWriters.Add(logWriter);
        ++LogWriterCount;
      }
    }


    /// <summary>
    /// This function will have no effect if the log writer has not been added
    /// </summary>
    public static void RemoveLogWriter(ILogWriter logWriter)
    {
      if (logWriter == null)
        return;

      lock (mLock)
      {
        if (LogWriters.Contains(logWriter) == false)
          return;

        LogWriters.Remove(logWriter);
        --LogWriterCount;
      }
    }


    public static void ClearAllLogWriters()
    {
      lock (mLock)
      {
        LogWriters.Clear();
        LogWriterCount = 0;
      }
    }


    /// <summary>
    /// Log a message if the level is equal to or less than the logger level.
    /// If the level is above the logger level the message will be disgarded.
    /// </summary>
    public static void Log(LogLevel level, string message)
    {
      // The current level is checked first because this is a common early return scenario
      if (level > LogLevel)
        return;

      if (LogWriterCount == 0)
        return;

      // It is anticipated that execution where the lock is not available
      // will be so rare that performance impact will be negligible
      lock (mLock)
      {
        try
        {
          var stackTrace = new StackTrace(1, true);

          foreach (var logwriter in LogWriters)
            logwriter.Log(level, message, stackTrace);
        }
        catch (Exception)
        {
          // Do nothing. Can't log because it's logging that's failed. Can't throw because
          // an application failure due to debug logging would be ridiculous.
        }
      }
    }
  }
}