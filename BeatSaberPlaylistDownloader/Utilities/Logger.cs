using log4net;
using log4net.Appender;
using log4net.Core;
using log4net.Layout;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BeatSaberPlaylistDownloader.Utilities
{
    public class Logger
    {
        public static void Setup()
        {
            var logLoc = @".\Logs\";

            if (!Directory.Exists(Path.GetDirectoryName(logLoc)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(logLoc));
            }

            log4net.Repository.Hierarchy.Hierarchy hierarchy = (log4net.Repository.Hierarchy.Hierarchy)LogManager.GetRepository();

            PatternLayout patternLayout = new PatternLayout();
            patternLayout.ConversionPattern = "%date [%thread] %level %logger - %message%newline%exception";
            patternLayout.ActivateOptions();

            hierarchy.Root.RemoveAllAppenders();

            #region File Appender
            RollingFileAppender roller = new RollingFileAppender();
            roller.AppendToFile = true;
            roller.File = logLoc;
            roller.Layout = patternLayout;
            roller.MaxSizeRollBackups = 10;
            roller.MaximumFileSize = "1MB";
            roller.RollingStyle = RollingFileAppender.RollingMode.Date;
            roller.DatePattern = @"yyyyMMdd.lo\g";
            roller.StaticLogFileName = false;
            roller.ActivateOptions();
            hierarchy.Root.AddAppender(roller);
            #endregion


            ColoredConsoleAppender coloredConsoleAppender = new ColoredConsoleAppender();
            coloredConsoleAppender.AddMapping(new ColoredConsoleAppender.LevelColors()
            {
                Level = Level.Fatal,
                ForeColor = ColoredConsoleAppender.Colors.White | ColoredConsoleAppender.Colors.HighIntensity,
                BackColor = ColoredConsoleAppender.Colors.Red | ColoredConsoleAppender.Colors.HighIntensity
            });
            coloredConsoleAppender.AddMapping(new ColoredConsoleAppender.LevelColors()
            {
                Level = Level.Error,
                ForeColor = ColoredConsoleAppender.Colors.Red | ColoredConsoleAppender.Colors.HighIntensity
            });
            coloredConsoleAppender.AddMapping(new ColoredConsoleAppender.LevelColors()
            {
                Level = Level.Warn,
                ForeColor = ColoredConsoleAppender.Colors.Yellow
            });
            coloredConsoleAppender.AddMapping(new ColoredConsoleAppender.LevelColors()
            {
                Level = Level.Info,
                ForeColor = ColoredConsoleAppender.Colors.Cyan
            });
            coloredConsoleAppender.AddMapping(new ColoredConsoleAppender.LevelColors()
            {
                Level = Level.Debug,
                ForeColor = ColoredConsoleAppender.Colors.White
            });
            coloredConsoleAppender.Layout = patternLayout;
            coloredConsoleAppender.ActivateOptions();
            hierarchy.Root.AddAppender(coloredConsoleAppender);
            hierarchy.Root.Level = Level.Debug;
            hierarchy.Configured = true;

            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
        }

        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            LogManager.GetLogger("LOGGER").Fatal("Unhandled Exception:", (Exception)e.ExceptionObject);
        }
    }
    public static class LoggerExtensions
    {
        public static ILog GetLogger(this object instance)
        {
            return LogManager.GetLogger(instance.GetType());
        }
    }
}