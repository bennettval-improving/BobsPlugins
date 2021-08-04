using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace BobsPlugins
{
    public static class USPSXmlHelper
    {
        static USPSXmlHelper() { }

        public static Address GetAddress(XDocument xmlDoc)
        {
            var addressNodes = xmlDoc.Descendants("Address");
            if (addressNodes != null && addressNodes.Count() > 0)
            {
                var address = addressNodes.FirstOrDefault();
                return new Address
                {
                    Line1 = address.Element("Address2")?.Value,
                    Line2 = address.Element("Address1")?.Value,
                    City = address.Element("City")?.Value,
                    State = address.Element("State")?.Value,
                    PostalCode = $"{address.Element("Zip5")?.Value}-{address.Element("Zip4")?.Value}" 
                };
            }
            return null;
        }

        public static bool ErrorExists(XDocument xmlDoc, out string errorDescription)
        {
            errorDescription = string.Empty;
            var errorNodeList = xmlDoc.Descendants("Error");
            if (errorNodeList != null && errorNodeList.Count() > 0)
            {
                var errorNode = errorNodeList.FirstOrDefault();
                errorDescription = errorNode.Element("Description")?.Value;
                return true;
            }
            return false;
        }
    }
}
