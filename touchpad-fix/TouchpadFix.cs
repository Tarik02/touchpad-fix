using System;
using System.ComponentModel;
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
		private static string instancePath = @"ROOT\SYNHIDMINI\0000";

		private static bool shouldFix;

		static void Main(string[] args)
		{
			if (args.Length != 0)
			{
				Console.WriteLine($"Usage:");
				Console.WriteLine($"    {AppName}");

				return;
			}

			try
			{
				FixTouchpad();
			}
			catch (Win32Exception e)
			{
				if (e.NativeErrorCode != 5) throw;

				Console.WriteLine("This application should be run by administrator.");
				Console.WriteLine("This is required in order to enable/disable device.");

				return;

			}

			ShowWindow(GetConsoleWindow(), SW_HIDE);

			SystemEvents.PowerModeChanged += OnPowerChange;

			var waitHandle = new EventWaitHandle(false, EventResetMode.AutoReset, "CF2D4313-33DE-489D-9721-6AFF69841DEB", out bool createdNew);

			if (!createdNew)
			{
				waitHandle.Set();

				return;
			}

			bool signaled;
			do
			{
				signaled = waitHandle.WaitOne(TimeSpan.FromSeconds(5));

				if (!shouldFix)
				{
					continue;
				}

				try
				{
					FixTouchpad();
					shouldFix = false;
				}
				catch (Exception ex)
				{
					Console.Error.WriteLine(ex);
				}
			} while (!signaled);
		}

		private static void OnPowerChange(object s, PowerModeChangedEventArgs e)
		{
			switch ( e.Mode )
			{
				case PowerModes.Resume:
					if (shouldFix) return;

					try
					{
						FixTouchpad();
					}
					catch (Exception ex)
					{
						Console.Error.WriteLine(ex);
						shouldFix = true;
					}
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
	}
}
