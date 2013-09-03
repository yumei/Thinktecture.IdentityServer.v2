﻿/*
 * Copyright (c) Dominick Baier.  All rights reserved.
 * see license.txt
 */

using System;
using System.ComponentModel.Composition;
using System.IdentityModel.Configuration;
using System.Reflection;
using System.ServiceModel;
using System.ServiceModel.Activation;
using System.ServiceModel.Description;
using System.ServiceModel.Security;
using Thinktecture.IdentityModel.WSTrust;
using Thinktecture.IdentityServer.Repositories;

namespace Thinktecture.IdentityServer.Protocols.WSTrust
{
    /// <summary>
    /// Abstracts away the details of the WS-Trust ServiceHost creation and configuration
    /// </summary>
    public class TokenServiceHostFactory : ServiceHostFactory
    {
        [Import]
        public IConfigurationRepository ConfigurationRepository { get; set; }

        public TokenServiceHostFactory() : base()
        {
            Container.Current.SatisfyImportsOnce(this);
        }

        /// <summary>
        /// Creates a service host to process WS-Trust 1.3 requests
        /// </summary>
        /// <param name="constructorString">The constructor string.</param>
        /// <param name="baseAddresses">The base addresses.</param>
        /// <returns>A WS-Trust ServiceHost</returns>
        public override ServiceHostBase CreateServiceHost(string constructorString, Uri[] baseAddresses)
        {
            var globalConfiguration = ConfigurationRepository.Global;
            var config = CreateSecurityTokenServiceConfiguration(constructorString);
            var host = new WSTrustServiceHost(config, baseAddresses);
            
            // add behavior for load balancing support
            host.Description.Behaviors.Add(new UseRequestHeadersForMetadataAddressBehavior());

            // modify address filter mode for load balancing
            var serviceBehavior = host.Description.Behaviors.Find<ServiceBehaviorAttribute>();
            serviceBehavior.AddressFilterMode = AddressFilterMode.Any;

            // add and configure a mixed mode security endpoint
            if (ConfigurationRepository.WSTrust.Enabled && 
                ConfigurationRepository.WSTrust.EnableMixedModeSecurity &&
                !ConfigurationRepository.Global.DisableSSL)
            {
                EndpointIdentity epi = null;
                
                if (ConfigurationRepository.WSTrust.EnableClientCertificateAuthentication)
                {
                    var sep2 = host.AddServiceEndpoint(
                        typeof(IWSTrust13SyncContract),
                        new CertificateWSTrustBinding(SecurityMode.TransportWithMessageCredential),
                        Endpoints.Paths.WSTrustMixedCertificate);

                    if (epi != null)
                    {
                        sep2.Address = new EndpointAddress(sep2.Address.Uri, epi);
                    }
                }

                var sep = host.AddServiceEndpoint(
                    typeof(IWSTrust13SyncContract),
                    new UserNameWSTrustBinding(SecurityMode.TransportWithMessageCredential),
                    Endpoints.Paths.WSTrustMixedUserName);

                if (epi != null)
                {
                    sep.Address = new EndpointAddress(sep.Address.Uri, epi);
                }
            }

            // add and configure a message security endpoint
            if (ConfigurationRepository.WSTrust.Enabled && ConfigurationRepository.WSTrust.EnableMessageSecurity)
            {
                var credential = new ServiceCredentials();
                credential.ServiceCertificate.Certificate = ConfigurationRepository.Keys.SigningCertificate;
                host.Description.Behaviors.Add(credential);

                if (ConfigurationRepository.WSTrust.EnableClientCertificateAuthentication)
                {
                    host.AddServiceEndpoint(
                        typeof(IWSTrust13SyncContract),
                        new CertificateWSTrustBinding(SecurityMode.Message),
                        Endpoints.Paths.WSTrustMessageCertificate);
                }

                host.AddServiceEndpoint(
                    typeof(IWSTrust13SyncContract),
                    new UserNameWSTrustBinding(SecurityMode.Message),
                    Endpoints.Paths.WSTrustMessageUserName);
            }

            return host;
        }

        protected virtual SecurityTokenServiceConfiguration CreateSecurityTokenServiceConfiguration(string constructorString)
        {
            Type type = Type.GetType(constructorString, true);
            if (!type.IsSubclassOf(typeof(SecurityTokenServiceConfiguration)))
            {
                throw new InvalidOperationException("SecurityTokenServiceConfiguration");
            }

            return (Activator.CreateInstance(
                type, 
                BindingFlags.CreateInstance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance, 
                null, 
                null, 
                null) as SecurityTokenServiceConfiguration);
        }
    }
}