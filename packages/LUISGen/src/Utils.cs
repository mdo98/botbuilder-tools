// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LUISGen
{
    public class Utils
    {
        public static string NormalizeName(dynamic name)
        {
            return ((string)name).Replace('.', '_').Replace(' ', '_');
        }

        public static bool IsPrebuilt(dynamic name, dynamic app)
        {
            bool isPrebuilt = false;
            if (app.prebuiltEntities != null)
            {
                foreach (var child in app.prebuiltEntities)
                {
                    if (child.name == name)
                    {
                        isPrebuilt = true;
                        break;
                    }
                }
            }
            return isPrebuilt;
        }

        public static bool IsHierarchical(dynamic name, dynamic app)
        {
            bool IsHierarchical = false;
            if (app.entities != null)
            {
                foreach (var child in app.entities)
                {
                    if (child.name == name)
                    {
                        IsHierarchical = child.children != null;
                        break;
                    }
                }
            }
            return IsHierarchical;
        }

        public static bool IsList(dynamic name, dynamic app)
        {
            bool isList = false;
            if (app.closedLists != null)
            {
                foreach (var list in app.closedLists)
                {
                    if (list.name == name)
                    {
                        isList = true;
                        break;
                    }
                    foreach (var role in list.roles)
                    {
                        if (role == name)
                        {
                            // This is not technically accurate since the same role
                            // can be found in multiple types for now.  
                            isList = true;
                            break;
                        }
                    }
                }
            }
            return isList;
        }

        public static string JsonPropertyName(dynamic property, dynamic app)
        {
            var name = ((string)property).Split(':').Last();
            if (!name.StartsWith("geographyV2") && !name.StartsWith("ordinalV2") && name.EndsWith("V2"))
            {
                name = name.Substring(0, name.Length - 2);
            }
            return NormalizeName(name);
        }

        // apply(name, type)
        public static void EntityApply(JObject entity, Action<string> action)
        {
            dynamic dynEntity = entity;
            action((string)dynEntity.name);
            if (dynEntity?.roles != null)
            {
                foreach (string role in from role in (JArray)dynEntity.roles orderby role select role)
                {
                    action(role);
                }
            }
        }

        public static JObject Entity(dynamic name)
        {
            dynamic obj = new JObject();
            obj.name = name;
            obj.roles = new JArray();
            return obj;
        }

        public static IEnumerable<JObject> OrderedEntities(dynamic entities) => from entity in (JArray)entities orderby entity["name"] select (JObject)entity;

        private static void WriteInstances(dynamic entities, Action<string> writeInstance)
        {
            if (entities != null)
            {
                foreach (var entity in OrderedEntities(entities))
                {
                    Utils.EntityApply(entity, writeInstance);
                }
            }
        }

        public static void WriteInstances(JObject obj, Action<string> writeInstance)
        {
            if (obj != null)
            {
                dynamic app = obj;
                var empty = new JArray();
                var lists = new List<JArray> {
                    app.entities,
                    app.prebuiltEntities,
                    app.closedLists,
                    app.regex_entities,
                    app.patternAnyEntities,
                    app.composites
                };
                var entities = OrderedEntities(new JArray(lists.SelectMany(a => a ?? empty)));
                foreach (dynamic entity in entities)
                {
                    Utils.EntityApply(entity, writeInstance);
                    if (IsHierarchical(entity, app))
                    {
                        foreach (var child in entity.children)
                        {
                            writeInstance((string)child);
                        }
                    }
                }
            }
        }
    }
}
