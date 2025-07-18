﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Abstractions;

namespace Microsoft.Identity.Web
{
    internal sealed class DefaultAuthorizationHeaderProvider : IAuthorizationHeaderProvider
    {
        private readonly ITokenAcquisition _tokenAcquisition;

        public DefaultAuthorizationHeaderProvider(ITokenAcquisition tokenAcquisition)
        {
            _tokenAcquisition = tokenAcquisition;
        }

        /// <inheritdoc/>
        public async Task<string> CreateAuthorizationHeaderForUserAsync(
            IEnumerable<string> scopes,
            AuthorizationHeaderProviderOptions? downstreamApiOptions = null,
            ClaimsPrincipal? claimsPrincipal = null,
            CancellationToken cancellationToken = default)
        {
            var newTokenAcquisitionOptions = CreateTokenAcquisitionOptionsFromApiOptions(downstreamApiOptions, cancellationToken);
            var result = await _tokenAcquisition.GetAuthenticationResultForUserAsync(
                scopes,
                downstreamApiOptions?.AcquireTokenOptions.AuthenticationOptionsName,
                downstreamApiOptions?.AcquireTokenOptions.Tenant,
                downstreamApiOptions?.AcquireTokenOptions.UserFlow,
                claimsPrincipal,
                newTokenAcquisitionOptions).ConfigureAwait(false);

            UpdateOriginalTokenAcquisitionOptions(downstreamApiOptions?.AcquireTokenOptions, newTokenAcquisitionOptions);
            return result.CreateAuthorizationHeader();
        }

        /// <inheritdoc/>
        public async Task<string> CreateAuthorizationHeaderForAppAsync(
            string scopes,
            AuthorizationHeaderProviderOptions? downstreamApiOptions = null,
            CancellationToken cancellationToken = default)
        {
            var result = await _tokenAcquisition.GetAuthenticationResultForAppAsync(
                scopes,
                downstreamApiOptions?.AcquireTokenOptions.AuthenticationOptionsName,
                downstreamApiOptions?.AcquireTokenOptions.Tenant,
                CreateTokenAcquisitionOptionsFromApiOptions(downstreamApiOptions, cancellationToken)).ConfigureAwait(false);

            return result.CreateAuthorizationHeader();
        }

        /// <inheritdoc/>
        public async Task<string> CreateAuthorizationHeaderAsync(
            IEnumerable<string> scopes,
            AuthorizationHeaderProviderOptions? downstreamApiOptions = null,
            ClaimsPrincipal? claimsPrincipal = null,
            CancellationToken cancellationToken = default)
        {
            Client.AuthenticationResult result;
            var newTokenAcquisitionOptions = CreateTokenAcquisitionOptionsFromApiOptions(downstreamApiOptions, cancellationToken);

            // Previously, with the API name we were able to distinguish between app and user token acquisition
            // This context is missing in the new API, so can we enforce that downstreamApiOptions.RequestAppToken
            // needs to be set to true to acquire a token for the app. We cannot rely on ClaimsPrincipal as it can be null for user token acquisition.
            // DevEx Before:
            // await authorizationHeaderProvider.CreateAuthorizationHeaderForAppAsync("https://graph.microsoft.com/.default").ConfigureAwait(false);
            // DevEx with the new API:
            // await authorizationHeaderProvider.CreateAuthorizationHeaderAsync(
            //  new [] { "https://graph.microsoft.com/.default" },
            //  new AuthorizationHeaderProviderOptions { RequestAppToken = true }).ConfigureAwait(false);
            if (downstreamApiOptions != null && (downstreamApiOptions.RequestAppToken || downstreamApiOptions.AcquireTokenOptions?.ManagedIdentity != null))
            {
                result = await _tokenAcquisition.GetAuthenticationResultForAppAsync(
                    scopes.FirstOrDefault()!,
                    downstreamApiOptions?.AcquireTokenOptions.AuthenticationOptionsName,
                    downstreamApiOptions?.AcquireTokenOptions.Tenant,
                    newTokenAcquisitionOptions).ConfigureAwait(false);
            }
            else
            {
                result = await _tokenAcquisition.GetAuthenticationResultForUserAsync(
                    scopes,
                    downstreamApiOptions?.AcquireTokenOptions?.AuthenticationOptionsName,
                    downstreamApiOptions?.AcquireTokenOptions?.Tenant,
                    downstreamApiOptions?.AcquireTokenOptions?.UserFlow,
                    claimsPrincipal,
                    newTokenAcquisitionOptions).ConfigureAwait(false);
            }

            UpdateOriginalTokenAcquisitionOptions(downstreamApiOptions?.AcquireTokenOptions, newTokenAcquisitionOptions);
            return result.CreateAuthorizationHeader();
        }

        private static TokenAcquisitionOptions CreateTokenAcquisitionOptionsFromApiOptions(
            AuthorizationHeaderProviderOptions? downstreamApiOptions,
            CancellationToken cancellationToken)
        {
            return new TokenAcquisitionOptions()
            {
                AuthenticationOptionsName = downstreamApiOptions?.AcquireTokenOptions.AuthenticationOptionsName,
                CancellationToken = cancellationToken,
                Claims = downstreamApiOptions?.AcquireTokenOptions.Claims,
                CorrelationId = downstreamApiOptions?.AcquireTokenOptions.CorrelationId ?? Guid.Empty,
                ExtraHeadersParameters = downstreamApiOptions?.AcquireTokenOptions.ExtraHeadersParameters,
                ExtraQueryParameters = downstreamApiOptions?.AcquireTokenOptions.ExtraQueryParameters,
                ExtraParameters = downstreamApiOptions?.AcquireTokenOptions.ExtraParameters,
                ForceRefresh = downstreamApiOptions?.AcquireTokenOptions.ForceRefresh ?? false,
                LongRunningWebApiSessionKey = downstreamApiOptions?.AcquireTokenOptions.LongRunningWebApiSessionKey,
                ManagedIdentity = downstreamApiOptions?.AcquireTokenOptions.ManagedIdentity,
                Tenant = downstreamApiOptions?.AcquireTokenOptions.Tenant,
                UserFlow = downstreamApiOptions?.AcquireTokenOptions.UserFlow,
                PopPublicKey = downstreamApiOptions?.AcquireTokenOptions.PopPublicKey,
                FmiPath = downstreamApiOptions?.AcquireTokenOptions.FmiPath,
            };
        }

        /// <summary>
        /// Since AcquireTokenOptions is recreated, we need to update the original TokenAcquisitionOptions wth the parameters that were
        /// updated in the new TokenAcquisitionOptions.
        /// </summary>
        private void UpdateOriginalTokenAcquisitionOptions(AcquireTokenOptions? acquireTokenOptions, TokenAcquisitionOptions newTokenAcquisitionOptions)
        {
            if (acquireTokenOptions is not null && newTokenAcquisitionOptions is not null)
            {
                acquireTokenOptions.LongRunningWebApiSessionKey = newTokenAcquisitionOptions.LongRunningWebApiSessionKey;
            }
        }
    }
}
