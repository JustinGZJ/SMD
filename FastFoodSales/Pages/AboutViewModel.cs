﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MaterialDesignThemes.Wpf;

namespace DAQ.Pages
{
    public class AboutViewModel : IMainTabViewModel
    {
        public PackIconKind PackIcon { get; set; } = PackIconKind.About;
        public string Header { get; set; } = "关于";
        public bool Visible { get; set; } = true;
        public int TabIndex { get; set; } = (int)Pages.TabIndex.ABOUT;

    }
}
