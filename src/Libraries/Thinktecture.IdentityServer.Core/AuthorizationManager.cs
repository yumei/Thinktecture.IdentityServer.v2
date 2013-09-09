﻿/*
 * Copyright (c) Dominick Baier.  All rights reserved.
 * see license.txt
 */

using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.Linq;
using System.Security.Claims;
using Thinktecture.IdentityModel.Authorization;
using Thinktecture.IdentityModel.Constants;
using Thinktecture.IdentityServer.Repositories;

namespace Thinktecture.IdentityServer
{
    public class AuthorizationManager : ClaimsAuthorizationManager
    {
        [Import]
        public IConfigurationRepository ConfigurationRepository { get; set; }

        public AuthorizationManager()
        {
            Container.Current.SatisfyImportsOnce(this);
        }

        public AuthorizationManager(IConfigurationRepository configuration)
        {
            ConfigurationRepository = configuration;
        }

        public override bool CheckAccess(AuthorizationContext context)
        {
            var action = context.Action.First();
            var id = context.Principal.Identities.First();

            // if application authorization request
            if (action.Type.Equals(ClaimsAuthorization.ActionType))
            {
                return AuthorizeCore(action, context.Resource, context.Principal.Identity as ClaimsIdentity);
            }

            // if ws-trust issue request
            if (action.Value.Equals(WSTrust13Constants.Actions.Issue))
            {
                return AuthorizeTokenIssuance(new Collection<Claim> { new Claim(ClaimsAuthorization.ResourceType, Constants.Resources.WSTrust) }, id);
            }

            return base.CheckAccess(context);
        }

        protected virtual bool AuthorizeCore(Claim action, Collection<Claim> resource, ClaimsIdentity id)
        {
            switch (action.Value)
            {
                case Constants.Actions.Issue:
                    return AuthorizeTokenIssuance(resource, id);
                case Constants.Actions.Administration:
                    return AuthorizeAdministration(resource, id);
                case Constants.Actions.WebApi:
                    return AuthorizeWebApi(resource, id);
            }

            return false;
        }

        protected virtual bool AuthorizeTokenIssuance(Collection<Claim> resource, ClaimsIdentity id)
        {
            if (!ConfigurationRepository.Global.EnforceUsersGroupMembership)
            {
                var authResult = id.IsAuthenticated;
                if (!authResult)
                {
                    Tracing.Error("Authorization for token issuance failed because the user is anonymous");
                }

                return authResult;
            }

            var roleResult = id.HasClaim(ClaimTypes.Role, Constants.Roles.IdentityServerUsers);
            if (!roleResult)
            {
                Tracing.Error(string.Format("Authorization for token issuance failed because user {0} is not in the {1} role", id.Name, Constants.Roles.IdentityServerUsers));
            }

            return roleResult;
        }

        protected virtual bool AuthorizeAdministration(Collection<Claim> resource, ClaimsIdentity id)
        {
            var roleResult = id.HasClaim(ClaimTypes.Role, Constants.Roles.IdentityServerAdministrators);
            if (!roleResult)
            {
                if (resource[0].Value != Constants.Resources.UI)
                {
                    Tracing.Error(string.Format("Administration authorization failed because user {0} is not in the {1} role", id.Name, Constants.Roles.IdentityServerAdministrators));
                }
            }

            return roleResult;
        }

        protected virtual bool AuthorizeWebApi(Collection<Claim> resource, ClaimsIdentity id)
        {
            var roleResult = id.HasClaim(ClaimTypes.Role, Constants.Roles.WebApi);
            if (!roleResult)
            {
                if (resource[0].Value != Constants.Resources.UI)
                {
                    Tracing.Error(string.Format("Web Api authorization failed because user {0} is not in the {1} role", id.Name, Constants.Roles.WebApi));
                }
            }

            return roleResult;
        }
    }
}