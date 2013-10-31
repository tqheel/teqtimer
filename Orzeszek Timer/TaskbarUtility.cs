//
// Copyright (C) 2013 Chris Dziemborowicz
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
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace OrzeszekTimer
{
	public enum TaskbarProgressState
	{
		NoProgress = 0,
		Indeterminate = 0x1,
		Normal = 0x2,
		Error = 0x4,
		Paused = 0x8
	}

	public static class TaskbarUtility
	{
		[GuidAttribute("56FDF344-FD6D-11d0-958A-006097C9A090")]
		[ClassInterfaceAttribute(ClassInterfaceType.None)]
		[ComImportAttribute()]
		private class CTaskbarList { }

		[ComImportAttribute()]
		[GuidAttribute("ea1afb91-9e28-4b86-90e9-9e9f8a5eefaf")]
		[InterfaceTypeAttribute(ComInterfaceType.InterfaceIsIUnknown)]
		private interface ITaskbarList3
		{
			[PreserveSig]
			void HrInit();
			[PreserveSig]
			void AddTab(IntPtr hwnd);
			[PreserveSig]
			void DeleteTab(IntPtr hwnd);
			[PreserveSig]
			void ActivateTab(IntPtr hwnd);
			[PreserveSig]
			void SetActiveAlt(IntPtr hwnd);

			[PreserveSig]
			void MarkFullscreenWindow(IntPtr hwnd, [MarshalAs(UnmanagedType.Bool)] bool fFullscreen);

			void SetProgressValue(IntPtr hwnd, UInt64 ullCompleted, UInt64 ullTotal);
			void SetProgressState(IntPtr hwnd, TBPFLAG tbpFlags);
			void RegisterTab(IntPtr hwndTab, IntPtr hwndMDI);
			void UnregisterTab(IntPtr hwndTab);
			void SetTabOrder(IntPtr hwndTab, IntPtr hwndInsertBefore);
			void SetTabActive(IntPtr hwndTab, IntPtr hwndMDI, TBATFLAG tbatFlags);
			void ThumbBarAddButtons(IntPtr hwnd, uint cButtons, [MarshalAs(UnmanagedType.LPArray)] THUMBBUTTON[] pButtons);
			void ThumbBarUpdateButtons(IntPtr hwnd, uint cButtons, [MarshalAs(UnmanagedType.LPArray)] THUMBBUTTON[] pButtons);
			void ThumbBarSetImageList(IntPtr hwnd, IntPtr himl);
			void SetOverlayIcon(IntPtr hwnd, IntPtr hIcon, [MarshalAs(UnmanagedType.LPWStr)] string pszDescription);
			void SetThumbnailTooltip(IntPtr hwnd, [MarshalAs(UnmanagedType.LPWStr)] string pszTip);
			void SetThumbnailClip(IntPtr hwnd, ref RECT prcClip);
		}

		private static ITaskbarList3 taskbarList;
		private static ITaskbarList3 TaskbarList
		{
			get
			{
				if (taskbarList == null)
					lock (typeof(TaskbarUtility))
						if (taskbarList == null)
						{
							taskbarList = (ITaskbarList3)new CTaskbarList();
							taskbarList.HrInit();
						}

				return taskbarList;
			}
		}

		[StructLayout(LayoutKind.Sequential)]
		private struct RECT
		{
			public int left;
			public int top;
			public int right;
			public int bottom;

			public RECT(int left, int top, int right, int bottom)
			{
				this.left = left;
				this.top = top;
				this.right = right;
				this.bottom = bottom;
			}
		}

		private enum TBATFLAG
		{
			TBATF_USEMDITHUMBNAIL = 0x1,
			TBATF_USEMDILIVEPREVIEW = 0x2
		}

		private enum TBPFLAG
		{
			TBPF_NOPROGRESS = 0,
			TBPF_INDETERMINATE = 0x1,
			TBPF_NORMAL = 0x2,
			TBPF_ERROR = 0x4,
			TBPF_PAUSED = 0x8
		}

		private enum THBFLAGS
		{
			THBF_ENABLED = 0,
			THBF_DISABLED = 0x1,
			THBF_DISMISSONCLICK = 0x2,
			THBF_NOBACKGROUND = 0x4,
			THBF_HIDDEN = 0x8
		}

		private enum THBMASK
		{
			THB_BITMAP = 0x1,
			THB_ICON = 0x2,
			THB_TOOLTIP = 0x4,
			THB_FLAGS = 0x8
		}

		[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
		private struct THUMBBUTTON
		{
			[MarshalAs(UnmanagedType.U4)]
			public THBMASK dwMask;
			public uint iId;
			public uint iBitmap;
			public IntPtr hIcon;
			[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
			public string szTip;
			[MarshalAs(UnmanagedType.U4)]
			public THBFLAGS dwFlags;
		}

		public static void SetProgressState(IntPtr hwnd, TaskbarProgressState state)
		{
			OperatingSystem os = Environment.OSVersion;
			if (os.Platform == PlatformID.Win32NT && (os.Version.Major > 6 || (os.Version.Major == 6 && os.Version.Minor >= 1)))
				try
				{
					TaskbarList.SetProgressState(hwnd, (TBPFLAG)state);
				}
				catch (Exception)
				{
				}
		}

		public static void SetProgressValue(IntPtr hwnd, ulong current, ulong maximum)
		{
			OperatingSystem os = Environment.OSVersion;
			if (os.Platform == PlatformID.Win32NT && (os.Version.Major > 6 || (os.Version.Major == 6 && os.Version.Minor >= 1)))
				try
				{
					TaskbarList.SetProgressValue(hwnd, current, maximum);
				}
				catch (Exception)
				{
				}
		}

		[StructLayout(LayoutKind.Sequential)]
		private struct FLASHWINFO
		{
			public UInt32 cbSize;
			public IntPtr hwnd;
			public Int32 dwFlags;
			public UInt32 uCount;
			public Int32 dwTimeout;
		}

		private enum FLASHWINFOFLAGS
		{
			FLASHW_STOP = 0,
			FLASHW_CAPTION = 0x1,
			FLASHW_TRAY = 0x2,
			FLASHW_ALL = FLASHW_CAPTION | FLASHW_TRAY,
			FLASHW_TIMER = 0x4,
			FLASHW_TIMERNOFG = 0xC
		}

		[DllImport("user32.dll")]
		private static extern int FlashWindowEx(ref FLASHWINFO pfwi);

		public static void FlashUntilActivated(IntPtr hwnd)
		{
			FLASHWINFO fwi = new FLASHWINFO();
			fwi.cbSize = Convert.ToUInt32(Marshal.SizeOf(typeof(FLASHWINFO)));
			fwi.hwnd = hwnd;
			fwi.dwFlags = (Int32)(FLASHWINFOFLAGS.FLASHW_TRAY | FLASHWINFOFLAGS.FLASHW_TIMERNOFG);
			FlashWindowEx(ref fwi);
		}

		public static void StartFlash(IntPtr hwnd)
		{
			FLASHWINFO fwi = new FLASHWINFO();
			fwi.cbSize = Convert.ToUInt32(Marshal.SizeOf(typeof(FLASHWINFO)));
			fwi.hwnd = hwnd;
			fwi.dwFlags = (Int32)(FLASHWINFOFLAGS.FLASHW_TRAY | FLASHWINFOFLAGS.FLASHW_TIMER);
			FlashWindowEx(ref fwi);
		}

		public static void StopFlash(IntPtr hwnd)
		{
			FLASHWINFO fwi = new FLASHWINFO();
			fwi.cbSize = Convert.ToUInt32(Marshal.SizeOf(typeof(FLASHWINFO)));
			fwi.hwnd = hwnd;
			fwi.dwFlags = (Int32)(FLASHWINFOFLAGS.FLASHW_STOP);
			FlashWindowEx(ref fwi);
		}
	}
}