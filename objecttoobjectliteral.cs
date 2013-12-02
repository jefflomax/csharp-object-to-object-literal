using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;

using profdata.WF.Enumerations;

namespace profdata.WF.Services.Handlers
{
	public static class ObjectToObjectLiteral
	{
		public static string NewLine = Environment.NewLine;
		public static string KeyProperty = "Key";

		public class Entity
		{
			public Type Type { get; set; }
			public long Key { get; set; }

			public Entity(object obj)
			{
				Type = obj.GetType();
				var keyProperty = Type.GetProperty(ObjectToObjectLiteral.KeyProperty);
				Key = (keyProperty == null)
					? 0
					: Convert.ToInt64(keyProperty.GetValue(obj));
			}
		}

		public class EntityEqualityComparer : IEqualityComparer<Entity>
		{
			public bool Equals( Entity lhs, Entity rhs )
			{
				if ( lhs.Type != rhs.Type )
					return false;

				return lhs.Key == rhs.Key;
			}

			public int GetHashCode( Entity entity)
			{
				return entity.Type.GetHashCode() + entity.Key.GetHashCode();
			}
		}

#if false
		public class ObjectEqualityComparer : IEqualityComparer<Object>
		{
			new public bool Equals(object lhs, object rhs)
			{
				return ReferenceEquals(lhs, rhs);
			}

			public int GetHashCode(object obj)
			{
				return obj.GetHashCode();
			}
		}
#endif

		public class Restriction
		{
			public string EntityName { get; set; }
			public HashSet<string> IncludeProperties { get; set; }

			public Restriction(string entityName, params string[] includeProperties)
			{
				EntityName = entityName;
				IncludeProperties = new HashSet<string>( includeProperties );
			}
		}


		public static string GetSampleGlobalExcludesProperties()
		{
			return "IsPersistent,Timestamp,ImportBatch";
		}


		public static Restriction[] GetSampleCommonRestrictions()
		{
			return new Restriction[]
			{
				new Restriction( "ShoppingCart", new string[] {"Key"}),
				new Restriction
				( 
					"Product", 
					new string[] 
					{
						"Key",
						"Code",
						"Description",
						"IsPaid"
					}
				)
			};
		}

		/// <summary>
		/// Convert NHibernate .NET object graph to C# Object Literal Contructor
		/// 
		/// </summary>
		/// <param name="obj">Object graph to serialize to C# Object Literal Constructor</param>
		/// <param name="excludeProperties">Properties to globally exculde</param>
		/// <param name="entityRestrictions">Entities to serialize as a limited set of properties, useful to limit depth of traversal</param>
		/// <returns>string containing Object Literal Constructor</returns>
		public static string ToObjectInitializer
		(
			Object obj,
			string globalExcludeProperties,
			params Restriction[] entityRestrictions
		)
		{
			var sb = new StringBuilder(1024);

			// Maintain a list of each entity encountered.  This is used to
			// prevent "parent" or duplicate references from causing an
			// infinite loop.  If the entity is a parent relationship, NHibernate
			// will have the same reference on both objects, but it if it simply
			// a second reference it will have a different reference
			var hist = new HashSet<Entity>();
			var comparer = new EntityEqualityComparer();

			// Place Entity Names with Property limitations in a dictionary
			// with a HashSet of Properties to include as the value
			Dictionary<string, HashSet<string>> restrictions = entityRestrictions
				.ToDictionary(k => k.EntityName, v => v.IncludeProperties);

			// Place properties to globally exclude in a HashSet
			string [] excludeProperties = globalExcludeProperties.Split(','); 
			HashSet<string> exclude = new HashSet<string>(excludeProperties);

			if (excludeProperties.Length > 0)
			{
				sb.Append("// Globally Excluded Properties:" + NewLine);
				sb.Append("//  " + globalExcludeProperties + NewLine);
			}

			if (restrictions.Count > 0) 
			{
				// Comment the restricted entities
				sb.Append("// Restricted Entities:" + NewLine);
				foreach (var restriction in entityRestrictions)
				{
					sb.AppendFormat(@"// {0}{1}", restriction.EntityName, NewLine);
					sb.AppendFormat
					(
						@"//  Allowed Properties: {0}{1}",
						String.Join(",", restriction.IncludeProperties),
						NewLine
					);
				}
			}

			sb.Append("var x = ");
			WalkObject(obj, sb, hist, comparer, restrictions, exclude, 0);
			sb.Append(";");

			return sb.ToString();
		}

		/// <summary>
		/// Create string of 0 or more Tab characters
		/// </summary>
		/// <param name="count">count of Tabs</param>
		/// <returns></returns>
		private static string Tabs(int count)
		{
			return (count == 0)
				? string.Empty
				: new String('\t', count);
		}

		/// <summary>
		/// Walk a NHibernate hydrated .NET object graph, returning a C# 3.0 Object Literal
		/// Constructor of public properties.  Parent and duplicate entity references are
		/// restricted to prevent infinite loops
		/// </summary>
		/// <param name="obj"></param>
		/// <param name="sb"></param>
		/// <param name="hist"></param>
		/// <param name="comparer"></param>
		/// <param name="restrictions"></param>
		/// <param name="exclude"></param>
		/// <param name="level"></param>
		/// <returns></returns>
		private static int WalkObject
		(
			Object obj,
			StringBuilder sb,
			HashSet<Entity> hist,
			IEqualityComparer<Entity> comparer,
			Dictionary<string, HashSet<string>> restrictions,
			HashSet<string> exclude,
			int level
		)
		{
			if (obj == null)
			{
				sb.Append("Object was NULL"); // TODO:  Comment if this happens
				System.Diagnostics.Debug.WriteLine("Encountered NULL object");
				return level;
			}

			var type = obj.GetType();
			var properties = type.GetProperties();
			var typeName = type.Name;

			var entity = new Entity(obj);

			// NHibernate uses EntityNameProxy to lazily hydrate objects
			if (typeName.EndsWith("Proxy"))
			{
				typeName = typeName.Substring(0, typeName.Length - "Proxy".Length);
			}

			// Company specific code
			// This class can't be constructed with an Object Literal, so
			// special case it and emit a constructor
			if (type.BaseType.FullName.Equals("full.assembly.name.here"))
			{
				var enumValue = type.GetProperty("Value").GetValue(obj);
				var enumName = type.GetProperty("DisplayName").GetValue(obj);
				sb.AppendFormat("new {0}({1},\"{2}\")", typeName, enumValue, enumName);
				return level;
			}

	
			if (hist.Contains(entity,comparer))
			{
				// Duplicate Entity, emit as Key only
				System.Diagnostics.Debug.WriteLine("Already Referenced Object " + typeName + ": " + entity.Key);
				
				sb.AppendFormat
				(
					"new {0}{{{1}={2}}} /* Reference */", 
					typeName, 
					KeyProperty,
					entity.Key
				);
				return level;
			}

			bool isRestricted = restrictions.ContainsKey(typeName);
			HashSet<string> includeProperties = (isRestricted)
				? restrictions[typeName]
				: null;

			System.Diagnostics.Debug.WriteLine("Adding Entity " + typeName + ":"+ entity.Key);
			hist.Add(entity);

			sb.AppendFormat("new {0} {{", typeName );

			bool appendComma = false;
			foreach (var property in properties)
			{
				if (isRestricted && !includeProperties.Contains(property.Name))
				{
					continue;
				}
				if (exclude.Contains(property.Name))
				{
					continue;
				}

				if (appendComma)
				{
					sb.Append("," + NewLine + Tabs(level));
				}
				else
				{
					sb.Append(NewLine + Tabs(level));
					appendComma = true;
				}

				var pt = property.PropertyType;
				var propertyName = property.Name;
				var name = pt.Name;
				System.Diagnostics.Debug.WriteLine("Property:{0} Type:{1}", propertyName, name);

				if (HandleBaseTypes(property, obj, sb,level))
					continue;

				var interfaces = property.PropertyType.GetInterfaces();
				var isList = interfaces.Contains(typeof(IList));
				var isEnumerable = property.PropertyType.FullName.StartsWith("System.Collections.Generic.IEnumerable");
				var isList1 = property.PropertyType.FullName.StartsWith("System.Collections.Generic.IList");
				//IEnumerable enumerableTest = property.GetValue(obj, null) as IEnumerable;
				//var isCollection = IsCollection(obj);
				//var isClass = property.PropertyType.IsClass;
				//var isGeneric = obj.GetType().IsGenericType;

				if (isList || isList1 || isEnumerable)
				{
					Object listObj = property.GetValue(obj, null);
					var list = (IList)property.GetValue(obj, null);
					//var collection = (ICollection)property.GetValue(obj, null);

					var listTypeName = property.PropertyType.GetGenericArguments()[0].Name;

					if (list != null && list.Count > 0)
					{
						sb.AppendFormat
						(
							"{0} = new List<{1}>{2}{3}{{{4}{5}", 
							property.Name, 
							listTypeName, 
							NewLine, 
							Tabs(level),
							NewLine,
							Tabs(level + 1)
						);
						WalkList(list, sb,hist, comparer, restrictions, exclude, level+1);
						sb.Append( NewLine + Tabs(level) + "}" );
					}
					else
					{
						sb.AppendFormat
						(
							"{0} = new List<{1}>()",
							property.Name,
							listTypeName
						);
					}
				}
				else if (property.PropertyType.IsEnum)
				{
					sb.AppendFormat("{0}{1} = {2}", Tabs(level), property.Name, property.GetValue(obj));
				}
				else
				{
					sb.AppendFormat("{0} = ", propertyName);
					WalkObject(property.GetValue(obj), sb, hist,comparer, restrictions, exclude, level+1 );
				}
			}

			sb.Append(NewLine + Tabs(level) + "}" );

			return level;
		}// WalkObject

		private static bool HandleBaseTypes(PropertyInfo property, Object obj, StringBuilder sb, int level)
		{
			DateTime workDt;
			string workStr;
			bool boolWork = false;
			var pt = property.PropertyType;
			var name = pt.Name;
			var value = property.GetValue(obj);

			var isNullable = pt.IsGenericType && pt.GetGenericTypeDefinition() == typeof(Nullable<>);
			if (isNullable)
			{
				name = pt.GetGenericArguments()[0].Name;
				if (property.GetValue(obj) == null)
				{
					sb.AppendFormat("{0} = null", property.Name);
					return true;
				}
			}

			if (pt == typeof (byte[]))
			{
				var stringBuilder = new StringBuilder();
				var ba = (byte[]) value;
				foreach (var by in ba)
				{
					if (stringBuilder.Length > 0)
						stringBuilder.Append(",");
					stringBuilder.Append(by.ToString(CultureInfo.InvariantCulture));
				}

				sb.AppendFormat("{0} = new byte[]{{{1}}}", property.Name, stringBuilder.ToString());
				return true;
			}

			switch (name)
			{
				case "Int16":
				case "Int32":
					sb.AppendFormat("{0} = {1}", property.Name, value);
					return true;

				case "Int64":
					sb.AppendFormat("{0} = {1}L", property.Name, value);
					return true;

				case "Float":
					sb.AppendFormat("{0} = {1}F", property.Name, value);
					return true;

				case "Double":
					sb.AppendFormat("{0} = {1}D", property.Name, value);
					return true;

				case "Decimal":
					sb.AppendFormat("{0} = {1}m", property.Name, value);
					return true;

				case "Boolean":
					try
					{
						boolWork = Convert.ToBoolean(value);
					}
					catch (Exception)
					{
						boolWork = false;
					}
					sb.AppendFormat("{0} = {1}", property.Name, (boolWork) ? "true" : "false");
					return true;

				case "DateTime":
					workDt = Convert.ToDateTime(value);
					if (workDt.Hour == 0 && workDt.Minute == 0 && workDt.Second == 0)
					{
						sb.AppendFormat
						(
							"{0} = new DateTime({1},{2},{3})",
							property.Name,
							workDt.Year,
							workDt.Month,
							workDt.Day
						);
					}
					else
					{
						sb.AppendFormat
						(
							"{0} = new DateTime({1},{2},{3},{4},{5},{6})",
							property.Name,
							workDt.Year,
							workDt.Month,
							workDt.Day,
							workDt.Hour,
							workDt.Minute,
							workDt.Second
						);
					}
					return true;

				case "String":
					// TODO: Just prefix string with @?
					workStr = Convert.ToString(value).Replace(@"\", @"\\");
					sb.AppendFormat("{0} = \"{1}\"", property.Name, workStr);
					return true;
			}

			return false;
		} // HandleBaseTypes

#if false
		private static bool IsCollection(object obj)
		{
			return typeof(ICollection).IsAssignableFrom(obj.GetType())
				|| typeof(ICollection<>).IsAssignableFrom(obj.GetType());
		}
#endif

		private static void WalkList
		(
			IList list,
			StringBuilder sb,
			HashSet<Entity> hist,
			IEqualityComparer<Entity> comparer,
			Dictionary<string, HashSet<string>> restrictions,
			HashSet<string> exclude,
			int level
		)
		{
			bool appendComma = false;
			foreach (object obj in list)
			{
				if (appendComma)
				{
					sb.Append("," + NewLine + Tabs(level));
				}
				appendComma = true;
				WalkObject(obj, sb, hist, comparer, restrictions, exclude, level);
			}
		}// WalkList
	}// class ObjectToObjectLiteral
}
