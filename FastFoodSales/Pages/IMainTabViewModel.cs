﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MaterialDesignThemes.Wpf;

namespace DAQ.Pages
{
    public interface IMainTabViewModel
    {
        int TabIndex { get; set; }
        PackIconKind PackIcon { get; set; }
        string Header { get; set; }
        bool Visible { get; set; }        
    }

    public enum TabIndex
    {
        RUNSTATUS,OEE,LASER,SCANNER,MESSAGES,VALUES,SETTING,ABOUT    
    }

}
