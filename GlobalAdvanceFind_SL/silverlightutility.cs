// =====================================================================
//
//  This file is part of the Microsoft Dynamics CRM SDK code samples.
//
//  Copyright (C) Microsoft Corporation.  All rights reserved.
//
//  This source code is intended only as a supplement to Microsoft
//  Development Tools and/or on-line documentation.  See these other
//  materials for detailed information regarding Microsoft code samples.
//
//  THIS CODE AND INFORMATION ARE PROVIDED "AS IS" WITHOUT WARRANTY OF ANY
//  KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE
//  IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A
//  PARTICULAR PURPOSE.
//
// =====================================================================
//<snippetSilverLightUtility>

using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Browser;
using System.ServiceModel;
using System.Text;
using System.ServiceModel.Description;
using GlobalAdvanceFind_SL.CrmSDK;

namespace GlobalAdvanceFind_SL.CrmSDK
{
    internal static class SilverlightUtility
    {
        public static string ConvertToString(Exception exception)
        {
            string prefix = "";

            StringBuilder sb = new StringBuilder();
            while (null != exception)
            {
                sb.Append(prefix);
                sb.AppendLine(exception.Message);
                sb.AppendLine(exception.StackTrace);

                prefix = "Innner Exception: ";
                exception = exception.InnerException;
            }

            return sb.ToString();
        }

        public static IOrganizationService GetSoapService()
        {
            Uri serviceUrl = CombineUrl(GetServerBaseUrl(), "/XRMServices/2011/Organization.svc/web");

            BasicHttpBinding binding = new BasicHttpBinding(Uri.UriSchemeHttps == serviceUrl.Scheme
                ? BasicHttpSecurityMode.Transport : BasicHttpSecurityMode.TransportCredentialOnly);
            binding.MaxReceivedMessageSize = int.MaxValue;
            binding.MaxBufferSize = int.MaxValue;
            binding.SendTimeout = TimeSpan.FromMinutes(2);

            return new GlobalAdvanceFind_SL.CrmSDK.OrganizationServiceClient(binding, new EndpointAddress(serviceUrl));
        }


        public static Uri GetServerBaseUrl()
        {
//            return new Uri("http://tk3cdevweb5/CCDDemo");

            string serverUrl = (string)GetContext().Invoke("getServerUrl");
            //Remove the trailing forwards slash returned by CRM Online
            //So that it is always consistent with CRM On Premises
            if (serverUrl.EndsWith("/"))
                serverUrl = serverUrl.Substring(0, serverUrl.Length - 1);

            return new Uri(serverUrl);
        }
        /// <summary>
        /// Combines a URI with a relative URI
        /// </summary>
        /// <param name="baseValue">Base (absolute) URI</param>
        /// <param name="value">Relative URI that is to be used</param>
        /// <returns>Combined URI</returns>
        public static Uri CombineUrl(Uri baseValue, string value)
        {
            if (null == baseValue)
            {
                throw new ArgumentNullException("baseValue");
            }
            else if (string.IsNullOrEmpty(value))
            {
                return baseValue;
            }

            //Ensure that a double '/' is not being added
            string newValue = baseValue.AbsoluteUri;
            if (!newValue.EndsWith("/", StringComparison.Ordinal))
            {
                //Check if there is a character at the beginning of value
                if (!value.StartsWith("/", StringComparison.Ordinal))
                {
                    newValue += "/";
                }
            }
            else if (value.StartsWith("/", StringComparison.Ordinal))
            {
                value = value.Substring(1);
            }

            //Create the combined URL
            return new Uri(newValue + value);
        }

        #region Private Methods
        private static ScriptObject GetContext()
        {
            ScriptObject xrmProperty = (ScriptObject)HtmlPage.Window.GetProperty("Xrm");
            if (null == xrmProperty)
            {
                //It may be that the global context should be used
                try
                {

                    ScriptObject globalContext = (ScriptObject)HtmlPage.Window.Invoke("GetGlobalContext");

                    return globalContext;
                }
                catch (System.InvalidOperationException)
                {
                    throw new InvalidOperationException("Property \"Xrm\" is null and the Global Context is not available.");
                }

            }

            ScriptObject pageProperty = (ScriptObject)xrmProperty.GetProperty("Page");
            if (null == pageProperty)
            {
                throw new InvalidOperationException("Property \"Xrm.Page\" is null");
            }

            ScriptObject contextProperty = (ScriptObject)pageProperty.GetProperty("context");
            if (null == contextProperty)
            {
                throw new InvalidOperationException("Property \"Xrm.Page.context\" is null");
            }

            return contextProperty;
        }
        #endregion


    }
}
//</snippetSilverLightUtility>