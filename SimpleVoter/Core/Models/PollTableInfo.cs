﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Helpers;
using SimpleVoter.Core.Enums;

namespace SimpleVoter.Core.Models
{
    public class PollTableInfo
    {
        public string SearchText { get; set; }
        public SortBy SortBy { get; set; }
        public SortDirection SortDirection { get; set; }
        public PagingInfo PagingInfo { get; set; }
    }
}