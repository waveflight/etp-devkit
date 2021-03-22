﻿//----------------------------------------------------------------------- 
// ETP DevKit, 1.2
//
// Copyright 2019 Energistics
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//   
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//-----------------------------------------------------------------------

using System;
using Energistics.Etp.Common;
using System.Collections.Generic;
using Energistics.Etp.Data;
using Energistics.Etp.Common.Datatypes;

namespace Energistics.Etp.Handlers
{
    public class CustomerDiscoveryHandler : Handler
    {
        protected override void InitializeRegistrarCore()
        {
            if (Registrar.IsEtpVersionSupported(EtpVersion.v11))
            {
                Registrar.Register(new v11.Protocol.Discovery.DiscoveryCustomerHandler());
            }
            if (Registrar.IsEtpVersionSupported(EtpVersion.v12))
            {
                Registrar.Register(new v12.Protocol.Dataspace.DataspaceCustomerHandler());
                Registrar.Register(new v12.Protocol.Discovery.DiscoveryCustomerHandler());
                Registrar.Register(new v12.Protocol.SupportedTypes.SupportedTypesCustomerHandler());
            }
        }

        protected override void InitializeSessionCore()
        {
            if (Session.EtpVersion == EtpVersion.v11)
            {
                Session.Handler<v11.Protocol.Discovery.IDiscoveryCustomer>().OnGetResourcesResponse += OnGetResourcesResponse;
            }
            else if (Session.EtpVersion == EtpVersion.v12)
            {
                Session.Handler<v12.Protocol.Dataspace.IDataspaceCustomer>().OnGetDataspacesResponse += OnGetDataspacesResponse;
                Session.Handler<v12.Protocol.Discovery.IDiscoveryCustomer>().OnGetResourcesResponse += OnGetResourcesResponse;
                Session.Handler<v12.Protocol.Discovery.IDiscoveryCustomer>().OnGetDeletedResourcesResponse += OnGetDeletedResourcesResponse;
                Session.Handler<v12.Protocol.SupportedTypes.ISupportedTypesCustomer>().OnGetSupportedTypesResponse += OnGetSupportedTypesResponse;
            }
        }


        public override void PrintConsoleOptions()
        {
            Console.WriteLine(" D - Discovery");
        }

        public override bool HandleConsoleInput(ConsoleKeyInfo info)
        {
            if (!IsKey(info, "D"))
                return false;

            var version = Session.EtpVersion;

            if (version == EtpVersion.v12)
                Console.WriteLine(" D - Dataspace - GetDataspaces");
            Console.WriteLine(" R - Discovery - GetResources");
            if (version == EtpVersion.v12)
                Console.WriteLine(" Q - Discovery - GetDeletedResources");
            if (version == EtpVersion.v12)
                Console.WriteLine(" S - SupportedTypes - GetSupportedTypes");
            Console.WriteLine();

            info = Console.ReadKey();

            Console.WriteLine(" - processing...");
            Console.WriteLine();

            if (IsKey(info, "D"))
            {
                if (version == EtpVersion.v12)
                {
                    Console.WriteLine("Getting dataspaces...");

                    Session.Handler<v12.Protocol.Dataspace.IDataspaceCustomer>().GetDataspaces();
                }
            }
            else if (IsKey(info, "R"))
            {
                Console.WriteLine("Enter resource URI:");
                var uri = Console.ReadLine();
                Console.WriteLine();

                if (version == EtpVersion.v11)
                    Session.Handler<v11.Protocol.Discovery.IDiscoveryCustomer>().GetResources(uri);
                else if (version == EtpVersion.v12)
                {
                    var context = GetContext(uri);
                    Session.Handler<v12.Protocol.Discovery.IDiscoveryCustomer>().GetResources(context.ContextInfo12, context.ContextScopeKind12, countObjects: true);
                }
            }
            else if (IsKey(info, "Q"))
            {
                if (version == EtpVersion.v12)
                {
                    Console.WriteLine("Enter deleted resources URI:");
                    var uri = Console.ReadLine();
                    Console.WriteLine();

                    Session.Handler<v12.Protocol.Discovery.IDiscoveryCustomer>().GetDeletedResources(uri);
                }
            }
            else if (IsKey(info, "S"))
            {
                if (version == EtpVersion.v12)
                {
                    Console.WriteLine("Enter supported type URI:");
                    var uri = Console.ReadLine();
                    Console.WriteLine();

                    Session.Handler<v12.Protocol.SupportedTypes.ISupportedTypesCustomer>().GetSupportedTypes(uri, v12.Datatypes.Object.ContextScopeKind.sources, returnEmptyTypes: true, countObjects: true);
                }
            }

            return true;
        }

        private void OnGetDataspacesResponse(object sender, ResponseEventArgs<v12.Protocol.Dataspace.GetDataspaces, v12.Protocol.Dataspace.GetDataspacesResponse> e)
        {
            if (e.Response != null)
                Console.WriteLine(EtpExtensions.Serialize(e.Response.Body.Dataspaces, true));
        }

        private void OnGetResourcesResponse(object sender, ResponseEventArgs<v11.Protocol.Discovery.GetResources, v11.Protocol.Discovery.GetResourcesResponse> e)
        {
            Console.WriteLine(EtpExtensions.Serialize(e.Response.Body.Resource, true));
        }

        private void OnGetResourcesResponse(object sender, DualResponseEventArgs<v12.Protocol.Discovery.GetResources, v12.Protocol.Discovery.GetResourcesResponse, v12.Protocol.Discovery.GetResourcesEdgesResponse> e)
        {
            if (e.Response1 != null)
                Console.WriteLine(EtpExtensions.Serialize(e.Response1.Body.Resources, true));
            if (e.Response2 != null)
                Console.WriteLine(EtpExtensions.Serialize(e.Response2.Body.Edges, true));
        }

        private void OnGetDeletedResourcesResponse(object sender, ResponseEventArgs<v12.Protocol.Discovery.GetDeletedResources, v12.Protocol.Discovery.GetDeletedResourcesResponse> e)
        {
            if (e.Response != null)
                Console.WriteLine(EtpExtensions.Serialize(e.Response.Body.DeletedResources, true));
        }

        private void OnGetSupportedTypesResponse(object sender, ResponseEventArgs<v12.Protocol.SupportedTypes.GetSupportedTypes, v12.Protocol.SupportedTypes.GetSupportedTypesResponse> e)
        {
            if (e.Response != null)
                Console.WriteLine(EtpExtensions.Serialize(e.Response.Body.SupportedTypes, true));
        }

        private MockGraphContext GetContext(string uri)
        {
            var context = new MockGraphContext
            {
                Uri = new EtpUri(uri),
                DataObjectTypes = new HashSet<EtpDataObjectType>(),
                Depth = 1,
                IncludeSelf = false,
                IncludeSources = true,
                IncludeTargets = false,
                IncludeSecondarySources = false,
                IncludeSecondaryTargets = false,
                NavigatePrimaryEdges = true,
                NavigateSecondaryEdges = false,
            };

            while (true)
            {
                Console.WriteLine("Choose an option:");
                Console.WriteLine(" D - Set depth");
                Console.WriteLine($" S - {(context.IncludeSelf ? "Exclude" : "Include")} Self");
                Console.WriteLine($" O - {(context.IncludeSources ? "Exclude" : "Include")} Sources");
                Console.WriteLine($" P - {(context.IncludeSecondarySources ? "Exclude" : "Include")} Secondary Sources");
                Console.WriteLine($" T - {(context.IncludeTargets ? "Exclude" : "Include")} Targets");
                Console.WriteLine($" U - {(context.IncludeSecondaryTargets ? "Exclude" : "Include")} Secondary Targets");
                Console.WriteLine($" 1 - {(context.NavigatePrimaryEdges ? "Do Not " : "")}Navigate Primary Edges");
                Console.WriteLine($" 2 - {(context.NavigateSecondaryEdges ? "Do Not " : "")}Navigate Primary Edges");
                Console.WriteLine($" Y - Add Data Types");
                Console.WriteLine($" (enter / other) - Submit request");
                Console.WriteLine();

                var info = Console.ReadKey();

                Console.WriteLine(" - processing...");
                Console.WriteLine();
                if (IsKey(info, "D"))
                {
                    Console.WriteLine("Enter depth:");
                    var depth = Console.ReadLine();
                    int depthValue;
                    if (int.TryParse(depth, out depthValue))
                        context.Depth = depthValue;
                }
                else if (IsKey(info, "S"))
                    context.IncludeSelf = !context.IncludeSelf;
                else if (IsKey(info, "O"))
                    context.IncludeSources = !context.IncludeSources;
                else if (IsKey(info, "P"))
                    context.IncludeSecondarySources = !context.IncludeSecondarySources;
                else if (IsKey(info, "T"))
                    context.IncludeTargets = !context.IncludeTargets;
                else if (IsKey(info, "U"))
                    context.IncludeSecondaryTargets = !context.IncludeSecondaryTargets;
                else if (IsKey(info, "1"))
                    context.NavigatePrimaryEdges = !context.NavigatePrimaryEdges;
                else if (IsKey(info, "2"))
                    context.NavigateSecondaryEdges = !context.NavigateSecondaryEdges;
                else if (IsKey(info, "Y"))
                {
                    Console.WriteLine("Enter data type:");
                    var dataType = Console.ReadLine();
                    var dataTypeValue = new EtpDataObjectType(dataType);
                    if (dataTypeValue.IsValid)
                        context.DataObjectTypes.Add(dataTypeValue);
                }
                else
                    break;
            }

            return context;
        }
    }
}