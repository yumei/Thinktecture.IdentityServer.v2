﻿/*
 * Copyright (c) Dominick Baier, Brock Allen.  All rights reserved.
 * see license.txt
 */

using System;

namespace Thinktecture.IdentityServer.Protocols.OAuth2
{
    [Serializable]
    public class AuthorizeRequestResourceOwnerException : AuthorizeRequestValidationException
    {
        public AuthorizeRequestResourceOwnerException(string message) : base(message)
        { }
    }
}
   
