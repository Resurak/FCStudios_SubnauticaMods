﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FCSModdingUtility
{
    public interface ICloseable
    {
        event EventHandler CloseRequest;
    }
}