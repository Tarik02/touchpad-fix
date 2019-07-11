using System;
using System.Threading;
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace touchpad_fix
{
	static class TouchpadFix
	{
		[DllImport("kernel32.dll")]
		static extern IntPtr GetConsoleWindow();

		[DllImport("user32.dll")]
		static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

		private const int SW_HIDE = 0;
		private const int SW_SHOW = 5;


		const string AppName = "touchpad-fix";
		const string Key = @"SOFTWARE\Microsoft\Windows\DWM";

		private static Guid guid = new Guid("{745a17a0-74d3-11d0-b6fe-00a0c90f57da}");
		private static string instancePath = @"HID\SYNHIDMINI&COL02\1&B12C6D1&0&0001";

		static void Main(string[] args)
		{
			if (args.Length != 0)
			{
				if (args.Length == 1)
				{
					switch (args[0])
					{
					case "set-startup":
						SetStartup(true);
						Console.WriteLine("Startup entry added.");
						return;
					case "unset-startup":
						SetStartup(false);
						Console.WriteLine("Startup entry removed.");
						return;
					}
				}

				Console.WriteLine($"Usage:");
				Console.WriteLine($"    {AppName} [action]");
				Console.WriteLine($"    If action is omitted, then app is started in daemon mode.");
				Console.WriteLine($"");
				Console.WriteLine($"Actions:");
				Console.WriteLine($"    set-startup      Make app auto run at Windows startup.");
				Console.WriteLine($"    unset-startup    Make app not auto run at Windows startup.");

				return;
			}

			ShowWindow(GetConsoleWindow(), SW_HIDE);

			FixTouchpad();

			SystemEvents.PowerModeChanged += OnPowerChange;

			try
			{
				var waitHandle = new EventWaitHandle(false, EventResetMode.AutoReset, "CF2D4313-33DE-489D-9721-6AFF69841DEB", out bool createdNew);
				var signaled = false;

				if (!createdNew)
				{
					waitHandle.Set();

					return;
				}

				var timer = new Timer(OnTimerElapsed, null, TimeSpan.Zero, TimeSpan.FromSeconds(10));

				do
				{
					signaled = waitHandle.WaitOne(TimeSpan.FromSeconds(5));
				} while (!signaled);
			}
			finally
			{
			}
		}

		private static void OnTimerElapsed(object state)
		{
		}

		private static void OnPowerChange(object s, PowerModeChangedEventArgs e)
		{
			switch ( e.Mode )
			{
				case PowerModes.Resume:
					FixTouchpad();
					break;
				case PowerModes.Suspend:
					break;
			}
		}

		private static void FixTouchpad()
		{
			DeviceHelper.SetDeviceEnabled(
				guid,
				instancePath,
				false
			);
			DeviceHelper.SetDeviceEnabled(
				guid,
				instancePath,
				true
			);
		}

		private static void SetStartup(bool value)
		{
			RegistryKey key = Registry.CurrentUser.OpenSubKey(
				@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true
			);

			if (value)
			{
				var executablePath = System.Reflection.Assembly.GetEntryAssembly().Location;
				key.SetValue(AppName, executablePath);
			}
			else
			{
				key.DeleteValue(AppName, false);
			}
		}
	}
}
