﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace QuickBooksMVCPoC.Models
{
    public class QuickBooksSettings
    {
        public string ClientId { get; set; }
        public string ClientSecret { get; set; }
        public string RedirectUri { get; set; }
        public string Environment { get; set; }
    }
}