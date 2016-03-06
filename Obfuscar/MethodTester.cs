#region Copyright (c) 2007 Ryan Williams <drcforbin@gmail.com>
/// <copyright>
/// Copyright (c) 2007 Ryan Williams <drcforbin@gmail.com>
/// 
/// Permission is hereby granted, free of charge, to any person obtaining a copy
/// of this software and associated documentation files (the "Software"), to deal
/// in the Software without restriction, including without limitation the rights
/// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
/// copies of the Software, and to permit persons to whom the Software is
/// furnished to do so, subject to the following conditions:
/// 
/// The above copyright notice and this permission notice shall be included in
/// all copies or substantial portions of the Software.
/// 
/// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
/// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
/// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
/// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
/// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
/// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
/// THE SOFTWARE.
/// </copyright>
#endregion
using System;
using System.Text.RegularExpressions;

using Mono.Cecil;
using Obfuscar.Helpers;

namespace Obfuscar
{
	class MethodTester : IPredicate<MethodKey>
	{
		private readonly MethodKey key;
		private readonly string name;
		private readonly Regex nameRx;
		private readonly string type;
		private readonly string attrib;
		private readonly string typeAttrib;
		private readonly string inherits;
		private readonly bool? isStatic;

		public MethodTester (MethodKey key)
		{
			this.key = key;
		}

		public MethodTester (string name, string type, string attrib, string typeAttrib)
		{
			this.name = name;
			this.type = type;
			this.attrib = attrib;
			this.typeAttrib = typeAttrib;
		}

		public MethodTester (Regex nameRx, string type, string attrib, string typeAttrib)
		{
			this.nameRx = nameRx;
			this.type = type;
			this.attrib = attrib;
			this.typeAttrib = typeAttrib;
		}

		public MethodTester (string name, string type, string attrib, string typeAttrib, string inherits, bool? isStatic)
			: this (name, type, attrib, typeAttrib)
		{
			this.inherits = inherits;
			this.isStatic = isStatic;
		}

		public MethodTester (Regex nameRx, string type, string attrib, string typeAttrib, string inherits, bool? isStatic)
			: this (nameRx, type, attrib, typeAttrib)
		{
			this.inherits = inherits;
			this.isStatic = isStatic;
		}

		public bool Test (MethodKey method, InheritMap map)
		{
			if (key != null)
				return method == key;

			// method name matches type regex?
			if (!String.IsNullOrEmpty (type) && !Helper.CompareOptionalRegex (method.TypeKey.Fullname, type)) {
				return false;
			}

			// method visibility matches
			if (!MemberVisibilityMatches (this.attrib, typeAttrib, method.MethodAttributes, method.DeclaringType)) {
				return false;
			}

			// method's name matches
			if (nameRx != null && !nameRx.IsMatch (method.Name)) {
				return false;
			}

			// method's name matches
			if (!string.IsNullOrEmpty (name) && !Helper.CompareOptionalRegex (method.Name, name)) {
				return false;
			}

			// check is method's static flag matches.
			if (isStatic.HasValue) {
				bool methodIsStatic = (method.MethodAttributes & MethodAttributes.Static) == MethodAttributes.Static;

				if (isStatic != methodIsStatic) {
					return false;
				}
			}

			// finally does method's type inherit?
			if (!string.IsNullOrEmpty (inherits)) {
				if (!map.Inherits (method.DeclaringType, inherits)) {
					return false;
				}
			}

			return true;
		}

	    static bool GetMemberVisibilityValue(string valueName, MethodAttributes methodAttributes, TypeDefinition declaringType)
	    {
	        valueName = valueName.ToLowerInvariant();
	        var visibility = methodAttributes & MethodAttributes.MemberAccessMask;
	        if (valueName.StartsWith("type."))
	        {
	            return TypeTester.GetTypeVisibilityValue(valueName.Substring("type.".Length), declaringType);
	        }
            else if (valueName == "public")
            {
                return visibility == MethodAttributes.Public;
            }
            else if (valueName == "protected")
            {
                return visibility == MethodAttributes.Family || visibility == MethodAttributes.FamORAssem || visibility == MethodAttributes.FamANDAssem;
            }
            else if (valueName == "internal")
            {
                return visibility == MethodAttributes.Assembly || visibility == MethodAttributes.FamORAssem ||
                       visibility == MethodAttributes.FamANDAssem;
            }
            else if (valueName == "private")
            {
                return visibility == MethodAttributes.Private;
            }
            else
            {
                throw new ObfuscarException($"Unrecognized value in expression: {valueName}");
            }
	    }

		static public bool MemberVisibilityMatches (string attribute, string typeAttribute, MethodAttributes methodAttributes, TypeDefinition declaringType)
		{
			if (!string.IsNullOrEmpty (typeAttribute)) {
			    if (!ExpressionEvaluator.Evaluate(typeAttribute, s => TypeTester.GetTypeVisibilityValue(s, declaringType)))
			    {
                    return false;
			    }
			}

			if (!string.IsNullOrEmpty (attribute)) {
				MethodAttributes accessmask = (methodAttributes & MethodAttributes.MemberAccessMask);
			    if (!ExpressionEvaluator.Evaluate(attribute, s => GetMemberVisibilityValue(s, accessmask, declaringType)))
			    {
                    return false;
			    }
			}		

			// No attrib value given: The Skip* rule is processed normally.
			return true;
		}
	}
}
