using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace Sone.Reflection
{
    public static class Reflection
    {
        /// <summary>
        /// Given a Type, returns an IEnumerable containing every Type which inherits from it.
        /// </summary>
        public static IEnumerable<Type> GetInheritors(Type t)
        {
            List<Type> objects = new List<Type>();

            foreach (Type type in Assembly.GetAssembly(t).GetTypes()
                                  .Where(myType => myType.IsClass && !myType.IsAbstract && myType.IsSubclassOf(t)))
            {
                objects.Add(type);
            }

            return objects;
        }

        /// <summary>
        /// Given a type and an attribute, return all the methods returned in the former marked with the latter attribute.
        /// </summary>
        public static IEnumerable<MethodInfo> GetMethodsWithAttribute(Type classType, Type attributeType)
        {
            return classType.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly).Where(methodInfo => methodInfo.GetCustomAttributes(attributeType, true).Length > 0);
        }
    }
}