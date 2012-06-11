//
// Copyright (C) 2010 Chris Dziemborowicz
//
// This file is part of Orzeszek Timer.
//
// Orzeszek Timer is free software: you can redistribute it and/or
// modify it under the terms of the GNU General Public License as
// published by the Free Software Foundation, either version 3 of the
// License, or (at your option) any later version.
//
// Orzeszek Timer is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program. If not, see <http://www.gnu.org/licenses/>.
//

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Media;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

using NotifyIcon = System.Windows.Forms.NotifyIcon;

namespace OrzeszekTimer
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		private Thread updaterThread;
		private SoundPlayer notification;
		private NotifyIcon notifyIcon;
		private WindowInteropHelper interopHelper;
		private WindowState restorableWindowState;

		private bool closeOnFinishThisTime = false;
		private bool closeFromTray = false;
		private bool notified = true;
		private DateTime start = DateTime.MinValue;
		private DateTime end = DateTime.MinValue;

		public MainWindow()
		{
			InitializeComponent();

			try
			{
				foreach (string soundFilePath in Directory.GetFiles("Sounds", "*.wav"))
				{
					MenuItem soundMenuItem = new MenuItem();
					soundMenuItem.Header = System.IO.Path.GetFileNameWithoutExtension(soundFilePath);
					soundMenuItem.Tag = System.IO.Path.GetFileName(soundFilePath);
					soundMenuItem.IsCheckable = true;
					soundMenuItem.Click += new RoutedEventHandler(SoundMenuItem_Click);

					MainContextMenu.Items.Insert(MainContextMenu.Items.Count - 2, soundMenuItem);
				}
			}
			catch (Exception)
			{
			}

			UpdateSoundMenuItems();

			ScaleInterfaceMenuItem.IsChecked = Settings.Default.ScaleInterface;
			LoopNotificationMenuItem.IsChecked = Settings.Default.LoopNotification;
			LoopTimerMenuItem.IsChecked = Settings.Default.LoopTimer;
			CloseOnFinishMenuItem.IsChecked = Settings.Default.CloseOnFinish;
			FlashOnFinishMenuItem.IsChecked = Settings.Default.FlashOnFinish;
			PopupOnFinishMenuItem.IsChecked = Settings.Default.PopupOnFinish;
			RememberTimerOnCloseMenuItem.IsChecked = Settings.Default.RememberTimerOnClose;
			ShowTimerInTrayMenuItem.IsChecked = Settings.Default.ShowTimerInTray;

			if (Settings.Default.LoopTimer)
			{
				Settings.Default.LoopNotification = false;
				LoopNotificationMenuItem.IsChecked = false;

				Settings.Default.CloseOnFinish = false;
				CloseOnFinishMenuItem.IsChecked = false;
			}

			if (Settings.Default.CloseOnFinish)
			{
				Settings.Default.LoopNotification = false;
				LoopNotificationMenuItem.IsChecked = false;

				Settings.Default.LoopTimer = false;
				LoopTimerMenuItem.IsChecked = false;
			}

			if (Settings.Default.WindowSettingsSaved)
				try
				{
					Height = Settings.Default.WindowHeight;
					Width = Settings.Default.WindowWidth;
					Left = Settings.Default.WindowLeft;
					Top = Settings.Default.WindowTop;
					WindowState = Settings.Default.WindowState;
				}
				catch (Exception)
				{
				}

			interopHelper = new WindowInteropHelper(this);

			UpdateNotifyIcon();
		}

		private void Window_Loaded(object sender, RoutedEventArgs e)
		{
			string args = (string)Application.Current.Properties["Args"];
			if (args != null)
				try
				{
					if (Regex.IsMatch(args, "(^|\\s+)-closeWhenDone($|\\s+)", RegexOptions.IgnoreCase))
					{
						args = Regex.Replace(args, "(^|\\s+)-closeWhenDone($|\\s+)", string.Empty, RegexOptions.IgnoreCase);
						closeOnFinishThisTime = true;
					}

					object o = FromString(args);

					if (o is DateTime)
					{
						start = DateTime.Now;
						end = (DateTime)o;
						notified = false;

						Settings.Default.LastTimeSpan = TimeSpan.Zero;

						MainTextBox.Template = (ControlTemplate)Resources["ValidTextBoxTemplate"];
						MainButton.Focus();
					}
					else if (o is TimeSpan)
					{
						TimeSpan ts = (TimeSpan)o;

						start = DateTime.Now;
						end = start.Add(ts);
						notified = false;

						Settings.Default.LastTimeSpan = ts;

						MainTextBox.Template = (ControlTemplate)Resources["ValidTextBoxTemplate"];
						MainButton.Focus();
					}

					StartTimer();
				}
				catch (Exception)
				{
					MainTextBox.Template = (ControlTemplate)Resources["InvalidTextBoxTemplate"];
				}
			else if (Settings.Default.RememberTimerOnClose && Settings.Default.TimerRunning && Settings.Default.CurrentStart != DateTime.MinValue && Settings.Default.CurrentEnd != DateTime.MinValue && DateTime.Now > Settings.Default.CurrentStart && DateTime.Now < Settings.Default.CurrentEnd)
			{
				start = Settings.Default.CurrentStart;
				end = Settings.Default.CurrentEnd;
				notified = false;

				MainTextBox.Template = (ControlTemplate)Resources["ValidTextBoxTemplate"];
				MainButton.Focus();

				StartTimer();
			}
			else
			{
				MainTextBox.Text = ToString(Settings.Default.LastTimeSpan.Ticks < 0 ? TimeSpan.Zero : Settings.Default.LastTimeSpan);
				MainTextBox.Focus();
			}
		}

		private void MainTextBox_KeyUp(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Enter)
				try
				{
					object o = FromString(MainTextBox.Text);

					if (o is DateTime)
					{
						start = DateTime.Now;
						end = (DateTime)o;
						notified = false;

						Settings.Default.LastTimeSpan = TimeSpan.Zero;

						MainTextBox.Template = (ControlTemplate)Resources["ValidTextBoxTemplate"];
						MainButton.Focus();
					}
					else if (o is TimeSpan)
					{
						TimeSpan ts = (TimeSpan)o;

						start = DateTime.Now;
						end = start.Add(ts);
						notified = false;

						Settings.Default.LastTimeSpan = ts;

						MainTextBox.Template = (ControlTemplate)Resources["ValidTextBoxTemplate"];
						MainButton.Focus();
					}
				}
				catch (Exception)
				{
					MainTextBox.Template = (ControlTemplate)Resources["InvalidTextBoxTemplate"];
				}
			else if (e.Key == Key.Escape)
			{
				MainTextBox.Template = (ControlTemplate)Resources["ValidTextBoxTemplate"];
				MainButton.Focus();
			}
		}

		private void MainTextBox_GotFocus(object sender, RoutedEventArgs e)
		{
			MainTextBox.SelectAll();
			TaskbarUtility.StopFlash(interopHelper.Handle);
		}

		private void MainTextBox_LostFocus(object sender, RoutedEventArgs e)
		{
			StartTimer();
		}

		private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
		{
			if (Settings.Default.ShowTimerInTray && !closeFromTray)
			{
				Hide();
				e.Cancel = true;
			}
			else
				try
				{
					Settings.Default.ScaleInterface = ScaleInterfaceMenuItem.IsChecked;
					Settings.Default.LoopNotification = LoopNotificationMenuItem.IsChecked;
					Settings.Default.LoopTimer = LoopTimerMenuItem.IsChecked;
					Settings.Default.CloseOnFinish = CloseOnFinishMenuItem.IsChecked;
					Settings.Default.FlashOnFinish = FlashOnFinishMenuItem.IsChecked;
					Settings.Default.PopupOnFinish = PopupOnFinishMenuItem.IsChecked;
					Settings.Default.RememberTimerOnClose = RememberTimerOnCloseMenuItem.IsChecked;
					Settings.Default.ShowTimerInTray = ShowTimerInTrayMenuItem.IsChecked;

					if (Settings.Default.RememberTimerOnClose && start != DateTime.MinValue && end != DateTime.MinValue && DateTime.Now < end)
					{
						Settings.Default.TimerRunning = true;
						Settings.Default.CurrentStart = start;
						Settings.Default.CurrentEnd = end;
					}
					else
					{
						Settings.Default.TimerRunning = false;
						Settings.Default.CurrentStart = DateTime.MinValue;
						Settings.Default.CurrentEnd = DateTime.MinValue;
					}

					Settings.Default.WindowHeight = RestoreBounds.Height;
					Settings.Default.WindowWidth = RestoreBounds.Width;
					Settings.Default.WindowLeft = RestoreBounds.Left;
					Settings.Default.WindowTop = RestoreBounds.Top;
					Settings.Default.WindowState = WindowState;
					Settings.Default.WindowSettingsSaved = true;
					Settings.Default.Save();
				}
				catch (Exception)
				{
				}
		}

		private void Window_Closed(object sender, EventArgs e)
		{
			if (updaterThread != null)
				updaterThread.Abort();

			if (notifyIcon != null)
				notifyIcon.Dispose();
		}

		private void Data_Updated(object sender, EventArgs e)
		{
			if (updaterThread == null)
			{
				updaterThread = new Thread(new ThreadStart(UpdateInterface));
				updaterThread.Start();
			}
		}

		private void StartTimer()
		{
			if (updaterThread == null)
			{
				updaterThread = new Thread(new ThreadStart(UpdateInterface));
				updaterThread.Start();
			}
		}

		private void UpdateInterface()
		{
			while (true)
			{
				DispatcherOperation op = Dispatcher.BeginInvoke(new Action(delegate()
				{
					DateTime now = DateTime.Now;
					TimeSpan elapsed = now - start;
					TimeSpan remaining = end - now;
					TimeSpan total = end - start;

					if (remaining.Ticks <= 0)
					{
						if (!MainTextBox.IsFocused)
							MainTextBox.Text = "Timer expired";
						if (notifyIcon != null)
							notifyIcon.Text = "Timer expired";
						Title = "Orzeszek Timer";

						MainProgressBar.Value = 100;
						TaskbarUtility.SetProgressState(interopHelper.Handle, TaskbarProgressState.NoProgress);

						if (!notified)
							try
							{
								notified = true;
								SoundPlayer sp = new SoundPlayer(System.IO.Path.Combine("Sounds", Settings.Default.AlarmSound));

								if (Settings.Default.CloseOnFinish || closeOnFinishThisTime)
									sp.PlaySync();
								else if (Settings.Default.LoopNotification)
								{
									if (notification != null)
										notification.Stop();

									notification = sp;
									notification.PlayLooping();

									StopNotificationButton.Visibility = System.Windows.Visibility.Visible;
								}
								else
									sp.Play();

								if (Settings.Default.PopupOnFinish)
								{
									Show();
									WindowState = restorableWindowState;
									Activate();
									Topmost = true;
									Topmost = false;
								}

								if (Settings.Default.FlashOnFinish)
									TaskbarUtility.StartFlash(interopHelper.Handle);
							}
							catch (Exception)
							{
							}

						if (Settings.Default.CloseOnFinish || closeOnFinishThisTime)
						{
							closeFromTray = true;
							Close();
						}
						else if (Settings.Default.LoopTimer && Settings.Default.LastTimeSpan != TimeSpan.Zero)
						{
							start = DateTime.Now;
							end = start.Add(Settings.Default.LastTimeSpan);
							notified = false;
						}
						else
						{
							updaterThread.Abort();
							updaterThread = null;
						}
					}
					else
					{
						if (!MainTextBox.IsFocused)
							MainTextBox.Text = ToString(remaining);
						if (notifyIcon != null)
							notifyIcon.Text = ToString(remaining);
						Title = MainTextBox.Text;

						MainProgressBar.Value = Math.Min(100.0, 100.0 * elapsed.Ticks / total.Ticks);
						TaskbarUtility.SetProgressValue(interopHelper.Handle, (ulong)MainProgressBar.Value, 100);
					}
				}
				));

				op.Wait();

				Thread.Sleep((int)Math.Max(Math.Min((end - start).TotalSeconds / MainProgressBar.ActualWidth, 1000), 10));
			}
		}

		private static object FromString(string s)
		{
			if (string.IsNullOrEmpty(s))
				return TimeSpan.Zero;

			double minutes;
			if (double.TryParse(s, out minutes))
				return TimeSpan.FromMinutes(minutes);

			DateTime dateTime;
			if (DateTime.TryParse(s, CultureInfo.CurrentCulture, DateTimeStyles.None, out dateTime) && dateTime > DateTime.Now)
				return dateTime;

			bool isDateTime = false;

			if (s.StartsWith("until ", StringComparison.CurrentCultureIgnoreCase))
			{
				s = s.Substring("until ".Length);
				isDateTime = true;
			}
			else if (s.StartsWith("to ", StringComparison.CurrentCultureIgnoreCase))
			{
				s = s.Substring("to ".Length);
				isDateTime = true;
			}

			if (isDateTime)
			{
				if (DateTime.TryParse(s, CultureInfo.CurrentCulture, DateTimeStyles.None, out dateTime))
					return dateTime;

				// @TODO: Parse other date formats.

				throw new FormatException();
			}

			try
			{
				TimeSpan ts = TimeSpan.Zero;
				string[] parts = Regex.Split(s, @"(?<=\d+\s*[a-zA-Z]+)\s*(?=\d+\s*[a-zA-Z]+)");
				foreach (string p in parts)
					if (!string.IsNullOrEmpty(p))
					{
						string[] subparts = Regex.Split(p, @"(?<=\d+)\s*(?=[a-zA-Z]+)");
						if (subparts.Length == 2)
							switch (subparts[1].ToLower()[0])
							{
								case 'd':
									ts = ts.Add(TimeSpan.FromDays(double.Parse(subparts[0])));
									break;
								case 'h':
									ts = ts.Add(TimeSpan.FromHours(double.Parse(subparts[0])));
									break;
								case 'm':
									ts = ts.Add(TimeSpan.FromMinutes(double.Parse(subparts[0])));
									break;
								case 's':
									ts = ts.Add(TimeSpan.FromSeconds(double.Parse(subparts[0])));
									break;
								default:
									throw new FormatException();
							}
						else
							throw new FormatException();
					}

				return ts;
			}
			catch (Exception)
			{
			}

			try
			{
				TimeSpan ts = TimeSpan.Zero;
				string[] parts = s.Split(new char[] { '.', ':', '-', ' ', '/', '\\', '\f', '\n', '\r', '\t', '\v', '\x85' }, StringSplitOptions.RemoveEmptyEntries);
				for (int i = 0; i < parts.Length; i++)
				{
					double d = double.Parse(parts[i]);
					switch (parts.Length - i)
					{
						case 1:
							ts = ts.Add(TimeSpan.FromSeconds(d));
							break;
						case 2:
							ts = ts.Add(TimeSpan.FromMinutes(d));
							break;
						case 3:
							ts = ts.Add(TimeSpan.FromHours(d));
							break;
						case 4:
							ts = ts.Add(TimeSpan.FromDays(d));
							break;
						default:
							throw new FormatException();
					}
				}

				return ts;
			}
			catch (Exception)
			{
			}

			TimeSpan timeSpan = TimeSpan.Zero;
			if (TimeSpan.TryParse(s, out timeSpan))
				return timeSpan;

			throw new FormatException();
		}

		private static string ToString(TimeSpan ts)
		{
			StringBuilder sb = new StringBuilder();

			if ((int)ts.TotalDays == 1)
				sb.AppendFormat("{0:D}\u00A0day ", (int)ts.TotalDays);
			else if ((int)ts.TotalDays != 0)
				sb.AppendFormat("{0:D}\u00A0days ", (int)ts.TotalDays);

			if ((int)ts.TotalHours == 1)
				sb.AppendFormat("{0:D}\u00A0hour ", ts.Hours);
			else if ((int)ts.TotalHours != 0)
				sb.AppendFormat("{0:D}\u00A0hours ", ts.Hours);

			if ((int)ts.TotalMinutes == 1)
				sb.AppendFormat("{0:D}\u00A0minute ", ts.Minutes);
			else if ((int)ts.TotalMinutes != 0)
				sb.AppendFormat("{0:D}\u00A0minutes ", ts.Minutes);

			if (ts.Seconds == 1)
				sb.AppendFormat("{0:D}\u00A0second", ts.Seconds);
			else
				sb.AppendFormat("{0:D}\u00A0seconds", ts.Seconds);

			return sb.ToString();
		}

		private void SoundMenuItem_Click(object sender, RoutedEventArgs e)
		{
			MenuItem soundMenuItem = sender as MenuItem;
			Settings.Default.AlarmSound = soundMenuItem.Tag == null ? string.Empty : (string)soundMenuItem.Tag;
			UpdateSoundMenuItems();
		}

		private void UpdateSoundMenuItems()
		{
			bool set = false;

			foreach (object o in MainContextMenu.Items)
			{
				MenuItem soundMenuItem = o as MenuItem;

				if (soundMenuItem != null)
					if (soundMenuItem.Tag != null && (string)soundMenuItem.Tag == Settings.Default.AlarmSound && !set)
					{
						soundMenuItem.IsChecked = true;
						set = true;
					}
					else if (soundMenuItem.Tag == null && string.IsNullOrEmpty(Settings.Default.AlarmSound) && !set)
					{
						soundMenuItem.IsChecked = true;
						set = true;
					}
					else
						soundMenuItem.IsChecked = false;
			}

			if (!set)
			{
				Settings.Default.AlarmSound = string.Empty;
				NoNotificationMenuItem.IsChecked = true;
			}
		}

		private void ScaleInterfaceMenuItem_Click(object sender, RoutedEventArgs e)
		{
			Settings.Default.ScaleInterface = ScaleInterfaceMenuItem.IsChecked;
			UpdateScaleInterface();
		}

		private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
		{
			UpdateScaleInterface();
		}

		private void UpdateScaleInterface()
		{
			if (Settings.Default.ScaleInterface)
			{
				double sizeFactor = Math.Min(ActualHeight / 150, ActualWidth / 300);
				double progressBarThickness = Math.Max(1, sizeFactor * 10);
				double fontSize = Math.Max(1, sizeFactor * 16);

				MainTextBox.Margin = new Thickness(progressBarThickness);
				MainTextBox.FontSize = fontSize;

				StopNotificationButton.Margin = new Thickness(progressBarThickness);
				StopNotificationButton.FontSize = fontSize;
			}
			else
			{
				MainTextBox.Margin = new Thickness(10);
				MainTextBox.FontSize = 16;
			}
		}

		private void StopNotificationButton_Click(object sender, RoutedEventArgs e)
		{
			if (notification != null)
			{
				notification.Stop();
				notification = null;

				StopNotificationButton.Visibility = System.Windows.Visibility.Collapsed;
				TaskbarUtility.StopFlash(interopHelper.Handle);
			};
		}

		private void LoopNotificationMenuItem_Click(object sender, RoutedEventArgs e)
		{
			Settings.Default.LoopNotification = LoopNotificationMenuItem.IsChecked;

			if (Settings.Default.LoopNotification)
			{
				Settings.Default.LoopTimer = false;
				LoopTimerMenuItem.IsChecked = false;

				Settings.Default.CloseOnFinish = false;
				CloseOnFinishMenuItem.IsChecked = false;
			}
		}

		private void LoopTimerMenuItem_Click(object sender, RoutedEventArgs e)
		{
			Settings.Default.LoopTimer = LoopTimerMenuItem.IsChecked;

			if (Settings.Default.LoopTimer)
			{
				Settings.Default.LoopNotification = false;
				LoopNotificationMenuItem.IsChecked = false;

				Settings.Default.CloseOnFinish = false;
				CloseOnFinishMenuItem.IsChecked = false;
			}
		}

		private void CloseOnFinishMenuItem_Click(object sender, RoutedEventArgs e)
		{
			Settings.Default.CloseOnFinish = CloseOnFinishMenuItem.IsChecked;

			if (Settings.Default.CloseOnFinish)
			{
				Settings.Default.LoopNotification = false;
				LoopNotificationMenuItem.IsChecked = false;

				Settings.Default.LoopTimer = false;
				LoopTimerMenuItem.IsChecked = false;
			}
		}

		private void FlashOnFinishMenuItem_Click(object sender, RoutedEventArgs e)
		{
			Settings.Default.FlashOnFinish = FlashOnFinishMenuItem.IsChecked;
		}

		private void PopupOnFinishMenuItem_Click(object sender, RoutedEventArgs e)
		{
			Settings.Default.PopupOnFinish = PopupOnFinishMenuItem.IsChecked;
		}

		private void Window_Activated(object sender, EventArgs e)
		{
			if (!Settings.Default.PopupOnFinish)
				TaskbarUtility.StopFlash(interopHelper.Handle);
		}

		private void Window_Deactivated(object sender, EventArgs e)
		{
			TaskbarUtility.StopFlash(interopHelper.Handle);
		}

		private void Window_MouseUp(object sender, MouseButtonEventArgs e)
		{
			TaskbarUtility.StopFlash(interopHelper.Handle);
		}

		private void Window_KeyUp(object sender, KeyEventArgs e)
		{
			TaskbarUtility.StopFlash(interopHelper.Handle);
		}

		private void ShowTimerInTrayMenuItem_Click(object sender, RoutedEventArgs e)
		{
			Settings.Default.ShowTimerInTray = ShowTimerInTrayMenuItem.IsChecked;
			UpdateNotifyIcon();
		}

		private void UpdateNotifyIcon()
		{
			if (Settings.Default.ShowTimerInTray && notifyIcon == null)
			{
				notifyIcon = new NotifyIcon();
				notifyIcon.Text = "Orzeszek Timer";
				notifyIcon.Icon = Resource.TrayIcon;
				notifyIcon.Click += new EventHandler(NotifyIconShow_Click);
				notifyIcon.ContextMenu = new System.Windows.Forms.ContextMenu();
				notifyIcon.ContextMenu.MenuItems.Add("&Show", new EventHandler(NotifyIconShow_Click));
				notifyIcon.ContextMenu.MenuItems.Add("E&xit", new EventHandler(NotifyIconClose_Click));
				notifyIcon.Visible = true;
			}
			else if (!Settings.Default.ShowTimerInTray && notifyIcon != null)
			{
				notifyIcon.Dispose();
				notifyIcon = null;
			}
		}

		private void NotifyIconShow_Click(object sender, EventArgs e)
		{
			Show();
			WindowState = restorableWindowState;
			Activate();
		}

		private void NotifyIconClose_Click(object sender, EventArgs e)
		{
			closeFromTray = true;
			Close();
		}

		private void Window_StateChanged(object sender, EventArgs e)
		{
			if (WindowState != System.Windows.WindowState.Minimized)
				restorableWindowState = WindowState;
		}
	}
}