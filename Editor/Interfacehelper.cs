﻿using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Object = UnityEngine.Object;


namespace SOSXR.EditorTools
{
    /// <summary>
    ///     This allows you to actually do a FindObject for Interfaces!
    ///     From: https://wiki.unity3d.com/index.php/Interface_Finder
    ///     Usage: var stuff = InterfaceHelper.FindObject<InterfaceName>();
    /// </summary>
    public static class InterfaceHelper
    {
        static InterfaceHelper()
        {
            InitInterfaceToComponentMapping();
        }


        private static Dictionary<Type, List<Type>> _interfaceToComponentMapping;
        private static Type[] _allTypes;


        private static void InitInterfaceToComponentMapping()
        {
            _interfaceToComponentMapping = new Dictionary<Type, List<Type>>();

            _allTypes = GetAllTypes();

            foreach (var curInterface in _allTypes)
            {
                //We're interested only in interfaces
                if (!curInterface.IsInterface)
                {
                    continue;
                }

                var typeName = curInterface.ToString().ToLower();

                //Skip system interfaces
                if (typeName.Contains("unity") || typeName.Contains("system.")
                                               || typeName.Contains("mono.") || typeName.Contains("mono.") || typeName.Contains("icsharpcode.")
                                               || typeName.Contains("nsubstitute") || typeName.Contains("nunit.") ||
                                               typeName.Contains("microsoft.")
                                               || typeName.Contains("boo.") || typeName.Contains("serializ") || typeName.Contains("json")
                                               || typeName.Contains("log.") || typeName.Contains("logging") || typeName.Contains("test")
                                               || typeName.Contains("editor") || typeName.Contains("debug"))
                {
                    continue;
                }

                var typesInherited = GetTypesInheritedFromInterface(curInterface);

                if (typesInherited.Count <= 0)
                {
                    continue;
                }

                var componentsList = new List<Type>();

                foreach (var curType in typesInherited)
                {
                    //Skip interfaces
                    if (curType.IsInterface)
                    {
                        continue;
                    }

                    //Ignore non-component classes
                    if (!(typeof(Component) == curType || curType.IsSubclassOf(typeof(Component))))
                    {
                        continue;
                    }

                    if (!componentsList.Contains(curType))
                    {
                        componentsList.Add(curType);
                    }
                }

                _interfaceToComponentMapping.Add(curInterface, componentsList);
            }
        }


        private static Type[] GetAllTypes()
        {
            var res = new List<Type>();

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                res.AddRange(assembly.GetTypes());
            }

            return res.ToArray();
        }


        private static IEnumerable<Type> GetTypesInheritedFromInterface<T>() where T : class
        {
            return GetTypesInheritedFromInterface(typeof(T));
        }


        private static IList<Type> GetTypesInheritedFromInterface(Type type)
        {
            //Caching
            _allTypes ??= GetAllTypes();

            return _allTypes.Where(curType => type.IsAssignableFrom(curType) && curType.IsSubclassOf(typeof(Component))).ToList();
        }


        public static IList<T> FindObjects<T>(bool firstOnly = false) where T : class
        {
            var resList = new List<T>();

            var types = _interfaceToComponentMapping[typeof(T)];

            if (null == types || types.Count <= 0)
            {
                Debug.LogError("No descendants found for type " + typeof(T));

                return null;
            }

            foreach (var curType in types)
            {
                var objects = firstOnly
                    ? new[] {Object.FindObjectOfType(curType)}
                    : Object.FindObjectsOfType(curType);

                if (null == objects || objects.Length <= 0)
                {
                    continue;
                }

                var tList = new List<T>();

                foreach (var curObj in objects)
                {
                    var curObjAsT = curObj as T;

                    if (null == curObjAsT)
                    {
                        Debug.LogError("Unable to cast '" + curObj.GetType() + "' to '" + typeof(T) + "'");

                        continue;
                    }

                    tList.Add(curObjAsT);
                }

                resList.AddRange(tList);
            }

            return resList;
        }


        public static T FindObject<T>() where T : class
        {
            var list = FindObjects<T>();

            return list[0];
        }


        public static IList<T> GetInterfaceComponents<T>(this Component component, bool firstOnly = false) where T : class
        {
            var types = _interfaceToComponentMapping[typeof(T)];

            if (null == types || types.Count <= 0)
            {
                Debug.LogError("No descendants found for type " + typeof(T));

                return null;
            }

            var resList = new List<T>();

            foreach (var curType in types)
            {
                //Optimization - don't get all objects if we need only one
                var components = firstOnly
                    ? new[] {component.GetComponent(curType)}
                    : component.GetComponents(curType);

                if (null == components || components.Length <= 0)
                {
                    continue;
                }

                var tList = new List<T>();

                foreach (var curComp in components)
                {
                    var curCompAsT = curComp as T;

                    if (null == curCompAsT)
                    {
                        Debug.LogError("Unable to cast '" + curComp.GetType() + "' to '" + typeof(T) + "'");

                        continue;
                    }

                    tList.Add(curCompAsT);
                }

                resList.AddRange(tList);
            }

            return resList;
        }


        public static T GetInterfaceComponent<T>(this Component component) where T : class
        {
            var list = GetInterfaceComponents<T>(component, true);

            return list[0];
        }
    }
}