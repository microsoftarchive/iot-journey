// Copyright (c) Microsoft Corporation. All rights reserved. See License.txt in the project root for license information.

using System.Runtime.Serialization;

namespace SendEvents
{
    [DataContract]
    public class Event
    {
        [DataMember]
        public string Id { get; set; }

        [DataMember]
        public double Lat { get; set; }

        [DataMember]
        public double Lng { get; set; }

        [DataMember]
        public long Time { get; set; }

        [DataMember]
        public string Code { get; set; }
    }
}
