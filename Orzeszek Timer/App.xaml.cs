﻿//
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
using System.Configuration;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows;

namespace OrzeszekTimer
{
	/// <summary>
	/// Interaction logic for App.xaml
	/// </summary>
	public partial class App : Application
	{
		private void Application_Startup(object sender, StartupEventArgs e)
		{
			if (e.Args != null && e.Args.Length > 0)
			{
				StringBuilder sb = new StringBuilder();
				foreach (string s in e.Args)
					sb.Append(sb.Length == 0 ? s : " " + s);
				Properties["Args"] = sb.ToString();
			}
		}
	}
}