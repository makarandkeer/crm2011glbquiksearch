// <copyright file="PostPublishAll.cs" company="Microsoft IT">
// Copyright (c) 2012 All Rights Reserved
// </copyright>
// <author>Microsoft IT</author>
// <date>9/17/2012 1:17:08 AM</date>
// <summary>Implements the PostPublishAll Plugin.</summary>
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.1
// </auto-generated>
namespace CRMUtil.ISV.GlobalQuickSearch_Plugin
{
    using System;
    using System.ServiceModel;
    using System.Xml;
    using System.IO;
    using System.Xml.Linq;
    using System.Collections.Generic;
    using System.Text;
    using System.Linq;

    using Microsoft.Crm.Sdk;
    using Microsoft.Crm.Sdk.Messages;
    using Microsoft.Xrm;
    using Microsoft.Xrm.Sdk;
    using Microsoft.Xrm.Sdk.Client;
    using Microsoft.Xrm.Sdk.Linq;
    using Microsoft.Xrm.Sdk.Messages;
    using Microsoft.Xrm.Sdk.Metadata;
    using Microsoft.Xrm.Sdk.Query;

    /// <summary>
    /// PostPublishAll Plugin.
    /// </summary>    
    public class PostPublishAll: Plugin
    {
        protected List<EntityMetadata> _entityMetadataList = new List<EntityMetadata>();

        /// <summary>
        /// Initializes a new instance of the <see cref="PostPublishAll"/> class.
        /// </summary>
        public PostPublishAll()
            : base(typeof(PostPublishAll))
        {
            base.RegisteredEvents.Add(new Tuple<int, string, string, Action<LocalPluginContext>>(40, "PublishAll", null, new Action<LocalPluginContext>(ExecutePostPublishAll)));

            // Note : you can register for more events here if this plugin is not specific to an individual entity and message combination.
            // You may also need to update your RegisterFile.crmregister plug-in registration file to reflect any change.
        }

        /// <summary>
        /// Executes the plug-in.
        /// </summary>
        /// <param name="localContext">The <see cref="LocalPluginContext"/> which contains the
        /// <see cref="IPluginExecutionContext"/>,
        /// <see cref="IOrganizationService"/>
        /// and <see cref="ITracingService"/>
        /// </param>
        /// <remarks>
        /// For improved performance, Microsoft Dynamics CRM caches plug-in instances.
        /// The plug-in's Execute method should be written to be stateless as the constructor
        /// is not called for every invocation of the plug-in. Also, multiple system threads
        /// could execute the plug-in at the same time. All per invocation state information
        /// is stored in the context. This means that you should not use global variables in plug-ins.
        /// </remarks>
        protected void ExecutePostPublishAll(LocalPluginContext localContext)
        {
            if (localContext == null)
            {
                throw new ArgumentNullException("localContext");
            }

            // TODO: Implement your custom Plug-in business logic.
            QueryByAttribute query = new QueryByAttribute("gqs_globalquicksearchconfig");
            query.AddAttributeValue("statecode", 0);

            query.ColumnSet = new ColumnSet("gqs_globalquicksearchconfigid", "gqs_name");

            EntityCollection ec = localContext.OrganizationService.RetrieveMultiple(query);

            if (ec != null && ec.Entities.Count > 0)
            {
                foreach (Entity e in ec.Entities)
                {
                    string entityLogicalName = e["gqs_name"].ToString();

                    base.GetAndSetFetchXml(entityLogicalName, e.Id, localContext.OrganizationService);
                }
            }
        }
                
    }
    
}
