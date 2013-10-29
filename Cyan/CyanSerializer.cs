using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Xml;
using System.Xml.Linq;

namespace Cyan
{
    public static class CyanSerializer
    {
        private static readonly XNamespace DefNamespace = "http://www.w3.org/2005/Atom";
        private static readonly XNamespace DNamespace = "http://schemas.microsoft.com/ado/2007/08/dataservices";
        private static readonly XNamespace MNamespace = "http://schemas.microsoft.com/ado/2007/08/dataservices/metadata";
        private static readonly XDeclaration Declaration = new XDeclaration("1.0", "utf-8", "yes");

        public static XDocument Serialize(this CyanEntity value)
        {
            XElement[] serializedProperties = value.GetFields()
                .Where(f => f.Key != "ETag")
                .Select(f => SerializeField(f.Key, f.Value))
                .ToArray();

            var document = SerializeDocument(serializedProperties);

            return document;
        }

        private static XElement SerializeField(string name, object value)
        {
            var serialized = SerializeProperty(value);

            var serializedValue = serialized.Item1 == "Edm.String"
                ? new[] {serialized.Item2}
                : new object[] {new XAttribute(MNamespace + "type", serialized.Item1), serialized.Item2};

            var ret = new XElement(DNamespace + name, serializedValue);

            return ret;
        }

        private static Tuple<string, string> SerializeProperty(object value)
        {
            var type = value.GetType();

            var typeName = type.Name;

            string azureTypeName;
            string serialized;
            switch (typeName)
            {
                case "Byte[]":
                    azureTypeName = "Edm.Binary";
                    serialized = Convert.ToBase64String((byte[]) value);
                    break;
                case "Boolean":
                    azureTypeName = "Edm.Boolean";
                    serialized = (bool) value ? "true" : "false";
                    break;
                case "DateTime":
                    azureTypeName = "Edm.DateTime";
                    serialized = XmlConvert.ToString((DateTime) value, XmlDateTimeSerializationMode.RoundtripKind);
                    break;
                case "Double":
                    azureTypeName = "Edm.Double";
                    serialized = value.ToString();
                    break;
                case "Guid":
                    azureTypeName = "Edm.Guid";
                    serialized = XmlConvert.ToString((Guid) value);
                    break;
                case "Int32":
                    azureTypeName = "Edm.Int32";
                    serialized = value.ToString();
                    break;
                case "Int64":
                    azureTypeName = "Edm.Int64";
                    serialized = value.ToString();
                    break;
                case "String":
                    azureTypeName = "Edm.String";
                    serialized = value.ToString();
                    break;
                default:
                    azureTypeName = "string";
                    serialized = value.ToString();
                    break;
            }

            return new Tuple<string, string>(azureTypeName, serialized);
        }

        private static XDocument SerializeDocument(XElement[] serializedProperties)
        {
            var document = new XDocument(
                Declaration,
                new XElement(DefNamespace + "entry",
                    new XAttribute(XNamespace.Xmlns + "d", DNamespace),
                    new XAttribute(XNamespace.Xmlns + "m", MNamespace),
                    new XAttribute("xmlns", DefNamespace),
                    new XElement(DefNamespace + "title"),
                    new XElement(DefNamespace + "updated",
                        XmlConvert.ToString(DateTime.UtcNow, XmlDateTimeSerializationMode.RoundtripKind)),
                    new XElement(DefNamespace + "author",
                        new XElement(DefNamespace + "name")),
                    new XElement(DefNamespace + "id"),
                    new XElement(DefNamespace + "content",
                        new XAttribute("type", "application/xml"),
// ReSharper disable once CoVariantArrayConversion
                        new XElement(MNamespace + "properties", serializedProperties))));

            return document;
        }

        public static IEnumerable<dynamic> DeserializeEntities(XElement element)
        {
            return element
                .Elements(DefNamespace + "entry")
                .Select(DeserializeEntity);
        }

        public static CyanEntity DeserializeEntity(XElement element)
        {
            var eTag = element.Attribute(MNamespace + "etag");

            var xElement = element
                .Element(DefNamespace + "content");
            if (xElement != null)
            {
                var properties = xElement
                    .Element(MNamespace + "properties");

                var ret = new CyanEntity {ETag = eTag != null ? HttpUtility.UrlDecode(eTag.Value) : null};

                if (properties == null) return ret;

                foreach (var item in properties.Elements())
                {
                    var nullAttribute = item.Attribute(MNamespace + "null");
                    if (nullAttribute != null && nullAttribute.Value == "true")
                        continue;

                    switch (item.Name.LocalName)
                    {
                        case "PartitionKey":
                            ret.PartitionKey = (string) DeserializeProperty(item);
                            break;
                        case "RowKey":
                            ret.RowKey = (string) DeserializeProperty(item);
                            break;
                        case "Timestamp":
                            ret.Timestamp = (DateTime) DeserializeProperty(item);
                            break;
                        default:
                            ret.Fields.Add(item.Name.LocalName, DeserializeProperty(item));
                            break;
                    }
                }

                return ret;
            }
            throw new ArgumentNullException("element");
        }

        private static object DeserializeProperty(XElement propertyElement)
        {
            var typeAttribute = propertyElement.Attribute(MNamespace + "type");
            if (typeAttribute == null)
                return propertyElement.Value;

            var typeName = typeAttribute.Value;

            object ret;
            switch (typeName)
            {
                case "Edm.Binary":
                    ret = Convert.FromBase64String(propertyElement.Value);
                    break;
                case "Edm.Boolean":
                    ret = bool.Parse(propertyElement.Value);
                    break;
                case "Edm.DateTime":
                    ret = XmlConvert.ToDateTime(propertyElement.Value, XmlDateTimeSerializationMode.RoundtripKind);
                    break;
                case "Edm.Double":
                    ret = double.Parse(propertyElement.Value);
                    break;
                case "Edm.Guid":
                    ret = XmlConvert.ToGuid(propertyElement.Value);
                    break;
                case "Edm.Int32":
                    ret = int.Parse(propertyElement.Value);
                    break;
                case "Edm.Int64":
                    ret = long.Parse(propertyElement.Value);
                    break;
                default:
                    ret = propertyElement.Value;
                    break;
            }

            return ret;
        }
    }
}