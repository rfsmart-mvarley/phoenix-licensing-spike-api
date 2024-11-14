﻿using Amazon.Runtime.Internal;
using Dapper;
using Microsoft.Extensions.Logging;
using Rfsmart.Phoenix.Common.Context;
using Rfsmart.Phoenix.Common.Models;
using Rfsmart.Phoenix.Database.Services;
using Rfsmart.Phoenix.Licensing.Interfaces;
using Rfsmart.Phoenix.Licensing.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rfsmart.Phoenix.Licensing.Persistence
{
    [ExcludeFromCodeCoverage]
    public class FeatureIssueRepository(
        ILogger<FeatureIssueRepository> _logger,
        IDbConnectionProvider _connectionProvider,
        IContextProvider<UserContext> _userContextProvider,
        IContextProvider<TenantContext> _tenantContextProvider
    ) : TenantRepositoryBase(_connectionProvider, _userContextProvider, _logger), IFeatureIssueRepository
    {
        private const string SelectColumnList =
            "created,created_by,last_modified,last_modified_by,feature_name,enabled_time,disabled_time,licensed_users";

        private static readonly Dictionary<FeatureIssueSort, string> _sortableFields =
            new()
            {
                [FeatureIssueSort.Created] = "created",
                [FeatureIssueSort.FeatureName] = "feature_name",
            };

        public async Task<FeatureIssueRecord> Insert(FeatureIssueRecord request)
        {
            _logger.LogDebug("Insert {@Request}", request);

            var featureRecord = await Exec(
                _tenantContextProvider.Context!,
                db =>
                    db.QuerySingleAsync<FeatureIssueRecord>(
                        $"""
                        insert into feature_issue
                        (
                            created,
                            created_by,
                            last_modified,
                            last_modified_by,
                            feature_name,
                            enabled_time,
                            disabled_time,
                            licensed_users
                        ) 
                        values 
                        (
                            CURRENT_TIMESTAMP,
                            @Caller,
                            CURRENT_TIMESTAMP,
                            @Caller,
                            @FeatureName,
                            @EnabledTime,
                            @DisabledTime,
                            @LicensedUsers
                        )
                        RETURNING {SelectColumnList};
                        """,
                        new
                        {
                            Caller,
                            request.FeatureName,
                            request.EnabledTime,
                            request.DisabledTime,
                            request.LicensedUsers,
                        }
                    )
            );

            return featureRecord
                ?? throw new DataMisalignedException(
                    "Feature record could not be retrieved after creation"
                );
        }

        public async Task<FeatureIssueRecord?> Get(string featureName)
        {
            _logger.LogDebug("Get {@Request}", featureName);

            var featureRecord = await Exec(
                _tenantContextProvider.Context!,
                db =>
                    db.QuerySingleAsync<FeatureIssueRecord>(
                        $"""
                        select * from feature_issue
                        where feature_name = @featureName
                        ORDER BY created DESC LIMIT 1;
                        """,
                        new
                        {
                            featureName,
                        }
                    )
            );

            return featureRecord;
        }
    }
}