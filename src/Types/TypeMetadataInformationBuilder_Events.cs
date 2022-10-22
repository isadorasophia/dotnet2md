using System.Collections.Immutable;
using System.Diagnostics;
using System.Reflection;
using System.Text;

namespace DotnetToMd.Metadata
{
    internal partial class TypeMetadataInformationBuilder
    {
        private ImmutableDictionary<string, PropertyInformation> FetchEvents()
        {
            Debug.Assert(_typeResult is not null);

            List<PropertyInformation> result = new();

            IEnumerable<EventInfo> events = _type.GetEvents(Utilities.DefaultFlags).Where(IsEventVisible);

            foreach (EventInfo @event in events)
            {
                if (@event.EventHandlerType is null)
                {
                    Debug.Fail("Why is the event type null?");
                    continue;
                }

                TypeInformation? eventType = TypeInformationBuilder.FetchOrCreate(_parser, @event.EventHandlerType);
                if (eventType is null)
                {
                    Debug.Fail("Unable to decode property type?");
                    continue;
                }

                ArgumentInformation returnInfo = new(default, eventType);
                PropertyInformation propertyInfo = new(MemberKind.Event, @event.Name, _typeResult, returnInfo);

                result.Add(propertyInfo);

                propertyInfo.Signature = CreateEventSignature(@event, eventType);
            }

            return result.ToDictionary(p => p.Name, p => p).ToImmutableDictionary();
        }

        private static bool IsEventVisible(EventInfo p)
        {
            if (p.GetAddMethod() is MethodInfo getMethod)
            {
                return IsMethodVisible(getMethod);
            }

            return false;
        }

        private static string? CreateEventSignature(EventInfo e, TypeInformation returnTypeInfo)
        {
            StringBuilder result = new();

            MethodInfo? raiseMethod = e.GetAddMethod();

            if (raiseMethod is null)
            {
                Debug.Fail("Property without a getter?");
                return null;
            }

            result.Append(GetMethodAccessorName(raiseMethod));

            if (raiseMethod.IsAbstract)
            {
                result.Append("abstract ");
            }

            if (raiseMethod.IsStatic)
            {
                result.Append("static ");
            }

            result.Append("event ");

            result.Append($"{returnTypeInfo.Name} {e.Name};");

            return result.ToString();
        }

        private static string GetMethodAccessorName(MethodBase m)
        {
            if (m.IsPublic)
            {
                return "public ";
            }
            else if (m.IsFamily)
            {
                return "protected ";
            }
            else if (m.IsPrivate)
            {
                return "private ";
            }
            else if (m.IsAssembly)
            {
                return "internal ";
            }

            return string.Empty;
        }
    }
}
