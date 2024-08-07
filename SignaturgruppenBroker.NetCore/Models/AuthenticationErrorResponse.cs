﻿using Microsoft.AspNetCore.Mvc;

namespace NetsBrokerIntegration.NetCore.Models
{
    public class AuthenticationErrorResponse
    {
        [BindProperty(Name = "error", SupportsGet = true)]
        public string Error { get; set; }
        public string Error_Description { get; set; }
    }
}
