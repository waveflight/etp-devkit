﻿//----------------------------------------------------------------------- 
// ETP DevKit, 1.2
//
// Copyright 2018 Energistics
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

using System.Collections.Generic;
using System.Linq;
using Avro.IO;
using Energistics.Etp.Common;
using Energistics.Etp.Common.Datatypes;
using Energistics.Etp.v12.Datatypes.Object;

namespace Energistics.Etp.v12.Protocol.StoreQuery
{
    /// <summary>
    /// Base implementation of the <see cref="IStoreQueryStore"/> interface.
    /// </summary>
    /// <seealso cref="Energistics.Etp.Common.EtpProtocolHandler" />
    /// <seealso cref="Energistics.Etp.v12.Protocol.StoreQuery.IStoreQueryStore" />
    public class StoreQueryStoreHandler : EtpProtocolHandler, IStoreQueryStore
    {
        /// <summary>
        /// The MaxResponseCount protocol capability key.
        /// </summary>
        public const string MaxResponseCount = "MaxResponseCount";

        /// <summary>
        /// Initializes a new instance of the <see cref="StoreQueryStoreHandler"/> class.
        /// </summary>
        public StoreQueryStoreHandler() : base((int)Protocols.StoreQuery, "store", "customer")
        {
        }

        /// <summary>
        /// Sends a FindObjectsResponse message to a customer.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <param name="objects">The list of <see cref="DataObject" /> objects.</param>
        /// <param name="sortOrder">The sort order.</param>
        /// <returns>The message identifier.</returns>
        public virtual long FindObjectsResponse(IMessageHeader request, IList<DataObject> objects, string sortOrder)
        {
            if (!objects.Any())
            {
                return Acknowledge(request.MessageId, MessageFlags.NoData);
            }

            long messageId = 0;

            for (var i=0; i<objects.Count; i++)
            {
                var messageFlags = i < objects.Count - 1
                    ? MessageFlags.MultiPart
                    : MessageFlags.FinalPart;

                var header = CreateMessageHeader(Protocols.StoreQuery, MessageTypes.StoreQuery.FindObjectsResponse, request.MessageId, messageFlags);

                var findObjectsResponse = new FindObjectsResponse
                {
                    DataObject = objects[i],
                    ServerSortOrder = sortOrder ?? string.Empty
                };

                messageId = Session.SendMessage(header, findObjectsResponse);
                sortOrder = string.Empty; // Only needs to be set in the first message
            }

            return messageId;
        }

        /// <summary>
        /// Handles the FindObjects event from a customer.
        /// </summary>
        public event ProtocolEventHandler<FindObjects, DataObjectResponse> OnFindObjects;

        /// <summary>
        /// Decodes the message based on the message type contained in the specified <see cref="IMessageHeader" />.
        /// </summary>
        /// <param name="header">The message header.</param>
        /// <param name="decoder">The message decoder.</param>
        /// <param name="body">The message body.</param>
        protected override void HandleMessage(IMessageHeader header, Decoder decoder, string body)
        {
            switch (header.MessageType)
            {
                case (int)MessageTypes.StoreQuery.FindObjects:
                    HandleFindObjects(header, decoder.Decode<FindObjects>(body));
                    break;

                default:
                    base.HandleMessage(header, decoder, body);
                    break;
            }
        }

        /// <summary>
        /// Handles the FindObjects message from a customer.
        /// </summary>
        /// <param name="header">The message header.</param>
        /// <param name="findObjects">The FindObjects message.</param>
        protected virtual void HandleFindObjects(IMessageHeader header, FindObjects findObjects)
        {
            var args = Notify(OnFindObjects, header, findObjects, new DataObjectResponse());
            HandleFindObjects(args);

            if (!args.Cancel)
            {
                FindObjectsResponse(header, args.Context.DataObjects, args.Context.ServerSortOrder);
            }
        }

        /// <summary>
        /// Handles the FindObjects message from a customer.
        /// </summary>
        /// <param name="args">The <see cref="ProtocolEventArgs{FindObjects}"/> instance containing the event data.</param>
        protected virtual void HandleFindObjects(ProtocolEventArgs<FindObjects, DataObjectResponse> args)
        {
        }
    }
}
