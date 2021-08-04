using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xrm.Sdk;

namespace BobsPlugins
{
    public class Address
    {
        private readonly Guid _id;
        private readonly string _line1;
        private readonly string _line2;
        private readonly string _city;
        private readonly string _state;
        private readonly string _postalCode;

        public Address() { }

        public Address(Entity entity)
        {
            entity.TryGetAttributeValue("accountid", out _id);
            Id = _id;

            entity.TryGetAttributeValue("address1_line1", out _line1);
            Line1 = _line1;

            entity.TryGetAttributeValue("address1_line2", out _line2);
            Line2 = _line2;

            entity.TryGetAttributeValue("address1_city", out _city);
            City = _city;

            entity.TryGetAttributeValue("address1_stateorprovince", out _state);
            State = _state;

            entity.TryGetAttributeValue("address1_postalcode", out _postalCode);
            PostalCode = _postalCode;
        }

        public Guid Id { get; set; }
        public string Line1 { get; set; }
        public string Line2 { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public string PostalCode { get; set; }

        public Address MergeAddresses(Address address)
        {
            if (string.IsNullOrWhiteSpace(Line1)) Line1 = address.Line1;
            if (string.IsNullOrWhiteSpace(Line2)) Line2 = address.Line2;
            if (string.IsNullOrWhiteSpace(City)) City = address.City;
            if (string.IsNullOrWhiteSpace(State)) State = address.State;
            if (string.IsNullOrWhiteSpace(PostalCode)) PostalCode = address.PostalCode;

            return this;
        }
    }
}
