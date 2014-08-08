﻿using System;
using System.Linq;
using Inedo.BuildMaster;
using Inedo.BuildMaster.Extensibility.Agents;
using Inedo.BuildMaster.Extensibility.Configurers.Extension;
using Inedo.BuildMaster.Web;
using Microsoft.TeamFoundation.Build.Client;

[assembly: ExtensionConfigurer(typeof(Inedo.BuildMasterExtensions.TFS.TfsConfigurer))]

namespace Inedo.BuildMasterExtensions.TFS
{
    [CustomEditor(typeof(TfsConfigurerEditor))]
    public sealed class TfsConfigurer : ExtensionConfigurerBase
    {
        public static readonly string TypeQualifiedName = typeof(TfsConfigurer).FullName + "," + typeof(TfsConfigurer).Assembly.GetName().Name;

        /// <summary>
        /// Gets or sets the server identifier.
        /// </summary>
        [Persistent]
        public int? ServerId { get; set; }
        /// <summary>
        /// The base url of the TFS store, should not include collection name, e.g. "http://server:port/tfs"
        /// </summary>
        [Persistent]
        public string BaseUrl { get; set; }
        /// <summary>
        /// The username used to connect to the server
        /// </summary>
        [Persistent]
        public string UserName { get; set; }
        /// <summary>
        /// The password used to connect to the server
        /// </summary>
        [Persistent]
        public string Password { get; set; }
        /// <summary>
        /// The domain of the server
        /// </summary>
        [Persistent]
        public string Domain { get; set; }
        /// <summary>
        /// Returns true if BuildMaster should connect to TFS using its own account, false if the credentials are specified
        /// </summary>
        [Persistent]
        public bool UseSystemCredentials { get; set; }

        public Uri BaseUri { get { return new Uri(this.BaseUrl); } }

        /// <summary>
        /// Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String" /> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return string.Empty;
        }

        internal TfsBuildInfo GetBuildInfo(string teamProject, string buildDefinition, string buildNumber, bool includeUnsuccessful)
        {
            using (var agent = Util.Agents.CreateAgentFromId(this.ServerId))
            {
                var methodExecuter = agent.GetService<IRemoteMethodExecuter>();
                return methodExecuter.InvokeFunc(GetBuildInfoInternal, teamProject, buildDefinition, buildNumber, includeUnsuccessful);
            }
        }

        private TfsBuildInfo GetBuildInfoInternal(string teamProject, string buildDefinition, string buildNumber, bool includeUnsuccessful)
        {
            using (var collection = TfsActionBase.GetTeamProjectCollection(this))
            {
                var buildService = collection.GetService<IBuildServer>();

                var spec = buildService.CreateBuildDetailSpec(teamProject, InedoLib.Util.CoalesceStr(buildDefinition, "*"));
                spec.BuildNumber = InedoLib.Util.CoalesceStr(buildNumber, "*");
                spec.MaxBuildsPerDefinition = 1;
                spec.QueryOrder = BuildQueryOrder.FinishTimeDescending;
                spec.Status = includeUnsuccessful ? (BuildStatus.Failed | BuildStatus.Succeeded | BuildStatus.PartiallySucceeded) : BuildStatus.Succeeded;

                var result = buildService.QueryBuilds(spec).Builds.FirstOrDefault();
                if (result != null)
                    return new TfsBuildInfo(result);
                else
                    return null;
            }
        }
    }
}