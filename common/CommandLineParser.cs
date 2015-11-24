﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace common
{
    public class CommandLineParser<T> where T: new()
    {
        private readonly T _job;
        private readonly List<PropertyInfo> _properties;
        private readonly  List<string> _requiredPropertyNames;
        private int _current;
        private string[] _args;

        public CommandLineParser()
        {
            _job = new T();
            _properties = GetPropertiesWithParameterAttribute(typeof(T));
            _requiredPropertyNames = _properties.Where(prop => GetParameterAttribute(prop).Required).Select(prop => prop.Name).ToList();
        }

        public T ParseCommandLine(string[] args)
        {
            _args = args;

            for (_current = 0; _current < args.Length; _current++)
            {
                ParseArgument();
            }

            if (_requiredPropertyNames.Any())
            {
                throw new ArgumentException("Missing option: " + _requiredPropertyNames.First());
            }

            return _job;
        }

        private void ParseArgument()
        {
            ParseArgument(_args[_current]);
        }

        private void ParseArgument(string currentArgument)
        {
            foreach (var property in _properties)
            {
                var parameterAttribute = GetParameterAttribute(property);
                var flag = parameterAttribute.Parameter;

                if (!currentArgument.StartsWith(flag))
                {
                    continue;
                }

                if (_requiredPropertyNames.Contains(property.Name))
                {
                    _requiredPropertyNames.Remove(property.Name);
                }


                if (property.PropertyType == typeof (string))
                {
                    var value = currentArgument.Length > 2 ? currentArgument.Substring(2) : _args[++_current];
                    if (!parameterAttribute.AllowSpaces && ContainsWhitespace(value))
                    {
                        throw new ArgumentException("Spaces aren't allowed in printer names.");
                    }

                    property.SetValue(_job, value, null);

                    break;
                }

                if (property.PropertyType == typeof (bool))
                {
                    property.SetValue(_job, true, null);

                    break;
                }
            }
        }

        private static bool ContainsWhitespace(string value)
        {
            return value.Any(c => new[] {'\x09', '\x0B', '\x0C', ' '}.Contains(c));
        }

        private static ParameterAttribute GetParameterAttribute(PropertyInfo propertyInfo)
        {
            return propertyInfo.GetCustomAttributes(false).OfType<ParameterAttribute>().First();
        }

        private static List<PropertyInfo> GetPropertiesWithParameterAttribute(Type type)
        {
            return type.GetProperties().Where(HasParameterAttribute).ToList();
        }

        private static bool HasParameterAttribute(PropertyInfo propertyInfo)
        {
            return propertyInfo.GetCustomAttributes(false).OfType<ParameterAttribute>().Any();
        }
    }
}