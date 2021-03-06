﻿using Avalonia;
using Avalonia.Logging.Serilog;
using Avalonia.Markup.Xaml;
using ModulesSample.Module_System_Logic;
using Prism.Avalonia.Infrastructure;
using Prism.DryIoc;
using Prism.Ioc;
using Prism.Modularity;
using Serilog;
using Avalonia.LinuxFramebuffer;
using System.Linq;
using System;
using System.Globalization;
using System.Threading;
using Avalonia.Dialogs;

namespace ModulesSample
{
    public class App : PrismApplication
    {
        public CallbackLogger CallbackLogger { get; } = new CallbackLogger();

        public static AppBuilder BuildAvaloniaApp() => 
            AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .With(new X11PlatformOptions
                {
                    EnableMultiTouch = true,
                    UseDBusMenu = true
                })
                .With(new Win32PlatformOptions
                {
                    EnableMultitouch = true,
                    AllowEglInitialization = true
                })
                .UseSkia()
                .UseManagedSystemDialogs();

        public static bool IsSingleViewLifetime =>
            Environment.GetCommandLineArgs()
                .Any(a => a == "--fbdev" || a == "--drm");

        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
            base.Initialize();
        }

        static int Main(string[] args)
        {
            double GetScaling()
            {
                var idx = Array.IndexOf(args, "--scaling");
                if (idx != 0 && args.Length > idx + 1 &&
                    double.TryParse(args[idx + 1], NumberStyles.Any, CultureInfo.InvariantCulture, out var scaling))
                    return scaling;
                return 1;
            }

            var builder = BuildAvaloniaApp();
            InitializeLogging();
            if (args.Contains("--fbdev"))
            {
                SilenceConsole();
                return builder.StartLinuxFbDev(args, scaling: GetScaling());
            }
            else if (args.Contains("--drm"))
            {
                SilenceConsole();
                return builder.StartLinuxDrm(args, scaling: GetScaling());
            }
            else
                return builder.StartWithClassicDesktopLifetime(args);
        }

        static void SilenceConsole()
        {
            new Thread(() =>
            {
                Console.CursorVisible = false;
                while (true)
                    Console.ReadKey(true);
            })
            { IsBackground = true }.Start();
        }

        private static void InitializeLogging()
        {
#if DEBUG
            SerilogLogger.Initialize(new LoggerConfiguration()
                .MinimumLevel.Warning()
                .WriteTo.Trace(outputTemplate: "{Area}: {Message}")
                .CreateLogger());
#endif
        }

        protected override void RegisterTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.RegisterInstance(CallbackLogger);
            containerRegistry.RegisterSingleton<IModuleTracker, ModuleTracker>();
        }

        protected override IAvaloniaObject CreateShell()
        {
            return Container.Resolve<MainWindow>();
        }

        protected override IModuleCatalog CreateModuleCatalog()
        {
            return new AggregateModuleCatalog();
        }

        protected override void ConfigureModuleCatalog(IModuleCatalog moduleCatalog)
        {
            moduleCatalog.AddModule<DummyModule.DummyModule>();
            moduleCatalog.AddModule<DummyModule2.DummyModule2>();

            base.ConfigureModuleCatalog(moduleCatalog);
        }
    }
}
